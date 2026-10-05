using MesSpc.Api.Services.Calibration;

namespace MesSpc.Calibration.Tests;

public class CalibrationRulesTests
{
    [Theory]
    [InlineData(31, null)] [InlineData(30, "BEFORE-30")]
    [InlineData(8, "BEFORE-30")] [InlineData(7, "BEFORE-7")]
    [InlineData(1, "BEFORE-7")] [InlineData(0, "BEFORE-0")]
    [InlineData(-1, "OVERDUE-1")] [InlineData(-7, "OVERDUE-1")]
    [InlineData(-8, "OVERDUE-8")]
    public void CatchUpSelectsOnlyCurrentStage(int days, string? expected)
    {
        var today = new DateOnly(2026, 9, 11);
        Assert.Equal(expected, CalibrationRules.Stage(today.AddDays(days), today, [30, 7, 0]));
    }

    [Fact]
    public void UsesTaipeiDateAtUtcBoundary()
    {
        Assert.Equal(new DateOnly(2026, 9, 12), CalibrationRules.Today(new DateTimeOffset(2026, 9, 11, 16, 0, 0, TimeSpan.Zero)));
    }

    [Theory]
    [InlineData(2024, 1, 31, 1, 2024, 2, 29)]
    [InlineData(2026, 8, 31, 1, 2026, 9, 30)]
    [InlineData(2024, 2, 29, 12, 2025, 2, 28)]
    public void MonthlyCycleClampsEndOfMonth(int y, int m, int d, int months, int ey, int em, int ed)
        => Assert.Equal(new DateOnly(ey, em, ed), CalibrationRules.NextDue(new(y,m,d), months));

    [Theory]
    [InlineData("Active", true)] [InlineData("InCalibration", false)]
    [InlineData("Inactive", false)] [InlineData("Retired", false)]
    public void OnlyActiveInstrumentsSend(string status, bool expected)
        => Assert.Equal(expected, CalibrationRules.CanNotify(status));

    [Theory]
    [InlineData(1, 15)] [InlineData(2, 60)] [InlineData(3, 240)] [InlineData(4, -1)]
    public void RetryHasBoundedBackoff(int attempts, int minutes)
        => Assert.Equal(minutes < 0 ? null : TimeSpan.FromMinutes(minutes), CalibrationRules.RetryDelay(attempts));

    [Fact]
    public void EditorDefaultPagesIncludeCalibrationManage()
    {
        Assert.True(CalibrationRules.CanManage("Editor", null));
        Assert.False(CalibrationRules.CanManage("Viewer", "[\"calibration.manage\"]"));
        Assert.True(CalibrationRules.CanManage("Editor", "[\"calibration.manage\"]"));
        Assert.False(CalibrationRules.CanManage("Editor", "[\"analysis.spc\"]"));
        Assert.False(CalibrationRules.CanManage("Editor", "broken"));
    }

    [Fact]
    public void UpcomingWindowFollowsMaxReminderDays()
    {
        var today = new DateOnly(2026, 9, 11);
        Assert.Equal(30, CalibrationRules.UpcomingWindowDays([30, 7, 0]));
        Assert.Equal(60, CalibrationRules.UpcomingWindowDays([60, 7, 0]));
        Assert.True(CalibrationRules.IsUpcoming(today.AddDays(30), today, 30));
        Assert.False(CalibrationRules.IsUpcoming(today.AddDays(31), today, 30));
        Assert.True(CalibrationRules.IsUpcoming(today.AddDays(20), today, 60));
        Assert.True(CalibrationRules.IsOverdue(today.AddDays(-1), today));
        Assert.False(CalibrationRules.IsOverdue(today, today));
        Assert.True(CalibrationRules.CountsInPortalSummary("InCalibration"));
        Assert.False(CalibrationRules.CountsInPortalSummary("Retired"));
        Assert.False(CalibrationRules.ShouldScan(new DateTimeOffset(2026, 9, 10, 23, 0, 0, TimeSpan.Zero)));
        Assert.True(CalibrationRules.ShouldScan(new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero)));
        Assert.True(CalibrationRules.ShouldScan(new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.FromHours(8))));
        var summary = CalibrationRules.CountSummary(
            [("Active", today.AddDays(10)), ("InCalibration", today.AddDays(-2)), ("Retired", today.AddDays(-2)), ("InCalibration", today)],
            today, 30);
        Assert.Equal(2, summary.Upcoming);
        Assert.Equal(1, summary.Overdue);
        Assert.Equal(1, summary.DueToday);
        Assert.Equal(2, summary.InCalibration);
    }

    [Fact]
    public void RejectsInvalidReminderSettings()
    {
        Assert.Throws<ArgumentException>(() => CalibrationRules.ValidateReminderDays([]));
        Assert.Throws<ArgumentException>(() => CalibrationRules.ValidateReminderDays([30,-1]));
        Assert.Throws<ArgumentException>(() => CalibrationRules.ValidateReminderDays([7,7]));
    }
}
