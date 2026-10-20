using System.Globalization;
namespace MesSpc.Api.Services;
/// <summary>Version history uses inclusive Taipei calendar days and an exclusive UTC upper bound.</summary>
public sealed record VersionHistoryDateRange(DateTime? FromUtc, DateTime? UntilUtc)
{
    public static VersionHistoryDateRange Parse(string? fromDate,string? toDate)
    {
        static DateOnly? Read(string? text)
        {
            if(string.IsNullOrWhiteSpace(text))return null;
            if(!DateOnly.TryParseExact(text,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))
                throw new ArgumentException("請使用YYYY-MM-DD有效日期，例如2026-10-19。");
            return date;
        }
        var from=Read(fromDate);var to=Read(toDate);
        if(from>to)throw new ArgumentException("起日不可晚於迄日。");
        static DateTime Utc(DateOnly day) => DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue).AddHours(-8),DateTimeKind.Utc);
        try {return new(from.HasValue?Utc(from.Value):null,to.HasValue?Utc(to.Value).AddDays(1):null);}
        catch(ArgumentOutOfRangeException){throw new ArgumentException("日期超出可查詢範圍，請使用有效日期。");}
    }
}
