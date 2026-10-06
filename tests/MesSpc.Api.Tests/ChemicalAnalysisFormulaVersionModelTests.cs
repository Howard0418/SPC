using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ChemicalAnalysisFormulaVersionModelTests
{
    [Fact]
    public void AppDbContext_ShouldMapChemicalAnalysisFormulaVersions()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"chemical-formula-version-model-{Guid.NewGuid():N}")
            .Options);

        var entityType = db.Model.FindEntityType(typeof(ChemicalAnalysisFormulaVersion));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ChemicalAnalysisFormulaVersions");
        entityType.FindProperty(nameof(ChemicalAnalysisFormulaVersion.ChangeType))!
            .GetMaxLength().Should().Be(32);
        entityType.FindProperty(nameof(ChemicalAnalysisFormulaVersion.ChangedBy))!
            .GetMaxLength().Should().Be(100);
        entityType.FindProperty(nameof(ChemicalAnalysisFormulaVersion.Reason))!
            .GetMaxLength().Should().Be(500);
        entityType.GetIndexes()
            .Should().Contain(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ChemicalAnalysisFormulaVersion.PartProcessCharacteristicId), nameof(ChemicalAnalysisFormulaVersion.VersionNo) }));
    }
}
