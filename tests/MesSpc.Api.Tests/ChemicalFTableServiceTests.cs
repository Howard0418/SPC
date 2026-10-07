using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MesSpc.Api.Tests;

public class ChemicalFTableServiceTests
{
    [Fact]
    public async Task EvaluateChemicalFormulaAsync_ShouldResolveActiveFTableCell()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-f-table-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);

        var version = new ChemicalFTableVersion
        {
            VersionCode = "F-TEST",
            DisplayName = "F 表測試版",
            IsActive = true,
            EffectiveAt = DateTime.UtcNow
        };
        db.ChemicalFTableVersions.Add(version);
        await db.SaveChangesAsync();
        db.ChemicalFTableCells.Add(new ChemicalFTableCell
        {
            VersionId = version.Id,
            SheetName = "F",
            CellAddress = "F!B3",
            NormalizedCellAddress = "F!B3",
            StandardSolution = "1N NaOH",
            NumericValue = 1.02m,
            RawValue = "1.02"
        });
        await db.SaveChangesAsync();

        var service = new ChemicalFTableService(db);
        var result = await service.EvaluateChemicalFormulaAsync("Primary * F!$B$3", new Dictionary<string, decimal>
        {
            ["Primary"] = 10m
        }, CancellationToken.None);

        result.Value.Should().Be(10.2m);
        result.CellReferences.Should().Equal("F!B3");
        result.EvaluatedExpression.Should().Be("Primary * F_B3");
    }

    [Fact]
    public async Task SyncCellsAsync_ShouldCreateVersionHistory_WhenAppliedCellsChange()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-f-table-history-{Guid.NewGuid():N}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new AppDbContext(options);
        var service = new ChemicalFTableService(db);

        var result = await service.SyncCellsAsync(
            [new ChemicalFTableSeedCell("F!B3", "1N NaOH", 1.05m)],
            true,
            "F-20261007",
            "F 表 20261007",
            "TEST",
            CancellationToken.None,
            "tester",
            "initial apply");

        result.Applied.Should().BeTrue();
        var history = await db.ChemicalFTableVersionHistories.SingleAsync();
        history.VersionNo.Should().Be(1);
        history.VersionCode.Should().Be("F-20261007");
        history.ChangeType.Should().Be("Create");
        history.ChangedBy.Should().Be("tester");
        history.CellsJson.Should().Contain("F!B3");
    }

    [Fact]
    public async Task RestoreVersionHistoryAsync_ShouldRestoreCellsAndRecordRestoreHistory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-f-table-restore-{Guid.NewGuid():N}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new AppDbContext(options);
        var service = new ChemicalFTableService(db);

        await service.SyncCellsAsync(
            [new ChemicalFTableSeedCell("F!B3", "1N NaOH", 1.05m)],
            true,
            "F-BASE",
            "F 表基準",
            "TEST",
            CancellationToken.None);
        var sourceHistory = await db.ChemicalFTableVersionHistories.SingleAsync();
        await service.SyncCellsAsync(
            [new ChemicalFTableSeedCell("F!B3", "1N NaOH", 0.95m)],
            true,
            "F-NEXT",
            "F 表新版",
            "TEST",
            CancellationToken.None);

        await service.RestoreVersionHistoryAsync(sourceHistory.Id, "tester", "restore baseline", CancellationToken.None);

        var active = await service.GetActiveDetailAsync(CancellationToken.None);
        active!.VersionCode.Should().Be("F-BASE");
        active.Cells.Single(x => x.CellAddress == "F!B3").NumericValue.Should().Be(1.05m);
        var restoreHistory = await db.ChemicalFTableVersionHistories
            .OrderByDescending(x => x.VersionNo)
            .FirstAsync();
        restoreHistory.ChangeType.Should().Be("Restore");
        restoreHistory.RestoredFromHistoryId.Should().Be(sourceHistory.Id);
    }
}
