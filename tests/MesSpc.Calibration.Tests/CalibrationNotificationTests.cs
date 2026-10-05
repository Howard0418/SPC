using Microsoft.Extensions.Configuration;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Calibration.Tests;

public class CalibrationNotificationTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AppDbContext db;
    private readonly FakeClock clock = new();
    private readonly FakeMail mail = new();
    private readonly CalibrationNotificationProcessor processor;
    public CalibrationNotificationTests()
    {
        connection.Open(); db=new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); db.Database.EnsureCreated();
        db.Operators.Add(new Operator { Id=1,OperatorCode="QA",OperatorName="QA",IsActive=true,Email="qa@example.invalid" });
        db.Add(new CalibrationInstrument { Id=1,Code="I1",Name="量規",Department="QA",CustodianOperatorId=1,NextCalibrationDate=new(2026,9,18),RecipientOperatorIdsJson="[1]" });
        db.Add(new CalibrationNotificationSetting { IsEnabled=true }); db.SaveChanges();
        processor=new(db,clock,mail,NullLogger<CalibrationNotificationProcessor>.Instance);
    }
    [Fact]
    public async Task RepeatedScanSendsCurrentStageOnceAndSkipsMissedStage()
    {
        await processor.ProcessAsync(default); await processor.ProcessAsync(default);
        Assert.Equal(1,mail.Count);
        Assert.Single(await db.Set<CalibrationNotification>().Where(x=>x.State=="Sent").ToListAsync());
        Assert.Contains(await db.Set<CalibrationNotification>().ToListAsync(),x=>x.Stage=="BEFORE-30" && x.State=="Skipped");
    }
    [Fact]
    public async Task DefiniteFailureRetriesOnlyWhenDue()
    {
        mail.State="Failed"; await processor.ProcessAsync(default); await processor.ProcessAsync(default);
        Assert.Equal(1,mail.Count);
        clock.Now=clock.Now.AddMinutes(15); mail.State="Sent"; await processor.ProcessAsync(default);
        Assert.Equal(2,mail.Count);
        Assert.Equal(2,await db.Set<CalibrationNotificationAttempt>().CountAsync());
    }
    [Fact]
    public async Task UnknownNeverAutomaticallyRetries()
    {
        mail.State="Unknown"; await processor.ProcessAsync(default); clock.Now=clock.Now.AddHours(5); await processor.ProcessAsync(default);
        Assert.Equal(1,mail.Count);
    }
    [Fact]
    public async Task DisabledAndRetiredNeverSend()
    {
        (await db.Set<CalibrationInstrument>().SingleAsync()).UsageStatus="Retired"; await db.SaveChangesAsync();
        await processor.ProcessAsync(default); Assert.Equal(0,mail.Count);
    }
    [Fact]
    public async Task MissingRecipientIsVisibleIssue()
    {
        (await db.Operators.SingleAsync()).Email=null; await db.SaveChangesAsync(); await processor.ProcessAsync(default);
        Assert.Equal("NoValidRecipients",(await db.Set<CalibrationInstrument>().SingleAsync()).NotificationIssue);
        Assert.Equal(0,mail.Count);
    }
    [Fact]
    public async Task InCalibrationDoesNotSend()
    {
        (await db.Set<CalibrationInstrument>().SingleAsync()).UsageStatus="InCalibration"; await db.SaveChangesAsync();
        await processor.ProcessAsync(default); Assert.Equal(0,mail.Count);
        Assert.Empty(await db.Set<CalibrationNotification>().ToListAsync());
    }
    [Fact]
    public async Task InactiveNeverSends()
    {
        (await db.Set<CalibrationInstrument>().SingleAsync()).UsageStatus="Inactive"; await db.SaveChangesAsync();
        await processor.ProcessAsync(default); Assert.Equal(0,mail.Count);
    }
    [Fact]
    public async Task ExcludingCustodianWithNoRecipientsIsVisibleIssue()
    {
        var row=await db.Set<CalibrationInstrument>().SingleAsync();
        row.IncludeCustodian=false; row.RecipientOperatorIdsJson="[]"; await db.SaveChangesAsync();
        await processor.ProcessAsync(default);
        Assert.Equal("NoValidRecipients",(await db.Set<CalibrationInstrument>().SingleAsync()).NotificationIssue);
        Assert.Equal(0,mail.Count);
    }
    [Fact]
    public async Task AfterThreeRetriesDoesNotSendAgain()
    {
        mail.State="Failed";
        await processor.ProcessAsync(default);
        clock.Now=clock.Now.AddMinutes(15); await processor.ProcessAsync(default);
        clock.Now=clock.Now.AddHours(1); await processor.ProcessAsync(default);
        clock.Now=clock.Now.AddHours(4); await processor.ProcessAsync(default);
        Assert.Equal(4,mail.Count);
        clock.Now=clock.Now.AddHours(5); await processor.ProcessAsync(default);
        Assert.Equal(4,mail.Count);
        var n=await db.Set<CalibrationNotification>().SingleAsync(x=>x.State=="Failed");
        Assert.Null(n.NextAttemptAt);
        Assert.Equal(4,n.AttemptCount);
    }
    [Fact]
    public async Task BeforeTaipeiEightDoesNotEnqueue()
    {
        clock.Now=new DateTimeOffset(2026,9,10,23,0,0,TimeSpan.Zero);
        await processor.ProcessAsync(default);
        Assert.Equal(0,mail.Count);
        Assert.Empty(await db.Set<CalibrationNotification>().ToListAsync());
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChatSendsOnceWithoutEmailAndHonorsDeliverySwitch(bool enabled)
    {
        var secrets = new CalibrationChatSecrets(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        var setting = await db.Set<CalibrationNotificationSetting>().SingleAsync();
        setting.NotificationChannel = "SynologyChat";
        setting.ChatWebhookProtected = secrets.Protect(CalibrationChatTests.Url);
        (await db.Operators.SingleAsync()).Email = null;
        await db.SaveChangesAsync();
        var chat = new FakeChat();
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["Calibration:DeliveryEnabled"] = enabled.ToString() }).Build();
        var p = new CalibrationNotificationProcessor(db, clock, mail, NullLogger<CalibrationNotificationProcessor>.Instance, chat, secrets, config);
        await p.ProcessAsync(default); await p.ProcessAsync(default);
        Assert.Equal(enabled ? 1 : 0, chat.Count); Assert.Equal(0, mail.Count);
        Assert.Null((await db.Set<CalibrationInstrument>().SingleAsync()).NotificationIssue);
        var sent = await db.Set<CalibrationNotification>().SingleAsync(x => x.State == (enabled ? "Sent" : "Failed"));
        Assert.Equal("SynologyChat", sent.Channel);
        Assert.DoesNotContain("secret-test-only", sent.RecipientKey);
    }
    [Fact]
    public async Task SwitchingChannelCancelsOldFailedEmailWork()
    {
        mail.State = "Failed"; await processor.ProcessAsync(default);
        var settings = await db.Set<CalibrationNotificationSetting>().SingleAsync();
        settings.NotificationChannel = "SynologyChat";
        await db.SaveChangesAsync();
        clock.Now = clock.Now.AddMinutes(15);
        await processor.ProcessAsync(default);
        Assert.Equal(1, mail.Count);
        Assert.Contains(await db.Set<CalibrationNotification>().ToListAsync(), x => x.State == "Cancelled");
    }
    private sealed class FakeChat : ICalibrationChatSender
    {
        public int Count;
        public Task<CalibrationMailResult> SendAsync(string url, string text, CancellationToken ct)
        { Count++; return Task.FromResult(new CalibrationMailResult("Sent", null)); }
    }
    public void Dispose(){db.Dispose();connection.Dispose();}
    private sealed class FakeClock : TimeProvider
    { public DateTimeOffset Now = new(2026,9,11,0,0,0,TimeSpan.Zero); public override DateTimeOffset GetUtcNow()=>Now; }
    private sealed class FakeMail : ICalibrationMailSender
    {
        public int Count; public string State="Sent";
        public Task<CalibrationMailResult> SendAsync(string email,string subject,string body,CancellationToken ct)
        { Count++; return Task.FromResult(new CalibrationMailResult(State,State=="Sent"?null:"TestFailure")); }
    }
}
