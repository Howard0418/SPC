using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class SpcReportSchedulerService(IServiceProvider services, ILogger<SpcReportSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "SPC 報表排程執行失敗。"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schedule = await db.SpcReportSchedules.FirstOrDefaultAsync(x => x.IsEnabled, ct);
        if (schedule is null) return;
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        if (now.Hour != schedule.SendTime.Hours || now.Minute != schedule.SendTime.Minutes) return;
        var weeklyDue = schedule.WeeklyEnabled && (int)now.DayOfWeek == schedule.WeeklyDayOfWeek &&
                        schedule.LastWeeklySentAt?.Date != DateTime.UtcNow.Date;
        var monthlyDue = schedule.MonthlyEnabled && now.Day == schedule.MonthlyDay &&
                         schedule.LastMonthlySentAt?.Date != DateTime.UtcNow.Date;
        if (!weeklyDue && !monthlyDue) return;

        var ids = JsonSerializer.Deserialize<List<int>>(schedule.RecipientOperatorIdsJson) ?? [];
        var recipients = await db.Operators.Where(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "").ToListAsync(ct);
        if (recipients.Count == 0) return;
        var end = TimeZoneInfo.ConvertTimeToUtc(now, tz);
        var startLocal = weeklyDue ? now.Date.AddDays(-7) : new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var endLocal = weeklyDue ? now.Date : new DateTime(now.Year, now.Month, 1);
        var start = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        end = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);
        var bytes = await scope.ServiceProvider.GetRequiredService<SpcOverviewReportService>().BuildExcelAsync(start, end, ct);
        var email = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
        var period = weeklyDue ? "週報" : "月報";
        foreach (var recipient in recipients)
            await email.SendReportEmailAsync(recipient.Email!, recipient.OperatorName, $"【SPC 項目管制總覽{period}】{now:yyyy-MM-dd}",
                $"<p>{recipient.OperatorName} 您好，附件為 {now:yyyy-MM-dd} 的 SPC 項目管制總覽{period}。</p>",
                bytes, $"SPC_Item_Overview_{period}_{now:yyyyMMdd}.xlsx");
        if (weeklyDue) schedule.LastWeeklySentAt = DateTime.UtcNow;
        if (monthlyDue) schedule.LastMonthlySentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
