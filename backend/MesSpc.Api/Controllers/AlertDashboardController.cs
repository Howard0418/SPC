using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public class AlertsController(AppDbContext db) : ControllerBase
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
}

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
