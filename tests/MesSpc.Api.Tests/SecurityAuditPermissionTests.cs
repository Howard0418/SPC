using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests;

public sealed class SecurityAuditPermissionTests
{
    private static ClaimsPrincipal User(int id, string role = "Editor", string? permissions = null) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, "qa.actor"), new Claim(ClaimTypes.Role, role),
            new Claim("permissions", permissions ?? "[\"admin.audit.read\",\"admin.audit.export\"]") }, "test"));
    private static IConfiguration Config(bool manager = false) => new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["Auth:Enabled"] = "true", ["Audit:SecurityAdministratorOperatorIds:0"] = manager ? "1" : null }).Build();
    private static async Task<AppDbContext> Db(string? permissions = null, string role = "Editor", bool active = true, bool deleted = false)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Operators.Add(new Operator { Id = 1, OperatorCode = "QA1", OperatorName = "QA", Username = "qa.actor", Role = role,
            IsActive = active, IsDeleted = deleted, PagePermissionsJson = permissions });
        await db.SaveChangesAsync();
        return db;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("broken")]
    [InlineData("[]")]
    [InlineData("[\"admin.audit.export\"]")]
    public async Task DefaultsMalformedJsonAndStaleClaimsNeverGrantRead(string? json)
    {
        await using var db = await Db(json);
        Assert.False(await new AuditPermissions(db, Config()).CanAccessAsync(User(1), "admin.audit.read"));
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("Editor")]
    public async Task ExplicitReadUsesCurrentDatabaseAndRevocationIsImmediate(string role)
    {
        await using var db = await Db("[\"admin.audit.read\",\"admin.audit.export\"]", role);
        var policy = new AuditPermissions(db, Config());
        Assert.True(await policy.CanAccessAsync(User(1), "admin.audit.read"));
        Assert.True(await policy.CanAccessAsync(User(1), "admin.audit.export"));
        Assert.False(await policy.CanAccessAsync(User(1), "admin.audit.evidence"));
        (await db.Operators.SingleAsync()).PagePermissionsJson = "[]";
        await db.SaveChangesAsync();
        Assert.False(await policy.CanAccessAsync(User(1), "admin.audit.read"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task InactiveOrDeletedIdentityCannotUseExplicitPermissions(bool active, bool deleted)
    {
        await using var db = await Db("[\"admin.audit.read\"]", active: active, deleted: deleted);
        Assert.False(await new AuditPermissions(db, Config()).CanAccessAsync(User(1), "admin.audit.read"));
    }

    [Fact]
    public async Task UnknownAnonymousUnknownCodeAndMissingReadDependencyAreDenied()
    {
        await using var db = await Db("[\"admin.audit.export\",\"admin.audit.evidence\"]");
        var policy = new AuditPermissions(db, Config());
        Assert.False(await policy.CanAccessAsync(User(1), "admin.audit.export"));
        Assert.False(await policy.CanAccessAsync(User(1), "admin.audit.evidence"));
        Assert.False(await policy.CanAccessAsync(User(2), "admin.audit.read"));
        Assert.False(await policy.CanAccessAsync(new ClaimsPrincipal(), "admin.audit.read"));
        Assert.False(await policy.CanAccessAsync(User(1), "admin.audit.*"));
    }

    [Fact]
    public async Task OnlyAllowlistedManagerCanGrantOthersAndNeverSelfGrant()
    {
        await using var db = await Db();
        var ordinary = new AuditPermissions(db, Config());
        var manager = new AuditPermissions(db, Config(manager: true));
        Assert.False(await ordinary.CanChangeAsync(User(1), 2, [], ["admin.audit.read"]));
        Assert.True(await manager.CanChangeAsync(User(1), 2, [], ["admin.audit.read", "admin.audit.export"]));
        Assert.False(await manager.CanChangeAsync(User(1), 1, [], ["admin.audit.read"]));
        Assert.False(await manager.CanChangeAsync(User(1), 2, [], ["admin.audit.export"]));
        Assert.False(await manager.CanChangeAsync(User(1), 2, [], ["admin.audit.unknown"]));
        Assert.True(await manager.CanChangeAsync(User(1), 1, ["admin.audit.read"], []));
        (await db.Operators.SingleAsync()).Role = "Viewer"; await db.SaveChangesAsync();
        Assert.False(await manager.CanChangeAsync(User(1), 2, [], ["admin.audit.read"]));
    }

    [Fact]
    public async Task PreserveSensitivePermissionsWithoutGrantDoesNotRequireManager()
    {
        await using var db = await Db();
        var policy = new AuditPermissions(db, Config());
        Assert.True(await policy.CanChangeAsync(User(1), 2, ["admin.audit.read"], ["admin.audit.read"]));
        Assert.True(await policy.CanChangeAsync(User(1), 2, [], ["analysis.spc"]));
    }

    [Fact]
    public void EditorCatalogDefaultsExcludeAllSensitiveAuditPermissions()
    {
        var explicitCodes = new[] { "admin.audit.read", "admin.audit.export", "admin.audit.evidence" };
        foreach (var role in new[] { "Editor", "Viewer" })
            Assert.DoesNotContain(SpcPagePermissions.Resolve(role, null), code => explicitCodes.Contains(code));
        Assert.All(explicitCodes, code => Assert.Contains(SpcPagePermissions.Catalog, item => item.Code == code));
        Assert.Contains("admin.operators", SpcPagePermissions.Resolve("Editor", null));
        Assert.Equal(explicitCodes, SpcPagePermissions.Resolve("Editor", JsonSerializer.Serialize(explicitCodes)));
    }
}
