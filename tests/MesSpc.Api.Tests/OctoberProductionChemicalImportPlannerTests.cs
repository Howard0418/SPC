using FluentAssertions;
using OctoberProductionChemicalImport;

namespace MesSpc.Api.Tests;

public sealed class OctoberProductionChemicalImportPlannerTests
{
    private static ProductionChemicalSourceRow Row(
        int ppcId = 2425,
        string day = "2026-10-01",
        string sourceId = "source-a",
        string machine = "C5",
        string phase = "OPEN",
        string stage = "GENERAL",
        int sourceRow = 7) =>
        new(ppcId, DateOnly.Parse(day), sourceId, new string('a', 64), "2026-10-01.xlsx", "C5", sourceRow,
            machine, phase, stage, "10", null, null, null, "86.13", null);

    [Fact]
    public void Build_SkipsEverySourceRowWhenPpcDayExistedBeforeImport()
    {
        var rows = new[]
        {
            Row(sourceId: "open", machine: "N1", stage: "OPEN"),
            Row(sourceId: "close", machine: "N1", stage: "CLOSE", sourceRow: 8),
            Row(day: "2026-10-02", sourceId: "new-day")
        };

        var plan = OctoberProductionChemicalImportPlanner.Build(rows,
            new HashSet<ProductionChemicalDayKey> { new(2425, new DateOnly(2026, 10, 1)) }, new HashSet<string>());

        plan.SkippedExisting.Select(x => x.SourceId).Should().BeEquivalentTo("open", "close");
        plan.Included.Should().ContainSingle(x => x.Source.SourceId == "new-day");
    }

    [Fact]
    public void Build_UsesApprovedNoonConventionAndKeepsLegalOpenCloseRowsForNewDay()
    {
        var plan = OctoberProductionChemicalImportPlanner.Build([
            Row(sourceId: "open", machine: "N2", stage: "OPEN"),
            Row(sourceId: "close", machine: "N2", stage: "CLOSE", sourceRow: 8)
        ], new HashSet<ProductionChemicalDayKey>(), new HashSet<string>());

        plan.Included.Should().HaveCount(2);
        plan.Included.Should().OnlyContain(x => x.MeasuredAt == "2026-10-01T12:00:00+08:00");
        plan.Included.Should().OnlyContain(x => x.SourceKey.Contains("not actual sampling time"));
        plan.Included.Select(x => x.Source.SamplingStage).Should().BeEquivalentTo("OPEN", "CLOSE");
    }

    [Fact]
    public void Build_ExcludesAllReviewedFormulaFConflicts()
    {
        var rows = new[] { Row(sourceId: "open"), Row(sourceId: "middle", phase: "MIDDLE", sourceRow: 8) };

        var plan = OctoberProductionChemicalImportPlanner.Build(rows, new HashSet<ProductionChemicalDayKey>(), new HashSet<string>(["open", "middle"]));

        plan.Included.Should().BeEmpty();
        plan.ExcludedConflict.Select(x => x.SourceId).Should().BeEquivalentTo("open", "middle");
    }

    [Theory]
    [InlineData("C5", "OPEN", "OPEN")]
    [InlineData("N1", "OPEN", "GENERAL")]
    [InlineData("N2", "MIDDLE", "GENERAL")]
    [InlineData("C5", "CLOSE", "GENERAL")]
    public void Build_RejectsInvalidApprovedPhaseOrStage(string machine, string phase, string stage)
    {
        Action act = () => OctoberProductionChemicalImportPlanner.Build([
            Row(machine: machine, phase: phase, stage: stage)
        ], new HashSet<ProductionChemicalDayKey>(), new HashSet<string>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_RejectsDuplicateFormalNaturalKey()
    {
        Action act = () => OctoberProductionChemicalImportPlanner.Build([
            Row(sourceId: "a"), Row(sourceId: "b", sourceRow: 8)
        ], new HashSet<ProductionChemicalDayKey>(), new HashSet<string>());

        act.Should().Throw<ArgumentException>().WithMessage("*duplicate*natural key*");
    }

    [Theory]
    [InlineData("test", "172.16.110.16", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_TEST", "production")]
    [InlineData("production", "other", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_2026", "test")]
    public void ValidateTarget_RejectsNonProduction(string app, string server, string database, string api)
    {
        Action act = () => OctoberProductionChemicalImportPlanner.ValidateTarget(app, server, database, api);
        act.Should().Throw<InvalidOperationException>();
    }
}
