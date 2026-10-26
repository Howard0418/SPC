using System.Globalization;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Localization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesSpc.Api.Tests;

public class NotificationContentTests
{
    [Fact]
    public void MissingMeasurementStaysBlankRatherThanBecomingZero()
    {
        var content=SpcMailContent.Alert(new AlertEvent { ActualValue=null },"raw","en-US");
        Assert.DoesNotContain("0.0000",content.HtmlBody);
        Assert.Contains("Measured value",content.HtmlBody);
    }
    [Theory]
    [InlineData("zh-TW", "工業品質即時警報通報")]
    [InlineData("zh-CN", "工业质量实时警报通知")]
    [InlineData("en-US", "Real-time quality alert")]
    public void AlertContentPreservesDataAndEscapesHtml(string locale, string heading)
    {
        var alert = new AlertEvent { Id = 7, PartId = 0, ProcessId = 0, ActualValue = 0,
            AlertType = AlertType.OutOfSpec, Message = "原始 <b>@:key {field}</b>",
            OccurredAt = new DateTime(2026,10,26,1,2,3,DateTimeKind.Utc) };
        var content = SpcMailContent.Alert(alert, "名字 <script>alert(1)</script>", locale);
        Assert.Contains(heading, content.HtmlBody);
        Assert.Contains("0.0000", content.HtmlBody);
        Assert.Contains("2026-10-26 09:02:03", content.HtmlBody);
        Assert.Contains("#ALT-00007", content.HtmlBody);
        Assert.Contains("OutOfSpec", content.Subject);
        Assert.Contains("&lt;b&gt;@:key {field}&lt;/b&gt;", content.HtmlBody);
        Assert.Contains("&lt;script&gt;", content.HtmlBody);
        Assert.DoesNotContain("<script>", content.HtmlBody);
        Assert.Equal("【品質異常警報 OutOfSpec】料號/站別 0-0 數值異常", content.LegacyFileSubject);
        Assert.Equal("原始 <b>@:key {field}</b>", alert.Message);
        Assert.Equal(0, alert.ActualValue);
    }

    [Fact]
    public async Task ConcurrentLocalesDoNotChangeCultureOrBackgroundDefault()
    {
        await Task.WhenAll(new[]{"zh-TW","zh-CN","en-US"}.Select(async locale => {
            var original = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                await Task.Yield();
                var alert = new AlertEvent { ActualValue = -1.25, AlertType = AlertType.OutOfControl };
                var content = SpcMailContent.Alert(alert,"raw",locale);
                Assert.Contains("-1.2500",content.HtmlBody);
                Assert.Contains("OOC",content.HtmlBody);
                Assert.Equal("fr-FR",CultureInfo.CurrentCulture.Name);
                var fallback = SpcMailContent.Alert(alert,"raw","en");
                Assert.Equal(SpcMailContent.Alert(alert,"raw",null),fallback);
            } finally { CultureInfo.CurrentCulture = original; }
        }));
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public async Task ManualEndpointsPassCanonicalLocaleWithoutChangingRecipientsOrOverrides(string locale)
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sender = new CapturingSender();
        var config = new ConfigurationBuilder().Build();
        var settings = new SettingsController(config,Mock.Of<IWebHostEnvironment>(),sender,NullLogger<SettingsController>.Instance,db)
            { ControllerContext = Context(locale) };
        var request = new TestSmtpReq { RecipientEmail="fixture@example.invalid", Settings=new SmtpSettingsDto { Host="smtp.invalid", Port=2525, EnableSsl=true, SaveToLocalDisk=false } };
        await settings.TestSmtp(request);
        await settings.TestAlertSmtp(request);
        var alerts = new AlertsController(db,sender,config) { ControllerContext = Context(locale) };
        await alerts.TestEmail("fixture@example.invalid");
        await alerts.SimulateAlert(new SimulateAlertReq(101,201,1.25,"OOS","原始原因","fixture@example.invalid"));
        Assert.Equal(4,sender.Calls.Count);
        Assert.All(sender.Calls,call => { Assert.Equal(locale,call.Locale);Assert.Equal("fixture@example.invalid",call.Email); });
        Assert.All(sender.Calls.Take(2),call => { Assert.Equal("smtp.invalid",call.Settings!.Host);Assert.Equal(2525,call.Settings.Port);Assert.True(call.Settings.EnableSsl);Assert.False(call.Settings.SaveToLocalDisk); });
        Assert.All(sender.Calls.Skip(2),call => Assert.Null(call.Settings));
        Assert.Equal("原始原因",db.AlertEvents.Single().Message);
    }

    [Fact]
    public void TestContentUsesTaipeiTimeAndKeepsOriginalFileSubjectAcrossLanguages()
    {
        var time = new DateTime(2026,10,26,1,2,3,DateTimeKind.Utc);
        var original = SpcMailContent.SmtpTest("host <b>@:key</b>",0,time,null);
        foreach(var locale in new[]{"zh-TW","zh-CN","en-US"}) {
            var content = SpcMailContent.SmtpTest("host <b>@:key</b>",0,time,locale);
            Assert.Contains("2026-10-26 09:02:03",content.HtmlBody);
            Assert.Contains("host &lt;b&gt;@:key&lt;/b&gt;:0",content.HtmlBody);
            Assert.DoesNotContain("<b>",content.HtmlBody);
            Assert.Equal(original.LegacyFileSubject,content.LegacyFileSubject);
        }
    }

    [Fact]
    public async Task FailedDeliveryRetainsOriginalResultAndAddsOnlySafeDisplayMetadata()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sender = new CapturingSender { Result=false };
        var controller = new SettingsController(new ConfigurationBuilder().Build(),Mock.Of<IWebHostEnvironment>(),sender,NullLogger<SettingsController>.Instance,db)
            {ControllerContext=Context("invalid-locale")};
        var result = Assert.IsType<OkObjectResult>(await controller.TestSmtp(new TestSmtpReq { RecipientEmail="fixture@example.invalid" }));
        var json = System.Text.Json.JsonSerializer.SerializeToElement(result.Value,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.False(json.GetProperty("success").GetBoolean());
        var recipient = json.GetProperty("recipients")[0];
        Assert.Equal("寄信服務回傳失敗。",recipient.GetProperty("errorMessage").GetString());
        Assert.Equal("spc.api.mailSmtpFailed",recipient.GetProperty("displayMessage").GetProperty("messageKey").GetString());
        Assert.Equal("zh-TW",Assert.Single(sender.Calls).Locale);
    }

    private static ControllerContext Context(string locale) {
        var http = new DefaultHttpContext();http.Request.Headers[ApiUiMessageCatalog.LocaleHeader]=locale;
        http.Request.Headers.AcceptLanguage="de-DE";return new ControllerContext { HttpContext=http };
    }

    private sealed class CapturingSender : IEmailNotificationService
    {
        public bool Result {get;init;}=true;
        public List<(string Email,string? Locale,SmtpSettingsOverride? Settings)> Calls {get;}=[];
        public Task<bool> SendAlertEmailAsync(AlertEvent alert,string recipientEmail,string recipientName,SmtpSettingsOverride? overrideSettings=null,string? uiLocale=null) {Calls.Add((recipientEmail,uiLocale,overrideSettings));return Task.FromResult(Result);}
        public Task<bool> SendTestEmailAsync(string recipientEmail,SmtpSettingsOverride? overrideSettings=null,string? uiLocale=null) {Calls.Add((recipientEmail,uiLocale,overrideSettings));return Task.FromResult(Result);}
        public Task<bool> SendHtmlEmailAsync(string recipientEmail,string recipientName,string subject,string htmlBody,SmtpSettingsOverride? overrideSettings=null, string? uiLocale = null, string? fileSubject = null)=>throw new InvalidOperationException("No real delivery");
        public Task<bool> SendReportEmailAsync(string recipientEmail,string recipientName,string subject,string htmlBody,byte[] excelBytes,string fileName, string? uiLocale = null)=>throw new InvalidOperationException("No real delivery");
    }
}
