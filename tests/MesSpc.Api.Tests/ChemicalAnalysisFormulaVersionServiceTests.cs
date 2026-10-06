using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ChemicalAnalysisFormulaVersionServiceTests
{
    [Fact]
    public async Task RecordChangeAsync_ShouldCreateNextVersion_WhenChemicalFormulaChanges()
    {
        await using var db = CreateDb();
        var mapping = await AddMappingAsync(db, "CHEM", """{"formula":"old"}""");
        var service = new ChemicalAnalysisFormulaVersionService(db);

        var result = await service.RecordChangeAsync(mapping.Id, mapping.ChemicalAnalysisConfigJson, """{"formula":"new"}""", "ihao_ting", "調整公式", CancellationToken.None);

        result.Created.Should().BeTrue();
        result.Version.Should().NotBeNull();
        result.Version!.VersionNo.Should().Be(1);
        result.Version.ConfigJson.Should().Be("""{"formula":"new"}""");
        result.Version.PreviousConfigJson.Should().Be("""{"formula":"old"}""");
        result.Version.ChangeType.Should().Be("Update");
        result.Version.ChangedBy.Should().Be("ihao_ting");
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldSkip_WhenFormulaIsUnchanged()
    {
        await using var db = CreateDb();
        var mapping = await AddMappingAsync(db, "CHEM", """{"formula":"same"}""");
        var service = new ChemicalAnalysisFormulaVersionService(db);

        var result = await service.RecordChangeAsync(mapping.Id, mapping.ChemicalAnalysisConfigJson, "  {\"formula\":\"same\"}  ", "ihao_ting", null, CancellationToken.None);

        result.Created.Should().BeFalse();
        (await db.ChemicalAnalysisFormulaVersions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldRejectNonChemicalMapping()
    {
        await using var db = CreateDb();
        var mapping = await AddMappingAsync(db, "PROCESS", """{"formula":"old"}""");
        var service = new ChemicalAnalysisFormulaVersionService(db);

        Func<Task> act = () => service.RecordChangeAsync(mapping.Id, mapping.ChemicalAnalysisConfigJson, """{"formula":"new"}""", "ihao_ting", null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only supports CHEM*");
    }

    [Fact]
    public async Task RestoreAsync_ShouldUpdateCurrentFormulaAndCreateRestoreVersion()
    {
        await using var db = CreateDb();
        var mapping = await AddMappingAsync(db, "CHEM", """{"formula":"current"}""");
        var oldVersion = new ChemicalAnalysisFormulaVersion
        {
            PartProcessCharacteristicId = mapping.Id,
            VersionNo = 1,
            ConfigJson = """{"formula":"old"}""",
            PreviousConfigJson = null,
            ChangeType = "Update",
            ChangedBy = "tester"
        };
        db.ChemicalAnalysisFormulaVersions.Add(oldVersion);
        await db.SaveChangesAsync();
        var service = new ChemicalAnalysisFormulaVersionService(db);

        var result = await service.RestoreAsync(mapping.Id, oldVersion.Id, "ihao_ting", "回復舊版", CancellationToken.None);

        result.Restored.Should().BeTrue();
        (await db.PartProcessCharacteristics.SingleAsync(x => x.Id == mapping.Id))
            .ChemicalAnalysisConfigJson.Should().Be("""{"formula":"old"}""");
        result.Version.Should().NotBeNull();
        result.Version!.VersionNo.Should().Be(2);
        result.Version.ChangeType.Should().Be("Restore");
        result.Version.RestoredFromVersionId.Should().Be(oldVersion.Id);
        result.Version.PreviousConfigJson.Should().Be("""{"formula":"current"}""");
    }

    private static AppDbContext CreateDb()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-formula-version-service-{Guid.NewGuid():N}")
            .Options);

    private static async Task<PartProcessCharacteristic> AddMappingAsync(AppDbContext db, string scope, string? formula)
    {
        var mapping = new PartProcessCharacteristic
        {
            ControlScope = scope,
            ChemicalAnalysisConfigJson = formula,
            IsEnabled = true
        };
        db.PartProcessCharacteristics.Add(mapping);
        await db.SaveChangesAsync();
        return mapping;
    }
}
