using System.Globalization;
using System.Net;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Services.Localization;

namespace MesSpc.Api.Services;

public sealed record LocalizedMailContent(string Subject, string HtmlBody, string LegacyFileSubject);

/// <summary>Pure mail presentation. Locale never changes parsing, recipients or delivery decisions.</summary>
public static class SpcMailContent
{
    private static string Text(string key, string? locale, params (string Name,string Value)[] args) =>
        ApiUiMessageCatalog.Shared.Format("spc.api."+key,locale,args.ToDictionary(x=>x.Name,x=>x.Value));

    public static string SenderName(string? locale) => Text("mailSender",locale);
    public static string TestRecipient(string? locale) => Text("mailTestRecipient",locale);

    /// <summary>Builds an alert without translating its original diagnostic, identifiers or enum.</summary>
    public static LocalizedMailContent Alert(AlertEvent alert, string recipientName, string? locale = null)
    {
        string H(string key, params (string Name,string Value)[] args) => WebUtility.HtmlEncode(Text(key,locale,args));
        var subjectArgs = new[] { ("type",alert.AlertType.ToString()),("part",alert.PartId.ToString(CultureInfo.InvariantCulture)),("process",alert.ProcessId.ToString(CultureInfo.InvariantCulture)) };
        var subject = Text("mailAlertSubject",locale,subjectArgs);
        var color = alert.AlertType==AlertType.OutOfSpec?"#dc2626":"#ea580c";
        var time = alert.OccurredAt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);
        string Row(string key,string value) => $"<tr><td style='padding:12px;border:1px solid #e2e8f0;color:#64748b;font-weight:bold;'>{H(key)}</td><td style='padding:12px;border:1px solid #e2e8f0;font-weight:bold;'>{WebUtility.HtmlEncode(value)}</td></tr>";
        var body = $@"<div style='font-family:Arial,sans-serif;border:1px solid #e2e8f0;padding:25px;border-radius:12px;max-width:650px;background:#fff;color:#1e293b;'>
<div style='border-bottom:2px solid {color};padding-bottom:15px;margin-bottom:20px;'><h2 style='color:{color};font-size:22px;'>🚨 {H("mailAlertHeading")}</h2><span style='background:{color};color:white;padding:4px 10px;border-radius:20px;'>{H(alert.AlertType==AlertType.OutOfSpec?"mailOos":"mailOoc")}</span></div>
<p>{H("mailGreeting",("name",recipientName))}</p><p style='color:#475569;'>{H("mailAlertIntro",("time",time))}</p>
<table style='width:100%;border-collapse:collapse;margin:20px 0;font-size:14px;'>
{Row("mailEvent","#ALT-"+alert.Id.ToString("D5",CultureInfo.InvariantCulture))}
{Row("mailType",alert.AlertType.ToString())}
{Row("mailPartProcess",$"Part ID: {alert.PartId.ToString(CultureInfo.InvariantCulture)} / Process ID: {alert.ProcessId.ToString(CultureInfo.InvariantCulture)}")}
{Row("mailMeasured",alert.ActualValue?.ToString("F4",CultureInfo.InvariantCulture) ?? "")}
{Row("mailReason",alert.Message)}
</table><div style='background:#fef2f2;border-left:4px solid #ef4444;padding:15px;margin:20px 0;'><strong>{H("mailActionLabel")}</strong> {H("mailAction")}</div>
<div style='margin-top:30px;padding-top:15px;border-top:1px solid #e2e8f0;font-size:12px;color:#94a3b8;text-align:center;'><p>{H("mailFooter")}</p><p>{H("mailNoReply")}</p></div></div>";
        return new(subject,body,Text("mailAlertSubject","zh-TW",subjectArgs));
    }

    /// <summary>Builds a test message from explicit SMTP display values and an injected UTC instant.</summary>
    public static LocalizedMailContent SmtpTest(string host,int port,DateTime utcNow,string? locale = null)
    {
        string H(string key,params (string Name,string Value)[] args) => WebUtility.HtmlEncode(Text(key,locale,args));
        var serverArgs = new[]{("host",host),("port",port.ToString(CultureInfo.InvariantCulture))};
        var time = utcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);
        var body = $@"<div style='font-family:Arial,sans-serif;border:1px solid #cbd5e1;padding:25px;border-radius:12px;max-width:650px;background:#fff;color:#1e293b;'>
<div style='border-bottom:2px solid #3b82f6;padding-bottom:15px;margin-bottom:20px;'><h2 style='color:#2563eb;font-size:22px;'>📢 {H("mailTestHeading")}</h2></div>
<p>{H("mailTestGreeting")}</p><p style='color:#475569;'>{H("mailTestIntro")}</p>
<div style='background:#f8fafc;border-left:4px solid #10b981;padding:15px;margin:20px 0;'><p style='font-weight:bold;color:#065f46;'>{H("mailTestTime",("time",time))}</p><p style='font-size:13px;color:#64748b;'>{H("mailServer",serverArgs)}</p></div>
<p style='font-size:14px;color:#64748b;'>{H("mailTestFuture")}</p></div>";
        return new(Text("mailTestSubject",locale,serverArgs),body,Text("mailTestSubject","zh-TW",serverArgs));
    }
}
