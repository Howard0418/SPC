using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc-report-settings")]
public class SpcReportSchedulesController(
    AppDbContext db,
    SpcOverviewReportService reportService,
    IEmailNotificationService emailService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var schedule = await db.SpcReportSchedules.OrderBy(x => x.Id).FirstOrDefaultAsync();
        var operators = await db.Operators.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Department).ThenBy(x => x.OperatorName)
            .Select(x => new { x.Id, x.OperatorCode, x.OperatorName, x.Department, x.Email })
            .ToListAsync();
        return Ok(new { schedule, recipientOperatorIds = ParseIds(schedule?.RecipientOperatorIdsJson), operators,
            departments = operators.Select(x => x.Department).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x) });
    }

    [HttpPut]
    public async Task<IActionResult> Put(UpdateScheduleRequest req)
    {
        if (!TimeSpan.TryParse(req.SendTime, out var sendTime)) return BadRequest("寄送時間格式不正確。");
        if (req.MonthlyDay is < 1 or > 28) return BadRequest("月報日期必須介於 1 到 28 日。");
        var ids = req.RecipientOperatorIds.Distinct().ToList();
        var validCount = await db.Operators.CountAsync(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "");
        if (validCount != ids.Count) return BadRequest("收件人包含停用或未設定 Email 的使用者。");
        var schedule = await db.SpcReportSchedules.FirstOrDefaultAsync() ?? new SpcReportSchedule();
        if (schedule.Id == 0) db.SpcReportSchedules.Add(schedule);
        schedule.Department = string.IsNullOrWhiteSpace(req.Department) ? null : req.Department.Trim();
        schedule.RecipientOperatorIdsJson = JsonSerializer.Serialize(ids);
        schedule.WeeklyEnabled = req.WeeklyEnabled;
        schedule.WeeklyDayOfWeek = Math.Clamp(req.WeeklyDayOfWeek, 0, 6);
        schedule.MonthlyEnabled = req.MonthlyEnabled;
        schedule.MonthlyDay = req.MonthlyDay;
        schedule.SendTime = sendTime;
        schedule.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync();
        return Ok(schedule);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime? start, [FromQuery] DateTime? end, CancellationToken ct)
    {
        var endUtc = end?.ToUniversalTime() ?? DateTime.UtcNow;
        var startUtc = start?.ToUniversalTime() ?? endUtc.AddMonths(-1);
        var bytes = await reportService.BuildExcelAsync(startUtc, endUtc, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SPC_Item_Overview_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpPost("send-now")]
    public async Task<IActionResult> SendNow(SendNowRequest req, CancellationToken ct)
    {
        var reportType = NormalizeReportType(req.ReportType);
        if (reportType is null) return BadRequest(new { message = "報表類型必須為 weekly 或 monthly。" });

        var ids = req.RecipientOperatorIds.Distinct().ToList();
        if (ids.Count == 0) return BadRequest(new { message = "請至少勾選一位有 Email 的收件人。" });

        var recipients = await db.Operators.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "")
            .OrderBy(x => x.Department)
            .ThenBy(x => x.OperatorName)
            .ToListAsync(ct);
        if (recipients.Count != ids.Count) return BadRequest(new { message = "收件人包含停用或未設定 Email 的使用者。" });

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var (startUtc, endUtc, periodLabel) = GetReportPeriod(reportType, nowLocal, tz);
        var bytes = await reportService.BuildExcelAsync(startUtc, endUtc, ct);
        var displayType = reportType == "weekly" ? "週報" : "月報";
        var fileName = $"SPC_Item_Overview_{displayType}_{nowLocal:yyyyMMdd_HHmm}.xlsx";
        var results = new List<SendNowRecipientResult>();

        foreach (var recipient in recipients)
        {
            try
            {
                var sent = await emailService.SendReportEmailAsync(
                    recipient.Email!,
                    recipient.OperatorName,
                    $"【SPC 項目管制總覽{displayType}測試寄送】{nowLocal:yyyy-MM-dd HH:mm}",
                    $"<p>{recipient.OperatorName} 您好，附件為 SPC 項目管制總覽{displayType}。</p><p>報表期間：{periodLabel}</p>",
                    bytes,
                    fileName);
                results.Add(new SendNowRecipientResult(
                    recipient.Id,
                    recipient.OperatorCode,
                    recipient.OperatorName,
                    recipient.Email!,
                    sent,
                    sent ? null : "寄信服務回傳失敗。"));
            }
            catch (Exception ex)
            {
                results.Add(new SendNowRecipientResult(
                    recipient.Id,
                    recipient.OperatorCode,
                    recipient.OperatorName,
                    recipient.Email!,
                    false,
                    ex.Message));
            }
        }

        return Ok(new
        {
            reportType,
            displayType,
            periodLabel,
            fileName,
            total = results.Count,
            success = results.Count(x => x.Success),
            failed = results.Count(x => !x.Success),
            recipients = results
        });
    }

    private static List<int> ParseIds(string? json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? []; }
        catch { return []; }
    }

    public record UpdateScheduleRequest(string? Department, List<int> RecipientOperatorIds,
        bool WeeklyEnabled, int WeeklyDayOfWeek, bool MonthlyEnabled, int MonthlyDay,
        string SendTime, bool IsEnabled);

    public record SendNowRequest(string ReportType, List<int> RecipientOperatorIds);
    public record SendNowRecipientResult(int Id, string OperatorCode, string OperatorName, string Email, bool Success, string? ErrorMessage);

    private static string? NormalizeReportType(string? reportType)
    {
        return reportType?.Trim().ToLowerInvariant() switch
        {
            "weekly" or "week" => "weekly",
            "monthly" or "month" => "monthly",
            _ => null
        };
    }

    private static (DateTime StartUtc, DateTime EndUtc, string PeriodLabel) GetReportPeriod(
        string reportType,
        DateTime nowLocal,
        TimeZoneInfo tz)
    {
        DateTime startLocal;
        DateTime endLocal;
        if (reportType == "weekly")
        {
            endLocal = nowLocal.Date;
            startLocal = endLocal.AddDays(-7);
        }
        else
        {
            endLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
            startLocal = endLocal.AddMonths(-1);
        }

        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, tz),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, tz),
            $"{startLocal:yyyy-MM-dd} ~ {endLocal.AddDays(-1):yyyy-MM-dd}");
    }
}
