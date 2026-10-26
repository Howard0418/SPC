using System.Net;
using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.Services.Localization;

/// <summary>Human report presentation only. Never changes data, parsing culture or scheduling.</summary>
public static class SpcReportPresentation
{
    public static string Text(string key, string? locale = null, IReadOnlyDictionary<string, string>? args = null)
        => ApiUiMessageCatalog.Shared.Format("spc.api." + key, locale, args);

    public static string Display(UiDisplayMessage? message, string original, string? locale = null)
    {
        if (message is null) return original;
        try { return ApiUiMessageCatalog.Shared.Format(message.MessageKey, locale, message.Args); }
        catch (KeyNotFoundException) { return original; }
        catch (ArgumentException) { return original; }
    }

    public static UiDisplayMessage ChartKind(string? groupType) => new(
        groupType?.Trim().ToUpperInvariant() == "TREND_CHART" ? "spc.api.reportTrendChart" : "spc.api.reportControlChart");

    public static UiDisplayMessage CalculationMethod(string? method, string? chartType, bool manual) => new("spc.api." +
        (manual ? "reportManualLimits" : method?.Trim().ToUpperInvariant() switch
        {
            "MR_METHOD" or "MOVING_RANGE_OF_XBAR" => "reportMovingRange",
            "SIGMA_METHOD" or "SAMPLE_STD_DEV" => "reportSampleStdDev",
            _ when string.Equals(chartType, "I_MR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(chartType, "I-MR", StringComparison.OrdinalIgnoreCase) => "reportMovingRange",
            _ when string.Equals(chartType, "XBAR_R", StringComparison.OrdinalIgnoreCase)
                || string.Equals(chartType, "XBAR-R", StringComparison.OrdinalIgnoreCase) => "reportStandardRange",
            _ => "reportStandardFormula"
        }));

    public static LocalizedMailContent ManualMail(string name, bool weekly, DateTime nowLocal, string range, string? locale = null)
    {
        var period = Text(weekly ? "reportWeekly" : "reportMonthly", locale);
        var date = nowLocal.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var subject = Text("reportMailSubjectTest", locale, new Dictionary<string,string> { ["period"] = period, ["date"] = date });
        var body = Text("reportMailBodyTest", locale, new Dictionary<string,string> { ["name"] = name, ["period"] = period });
        var line = Text("reportMailPeriodLine", locale, new Dictionary<string,string> { ["range"] = range });
        var legacyPeriod = weekly ? "週報" : "月報";
        return new(subject, $"<p>{WebUtility.HtmlEncode(body)}</p><p>{WebUtility.HtmlEncode(line)}</p>",
            $"【SPC 項目管制總覽{legacyPeriod}測試寄送】{date}");
    }

    public static LocalizedMailContent ScheduledMail(string name, bool weekly, DateTime nowLocal, string? locale = null)
    {
        var period = Text(weekly ? "reportWeekly" : "reportMonthly", locale);
        var date = nowLocal.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var args = new Dictionary<string,string> { ["period"] = period, ["date"] = date };
        var subject = Text("reportMailSubjectScheduled", locale, args);
        args["name"] = name;
        var body = Text("reportMailBodyScheduled", locale, args);
        var legacyPeriod = weekly ? "週報" : "月報";
        return new(subject, $"<p>{WebUtility.HtmlEncode(body)}</p>", $"【SPC 項目管制總覽{legacyPeriod}】{date}");
    }
}
