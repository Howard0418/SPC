using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class CurrentOperatorSessionTests
{
    [Fact]
    public async Task ResolvesCurrentRoleAndPermissionsFromSameSignedIdentity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = await CreateDbAsync(connection);
        var session = new CurrentOperatorSession(db);
        var signed = Principal("41", "qa.user");

        var first = await session.ResolveAsync(signed);
        Assert.NotNull(first);
        Assert.True(first.IsInRole(UserRoles.Editor));
        Assert.True(first.HasClaim("permission", "data.particles"));
        Assert.False(first.HasClaim("permission", AuditPermissions.Read));

        var op = await db.Operators.SingleAsync();
        op.Role = UserRoles.Viewer;
        op.PagePermissionsJson = SpcPagePermissions.NormalizeForStorage(["analysis.spc"]);
        await db.SaveChangesAsync();

        var next = await session.ResolveAsync(signed);
        Assert.NotNull(next);
        Assert.True(next.IsInRole(UserRoles.Viewer));
        Assert.False(next.IsInRole(UserRoles.Editor));
        Assert.Equal(["analysis.spc"], next.FindAll("permission").Select(x => x.Value).ToArray());
        Assert.Equal("fixture-session", next.FindFirstValue(JwtRegisteredClaimNames.Jti));
        Assert.Equal("4102444800", next.FindFirstValue(JwtRegisteredClaimNames.Exp));

        var request = new DefaultHttpContext { User = next };
        request.Request.Path = "/api/operators/41";
        request.Request.Method = HttpMethods.Put;
        var reachedAction = false;
        var guard = new ViewerWriteGuardMiddleware(_ => { reachedAction = true; return Task.CompletedTask; });
        await guard.InvokeAsync(request);
        Assert.Equal(StatusCodes.Status403Forbidden, request.Response.StatusCode);
        Assert.False(reachedAction);
    }

    [Theory]
    [InlineData("41", "qa.user", false, false)]
    [InlineData("41", "qa.user", true, true)]
    [InlineData("42", "qa.user", true, false)]
    [InlineData("41", "old.account", true, false)]
    [InlineData("invalid", "qa.user", true, false)]
    public async Task RejectsDisabledDeletedMissingOrReboundIdentities(string id, string name, bool active, bool deleted)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = await CreateDbAsync(connection, active, deleted);
        Assert.Null(await new CurrentOperatorSession(db).ResolveAsync(Principal(id, name)));
    }

    private static ClaimsPrincipal Principal(string id, string name) => new(new ClaimsIdentity(
    [
        new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, name), new(ClaimTypes.Role, UserRoles.Editor),
        new("permission", AuditPermissions.Read), new(JwtRegisteredClaimNames.Jti, "fixture-session"),
        new(JwtRegisteredClaimNames.Exp, "4102444800")
    ], "Bearer"));

    private static async Task<AppDbContext> CreateDbAsync(SqliteConnection connection, bool active = true, bool deleted = false)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Operators.Add(new()
        {
            Id = 41, OperatorCode = "P00041", OperatorName = "Fixture", Username = "DOMAIN\\QA.User",
            Role = UserRoles.Editor, IsActive = active, IsDeleted = deleted,
            PagePermissionsJson = SpcPagePermissions.NormalizeForStorage(["analysis.spc", "data.particles"])
        });
        await db.SaveChangesAsync();
        return db;
    }
}
