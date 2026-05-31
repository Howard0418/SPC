using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Route("api/v1/alerts")]
[Route("api/spc/alerts")]
public class AlertsController(AppDbContext db, IEmailNotificationService emailService, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool? isAcknowledged, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = db.AlertEvents.AsQueryable();
        if (isAcknowledged.HasValue) query = query.Where(x => x.IsAcknowledged == isAcknowledged.Value);
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt <= to.Value);
        return Ok(await query.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync());
    }

    [HttpPost("{id:int}/ack")]
    public async Task<IActionResult> Ack(int id)
    {
        var alert = await db.AlertEvents.FindAsync(id);
        if (alert is null) return NotFound();
        alert.IsAcknowledged = true;
        await db.SaveChangesAsync();
        return Ok(alert);
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromQuery] string? email)
    {
        var targetEmail = email ?? config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw";
        var success = await emailService.SendTestEmailAsync(targetEmail);
        return Ok(new { success, recipient = targetEmail, outboxFolder = config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox" });
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulateAlert([FromBody] SimulateAlertReq req)
    {
        var alert = new AlertEvent
        {
            OccurredAt = DateTime.UtcNow,
            PartId = req.PartId > 0 ? req.PartId : 101,
            ProcessId = req.ProcessId > 0 ? req.ProcessId : 201,
            CharacteristicId = 301,
            ActualValue = req.ActualValue > 0 ? req.ActualValue : 105.85,
            AlertType = req.AlertType == "OOS" ? AlertType.OutOfSpec : AlertType.OutOfControl,
            Message = req.Message ?? (req.AlertType == "OOS" ? "【模擬測試】量測數值超出規格上限 USL (100.0)" : "【模擬測試】量測數值違反 3-Sigma 管制上限 (102.5)")
        };

        db.AlertEvents.Add(alert);
        await db.SaveChangesAsync();

        var targetEmail = req.TargetEmail ?? config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw";
        var emailSent = await emailService.SendAlertEmailAsync(alert, targetEmail, "品管工程師 (測試)");

        return Ok(new { alert, emailSent, recipient = targetEmail, outboxFolder = config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox" });
    }
}

public record SimulateAlertReq(int PartId, int ProcessId, double ActualValue, string AlertType, string? Message, string? TargetEmail);

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var today = DateTime.UtcNow.Date;
        var batches = await db.MeasurementBatches.CountAsync(x => x.MeasuredAt >= today);
        var alerts = await db.AlertEvents.CountAsync(x => x.OccurredAt >= today);
        var totalValues = await db.MeasurementValues.CountAsync();
        var totalAlerts = await db.AlertEvents.CountAsync();
        var rate = totalValues == 0 ? 0 : (double)totalAlerts / totalValues;
        var recentAlerts = await db.AlertEvents.OrderByDescending(x => x.OccurredAt).Take(20).ToListAsync();
        return Ok(new { todayBatchCount = batches, todayAlertCount = alerts, alertRate = rate, recentAlerts });
    }
}
