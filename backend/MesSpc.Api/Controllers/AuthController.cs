using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var normalizedUsername = req.Username.Trim();
        var systemUser = await db.Operators.FirstOrDefaultAsync(x =>
            x.IsActive && x.Username != null && x.Username == normalizedUsername);

        string displayName;
        string operatorCode;
        string role;
        int? userId;

        if (systemUser is not null && passwordHasher.Verify(req.Password, systemUser.PasswordHash))
        {
            displayName = systemUser.OperatorName;
            operatorCode = systemUser.OperatorCode;
            role = UserRoles.Normalize(systemUser.Role);
            userId = systemUser.Id;
        }
        else
        {
            var demoUser = config["Auth:DemoUsername"] ?? "demo";
            var demoPass = config["Auth:DemoPassword"] ?? "demo123";
            if (req.Username != demoUser || req.Password != demoPass)
                return Unauthorized(new { message = "帳號或密碼錯誤，或帳號尚未設定登入密碼" });

            displayName = "系統管理員";
            operatorCode = demoUser;
            role = UserRoles.Editor;
            userId = null;
        }

        var keyStr = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(keyStr) || keyStr.Length < 32)
            return StatusCode(500, new { message = "Auth:JwtKey 未設定或過短（至少 32 字元）" });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, normalizedUsername),
            new(ClaimTypes.Role, role),
            new("displayName", displayName),
            new("operatorCode", operatorCode)
        };
        if (userId.HasValue) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
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
}
