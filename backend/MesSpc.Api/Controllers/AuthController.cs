using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController(IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = config["Auth:DemoUsername"] ?? "demo";
        var pass = config["Auth:DemoPassword"] ?? "demo123";
        if (req.Username != user || req.Password != pass)
            return Unauthorized(new { message = "帳號或密碼錯誤" });

        var keyStr = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(keyStr) || keyStr.Length < 32)
            return StatusCode(500, new { message = "Auth:JwtKey 未設定或過短（至少 32 字元）" });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Name, req.Username)],
            expires: expires,
            signingCredentials: creds);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(token);
        return Ok(new { token = jwt, expiresAt = expires });
    }

    public record LoginRequest(string Username, string Password);
}
