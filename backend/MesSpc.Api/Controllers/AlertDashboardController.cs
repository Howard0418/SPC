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
        var alerts = await query.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync();
        return Ok(await AlertDtoMapper.EnrichAlertsAsync(db, alerts));
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

    [HttpPost("{id:int}/handle")]
    public async Task<IActionResult> Handle(int id, [FromBody] HandleAlertReq req)
    {
        var alert = await db.AlertEvents.FindAsync(id);
        if (alert is null) return NotFound();
        
        alert.IsAcknowledged = true;
        alert.Status = "Closed";
        alert.RootCause = req.RootCause;
        alert.CorrectiveAction = req.CorrectiveAction;
        
        await db.SaveChangesAsync();
        return Ok(alert);
    }

    [HttpPut("{id:int}/workflow")]
    public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] AlertWorkflowReq req)
    {
        var alert = await db.AlertEvents.FindAsync(id);
        if (alert is null) return NotFound();

        alert.Status = string.IsNullOrWhiteSpace(req.Status) ? alert.Status : req.Status;
        alert.RootCause = req.RootCause;
        alert.CorrectiveAction = req.CorrectiveAction;
        alert.ResponsibleUser = req.ResponsibleUser;

        if (string.Equals(alert.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            alert.IsAcknowledged = true;
            alert.ClosedAt ??= DateTime.UtcNow;
        }

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
public record HandleAlertReq(string RootCause, string CorrectiveAction);
public record AlertWorkflowReq(string? Status, string? RootCause, string? CorrectiveAction, string? ResponsibleUser);

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var today = DateTime.UtcNow.Date;
        var batches = await db.UploadBatches.CountAsync(x => x.ConfirmedAt >= today);
        var alerts = await db.AlertEvents.CountAsync(x => x.OccurredAt >= today);
        var totalValues = await db.VariableMeasurements.CountAsync() + await db.AttributeMeasurements.CountAsync();
        var totalAlerts = await db.AlertEvents.CountAsync();
        var rate = totalValues == 0 ? 0 : (double)totalAlerts / totalValues;
        var recentAlerts = await db.AlertEvents.OrderByDescending(x => x.OccurredAt).Take(20).ToListAsync();
        var trend = await GetTrendAsync(today);
        var bottomCpk = await GetBottomCpkAsync();
        var pareto = await GetParetoAsync(today.AddDays(-30));

        return Ok(new
        {
            todayBatchCount = batches,
            todayAlertCount = alerts,
            alertRate = rate,
            recentAlerts = await AlertDtoMapper.EnrichAlertsAsync(db, recentAlerts),
            trend,
            bottomCpk,
            pareto
        });
    }

    private async Task<List<object>> GetTrendAsync(DateTime today)
    {
        var variable = await db.VariableMeasurements
            .Where(x => x.MeasuredAt >= today)
            .GroupBy(x => x.MeasuredAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();

        var attribute = await db.AttributeMeasurements
            .Where(x => x.MeasuredAt >= today)
            .GroupBy(x => x.MeasuredAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();

        return variable.Concat(attribute)
            .GroupBy(x => x.Hour)
            .Select(g => new { hour = g.Key, count = g.Sum(x => x.Count) })
            .OrderBy(x => x.hour)
            .Cast<object>()
            .ToList();
    }

    private async Task<List<object>> GetBottomCpkAsync()
    {
        var ppcs = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => x.IsEnabled)
            .ToListAsync();

        var result = new List<object>();
        foreach (var ppc in ppcs)
        {
            var values = await db.VariableMeasurements
                .AsNoTracking()
                .Where(x => x.PartProcessCharacteristicId == ppc.Id)
                .OrderByDescending(x => x.MeasuredAt)
                .Take(200)
                .Select(x => x.MeasuredValue)
                .ToListAsync();

            if (values.Count < 2) continue;
            var mean = values.Average();
            var stdev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1));
            if (stdev <= 0) continue;

            var usl = ppc.USL ?? mean + 3 * stdev;
            var lsl = ppc.LSL ?? mean - 3 * stdev;
            var cpu = (usl - mean) / (3 * stdev);
            var cpl = (mean - lsl) / (3 * stdev);
            var cpk = Math.Round(Math.Min(cpu, cpl), 2);
            result.Add(new
            {
                ppcId = ppc.Id,
                name = $"{ppc.Part?.PartNo} / {ppc.Process?.ProcessCode} / {ppc.Characteristic?.CharacteristicCode}",
                cpk
            });
        }

        return result
            .OrderBy(x => ((dynamic)x).cpk)
            .Take(5)
            .ToList();
    }

    private async Task<List<object>> GetParetoAsync(DateTime from)
    {
        var groups = await db.AlertEvents
            .AsNoTracking()
            .Where(x => x.OccurredAt >= from)
            .GroupBy(x => new { x.PartId, x.ProcessId, x.CharacteristicId })
            .Select(g => new { g.Key.PartId, g.Key.ProcessId, g.Key.CharacteristicId, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var total = groups.Sum(x => x.Count);
        var ppcIds = groups.Select(g => new { g.PartId, g.ProcessId, g.CharacteristicId }).ToList();
        var partIds = ppcIds.Select(x => x.PartId).Distinct().ToList();
        var processIds = ppcIds.Select(x => x.ProcessId).Distinct().ToList();
        var characteristicIds = ppcIds.Select(x => x.CharacteristicId).Distinct().ToList();
        var ppcs = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => partIds.Contains(x.PartId) && processIds.Contains(x.ProcessId) && characteristicIds.Contains(x.CharacteristicId))
            .ToListAsync();

        double cumulative = 0;
        return groups.Select(g =>
        {
            var ppc = ppcs.FirstOrDefault(x => x.PartId == g.PartId && x.ProcessId == g.ProcessId && x.CharacteristicId == g.CharacteristicId);
            var pct = total > 0 ? (double)g.Count / total * 100 : 0;
            cumulative += pct;
            return new
            {
                name = ppc is null ? $"{g.PartId}/{g.ProcessId}/{g.CharacteristicId}" : $"{ppc.Part?.PartNo} / {ppc.Process?.ProcessCode} / {ppc.Characteristic?.CharacteristicCode}",
                count = g.Count,
                cumulativePercentage = Math.Round(cumulative, 1),
                ppcId = ppc?.Id
            };
        }).Cast<object>().ToList();
    }
}

internal static class AlertDtoMapper
{
    public static async Task<List<object>> EnrichAlertsAsync(AppDbContext db, List<AlertEvent> alerts)
    {
        var keys = alerts
            .Select(x => new { x.PartId, x.ProcessId, x.CharacteristicId })
            .Distinct()
            .ToList();

        var partIds = keys.Select(x => x.PartId).Distinct().ToList();
        var processIds = keys.Select(x => x.ProcessId).Distinct().ToList();
        var characteristicIds = keys.Select(x => x.CharacteristicId).Distinct().ToList();

        var ppcs = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => partIds.Contains(x.PartId) && processIds.Contains(x.ProcessId) && characteristicIds.Contains(x.CharacteristicId))
            .ToListAsync();

        return alerts.Select(a =>
        {
            var ppc = ppcs.FirstOrDefault(x =>
                x.PartId == a.PartId &&
                x.ProcessId == a.ProcessId &&
                x.CharacteristicId == a.CharacteristicId);

            return new
            {
                a.Id,
                a.OccurredAt,
                a.PartId,
                a.ProcessId,
                a.CharacteristicId,
                PpcId = ppc?.Id,
                PartNo = ppc?.Part?.PartNo,
                PartName = ppc?.Part?.PartName,
                ProcessCode = ppc?.Process?.ProcessCode,
                ProcessName = ppc?.Process?.ProcessName,
                CharacteristicCode = ppc?.Characteristic?.CharacteristicCode,
                CharacteristicName = ppc?.Characteristic?.CharacteristicName,
                a.ActualValue,
                a.AlertType,
                a.Message,
                a.UploadBatchId,
                a.MeasurementBatchId,
                a.VariableMeasurementId,
                a.AttributeMeasurementId,
                a.IsAcknowledged,
                a.Status,
                a.RootCause,
                a.CorrectiveAction,
                a.ResponsibleUser,
                a.ClosedAt
            };
        }).Cast<object>().ToList();
    }
}
