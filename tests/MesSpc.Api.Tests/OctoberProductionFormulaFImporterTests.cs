using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OctoberProductionFormulaF;

namespace MesSpc.Api.Tests;

public sealed class OctoberProductionFormulaFImporterTests
{
    [Fact]
    public async Task Apply_CreatesImmutableHistoryAndDisabledSchedule_WithoutChangingCurrentMaster()
    {
        await using var db = Db();
        var ppc = await SeedAsync(db);
        var before = ppc.ChemicalAnalysisConfigJson;
        var importer = new OctoberProductionFormulaFImporter(db, new EphemeralDataProtectionProvider());

        var result = await importer.ApplyAsync(Plan(ppc.Id), "tester", "approved October history", default);

        result.FHistoriesCreated.Should().Be(1);
        result.FormulaVersionsCreated.Should().Be(1);
        result.ScheduleRevisionsCreated.Should().Be(1);
        (await db.PartProcessCharacteristics.SingleAsync()).ChemicalAnalysisConfigJson.Should().Be(before);
        (await db.ChemicalFormulaPolicies.SingleAsync()).IsEnabled.Should().BeFalse();
        (await db.ChemicalFormulaEffectivities.SingleAsync()).FTableHistoryId.Should().NotBeNull();
        (await db.ChemicalFTableVersions.SingleAsync()).IsActive.Should().BeTrue();
        (await db.ChemicalFTableCells.SingleAsync(x => x.NormalizedCellAddress == "F!B3")).NumericValue.Should().Be(.99m);
        (await db.VariableMeasurements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Apply_SecondIdenticalRunIsNoChange()
    {
        await using var db = Db();
        var ppc = await SeedAsync(db);
        var importer = new OctoberProductionFormulaFImporter(db, new EphemeralDataProtectionProvider());
        await importer.ApplyAsync(Plan(ppc.Id), "tester", "approved October history", default);

        var before = new
        {
            F = await db.ChemicalFTableVersionHistories.CountAsync(),
            Formula = await db.ChemicalAnalysisFormulaVersions.CountAsync(),
            Revision = await db.ChemicalFormulaScheduleRevisions.CountAsync(),
            Period = await db.ChemicalFormulaEffectivities.CountAsync()
        };
        var result = await importer.ApplyAsync(Plan(ppc.Id), "tester", "approved October history", default);

        result.TotalCreated.Should().Be(0);
        (await db.ChemicalFTableVersionHistories.CountAsync()).Should().Be(before.F);
        (await db.ChemicalAnalysisFormulaVersions.CountAsync()).Should().Be(before.Formula);
        (await db.ChemicalFormulaScheduleRevisions.CountAsync()).Should().Be(before.Revision);
        (await db.ChemicalFormulaEffectivities.CountAsync()).Should().Be(before.Period);
    }

    [Fact]
    public async Task Apply_DifferentExistingScheduleRejectsBeforeCreatingNewHistory()
    {
        await using var db = Db();
        var ppc = await SeedAsync(db);
        var importer = new OctoberProductionFormulaFImporter(db, new EphemeralDataProtectionProvider());
        await importer.ApplyAsync(Plan(ppc.Id), "tester", "approved October history", default);
        var counts = new
        {
            F = await db.ChemicalFTableVersionHistories.CountAsync(),
            Formula = await db.ChemicalAnalysisFormulaVersions.CountAsync(),
            Revision = await db.ChemicalFormulaScheduleRevisions.CountAsync()
        };
        var changed = OctoberProductionFormulaFPlanner.Build([
            Row(ppc.Id, "2026-10-01", "Primary*8.7*F!B3", F(1.02m))
        ]);

        Func<Task> act = () => importer.ApplyAsync(changed, "tester", "changed", default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*schedule*conflict*");
        (await db.ChemicalFTableVersionHistories.CountAsync()).Should().Be(counts.F);
        (await db.ChemicalAnalysisFormulaVersions.CountAsync()).Should().Be(counts.Formula);
        (await db.ChemicalFormulaScheduleRevisions.CountAsync()).Should().Be(counts.Revision);
    }

    private static ProductionFormulaFPlan Plan(int id) => OctoberProductionFormulaFPlanner.Build([
        Row(id, "2026-10-01", "Primary*8.7*F!$B$3", F())
    ]);

    private static SourceScheduleRow Row(int id, string day, string formula, IReadOnlyDictionary<string, decimal> cells)
        => new(id, "C5|TANK-000255|CHAR-000830|ml/L", DateOnly.Parse(day), formula, cells, "source");

    private static IReadOnlyDictionary<string, decimal> F(decimal b3 = .99m) =>
        new Dictionary<string, decimal>
        {
            ["B3"] = b3, ["B4"] = 1, ["B5"] = 1, ["B6"] = 1,
            ["B7"] = 1, ["B8"] = 1, ["B9"] = 1, ["B10"] = 1
        };

    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<PartProcessCharacteristic> SeedAsync(AppDbContext db)
    {
        var ppc = new PartProcessCharacteristic
        {
            ControlScope = "CHEM", IsEnabled = true,
            ChemicalAnalysisConfigJson = "{\"enabled\":true,\"concentrationFormula\":\"Primary*8.7*.99\",\"decimalPlaces\":2}"
        };
        db.Add(ppc);
        var active = new ChemicalFTableVersion { VersionCode = "CURRENT", DisplayName = "Current", IsActive = true };
        db.Add(active);
        await db.SaveChangesAsync();
        foreach (var cell in F())
            db.Add(new ChemicalFTableCell
            {
                VersionId = active.Id, CellAddress = "F!" + cell.Key, NormalizedCellAddress = "F!" + cell.Key,
                NumericValue = cell.Value
            });
        await db.SaveChangesAsync();
        return ppc;
    }
}
