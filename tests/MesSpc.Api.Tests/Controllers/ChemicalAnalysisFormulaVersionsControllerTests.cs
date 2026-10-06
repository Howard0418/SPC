using System.Security.Claims;
using FluentAssertions;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests.Controllers;

public class ChemicalAnalysisFormulaVersionsControllerTests
{
    [Fact]
    public async Task GetVersions_WhenChemicalMappingExists_ReturnsVersionsNewestFirst()
    {
        await using var db = CreateDb();
        var mapping = await SeedMappingAsync(db, "CHEM", """{"formula":"current"}""");
        db.ChemicalAnalysisFormulaVersions.AddRange(
            Version(mapping.Id, 1, """{"formula":"v1"}"""),
            Version(mapping.Id, 2, """{"formula":"v2"}"""));
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.GetVersions(mapping.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("\"VersionNo\":2");
        json.IndexOf("\"VersionNo\":2", StringComparison.Ordinal).Should()
            .BeLessThan(json.IndexOf("\"VersionNo\":1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RestoreVersion_WhenVersionExists_UpdatesFormulaAndCreatesRestoreVersion()
    {
        await using var db = CreateDb();
        var mapping = await SeedMappingAsync(db, "CHEM", """{"formula":"current"}""");
        var oldVersion = Version(mapping.Id, 1, """{"formula":"old"}""");
        db.ChemicalAnalysisFormulaVersions.Add(oldVersion);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.RestoreVersion(mapping.Id, oldVersion.Id, new RestoreChemicalAnalysisFormulaVersionRequest("回復測試"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        (await db.PartProcessCharacteristics.SingleAsync(x => x.Id == mapping.Id))
            .ChemicalAnalysisConfigJson.Should().Be("""{"formula":"old"}""");
        var restore = await db.ChemicalAnalysisFormulaVersions.SingleAsync(x => x.ChangeType == "Restore");
        restore.RestoredFromVersionId.Should().Be(oldVersion.Id);
        restore.VersionNo.Should().Be(2);
    }

    [Fact]
    public async Task GetVersions_WhenMappingIsNotChemical_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var mapping = await SeedMappingAsync(db, "PROCESS", null);
        var controller = CreateController(db);

        var result = await controller.GetVersions(mapping.Id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void RestoreVersion_ShouldRequireAdminOrEditorRole()
    {
        var method = typeof(ChemicalAnalysisFormulaVersionsController).GetMethod(nameof(ChemicalAnalysisFormulaVersionsController.RestoreVersion));

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();
        authorize.Roles.Should().Be("Admin,Editor");
    }

    [Fact]
    public async Task RestoreVersion_WhenMappingIsNotChemical_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var mapping = await SeedMappingAsync(db, "PROCESS", """{"formula":"current"}""");
        var version = Version(mapping.Id, 1, """{"formula":"old"}""");
        db.ChemicalAnalysisFormulaVersions.Add(version);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.RestoreVersion(mapping.Id, version.Id, new RestoreChemicalAnalysisFormulaVersionRequest(null), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static ChemicalAnalysisFormulaVersion Version(int ppcId, int versionNo, string configJson) => new()
    {
        PartProcessCharacteristicId = ppcId,
        VersionNo = versionNo,
        ConfigJson = configJson,
        ChangeType = "Update",
        ChangedBy = "tester",
        ChangedAt = DateTime.UtcNow.AddMinutes(versionNo)
    };

    private static async Task<PartProcessCharacteristic> SeedMappingAsync(AppDbContext db, string scope, string? configJson)
    {
        var mapping = new PartProcessCharacteristic
        {
            ControlScope = scope,
            ProcessId = 1,
            CharacteristicId = 1,
            ChemicalAnalysisConfigJson = configJson,
            IsEnabled = true
        };
        db.PartProcessCharacteristics.Add(mapping);
        await db.SaveChangesAsync();
        return mapping;
    }

    private static ChemicalAnalysisFormulaVersionsController CreateController(AppDbContext db)
    {
        var controller = new ChemicalAnalysisFormulaVersionsController(db, new ChemicalAnalysisFormulaVersionService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "spc-editor"), new Claim(ClaimTypes.Role, "Editor")],
                    "TestAuth"))
            }
        };
        return controller;
    }

    private static AppDbContext CreateDb()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-formula-version-controller-{Guid.NewGuid():N}")
            .Options);
}
