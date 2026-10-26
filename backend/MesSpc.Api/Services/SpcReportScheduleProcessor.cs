using MesSpc.Api.Services.Localization;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record SpcReportScheduleProcessResult(bool WasDue, int RecipientCount, int SuccessfulSendCount, string? Period);

public class SpcReportScheduleProcessor(
    AppDbContext db,
    ISpcOverviewReportBuilder reportBuilder,
    IEmailNotificationService email,
    ILogger<SpcReportScheduleProcessor> logger)
{
    public async Task<SpcReportScheduleProcessResult> ProcessAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var schedule = await db.SpcReportSchedules.FirstOrDefaultAsync(x => x.IsEnabled, ct);
        if (schedule is null) return new(false, 0, 0, null);

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        if (now.Hour != schedule.SendTime.Hours || now.Minute != schedule.SendTime.Minutes)
            return new(false, 0, 0, null);

        var weeklyDue = schedule.WeeklyEnabled && (int)now.DayOfWeek == schedule.WeeklyDayOfWeek &&
                        schedule.LastWeeklySentAt?.Date != utcNow.Date;
        var monthlyDue = schedule.MonthlyEnabled && now.Day == schedule.MonthlyDay &&
                         schedule.LastMonthlySentAt?.Date != utcNow.Date;
        if (!weeklyDue && !monthlyDue) return new(false, 0, 0, null);

        var ids = JsonSerializer.Deserialize<List<int>>(schedule.RecipientOperatorIdsJson) ?? [];
        var recipients = await db.Operators
            .Where(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "")
            .ToListAsync(ct);
        if (recipients.Count == 0) return new(true, 0, 0, weeklyDue ? "週報" : "月報");

        var startLocal = weeklyDue ? now.Date.AddDays(-7) : new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var endLocal = weeklyDue ? now.Date : new DateTime(now.Year, now.Month, 1);
        var start = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var end = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);
        var bytes = await reportBuilder.BuildExcelAsync(start, end, ct);

        var period = weeklyDue ? "週報" : "月報";
        var successfulSendCount = 0;
        foreach (var recipient in recipients)
        {
            var content = SpcReportPresentation.ScheduledMail(recipient.OperatorName, weeklyDue, now);
            var sent = await email.SendReportEmailAsync(
                recipient.Email!,
                recipient.OperatorName,
                content.Subject,
                content.HtmlBody,
                bytes,
                $"SPC_Item_Overview_{period}_{now:yyyyMMdd}.xlsx");

            if (sent) successfulSendCount++;
        }

        if (successfulSendCount == 0)
        {
            logger.LogWarning("SPC 報表排程{Period}寄送全部失敗，保留 LastSent 以便下次排程重試。", period);
            return new(true, recipients.Count, 0, period);
        }

        if (weeklyDue) schedule.LastWeeklySentAt = utcNow;
        if (monthlyDue) schedule.LastMonthlySentAt = utcNow;
        await db.SaveChangesAsync(ct);
        return new(true, recipients.Count, successfulSendCount, period);
    }
}
