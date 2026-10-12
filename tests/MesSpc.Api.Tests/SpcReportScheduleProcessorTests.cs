using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class SpcReportScheduleProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldNotUpdateWeeklySentAt_WhenAllRecipientsFail()
    {
        await using var db = CreateDbContext();
        db.SpcReportSchedules.Add(new SpcReportSchedule
        {
            WeeklyEnabled = true,
            WeeklyDayOfWeek = 1,
            SendTime = new TimeSpan(8, 0, 0),
            RecipientOperatorIdsJson = "[1,2]"
        });
        db.Operators.AddRange(
            new Operator { Id = 1, OperatorCode = "OP001", OperatorName = "A", Email = "a@example.test", IsActive = true },
            new Operator { Id = 2, OperatorCode = "OP002", OperatorName = "B", Email = "b@example.test", IsActive = true });
        await db.SaveChangesAsync();

        var email = new StubEmailService(_ => false);
        var processor = CreateProcessor(db, email);

        var result = await processor.ProcessAsync(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(result.WasDue);
        Assert.Equal(0, result.SuccessfulSendCount);
        Assert.Null((await db.SpcReportSchedules.SingleAsync()).LastWeeklySentAt);
    }

    [Fact]
    public async Task ProcessAsync_ShouldUpdateMonthlySentAt_WhenAtLeastOneRecipientSucceeds()
    {
        await using var db = CreateDbContext();
        db.SpcReportSchedules.Add(new SpcReportSchedule
        {
            MonthlyEnabled = true,
            MonthlyDay = 1,
            SendTime = new TimeSpan(8, 0, 0),
            RecipientOperatorIdsJson = "[1,2]"
        });
        db.Operators.AddRange(
            new Operator { Id = 1, OperatorCode = "OP001", OperatorName = "A", Email = "a@example.test", IsActive = true },
            new Operator { Id = 2, OperatorCode = "OP002", OperatorName = "B", Email = "b@example.test", IsActive = true });
        await db.SaveChangesAsync();

        var email = new StubEmailService(address => address == "a@example.test");
        var processor = CreateProcessor(db, email);
        var nowUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await processor.ProcessAsync(nowUtc);

        Assert.True(result.WasDue);
        Assert.Equal(1, result.SuccessfulSendCount);
        Assert.Equal(nowUtc.Date, (await db.SpcReportSchedules.SingleAsync()).LastMonthlySentAt?.Date);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSkip_WhenCurrentMinuteDoesNotMatchSchedule()
    {
        await using var db = CreateDbContext();
        db.SpcReportSchedules.Add(new SpcReportSchedule
        {
            WeeklyEnabled = true,
            WeeklyDayOfWeek = 1,
            SendTime = new TimeSpan(8, 30, 0),
            RecipientOperatorIdsJson = "[1]"
        });
        db.Operators.Add(new Operator { Id = 1, OperatorCode = "OP001", OperatorName = "A", Email = "a@example.test", IsActive = true });
        await db.SaveChangesAsync();

        var report = new StubReportBuilder();
        var email = new StubEmailService(_ => true);
        var processor = new SpcReportScheduleProcessor(db, report, email, NullLogger<SpcReportScheduleProcessor>.Instance);

        var result = await processor.ProcessAsync(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc));

        Assert.False(result.WasDue);
        Assert.Equal(0, email.Attempts);
        Assert.Equal(0, report.BuildCount);
        Assert.Null((await db.SpcReportSchedules.SingleAsync()).LastWeeklySentAt);
    }

    private static SpcReportScheduleProcessor CreateProcessor(AppDbContext db, StubEmailService email)
        => new(db, new StubReportBuilder(), email, NullLogger<SpcReportScheduleProcessor>.Instance);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"spc-report-schedule-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubReportBuilder : ISpcOverviewReportBuilder
    {
        public int BuildCount { get; private set; }

        public Task<byte[]> BuildExcelAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
        {
            BuildCount++;
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }
    }

    private sealed class StubEmailService(Func<string, bool> sendResult) : IEmailNotificationService
    {
        public int Attempts { get; private set; }

        public Task<bool> SendAlertEmailAsync(AlertEvent alert, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null)
            => Task.FromResult(true);

        public Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null)
            => Task.FromResult(true);

        public Task<bool> SendHtmlEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null)
            => Task.FromResult(true);

        public Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName)
        {
            Attempts++;
            return Task.FromResult(sendResult(recipientEmail));
        }
    }
}
