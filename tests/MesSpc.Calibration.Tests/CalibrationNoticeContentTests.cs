using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using MesSpc.Api.Services.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesSpc.Calibration.Tests;

public class CalibrationNoticeContentTests
{
    private const string Raw="原名 <b>@:key {name}</b>";
    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public void PureEmailAndChatPreserveIdentifiersDateAndStage(string locale)
    {
        var due=new DateOnly(2027,1,1);
        var content=CalibrationNoticeContent.Scheduled("I0",Raw,due,"BEFORE-0",locale);
        Assert.Contains("2027-01-01",content.HtmlBody);Assert.Contains("BEFORE-0",content.HtmlBody);
        Assert.Contains("&lt;b&gt;@:key {name}&lt;/b&gt;",content.HtmlBody);Assert.DoesNotContain("<b>",content.HtmlBody);
        Assert.Contains(Raw,content.Text);Assert.Contains("BEFORE-0",content.Text);
        Assert.DoesNotContain("token=",content.Text);Assert.Contains("I0",content.Subject);
        var test=CalibrationNoticeContent.TestEmail("I0",Raw,due,"TEST",locale);
        Assert.Contains(ApiUiMessageCatalog.Shared.Format("spc.api.calibrationMailTestIntro",locale),WebUtility.HtmlDecode(test.HtmlBody));
        Assert.Equal($"【測試】儀器校正提醒 I0 {Raw}",test.LegacyFileSubject);
        Assert.Contains(Raw,CalibrationNoticeContent.TestChat("I0",Raw,due,locale));
    }

    [Fact]
    public void BackgroundDefaultsToTraditionalChineseWithoutReadingCulture()
    {
        var culture=CultureInfo.CurrentCulture;var ui=CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo("en-US");
            var expected=CalibrationNoticeContent.Scheduled("I0",Raw,new(2027,1,1),"OVERDUE-1","zh-TW");
            Assert.Equal(expected,CalibrationNoticeContent.Scheduled("I0",Raw,new(2027,1,1),"OVERDUE-1"));
            Assert.Equal(expected,CalibrationNoticeContent.Scheduled("I0",Raw,new(2027,1,1),"OVERDUE-1","en"));
            Assert.Equal("fr-FR",CultureInfo.CurrentCulture.Name);Assert.Equal("en-US",CultureInfo.CurrentUICulture.Name);
        } finally {CultureInfo.CurrentCulture=culture;CultureInfo.CurrentUICulture=ui;}
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public async Task ManualEmailUsesRequestLanguageAndLeavesResponseAndNotificationRowsUnchanged(string locale)
    {
        using var factory=new CalibrationTestEmailFactory();using var client=factory.Client();
        client.DefaultRequestHeaders.Add(ApiUiMessageCatalog.LocaleHeader,locale);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de-DE");
        var response=await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-email",new {email="qa@example.invalid"});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var json=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("測試量規",json.GetProperty("data").GetProperty("instrumentName").GetString());
        Assert.Equal("I-TEST",json.GetProperty("data").GetProperty("instrumentCode").GetString());
        Assert.Contains(ApiUiMessageCatalog.Shared.Format("spc.api.calibrationMailTestIntro",locale),WebUtility.HtmlDecode(Assert.Single(factory.Mail.Sent).Body));
        Assert.Equal(locale,Assert.Single(factory.Mail.Locales));
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0,await db.Set<CalibrationNotification>().CountAsync());
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")][InlineData("en")]
    public async Task ManualChatUsesCanonicalLocaleAndNeverQueuesOrIncludesWebhook(string locale)
    {
        using var factory=new CalibrationTestEmailFactory();using var client=factory.Client();
        client.DefaultRequestHeaders.Add(ApiUiMessageCatalog.LocaleHeader,locale);
        var response=await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-chat",new {chatWebhookUrl=CalibrationChatTests.Url});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var text=Assert.Single(factory.Chat.Sent);
        Assert.Contains(ApiUiMessageCatalog.Shared.Format("spc.api.calibrationMailTestChatHeading",locale),text);
        Assert.Contains("I-TEST",text);Assert.Contains("測試量規",text);Assert.Contains("2026-09-20",text);
        Assert.DoesNotContain("token=",text);Assert.DoesNotContain("secret-test-only",await response.Content.ReadAsStringAsync());
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0,await db.Set<CalibrationNotification>().CountAsync());Assert.Empty(factory.Mail.Sent);
    }

    [Fact]
    public async Task GeneratedSampleNameIsDisplayOnlyAndLegacyDataAndFileSubjectStayUnchanged()
    {
        using var factory=new CalibrationTestEmailFactory();using var client=factory.Client();
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.RemoveRange(await db.Set<CalibrationInstrument>().ToListAsync());await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Add(ApiUiMessageCatalog.LocaleHeader,"en-US");
        var response=await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-email",new {email="qa@example.invalid"});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var json=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("測試樣本（非正式儀器）",json.GetProperty("data").GetProperty("instrumentName").GetString());
        Assert.Equal("TEST",json.GetProperty("data").GetProperty("stage").GetString());
        Assert.Contains("Test sample (not a registered instrument)",WebUtility.HtmlDecode(Assert.Single(factory.Mail.Sent).Body));
        Assert.Equal("【測試】儀器校正提醒 TEST-CAL 測試樣本（非正式儀器）",Assert.Single(factory.Mail.FileSubjects));
    }

    [Fact]
    public async Task ManualFailureAndUnknownKeepOriginalResultAndNeverQueue()
    {
        using var factory=new CalibrationTestEmailFactory();using var client=factory.Client();
        client.DefaultRequestHeaders.Add(ApiUiMessageCatalog.LocaleHeader,"en-US");factory.Mail.Result=false;factory.Chat.State="Unknown";
        var email=await (await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-email",new {email="qa@example.invalid"})).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(email.GetProperty("success").GetBoolean());Assert.False(email.GetProperty("data").GetProperty("smtpSent").GetBoolean());Assert.Single(factory.Mail.Sent);
        var chat=await (await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-chat",new {chatWebhookUrl=CalibrationChatTests.Url})).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(chat.GetProperty("success").GetBoolean());Assert.Equal("Unknown",chat.GetProperty("data").GetProperty("state").GetString());Assert.Equal("spc.api.chatTestUnknown",chat.GetProperty("messageKey").GetString());Assert.Single(factory.Chat.Sent);
        using var scope=factory.Services.CreateScope();Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<CalibrationNotification>().ToListAsync());
    }
}
