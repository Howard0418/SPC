using System.Globalization;
using System.Net;
using MesSpc.Api.Services.Localization;

namespace MesSpc.Api.Services.Calibration;

public sealed record CalibrationNotice(string Subject,string HtmlBody,string Text);

/// <summary>Pure notification text; never reads request state, notification eligibility or transport configuration.</summary>
public static class CalibrationNoticeContent
{
    private static string T(string key,string? locale,params (string Key,string Value)[] args) =>
        ApiUiMessageCatalog.Shared.Format("spc.api.calibrationMail"+key,locale,args.ToDictionary(x=>x.Key,x=>x.Value));
    private static string Date(DateOnly due) => due.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    private static string Html(IEnumerable<string> paragraphs) => string.Concat(paragraphs.Select(text=>"<p>"+WebUtility.HtmlEncode(text)+"</p>"));

    public static string TestRecipient(string? locale) => T("Recipient",locale);

    /// <summary>Background callers omit locale and therefore always use Traditional Chinese.</summary>
    public static CalibrationNotice Scheduled(string code,string name,DateOnly due,string stage,string? locale = null)
    {
        var subject=T("Subject",locale,("code",code),("name",name));
        var paragraphs=new[]{T("InstrumentLine",locale,("code",code),("name",name)),T("DueLine",locale,("date",Date(due))),T("StageLine",locale,("stage",stage)),T("Open",locale)};
        return new(subject,Html(paragraphs),string.Join('\n',new[]{subject}.Concat(paragraphs)));
    }

    /// <summary>Localizes only generated sample display names; preserves the original outbox subject.</summary>
    public static LocalizedMailContent TestEmail(string code,string name,DateOnly due,string stage,string? locale = null,bool isFallback = false)
    {
        var displayName=isFallback?T("SampleNameEmail",locale):name;
        var paragraphs=new[]{T("TestIntro",locale),T("InstrumentLine",locale,("code",code),("name",displayName)),T("DueLine",locale,("date",Date(due))),T("StageLine",locale,("stage",stage)),T("Open",locale)};
        return new(T("TestSubject",locale,("code",code),("name",displayName)),Html(paragraphs),T("TestSubject","zh-TW",("code",code),("name",name)));
    }

    /// <summary>Returns plain Chat text; URL, token and payload transport remain the caller's responsibility.</summary>
    public static string TestChat(string code,string name,DateOnly due,string? locale = null,bool isFallback = false) =>
        string.Join('\n',T("TestChatHeading",locale),T("InstrumentLine",locale,("code",code),("name",isFallback?T("SampleNameChat",locale):name)),T("DueLine",locale,("date",Date(due))),T("ChatTestIntro",locale));
}
