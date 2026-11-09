using MesSpc.Api.Services.Localization;
using System.Globalization;

namespace MesSpc.Api.Services;

public sealed record CpkReportMonthRange(string Label, DateTime FromUtc, DateTime UntilUtc)
{
    private static readonly TimeSpan TaipeiOffset = TimeSpan.FromHours(8);

    public static CpkReportMonthRange Parse(string? month, DateTime utcNow)
    {
        DateOnly firstDay;
        if (month is null)
        {
            var taipeiNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)).ToOffset(TaipeiOffset);
            firstDay = new DateOnly(taipeiNow.Year, taipeiNow.Month, 1);
        }
        else if (!DateOnly.TryParseExact(
                     $"{month}-01",
                     "yyyy-MM-dd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out firstDay)
                 || month.Length != 7)
        {
            throw ApiUiException.Attach(
                new ArgumentException("月份格式不正確，請使用 yyyy-MM，例如 2026-10。"),
                "spc.api.reportMonthInvalid");
        }

        try
        {
            var nextMonth = firstDay.AddMonths(1);
            var fromUtc = new DateTimeOffset(firstDay.ToDateTime(TimeOnly.MinValue), TaipeiOffset).UtcDateTime;
            var untilUtc = new DateTimeOffset(nextMonth.ToDateTime(TimeOnly.MinValue), TaipeiOffset).UtcDateTime;
            return new(firstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture), fromUtc, untilUtc);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw ApiUiException.Attach(
                new ArgumentException("月份超出可匯出範圍，請使用有效月份。"),
                "spc.api.reportMonthInvalid");
        }
    }
}
