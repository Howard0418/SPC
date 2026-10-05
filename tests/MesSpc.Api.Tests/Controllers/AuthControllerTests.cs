using System.Security.Cryptography;
using System.Text;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task PortalSso_WhenNonceIsReplayed_ReturnsUnauthorized()
    {
        const string sharedKey = "test-portal-sso-key-that-is-at-least-32-characters-long";
        const string jwtKey = "test-jwt-signing-key-that-is-at-least-32-characters-long";
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"spc-auth-{Guid.NewGuid():N}")
            .Options);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:PortalSsoKey"] = sharedKey,
            ["Auth:JwtKey"] = jwtKey
        }).Build();
        var controller = new AuthController(configuration, db);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        const string username = "qa.user";
        const string displayName = "QA User";
        var payload = $"{username}|{displayName}|{timestamp}|{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        var request = new AuthController.PortalSsoRequest(username, displayName, timestamp, nonce, signature);

        var first = await controller.PortalSso(request);
        var replay = await controller.PortalSso(request);

        Assert.IsType<OkObjectResult>(first);
        Assert.IsType<UnauthorizedObjectResult>(replay);
        Assert.Single(db.Operators);
    }
}
