using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

public record CreateWorkOrderReq(string WorkOrderNo, int ProductId, int PlannedQty, DateTime? PlannedStartTime, DateTime? PlannedEndTime);
public record UpdateWorkOrderReq(int PlannedQty, int ActualQty, string Status, DateTime? PlannedStartTime, DateTime? PlannedEndTime);
public record OpenSessionReq(int WorkOrderId, int StationId, string? LotNo, string? SerialNo, string OperatorName);
public record SubmitSessionMeasurementsReq(string BatchNo, DateTime MeasuredAt, List<MeasurementValueDto> Values);
public record UpdateAlertWorkflowReq(string Status, string? RootCause, string? CorrectiveAction, string? ResponsibleUser);

[ApiController]
[Route("api/v2/work-orders")]
public class WorkOrdersV2Controller(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] int? productId, [FromQuery] string? workOrderNo)
    {
        var query = db.WorkOrders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(workOrderNo)) query = query.Where(x => x.WorkOrderNo == workOrderNo);
        return Ok(await query.OrderByDescending(x => x.Id).Take(300).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkOrderReq req)
    {
        var wo = new WorkOrder
        {
            WorkOrderNo = req.WorkOrderNo,
            ProductId = req.ProductId,
            PlannedQty = req.PlannedQty,
            PlannedStartTime = req.PlannedStartTime,
            PlannedEndTime = req.PlannedEndTime,
            Status = "Planned"
        };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();
        return Ok(wo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateWorkOrderReq req)
    {
        var wo = await db.WorkOrders.FindAsync(id);
        if (wo is null) return NotFound();
        wo.PlannedQty = req.PlannedQty;
        wo.ActualQty = req.ActualQty;
        wo.Status = req.Status;
        wo.PlannedStartTime = req.PlannedStartTime;
        wo.PlannedEndTime = req.PlannedEndTime;
        wo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(wo);
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var wo = await db.WorkOrders.FindAsync(id);
        if (wo is null) return NotFound();
        wo.Status = "InProgress";
        wo.ActualStartTime = DateTime.UtcNow;
        wo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(wo);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var wo = await db.WorkOrders.FindAsync(id);
        if (wo is null) return NotFound();
        wo.Status = "Completed";
        wo.ActualEndTime = DateTime.UtcNow;
        wo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(wo);
    }
}

[ApiController]
[Route("api/v2/station-ops")]
public class StationOpsV2Controller(AppDbContext db, SpcService spcService) : ControllerBase
{
    [HttpPost("open-session")]
    public async Task<IActionResult> OpenSession(OpenSessionReq req)
    {
        var session = new StationOperationSession
        {
            WorkOrderId = req.WorkOrderId,
            StationId = req.StationId,
            LotNo = req.LotNo,
            SerialNo = req.SerialNo,
            OperatorName = req.OperatorName,
            Status = "Open",
            StartedAt = DateTime.UtcNow
        };
        db.StationOperationSessions.Add(session);
        await db.SaveChangesAsync();
        return Ok(new { sessionId = session.Id, status = session.Status });
    }

    [HttpGet("{sessionId:int}/context")]
    public async Task<IActionResult> GetContext(int sessionId)
    {
        var session = await db.StationOperationSessions.FindAsync(sessionId);
        if (session is null) return NotFound();
        var wo = await db.WorkOrders.FindAsync(session.WorkOrderId);
        if (wo is null) return NotFound("Work order not found.");

        var itemIds = await db.ProductStationItems
            .Where(x => x.ProductId == wo.ProductId && x.StationId == session.StationId && x.IsActive)
            .Select(x => x.InspectionItemId)
            .ToListAsync();
        var items = await db.InspectionItems.Where(x => itemIds.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
        return Ok(new { session, workOrder = wo, inspectionItems = items });
    }

    [HttpPost("{sessionId:int}/submit-measurements")]
    public async Task<IActionResult> SubmitMeasurements(int sessionId, SubmitSessionMeasurementsReq req)
    {
        var session = await db.StationOperationSessions.FindAsync(sessionId);
        if (session is null) return NotFound();
        var wo = await db.WorkOrders.FindAsync(session.WorkOrderId);
        if (wo is null) return NotFound("Work order not found.");

        var batch = new MeasurementBatch
        {
            BatchNo = req.BatchNo,
            ProductId = wo.ProductId,
            StationId = session.StationId,
            WorkOrderId = wo.Id,
            StationOperationSessionId = session.Id,
            LotNo = session.LotNo,
            SerialNo = session.SerialNo,
            MeasuredAt = req.MeasuredAt,
            OperatorName = session.OperatorName,
            SourceType = SourceType.Manual,
            Values = req.Values.Select(v => new MeasurementValue
            {
                InspectionItemId = v.InspectionItemId,
                SampleNo = v.SampleNo,
                ValueNumeric = v.ValueNumeric,
                ValueText = v.ValueText,
                ValueBool = v.ValueBool
            }).ToList()
        };

        db.MeasurementBatches.Add(batch);
        await db.SaveChangesAsync();
        var alerts = await spcService.EvaluateBatchAsync(batch);
        return Ok(new { batchId = batch.Id, alertsCount = alerts.Count, alerts });
    }

    [HttpPost("{sessionId:int}/close")]
    public async Task<IActionResult> CloseSession(int sessionId)
    {
        var session = await db.StationOperationSessions.FindAsync(sessionId);
        if (session is null) return NotFound();
        session.Status = "Closed";
        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(session);
    }
}

[ApiController]
[Route("api/v2/traceability")]
public class TraceabilityV2Controller(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string? workOrderNo, [FromQuery] string? lotNo, [FromQuery] string? serialNo)
    {
        var q = db.MeasurementBatches.Include(x => x.Values).AsQueryable();
        if (!string.IsNullOrWhiteSpace(lotNo)) q = q.Where(x => x.LotNo == lotNo);
        if (!string.IsNullOrWhiteSpace(serialNo)) q = q.Where(x => x.SerialNo == serialNo);
        if (!string.IsNullOrWhiteSpace(workOrderNo))
        {
            var ids = await db.WorkOrders.Where(x => x.WorkOrderNo == workOrderNo).Select(x => x.Id).ToListAsync();
            q = q.Where(x => x.WorkOrderId.HasValue && ids.Contains(x.WorkOrderId.Value));
        }

        var batches = await q.OrderByDescending(x => x.MeasuredAt).Take(200).ToListAsync();
        var batchIds = batches.Select(x => x.Id).ToList();
        var alerts = new List<AlertEvent>(); // Legacy link to AlertEvent removed
        return Ok(new { batches, alerts });
    }
}

[ApiController]
[Route("api/v2/alerts")]
public class AlertsWorkflowV2Controller(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status)
    {
        var q = db.AlertEvents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        return Ok(await q.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync());
    }

    [HttpPut("{id:int}/workflow")]
    public async Task<IActionResult> UpdateWorkflow(int id, UpdateAlertWorkflowReq req)
    {
        var alert = await db.AlertEvents.FindAsync(id);
        if (alert is null) return NotFound();
        alert.Status = req.Status;
        alert.RootCause = req.RootCause;
        alert.CorrectiveAction = req.CorrectiveAction;
        alert.ResponsibleUser = req.ResponsibleUser;
        alert.UpdatedAt = DateTime.UtcNow;
        if (string.Equals(req.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            alert.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(alert);
    }
}

