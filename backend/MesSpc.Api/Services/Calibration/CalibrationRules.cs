using System.Text.Json;
using MesSpc.Api.Services.Security;

namespace MesSpc.Api.Services.Calibration;

/// <summary>校正日期、補查階段與明確管理權限；不依伺服器本地時區。</summary>
public static class CalibrationRules
{
    public static DateOnly Today(DateTimeOffset utc) => DateOnly.FromDateTime(utc.ToOffset(TimeSpan.FromHours(8)).DateTime);
    public static DateOnly NextDue(DateOnly date, int months)
    {
        if (months is < 1 or > 120) throw new ArgumentException("校正週期必須為 1～120 個月。");
        return date.AddMonths(months);
    }
    public static bool CanNotify(string status) => status == "Active";
    public static bool CountsInPortalSummary(string status) => status is "Active" or "InCalibration";
    public static int UpcomingWindowDays(int[] days) => days.Length == 0 ? 30 : days.Max();
    public static bool IsOverdue(DateOnly due, DateOnly today) => due < today;
    public static bool IsUpcoming(DateOnly due, DateOnly today, int windowDays)
        => due >= today && due <= today.AddDays(windowDays);
    public static bool ShouldScan(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(8)).Hour >= 8;
    public readonly record struct CalibrationSummaryCounts(int Upcoming, int Overdue, int DueToday, int InCalibration);
    public static CalibrationSummaryCounts CountSummary(IEnumerable<(string Status, DateOnly Due)> rows, DateOnly today, int windowDays)
    {
        var listed = rows.Where(x => CountsInPortalSummary(x.Status)).ToArray();
        return new(
            listed.Count(x => IsUpcoming(x.Due, today, windowDays)),
            listed.Count(x => IsOverdue(x.Due, today)),
            listed.Count(x => x.Due == today),
            listed.Count(x => x.Status == "InCalibration"));
    }
    public static bool CanManage(string? role, string? json)
    {
        if (UserRoles.Normalize(role) != UserRoles.Editor) return false;
        if (string.IsNullOrWhiteSpace(json))
            return SpcPagePermissions.Catalog.Any(x => x.Code == "calibration.manage");
        try
        {
            var codes = JsonSerializer.Deserialize<string[]>(json);
            return codes?.Contains("calibration.manage") == true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    public static int[] ValidateReminderDays(int[] days)
    {
        if (days.Length == 0 || days.Length > 12 || days.Any(x => x < 0 || x > 365) || days.Distinct().Count() != days.Length)
            throw new ArgumentException("提醒天數需為 0～365、不重複，最多 12 個。");
        return days.OrderDescending().ToArray();
    }
    public static string? Stage(DateOnly due, DateOnly today, int[] days)
    {
        var remaining = due.DayNumber - today.DayNumber;
        if (remaining < 0) return $"OVERDUE-{1 + ((-remaining - 1) / 7) * 7}";
        var crossed = days.Where(d => d >= remaining).Order().ToArray();
        return crossed.Length == 0 ? null : $"BEFORE-{crossed[0]}";
    }
    public static TimeSpan? RetryDelay(int attempts) => attempts switch
    { 1 => TimeSpan.FromMinutes(15), 2 => TimeSpan.FromHours(1), 3 => TimeSpan.FromHours(4), _ => null };
}
