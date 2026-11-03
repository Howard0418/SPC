using MesSpc.Api.Services.Localization;
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
using MesSpc.Api.Services.Auditing;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ServiceFilter(typeof(AuthenticationAuditResultFilter))]
public class AuthController(
    IConfiguration config,
    AppDbContext db,
    AuthenticationAudit? audit = null) : ControllerBase
{
    private static readonly ConcurrentDictionary<string, long> UsedPortalSsoNonces = new(StringComparer.Ordinal);
    private static readonly SemaphoreSlim IdentityGate = new(1, 1);
    private (int Status, string Code, string? Account, int? OperatorId)? deferredDenial;

    [HttpPost("portal-sso"), AllowAnonymous]
    public async Task<IActionResult> PortalSso([FromBody] PortalSsoRequest req)
    {
        deferredDenial = null;
        var sharedKey = config["Auth:PortalSsoKey"];
        if (string.IsNullOrWhiteSpace(sharedKey) || sharedKey.Length < 32)
            return await RejectAsync(503, "AUTH_SSO_NOT_CONFIGURED", "spc.api.ssoNotConfigured", req.Username);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (string.IsNullOrWhiteSpace(req.Username)
            || string.IsNullOrWhiteSpace(req.Nonce)
            || req.Nonce.Length > 128
            || req.Username.Length > 256 || req.Timestamp < now - 60 || req.Timestamp > now + 60)
            return await RejectAsync(401, "AUTH_SSO_EXPIRED", "spc.api.ssoExpired", req.Username);

        var normalizedUsername = NormalizeAccount(req.Username);
        if (req.DisplayName?.Length > 200 || req.OperatorCode?.Length > 100 || req.Department?.Length > 500
            || req.Email?.Length > 500 || req.Signature?.Length > 512)
            return await RejectAsync(400, "AUTH_REQUEST_INVALID", "spc.api.validationFailed", req.Username);
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
        try { supplied = Convert.FromBase64String(req.Signature ?? ""); }
        catch { return await RejectAsync(401, "AUTH_SSO_SIGNATURE", "spc.api.ssoSignature", req.Username); }
        var validCurrent = supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
        var validLegacy = supplied.Length == legacyExpected.Length && CryptographicOperations.FixedTimeEquals(supplied, legacyExpected);
        if (!validCurrent && !validLegacy)
            return await RejectAsync(401, "AUTH_SSO_SIGNATURE", "spc.api.ssoSignature", req.Username);

        foreach (var expiredNonce in UsedPortalSsoNonces.Where(x => x.Value < now).Select(x => x.Key))
            UsedPortalSsoNonces.TryRemove(expiredNonce, out _);
        if (!UsedPortalSsoNonces.TryAdd(req.Nonce, now + 120))
            return await RejectAsync(401, "AUTH_SSO_REPLAYED", "spc.api.ssoReplayed", req.Username);

        // Legacy signatures do not cover employee number, department or email.
        var result = await ResolvePortalIdentityAsync(normalizedUsername, displayName,
            validCurrent ? operatorCode : null, validCurrent ? department : null,
            validCurrent ? email : null, validCurrent, !string.IsNullOrWhiteSpace(req.DisplayName));
        // Persist denials only after the read-only identity transaction has been disposed.
        if (deferredDenial is { } denied && audit is not null)
            await audit.FailureAsync(denied.Code, denied.Status, denied.Account, denied.OperatorId);
        return result;
    }

    private async Task<IActionResult> ResolvePortalIdentityAsync(string username, string displayName,
        string? operatorCode, string? department, string? email, bool canProvision, bool updateDisplayName)
    {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
        await IdentityGate.WaitAsync(ct);
        try
        {
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
                    return await RejectAsync(409, "AUTH_ACCOUNT_AMBIGUOUS", "spc.api.accountAmbiguous", username);
                var systemUser = usernameMatches.SingleOrDefault();
                var desiredCode = operatorCode ?? username.ToUpperInvariant();
                var normalizedCode = NormalizeOperatorCode(desiredCode);
                if (canProvision)
                {
                    var codeMatches = await db.Operators.IgnoreQueryFilters().Where(x =>
                        x.OperatorCode.Replace("-", "").Replace(" ", "").ToLower() == normalizedCode).ToListAsync(ct);
                    if (codeMatches.Count > 1)
                        return await RejectAsync(409, "AUTH_EMPLOYEE_AMBIGUOUS", "spc.api.employeeAmbiguous", username);
                    var codeOwner = codeMatches.SingleOrDefault();
                    if (codeOwner is not null)
                    {
                        if (systemUser is not null && systemUser.Id != codeOwner.Id)
                            return await RejectAsync(409, "AUTH_IDENTITY_CONFLICT", "spc.api.identityConflict", username);
                        var storedAccount = string.IsNullOrWhiteSpace(codeOwner.Username) ? null : NormalizeAccount(codeOwner.Username);
                        var isEmployeeAlias = operatorCode is not null && NormalizeOperatorCode(username) == normalizedCode;
                        var storedIsEmployeeAlias = storedAccount is not null && NormalizeOperatorCode(storedAccount) == normalizedCode;
                        if (storedAccount is not null && storedAccount != username && !isEmployeeAlias && !storedIsEmployeeAlias)
                            return await RejectAsync(409, "AUTH_EMPLOYEE_BOUND", "spc.api.employeeBound", username);
                        systemUser ??= codeOwner;
                        if (isEmployeeAlias && storedAccount is not null && !storedIsEmployeeAlias)
                            username = storedAccount; // An employee-number login must retain the canonical AD account.
                    }
                }
                if (systemUser is not null && (!systemUser.IsActive || systemUser.IsDeleted))
                    return await RejectAsync(401, "AUTH_ACCOUNT_DISABLED", "spc.api.accountDisabled", username, systemUser.Id);
                var created = systemUser is null;
                if (systemUser is null)
                {
                    if (!canProvision)
                        return await RejectAsync(409, "AUTH_LEGACY_PROVISION_DENIED", "spc.api.legacyProvisionDenied", username);
                    systemUser = new MesSpc.Api.Domain.Entities.Operator
                    {
                        OperatorCode = desiredCode,
                        OperatorName = displayName,
                        Department = department,
                        Email = email,
                        Username = username,
                        Role = UserRoles.Viewer,
                        IsActive = true,
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
                var jti = Guid.NewGuid().ToString("N");
                var tokenResult = CreateTokenResult(systemUser, username, TimeSpan.FromMinutes(60), jti);
                if (audit?.Enabled == true)
                {
                    if (tokenResult is not OkObjectResult) throw new InvalidOperationException("AUTH_TOKEN_CREATION_FAILED");
                    await audit.StageSuccessAsync(systemUser.Id, jti, !canProvision, created, ct);
                    await db.SaveChangesAsync(ct);
                }
                if (transaction is not null) await transaction.CommitAsync(ct);
                return tokenResult;
            }
            catch when (audit?.Enabled == true)
            {
                if (db.Database.CurrentTransaction is { } active) await active.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                await audit.FailureAsync("AUTH_AUDIT_UNAVAILABLE", 503, username);
                return StatusCode(503, ApiUiError.Create("AUTH_AUDIT_UNAVAILABLE", "spc.api.authAuditUnavailable"));
            }
        }
        finally { IdentityGate.Release(); }
    }

    private static string NormalizeOperatorCode(string value) => value.Trim().Replace("-", "").Replace(" ", "").ToLowerInvariant();

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login()
    {
        if (audit is not null) await audit.FailureAsync("AUTH_LOCAL_DISABLED", 410, null, eventCode: "auth.local_login.request");
        return StatusCode(StatusCodes.Status410Gone, ApiUiError.Create("AUTH_LOCAL_DISABLED", "spc.api.localLoginDisabled"));
    }

    /// <summary>Records a logout request; does not revoke existing JWTs.</summary>
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        if (audit is not null) await audit.LogoutAsync(User);
        return NoContent();
    }

    private async Task<IActionResult> RejectAsync(int status, string code, string key, string? account, int? operatorId = null)
    {
        if (audit?.Enabled == true)
        {
            if (db.Database.CurrentTransaction is not null) deferredDenial = (status, code, account, operatorId);
            else await audit.FailureAsync(code, status, account, operatorId);
        }
        var body = ApiUiError.Create(code, key);
        return status switch { 401 => Unauthorized(body), 409 => Conflict(body), _ => StatusCode(status, body) };
    }


    private IActionResult CreateTokenResult(MesSpc.Api.Domain.Entities.Operator systemUser, string normalizedUsername, TimeSpan lifetime, string jti)
    {
        var keyStr = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(keyStr) || keyStr.Length < 32)
            return StatusCode(500, ApiUiError.Create("AUTH_JWT_NOT_CONFIGURED", "spc.api.jwtNotConfigured"));

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
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));
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
