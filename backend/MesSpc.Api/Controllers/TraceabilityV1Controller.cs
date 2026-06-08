using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/traceability")]
public class TraceabilityV1Controller(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string? workOrderNo, [FromQuery] string? lotNo, [FromQuery] string? serialNo, CancellationToken ct = default)
    {
        var variableQuery = db.VariableMeasurements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(workOrderNo)) variableQuery = variableQuery.Where(x => x.WorkOrderNo == workOrderNo);
        if (!string.IsNullOrWhiteSpace(lotNo)) variableQuery = variableQuery.Where(x => x.LotNo == lotNo);
        if (!string.IsNullOrWhiteSpace(serialNo)) variableQuery = variableQuery.Where(x => x.SerialNo == serialNo);

        var attributeQuery = db.AttributeMeasurements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(workOrderNo)) attributeQuery = attributeQuery.Where(x => x.WorkOrderNo == workOrderNo);
        if (!string.IsNullOrWhiteSpace(lotNo)) attributeQuery = attributeQuery.Where(x => x.LotNo == lotNo);

        var variableRows = await variableQuery
            .OrderByDescending(x => x.MeasuredAt)
            .Take(300)
            .ToListAsync(ct);

        var attributeRows = await attributeQuery
            .OrderByDescending(x => x.MeasuredAt)
            .Take(300)
            .ToListAsync(ct);

        var ppcIds = variableRows.Select(x => x.PartProcessCharacteristicId)
            .Concat(attributeRows.Select(x => x.PartProcessCharacteristicId))
            .Distinct()
            .ToList();

        var ppcs = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => ppcIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var uploadBatchIds = variableRows.Select(x => x.UploadBatchId)
            .Concat(attributeRows.Select(x => x.UploadBatchId))
            .Distinct()
            .ToList();

        var alerts = await db.AlertEvents
            .AsNoTracking()
            .Where(x => x.UploadBatchId.HasValue && uploadBatchIds.Contains(x.UploadBatchId.Value))
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync(ct);

        var variableGroups = variableRows
            .GroupBy(x => new TraceGroupKey(x.UploadBatchId, x.PartProcessCharacteristicId, x.WorkOrderNo, x.LotNo, x.SerialNo, "Variable"))
            .Select(g => ToVariableBatch(g.Key, g.OrderBy(x => x.SampleNo).ThenBy(x => x.Id).ToList(), ppcs, alerts));

        var attributeGroups = attributeRows
            .GroupBy(x => new TraceGroupKey(x.UploadBatchId, x.PartProcessCharacteristicId, x.WorkOrderNo, x.LotNo, null, "Attribute"))
            .Select(g => ToAttributeBatch(g.Key, g.OrderBy(x => x.SampleNo).ThenBy(x => x.Id).ToList(), ppcs, alerts));

        var batches = variableGroups
            .Concat(attributeGroups)
            .OfType<TraceBatch>()
            .OrderByDescending(x => x!.MeasuredAt)
            .Take(200)
            .ToList();

        var alertItems = batches
            .SelectMany(b => b.Alerts.Select(a => new
            {
                BatchId = b.Id,
                a.Id,
                a.OccurredAt,
                a.AlertType,
                a.Message,
                a.Status
            }))
            .ToList();

        return Ok(new { batches, alerts = alertItems });
    }

    private static TraceBatch? ToVariableBatch(TraceGroupKey key, List<VariableMeasurement> rows, Dictionary<int, PartProcessCharacteristic> ppcs, List<AlertEvent> alerts)
    {
        if (!ppcs.TryGetValue(key.PpcId, out var ppc)) return null;

        var groupAlerts = alerts
            .Where(a => a.UploadBatchId == key.UploadBatchId && a.CharacteristicId == ppc.CharacteristicId)
            .ToList();

        return new TraceBatch
        {
            Id = $"{key.UploadBatchId}:{key.PpcId}:{key.DataCategory}:{key.LotNo}:{key.SerialNo}",
            UploadBatchId = key.UploadBatchId,
            PpcId = key.PpcId,
            DataCategory = key.DataCategory,
            PartId = ppc.PartId,
            ProcessId = ppc.ProcessId,
            CharacteristicId = ppc.CharacteristicId,
            PartNo = ppc.Part?.PartNo,
            PartName = ppc.Part?.PartName,
            ProcessCode = ppc.Process?.ProcessCode,
            ProcessName = ppc.Process?.ProcessName,
            CharacteristicCode = ppc.Characteristic?.CharacteristicCode,
            CharacteristicName = ppc.Characteristic?.CharacteristicName,
            WorkOrderNo = key.WorkOrderNo,
            LotNo = key.LotNo,
            SerialNo = key.SerialNo,
            MeasuredAt = rows.Max(x => x.MeasuredAt),
            OperatorName = rows.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Operator))?.Operator,
            Values = rows.Select(x => new
            {
                x.Id,
                x.SampleNo,
                x.MeasuredValue,
                x.MeasuredAt,
                x.Operator
            }).Cast<object>().ToList(),
            Alerts = groupAlerts
        };
    }

    private static TraceBatch? ToAttributeBatch(TraceGroupKey key, List<AttributeMeasurement> rows, Dictionary<int, PartProcessCharacteristic> ppcs, List<AlertEvent> alerts)
    {
        if (!ppcs.TryGetValue(key.PpcId, out var ppc)) return null;

        var groupAlerts = alerts
            .Where(a => a.UploadBatchId == key.UploadBatchId && a.CharacteristicId == ppc.CharacteristicId)
            .ToList();

        return new TraceBatch
        {
            Id = $"{key.UploadBatchId}:{key.PpcId}:{key.DataCategory}:{key.LotNo}",
            UploadBatchId = key.UploadBatchId,
            PpcId = key.PpcId,
            DataCategory = key.DataCategory,
            PartId = ppc.PartId,
            ProcessId = ppc.ProcessId,
            CharacteristicId = ppc.CharacteristicId,
            PartNo = ppc.Part?.PartNo,
            PartName = ppc.Part?.PartName,
            ProcessCode = ppc.Process?.ProcessCode,
            ProcessName = ppc.Process?.ProcessName,
            CharacteristicCode = ppc.Characteristic?.CharacteristicCode,
            CharacteristicName = ppc.Characteristic?.CharacteristicName,
            WorkOrderNo = key.WorkOrderNo,
            LotNo = key.LotNo,
            SerialNo = key.SerialNo,
            MeasuredAt = rows.Max(x => x.MeasuredAt),
            OperatorName = rows.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Operator))?.Operator,
            Values = rows.Select(x => new
            {
                x.Id,
                x.SampleNo,
                x.InspectedQty,
                x.DefectQty,
                x.DefectCount,
                x.UnitCount,
                x.MeasuredAt,
                x.Operator
            }).Cast<object>().ToList(),
            Alerts = groupAlerts
        };
    }

    private sealed record TraceGroupKey(Guid UploadBatchId, int PpcId, string? WorkOrderNo, string? LotNo, string? SerialNo, string DataCategory);

    private sealed class TraceBatch
    {
        public string Id { get; set; } = string.Empty;
        public Guid UploadBatchId { get; set; }
        public int PpcId { get; set; }
        public string DataCategory { get; set; } = string.Empty;
        public int PartId { get; set; }
        public int ProcessId { get; set; }
        public int CharacteristicId { get; set; }
        public string? PartNo { get; set; }
        public string? PartName { get; set; }
        public string? ProcessCode { get; set; }
        public string? ProcessName { get; set; }
        public string? CharacteristicCode { get; set; }
        public string? CharacteristicName { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? LotNo { get; set; }
        public string? SerialNo { get; set; }
        public DateTime MeasuredAt { get; set; }
        public string? OperatorName { get; set; }
        public List<object> Values { get; set; } = [];
        public List<AlertEvent> Alerts { get; set; } = [];
    }
}
