using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IConfiguration config,
    AppDbContext db) : ControllerBase
{
    private static readonly ConcurrentDictionary<string, long> UsedPortalSsoNonces = new(StringComparer.Ordinal);
    private static readonly SemaphoreSlim IdentityGate = new(1, 1);

    [HttpPost("portal-sso"), AllowAnonymous]
    public async Task<IActionResult> PortalSso([FromBody] PortalSsoRequest req)
    {
        var sharedKey = config["Auth:PortalSsoKey"];
        if (string.IsNullOrWhiteSpace(sharedKey) || sharedKey.Length < 32)
            return StatusCode(503, new { message = "Portal SSO 尚未設定。" });

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (string.IsNullOrWhiteSpace(req.Username)
            || string.IsNullOrWhiteSpace(req.Nonce)
            || req.Nonce.Length > 128
            || Math.Abs(now - req.Timestamp) > 60)
            return Unauthorized(new { message = "Portal SSO 請求已失效。" });

        var normalizedUsername = NormalizeAccount(req.Username);
        var displayName = string.IsNullOrWhiteSpace(req.DisplayName) ? normalizedUsername : req.DisplayName.Trim();
        var operatorCode = CleanOptional(req.OperatorCode);
        var department = CleanOptional(req.Department);
        var email = CleanOptional(req.Email);
        var payload = $"{normalizedUsername}|{displayName}|{operatorCode}|{department}|{email}|{req.Timestamp}|{req.Nonce}";
        var legacyPayload = $"{normalizedUsername}|{displayName}|{req.Timestamp}|{req.Nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedKey));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var legacyExpected = hmac.ComputeHash(Encoding.UTF8.GetBytes(legacyPayload));
        byte[] supplied;
        try { supplied = Convert.FromBase64String(req.Signature); }
        catch { return Unauthorized(new { message = "Portal SSO 簽章不正確。" }); }
        var validCurrent = supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
        var validLegacy = supplied.Length == legacyExpected.Length && CryptographicOperations.FixedTimeEquals(supplied, legacyExpected);
        if (!validCurrent && !validLegacy)
            return Unauthorized(new { message = "Portal SSO 簽章不正確。" });

        foreach (var expiredNonce in UsedPortalSsoNonces.Where(x => x.Value < now).Select(x => x.Key))
            UsedPortalSsoNonces.TryRemove(expiredNonce, out _);
        if (!UsedPortalSsoNonces.TryAdd(req.Nonce, now + 120))
            return Unauthorized(new { message = "Portal SSO 請求已使用。" });

        // Legacy signatures do not cover employee number, department or email.
        return await ResolvePortalIdentityAsync(normalizedUsername, displayName,
            validCurrent ? operatorCode : null, validCurrent ? department : null,
            validCurrent ? email : null, validCurrent, !string.IsNullOrWhiteSpace(req.DisplayName));
    }

    private async Task<IActionResult> ResolvePortalIdentityAsync(string username, string displayName,
        string? operatorCode, string? department, string? email, bool canProvision, bool updateDisplayName)
    {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
        await IdentityGate.WaitAsync(ct);
        try
        {
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(ct) : null;
            if (db.Database.IsSqlServer())
            {
                // Serialize identity provisioning across IIS workers as well as within this process.
                await db.Database.ExecuteSqlRawAsync("""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource=N'SPC:PortalSso:Operators',
                        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                    IF @result < 0 THROW 51000, N'無法取得登入身分鎖定，請稍後重試。', 1;
                    """, ct);
            }
            var usernameMatches = await db.Operators.IgnoreQueryFilters().Where(x => x.Username != null &&
                (x.Username.Trim().ToLower() == username || x.Username.Trim().ToLower().EndsWith("\\" + username)
                 || x.Username.Trim().ToLower().StartsWith(username + "@"))).ToListAsync(ct);
            if (usernameMatches.Count > 1)
                return Conflict(new { message = "此 AD 帳號對應多筆 SPC 使用者，請洽管理員確認，系統不會另建帳號。" });
            var systemUser = usernameMatches.SingleOrDefault();
            var desiredCode = operatorCode ?? username.ToUpperInvariant();
            var normalizedCode = NormalizeOperatorCode(desiredCode);
            if (canProvision)
            {
                var codeMatches = await db.Operators.IgnoreQueryFilters().Where(x =>
                    x.OperatorCode.Replace("-", "").Replace(" ", "").ToLower() == normalizedCode).ToListAsync(ct);
                if (codeMatches.Count > 1)
                    return Conflict(new { message = "此工號對應多筆 SPC 使用者，請洽管理員確認，系統不會另建帳號。" });
                var codeOwner = codeMatches.SingleOrDefault();
                if (codeOwner is not null)
                {
                    if (systemUser is not null && systemUser.Id != codeOwner.Id)
                        return Conflict(new { message = "AD 帳號與工號分屬不同 SPC 使用者，請洽管理員確認，系統不會自動合併或另建帳號。" });
                    var storedAccount = string.IsNullOrWhiteSpace(codeOwner.Username) ? null : NormalizeAccount(codeOwner.Username);
                    var isEmployeeAlias = operatorCode is not null && NormalizeOperatorCode(username) == normalizedCode;
                    var storedIsEmployeeAlias = storedAccount is not null && NormalizeOperatorCode(storedAccount) == normalizedCode;
                    if (storedAccount is not null && storedAccount != username && !isEmployeeAlias && !storedIsEmployeeAlias)
                        return Conflict(new { message = "此工號已綁定其他 AD 帳號，請洽管理員確認，系統不會另建帳號。" });
                    systemUser ??= codeOwner;
                    if (isEmployeeAlias && storedAccount is not null && !storedIsEmployeeAlias)
                        username = storedAccount; // An employee-number login must retain the canonical AD account.
                }
            }
            if (systemUser is not null && (!systemUser.IsActive || systemUser.IsDeleted))
                return Unauthorized(new { message = "此 SPC 操作者帳號已停用。" });
            if (systemUser is null)
            {
                if (!canProvision)
                    return Conflict(new { message = "舊版登入交換不能建立新使用者，請由入口網站重新登入並傳送完整帳號／工號資訊。" });
                systemUser = new MesSpc.Api.Domain.Entities.Operator
                {
                    OperatorCode = desiredCode, OperatorName = displayName, Department = department,
                    Email = email, Username = username, Role = UserRoles.Viewer, IsActive = true,
                    CreatedBy = "PortalSSO"
                };
                db.Operators.Add(systemUser);
            }
            else
            {
                var changed = false;
                if (systemUser.Username != username) { systemUser.Username = username; changed = true; }
                if (updateDisplayName && systemUser.OperatorName != displayName) { systemUser.OperatorName = displayName; changed = true; }
                if (operatorCode is not null && NormalizeOperatorCode(systemUser.OperatorCode) != normalizedCode)
                { systemUser.OperatorCode = desiredCode; changed = true; }
                if (department is not null && systemUser.Department != department) { systemUser.Department = department; changed = true; }
                if (email is not null && systemUser.Email != email) { systemUser.Email = email; changed = true; }
                if (changed) { systemUser.UpdatedAt = DateTime.UtcNow; systemUser.UpdatedBy = "PortalSSO"; }
            }
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return CreateTokenResult(systemUser, username, TimeSpan.FromMinutes(60));
        }
        finally { IdentityGate.Release(); }
    }

    private static string NormalizeOperatorCode(string value) => value.Trim().Replace("-", "").Replace(" ", "").ToLowerInvariant();

    [HttpPost("login"), AllowAnonymous]
    public IActionResult Login() => StatusCode(StatusCodes.Status410Gone,
        new { message = "SPC 已停用本機密碼登入，請使用 AD 帳號或工號登入。" });


    private IActionResult CreateTokenResult(MesSpc.Api.Domain.Entities.Operator systemUser, string normalizedUsername, TimeSpan lifetime)
    {
        var keyStr = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(keyStr) || keyStr.Length < 32)
            return StatusCode(500, new { message = "Auth:JwtKey 未設定或過短（至少 32 字元）" });

        var displayName = systemUser.OperatorName;
        var operatorCode = systemUser.OperatorCode;
        var role = UserRoles.Normalize(systemUser.Role);
        var userId = systemUser.Id;
        var permissions = SpcPagePermissions.Resolve(systemUser.Role, systemUser.PagePermissionsJson);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(lifetime);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, normalizedUsername),
            new(ClaimTypes.Role, role),
            new("displayName", displayName),
            new("operatorCode", operatorCode)
        };
        claims.AddRange(permissions.Select(x => new Claim("permission", x)));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: creds);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(token);
        return Ok(new
        {
            token = jwt,
            expiresAt = expires,
            user = new { id = userId, username = normalizedUsername, operatorCode, displayName, role, permissions }
        });
    }

    private static string NormalizeAccount(string account)
    {
        var value = account.Trim();
        var slash = value.LastIndexOf('\\');
        if (slash >= 0 && slash < value.Length - 1) value = value[(slash + 1)..];
        var at = value.IndexOf('@');
        if (at > 0) value = value[..at];
        return value.ToLowerInvariant();
    }

    public record PortalSsoRequest(string Username, string? DisplayName, long Timestamp, string Nonce, string Signature, string? OperatorCode = null, string? Department = null, string? Email = null);
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
