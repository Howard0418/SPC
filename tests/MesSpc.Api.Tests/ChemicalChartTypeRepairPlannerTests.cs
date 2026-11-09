using FluentAssertions;
using OctoberProductionChemicalCorrection;

namespace MesSpc.Api.Tests;

public sealed class ChemicalChartTypeRepairPlannerTests
{
    [Fact]
    public void Build_PlansOnlyNullMappingsWithUniqueIMrEvidence()
    {
        var result = ChemicalChartTypeRepairPlanner.Build([
            new(2425, null, ["I_MR"], 4),
            new(2426, "I_MR", ["I_MR"], 7)
        ]);

        result.ToUpdate.Should().Equal(2425);
        result.AlreadyCorrect.Should().Equal(2426);
        result.FailedJobCount.Should().Be(11);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "XBAR_R")]
    [InlineData("XBAR_R", "I_MR")]
    public void Build_RejectsMissingConflictingOrOverwriteCandidate(string? current, string? candidate)
    {
        var candidates = candidate is null ? Array.Empty<string>() : [candidate];

        Action act = () => ChemicalChartTypeRepairPlanner.Build([new(2425, current, candidates, 4)]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_RejectsDuplicateOrMixedEvidence()
    {
        Action duplicate = () => ChemicalChartTypeRepairPlanner.Build([
            new(2425, null, ["I_MR"], 4), new(2425, null, ["I_MR"], 4)
        ]);
        Action mixed = () => ChemicalChartTypeRepairPlanner.Build([
            new(2425, null, ["I_MR", "XBAR_R"], 4)
        ]);

        duplicate.Should().Throw<InvalidOperationException>();
        mixed.Should().Throw<InvalidOperationException>();
    }
}
