using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

internal static class LegacyV2Api
{
    public static ObjectResult Gone(string replacement) => new(
        new
        {
            message = "V2 API 已封存，禁止再寫入舊版資料。請改用 V1 流程。",
            replacement
        })
    {
        StatusCode = StatusCodes.Status410Gone
    };
}

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
    public IActionResult Create(CreateWorkOrderReq req) => LegacyV2Api.Gone("/api/v1/manual-measurements");

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, UpdateWorkOrderReq req) => LegacyV2Api.Gone("/api/v1/manual-measurements");

    [HttpPost("{id:int}/start")]
    public IActionResult Start(int id) => LegacyV2Api.Gone("/api/v1/manual-measurements");

    [HttpPost("{id:int}/complete")]
    public IActionResult Complete(int id) => LegacyV2Api.Gone("/api/v1/manual-measurements");
}

[ApiController]
[Route("api/v2/station-ops")]
public class StationOpsV2Controller(AppDbContext db) : ControllerBase
{
    [HttpPost("open-session")]
    public IActionResult OpenSession(OpenSessionReq req) => LegacyV2Api.Gone("/api/v1/manual-measurements");

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
    public IActionResult SubmitMeasurements(int sessionId, SubmitSessionMeasurementsReq req) => LegacyV2Api.Gone("/api/v1/manual-measurements");

    [HttpPost("{sessionId:int}/close")]
    public IActionResult CloseSession(int sessionId) => LegacyV2Api.Gone("/api/v1/manual-measurements");
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
    public IActionResult UpdateWorkflow(int id, UpdateAlertWorkflowReq req) => LegacyV2Api.Gone("/api/v1/alerts/{id}/workflow");
}

