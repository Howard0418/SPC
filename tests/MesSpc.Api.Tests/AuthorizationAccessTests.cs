using System.Security.Claims;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class AuthorizationAccessTests
{
    [Fact]
    public async Task CanManageCalibrationAsync_ShouldAllowActiveEditorWithPermission()
    {
        await using var db = CreateDbContext();
        db.Operators.Add(new Operator
        {
            OperatorCode = "OP-001",
            OperatorName = "Test Editor",
            Username = "editor",
            Role = UserRoles.Editor,
            IsActive = true,
            PagePermissionsJson = """["calibration.manage"]"""
        });
        await db.SaveChangesAsync();

        var access = new AuthorizationAccess(db);

        Assert.True(await access.CanManageCalibrationAsync(CreateUser("editor", UserRoles.Editor)));
    }

    [Fact]
    public async Task CanManageCalibrationAsync_ShouldDenyViewer()
    {
        await using var db = CreateDbContext();
        db.Operators.Add(new Operator
        {
            OperatorCode = "OP-002",
            OperatorName = "Test Viewer",
            Username = "viewer",
            Role = UserRoles.Viewer,
            IsActive = true,
            PagePermissionsJson = """["calibration.manage"]"""
        });
        await db.SaveChangesAsync();

        var access = new AuthorizationAccess(db);

        Assert.False(await access.CanManageCalibrationAsync(CreateUser("viewer", UserRoles.Viewer)));
    }

    [Fact]
    public async Task CanManageCalibrationAsync_ShouldDenyInactiveEditor()
    {
        await using var db = CreateDbContext();
        db.Operators.Add(new Operator
        {
            OperatorCode = "OP-003",
            OperatorName = "Inactive Editor",
            Username = "inactive",
            Role = UserRoles.Editor,
            IsActive = false,
            PagePermissionsJson = """["calibration.manage"]"""
        });
        await db.SaveChangesAsync();

        var access = new AuthorizationAccess(db);

        Assert.False(await access.CanManageCalibrationAsync(CreateUser("inactive", UserRoles.Editor)));
    }

    [Fact]
    public async Task CanManageCalibrationAsync_ShouldUseClaimPermission_WhenOperatorHasNoPermissionJson()
    {
        await using var db = CreateDbContext();
        db.Operators.Add(new Operator
        {
            OperatorCode = "OP-004",
            OperatorName = "Claim Editor",
            Username = "claim-editor",
            Role = UserRoles.Editor,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var access = new AuthorizationAccess(db);
        var user = CreateUser("claim-editor", UserRoles.Editor, new Claim("permissions", """["calibration.manage"]"""));

        Assert.True(await access.CanManageCalibrationAsync(user));
    }

    [Fact]
    public async Task CanManageCalibrationAsync_ShouldDenyUnknownEditorEvenWithValidRoleClaim()
    {
        await using var db = CreateDbContext();
        var access = new AuthorizationAccess(db);

        Assert.False(await access.CanManageCalibrationAsync(CreateUser("unknown", UserRoles.Editor)));
    }

    [Fact]
    public async Task CanAccessPageAsync_ShouldRequireActiveEditorAndExplicitParticleEntryPermission()
    {
        await using var db = CreateDbContext();
        db.Operators.AddRange(
            new Operator { OperatorCode = "OP-005", OperatorName = "Particle Editor", Username = "particle-editor", Role = UserRoles.Editor, IsActive = true, PagePermissionsJson = """["system.page-permissions.v2","data.particles"]""" },
            new Operator { OperatorCode = "OP-006", OperatorName = "Analysis Editor", Username = "analysis-editor", Role = UserRoles.Editor, IsActive = true, PagePermissionsJson = """["system.page-permissions.v2","analysis.particles"]""" });
        await db.SaveChangesAsync();
        var access = new AuthorizationAccess(db);

        Assert.True(await access.CanAccessPageAsync(CreateUser("particle-editor", UserRoles.Editor), "data.particles"));
        Assert.False(await access.CanAccessPageAsync(CreateUser("analysis-editor", UserRoles.Editor), "data.particles"));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreateUser(string name, string role, params Claim[] extraClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };
        claims.AddRange(extraClaims);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
