using FluentAssertions;
using OctoberProductionFormulaF;

namespace MesSpc.Api.Tests;

public sealed class OctoberProductionFormulaFPlannerTests
{
    private static IReadOnlyDictionary<string, decimal> F(decimal b3 = .99m, decimal b5 = 1m) =>
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["B3"] = b3, ["B4"] = 1m, ["B5"] = b5, ["B6"] = 1m,
            ["B7"] = 1m, ["B8"] = 1m, ["B9"] = 1m, ["B10"] = 1m
        };

    private static SourceScheduleRow Row(
        int ppcId = 2425,
        string day = "2026-10-01",
        string formula = "Primary*8.7*F!$B$3",
        IReadOnlyDictionary<string, decimal>? cells = null,
        string source = "source-a") =>
        new(ppcId, "C5|TANK-000255|CHAR-000830|ml/L", DateOnly.Parse(day), formula, cells ?? F(), source);

    [Fact]
    public void Build_IsolatesEveryRowInAConflictingPpcDay_AndKeepsConfirmedDays()
    {
        var plan = OctoberProductionFormulaFPlanner.Build([
            Row(source: "open", cells: F(.995m)),
            Row(source: "middle", cells: F(1.02m, .983m)),
            Row(day: "2026-10-02", source: "next-day")
        ]);

        plan.Conflicts.Should().ContainSingle();
        plan.Conflicts[0].SourceIds.Should().BeEquivalentTo("open", "middle");
        plan.Periods.Should().ContainSingle(x => x.Day == new DateOnly(2026, 10, 2));
        plan.IncludedRowCount.Should().Be(1);
        plan.ExcludedRowCount.Should().Be(2);
    }

    [Fact]
    public void Build_DeduplicatesEquivalentFormulaAndFullFDefinitions()
    {
        var plan = OctoberProductionFormulaFPlanner.Build([
            Row(source: "a"),
            Row(day: "2026-10-02", formula: " Primary * 8.7 * F!B3 ", source: "b")
        ]);

        plan.Formulas.Should().ContainSingle();
        plan.FTables.Should().ContainSingle();
        plan.Periods.Should().HaveCount(2);
        plan.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public void Build_RejectsIncompleteFTableBeforePlanning()
    {
        var cells = F().Where(x => x.Key != "B10").ToDictionary();
        Action act = () => OctoberProductionFormulaFPlanner.Build([Row(cells: cells)]);
        act.Should().Throw<ArgumentException>().WithMessage("*B3*B10*");
    }

    [Theory]
    [InlineData("test", "172.16.110.16", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_TEST", "production")]
    [InlineData("production", "other", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_2026", "test")]
    public void ValidateTarget_RejectsAnyNonProductionIdentity(string app, string server, string database, string api)
    {
        Action act = () => OctoberProductionFormulaFPlanner.ValidateTarget(app, server, database, api);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateTarget_AcceptsOnlyVerifiedProductionIdentity()
    {
        Action act = () => OctoberProductionFormulaFPlanner.ValidateTarget(
            "production", "172.16.110.16", "PMR_SPC_2026", "production");
        act.Should().NotThrow();
    }

    [Fact]
    public void ScheduleEquals_IsOrderIndependentButDetectsDifferentF()
    {
        ScheduleKey[] desired =
        [
            new(2425, new DateOnly(2026, 10, 1), "formula-a", "f-a"),
            new(2425, new DateOnly(2026, 10, 2), "formula-a", "f-b")
        ];

        OctoberProductionFormulaFPlanner.ScheduleEquals(desired, desired.Reverse()).Should().BeTrue();
        OctoberProductionFormulaFPlanner.ScheduleEquals(
            desired,
            [desired[0], desired[1] with { FSignature = "f-other" }]).Should().BeFalse();
    }
}
