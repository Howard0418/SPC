using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;

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
}
