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

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController(
    IConfiguration config,
    AppDbContext db,
    UserPasswordHasher passwordHasher) : ControllerBase
{
    [HttpPost("portal-sso")]
    public async Task<IActionResult> PortalSso([FromBody] PortalSsoRequest req)
    {
        var sharedKey = config["Auth:PortalSsoKey"];
        if (string.IsNullOrWhiteSpace(sharedKey) || sharedKey.Length < 32)
            return StatusCode(503, new { message = "Portal SSO 尚未設定。" });

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (string.IsNullOrWhiteSpace(req.Username)
            || string.IsNullOrWhiteSpace(req.Nonce)
            || Math.Abs(now - req.Timestamp) > 60)
            return Unauthorized(new { message = "Portal SSO 請求已失效。" });

        var normalizedUsername = req.Username.Trim();
        var payload = $"{normalizedUsername}|{req.Timestamp}|{req.Nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedKey));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        byte[] supplied;
        try { supplied = Convert.FromBase64String(req.Signature); }
        catch { return Unauthorized(new { message = "Portal SSO 簽章不正確。" }); }
        if (supplied.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(supplied, expected))
            return Unauthorized(new { message = "Portal SSO 簽章不正確。" });

        var systemUser = await db.Operators.FirstOrDefaultAsync(x =>
            x.IsActive && x.Username != null && x.Username == normalizedUsername);
        if (systemUser is null)
            return Unauthorized(new { message = "此帳號尚未建立 SPC 操作者或已停用。" });

        return CreateTokenResult(systemUser, normalizedUsername, TimeSpan.FromMinutes(60));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var normalizedUsername = req.Username.Trim();
        var systemUser = await db.Operators.FirstOrDefaultAsync(x =>
            x.IsActive && x.Username != null && x.Username == normalizedUsername);

        if (systemUser is null || !passwordHasher.Verify(req.Password, systemUser.PasswordHash))
            return Unauthorized(new { message = "帳號或密碼錯誤，或帳號尚未設定登入密碼" });

        return CreateTokenResult(systemUser, normalizedUsername, TimeSpan.FromHours(8));
    }

    private IActionResult CreateTokenResult(MesSpc.Api.Domain.Entities.Operator systemUser, string normalizedUsername, TimeSpan lifetime)
    {
        var keyStr = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(keyStr) || keyStr.Length < 32)
            return StatusCode(500, new { message = "Auth:JwtKey 未設定或過短（至少 32 字元）" });

        var displayName = systemUser.OperatorName;
        var operatorCode = systemUser.OperatorCode;
        var role = UserRoles.Normalize(systemUser.Role);
        var userId = systemUser.Id;
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
            user = new { id = userId, username = normalizedUsername, operatorCode, displayName, role }
        });
    }

    public record LoginRequest(string Username, string Password);
    public record PortalSsoRequest(string Username, long Timestamp, string Nonce, string Signature);
}
