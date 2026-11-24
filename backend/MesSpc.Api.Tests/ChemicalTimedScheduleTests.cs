using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class ChemicalTimedScheduleTests
{
    [Fact]
    public void TimedCutoverPreservesExactBoundaryAndEarlierCalendarPeriod()
    {
        var periods = ChemicalFormulaCalculation.ValidatePeriods([
            new(291, 247, "2026-11-24", "2026-11-24"),
            new(291, 247, "2026-11-25T00:00:00+08:00", "2026-11-25T10:30:00+08:00"),
            new(1544, 247, "2026-11-25T10:30:00+08:00", null)
        ]);

        var at = new DateTime(2026, 11, 25, 2, 30, 0, DateTimeKind.Utc);
        Assert.Equal(at, periods[1].UntilUtc);
        Assert.Equal(at, periods[2].FromUtc);
        Assert.Equal(periods[0].UntilUtc, periods[1].FromUtc);
    }

    [Fact]
    public void AdjustmentChangeSplitsCurrentIntervalAtSave()
    {
        var saved = new DateTime(2026, 11, 25, 2, 20, 0, DateTimeKind.Utc);
        var start = saved.Date;
        var next = start.AddDays(1);
        var periods = ChemicalFormulaCalculation.ScheduleAdjustmentChange([
            new(291, 247, start, next),
            new(300, 248, next, null)
        ], 1544, saved);

        var cutover = saved;
        Assert.Equal(new ChemicalUtcPeriod(291, 247, start, cutover), periods[0]);
        Assert.Equal(new ChemicalUtcPeriod(1544, 247, cutover, next), periods[1]);
        Assert.Equal(new ChemicalUtcPeriod(300, 248, next, null), periods[2]);
    }

    [Fact]
    public void GapFillUsesReferenceSnapshotAndLeavesExistingIntervalsUntouched()
    {
        var existing = new ChemicalUtcPeriod(291, 247, new DateTime(2026, 11, 24, 16, 0, 0, DateTimeKind.Utc), null);
        var result = ChemicalFormulaCalculation.FillScheduleDay([existing], "2026-11-23",
            "2026-11-25T12:00:00+08:00", 291, 247);

        Assert.Equal(existing, result[0]);
        Assert.Equal(new ChemicalUtcPeriod(291, 247, new DateTime(2026, 11, 22, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 23, 16, 0, 0, DateTimeKind.Utc)), result[1]);
        Assert.Throws<ArgumentException>(() => ChemicalFormulaCalculation.FillScheduleDay([existing], "2026-11-25",
            "2026-11-25T12:00:00+08:00", 291, 247));
        Assert.Throws<ChemicalPreviewConflictException>(() => ChemicalFormulaCalculation.FillScheduleDay([existing], "2026-11-23",
            "2026-11-25T12:00:00+08:00", 291, 248));
    }

    [Theory]
    [InlineData("2026-11-25", "2026-11-25T10:30:00+08:00")]
    [InlineData("2026-11-25T10:30:00", null)]
    [InlineData("2026-11-25T10:30:00+08:00", "2026-11-25T10:29:00+08:00")]
    public void RejectsAmbiguousOrReversedTimedIntervals(string start, string? end)
    {
        Assert.Throws<ArgumentException>(() =>
            ChemicalFormulaCalculation.ValidatePeriods([new(291, 247, start, end)]));
    }
}
