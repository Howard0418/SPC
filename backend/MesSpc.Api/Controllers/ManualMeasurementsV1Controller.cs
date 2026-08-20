using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MesSpc.Api.Controllers;

public record ManualMeasurementValueDto(int SampleNo, double ValueNumeric);

public record CreateManualMeasurementReq(
    int PartProcessCharacteristicId,
    string BatchNo,
    DateTime MeasuredAt,
    string? OperatorName,
    string? WorkOrderNo,
    string? LotNo,
    string? SerialNo,
    List<ManualMeasurementValueDto> Values);

public record UpdateManualMeasurementItemReq(
    double MeasuredValue,
    double? RecheckValue,
    string? AdjustAction,
    double? AdjustAmount,
    DateTime MeasuredAt,
    string? OperatorName);

[ApiController]
[Route("api/v1/manual-measurements")]
public class ManualMeasurementsV1Controller(AppDbContext db, SpcService spcService) : ControllerBase
{
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] int machineId, [FromQuery] DateOnly date, CancellationToken ct = default)
    {
        if (machineId <= 0) return BadRequest("machineId is required.");
        var dailyDate = date.ToDateTime(TimeOnly.MinValue);
        var rows = await db.VariableMeasurements.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.PortalDailyDate == dailyDate && x.SourceType == SourceType.Manual)
            .OrderBy(x => x.PartProcessCharacteristicId).ThenBy(x => x.SampleNo).ToListAsync(ct);
        if (rows.Count == 0) return Ok(new { date, machineId, exists = false, rows = Array.Empty<object>() });

        var ppcIds = rows.Select(x => x.PartProcessCharacteristicId).Distinct().ToList();
        var mappings = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Characteristic).Include(x => x.Tank)
            .Where(x => ppcIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var batchIds = rows.Select(x => x.UploadBatchId).Distinct().ToList();
        var details = await db.UploadDetails.AsNoTracking()
            .Where(x => batchIds.Contains(x.UploadBatchId) && x.IsValid).OrderByDescending(x => x.RowNo).ToListAsync(ct);
        var detailPayloads = details.Select(x =>
        {
            try { return (x.UploadBatchId, Payload: JsonSerializer.Deserialize<Dictionary<string, string?>>(x.PayloadJson)); }
            catch { return (x.UploadBatchId, Payload: (Dictionary<string, string?>?)null); }
        }).Where(x => x.Payload is not null).ToList();

        var result = rows.Select(row =>
        {
            mappings.TryGetValue(row.PartProcessCharacteristicId, out var mapping);
            var characteristicCode = mapping?.Characteristic?.CharacteristicCode;
            var tankCode = mapping?.Tank?.TankCode;
            var payload = detailPayloads.FirstOrDefault(x => x.UploadBatchId == row.UploadBatchId
                && string.Equals(x.Payload!.GetValueOrDefault("CharacteristicCode"), characteristicCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Payload!.GetValueOrDefault("TankCode"), tankCode, StringComparison.OrdinalIgnoreCase)).Payload;
            return new
            {
                row.Id,
                ppcId = row.PartProcessCharacteristicId,
                row.UploadBatchId,
                row.MeasuredValue,
                row.RecheckValue,
                row.AdjustAction,
                row.AdjustAmount,
                row.MeasuredAt,
                operatorName = row.Operator,
                titrationValue = payload?.GetValueOrDefault("TitrationValue"),
                recheckTitrationValue = payload?.GetValueOrDefault("RecheckTitrationValue")
            };
        }).ToList();
        return Ok(new { date, machineId, exists = true, rows = result });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(
        [FromQuery] int ppcId,
        [FromQuery] DateOnly date,
        [FromQuery] string? operatorName,
        [FromQuery] int timezoneOffsetMinutes = 480,
        CancellationToken ct = default)
    {
        if (ppcId <= 0) return BadRequest("ppcId is required.");
        timezoneOffsetMinutes = Math.Clamp(timezoneOffsetMinutes, -840, 840);
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var utcStart = localStart.AddMinutes(-timezoneOffsetMinutes);
        var utcEnd = utcStart.AddDays(1);
        var normalizedOperator = operatorName?.Trim();

        var query = db.VariableMeasurements.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == ppcId
                     && x.SourceType == SourceType.Manual
                     && x.MeasuredAt >= utcStart && x.MeasuredAt < utcEnd);
        if (!string.IsNullOrWhiteSpace(normalizedOperator))
            query = query.Where(x => x.Operator == normalizedOperator);

        var batchId = await query.OrderByDescending(x => x.MeasuredAt)
            .Select(x => (Guid?)x.UploadBatchId).FirstOrDefaultAsync(ct);
        if (!batchId.HasValue) return NotFound("找不到符合日期、班別/人員與檢驗項目的既有資料。");

        var rows = await db.VariableMeasurements.AsNoTracking()
            .Where(x => x.UploadBatchId == batchId.Value && x.PartProcessCharacteristicId == ppcId)
            .OrderBy(x => x.SampleNo).ToListAsync(ct);
        var batch = await db.UploadBatches.AsNoTracking().FirstAsync(x => x.UploadBatchId == batchId.Value, ct);
        var first = rows[0];
        return Ok(new
        {
            batchId,
            batchNo = batch.OriginalFileName,
            first.WorkOrderNo,
            first.LotNo,
            first.SerialNo,
            first.MeasuredAt,
            operatorName = first.Operator,
            values = rows.Select(x => new { x.SampleNo, valueNumeric = x.MeasuredValue })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateManualMeasurementReq req, CancellationToken ct = default)
    {
        if (req.PartProcessCharacteristicId <= 0) return BadRequest("PartProcessCharacteristicId is required.");
        if (req.Values.Count == 0) return BadRequest("At least one measurement value is required.");

        var mapping = await db.PartProcessCharacteristics
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .FirstOrDefaultAsync(x => x.Id == req.PartProcessCharacteristicId && x.IsEnabled, ct);

        if (mapping is null) return NotFound("Part process characteristic not found or disabled.");

        var dataCategory = mapping.Characteristic?.DataCategory ?? "Variable";
        if (!string.Equals(dataCategory, "Variable", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Manual entry currently supports variable measurements. Please use attribute import for count data.");
        }

        var machineId = await db.Machines
            .Where(x => x.ProcessId == mapping.ProcessId && x.IsEnabled)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (machineId == 0)
        {
            var machine = new Machine
            {
                MachineCode = $"MANUAL-{mapping.ProcessId}",
                MachineName = $"Manual Entry Station {mapping.ProcessId}",
                ProcessId = mapping.ProcessId,
                Status = "Active",
                IsEnabled = true
            };
            db.Machines.Add(machine);
            await db.SaveChangesAsync(ct);
            machineId = machine.Id;
        }

        var uploadBatch = new UploadBatch
        {
            UploadType = "Variable",
            SourceType = "Manual",
            ImportStatus = "Imported",
            OriginalFileName = req.BatchNo,
            TotalRows = req.Values.Count,
            ValidRows = req.Values.Count,
            ErrorRows = 0,
            ConfirmedAt = DateTime.UtcNow,
            CreatedBy = req.OperatorName
        };

        db.UploadBatches.Add(uploadBatch);
        await db.SaveChangesAsync(ct);

        var measurements = new List<VariableMeasurement>();
        var spcCount = 0;

        foreach (var value in req.Values.OrderBy(x => x.SampleNo))
        {
            var measurement = new VariableMeasurement
            {
                UploadBatchId = uploadBatch.UploadBatchId,
                PartId = mapping.PartId ?? 0,
                ProcessId = mapping.ProcessId,
                MachineId = machineId,
                CharacteristicId = mapping.CharacteristicId,
                PartProcessCharacteristicId = mapping.Id,
                WorkOrderNo = req.WorkOrderNo,
                LotNo = req.LotNo,
                SerialNo = req.SerialNo,
                SampleNo = value.SampleNo,
                MeasuredValue = value.ValueNumeric,
                MeasuredAt = req.MeasuredAt,
                Operator = req.OperatorName,
                SourceType = SourceType.Manual,
                SourceReference = req.BatchNo
            };

            db.VariableMeasurements.Add(measurement);
            await db.SaveChangesAsync(ct);
            measurements.Add(measurement);

            var result = await spcService.CalculateVariableAsync(measurement, ct);
            if (result is not null) spcCount++;
        }

        var alerts = await db.AlertEvents
            .Where(x => x.UploadBatchId == uploadBatch.UploadBatchId)
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync(ct);

        return Ok(new
        {
            Batch = new
            {
                Id = uploadBatch.UploadBatchId,
                uploadBatch.UploadBatchId,
                req.BatchNo,
                req.LotNo,
                req.SerialNo,
                req.MeasuredAt,
                OperatorName = req.OperatorName,
                PpcId = mapping.Id,
                PartNo = mapping.Part?.PartNo,
                ProcessCode = mapping.Process?.ProcessCode,
                CharacteristicCode = mapping.Characteristic?.CharacteristicCode,
                Values = measurements.Select(x => new
                {
                    x.Id,
                    x.SampleNo,
                    x.MeasuredValue,
                    x.MeasuredAt
                }).ToList()
            },
            Imported = measurements.Count,
            SpcCount = spcCount,
            Alerts = alerts
        });
    }

    [HttpPut("{uploadBatchId:guid}")]
    public async Task<IActionResult> Update(Guid uploadBatchId, CreateManualMeasurementReq req, CancellationToken ct = default)
    {
        if (req.PartProcessCharacteristicId <= 0 || req.Values.Count == 0)
            return BadRequest("檢驗項目與量測值為必填。");

        var mapping = await db.PartProcessCharacteristics
            .FirstOrDefaultAsync(x => x.Id == req.PartProcessCharacteristicId && x.IsEnabled, ct);
        if (mapping is null) return NotFound("檢驗項目不存在或已停用。");

        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        var rows = await db.VariableMeasurements
            .Where(x => x.UploadBatchId == uploadBatchId && x.PartProcessCharacteristicId == req.PartProcessCharacteristicId)
            .OrderBy(x => x.SampleNo).ToListAsync(ct);
        if (batch is null || rows.Count == 0) return NotFound("找不到要修改的手動量測批次。");
        if (rows.Any(x => x.SourceType != SourceType.Manual))
            return Conflict("此資料不是手動錄入來源，禁止在本頁修改。");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var before = rows.Select(x => new { x.Id, x.SampleNo, x.MeasuredValue, x.MeasuredAt, x.Operator }).ToList();
        var measurementIds = rows.Select(x => x.Id).ToList();
        db.SpcCalculationResults.RemoveRange(await db.SpcCalculationResults
            .Where(x => x.VariableMeasurementId.HasValue && measurementIds.Contains(x.VariableMeasurementId.Value))
            .ToListAsync(ct));

        var oldAlerts = await db.AlertEvents
            .Where(x => x.VariableMeasurementId.HasValue && measurementIds.Contains(x.VariableMeasurementId.Value))
            .ToListAsync(ct);
        foreach (var alert in oldAlerts)
        {
            alert.Status = "Superseded";
            alert.IsAcknowledged = true;
            alert.ClosedAt = DateTime.UtcNow;
            alert.Message += " [量測資料已修改，原警報失效]";
            alert.UpdatedAt = DateTime.UtcNow;
            alert.UpdatedBy = req.OperatorName;
        }

        var requested = req.Values.OrderBy(x => x.SampleNo).ToList();
        var bySample = rows.ToDictionary(x => x.SampleNo);
        foreach (var value in requested)
        {
            if (!bySample.TryGetValue(value.SampleNo, out var row))
            {
                row = new VariableMeasurement
                {
                    UploadBatchId = uploadBatchId,
                    PartId = mapping.PartId ?? 0,
                    ProcessId = mapping.ProcessId,
                    MachineId = rows[0].MachineId,
                    CharacteristicId = mapping.CharacteristicId,
                    PartProcessCharacteristicId = mapping.Id,
                    SampleNo = value.SampleNo,
                    SourceType = SourceType.Manual
                };
                db.VariableMeasurements.Add(row);
            }
            row.MeasuredValue = value.ValueNumeric;
            row.MeasuredAt = req.MeasuredAt;
            row.Operator = req.OperatorName;
            row.WorkOrderNo = req.WorkOrderNo;
            row.LotNo = req.LotNo;
            row.SerialNo = req.SerialNo;
            row.SourceReference = req.BatchNo;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = req.OperatorName;
        }
        var requestedSamples = requested.Select(x => x.SampleNo).ToHashSet();
        db.VariableMeasurements.RemoveRange(rows.Where(x => !requestedSamples.Contains(x.SampleNo)));

        batch.OriginalFileName = req.BatchNo;
        var totalAfterUpdate = await db.VariableMeasurements.CountAsync(x => x.UploadBatchId == uploadBatchId, ct)
            - rows.Count + requested.Count;
        batch.TotalRows = totalAfterUpdate;
        batch.ValidRows = totalAfterUpdate;
        batch.UpdatedAt = DateTime.UtcNow;
        batch.UpdatedBy = req.OperatorName;
        var nextAuditRow = (await db.UploadDetails.Where(x => x.UploadBatchId == uploadBatchId)
            .MaxAsync(x => (int?)x.RowNo, ct) ?? 0) + 1;
        db.UploadDetails.Add(new UploadDetail
        {
            UploadBatchId = uploadBatchId,
            RowNo = nextAuditRow,
            PayloadJson = JsonSerializer.Serialize(new { action = "ManualMeasurementUpdated", changedAt = DateTime.UtcNow, changedBy = req.OperatorName, before, after = req.Values }),
            IsValid = true,
            CreatedBy = req.OperatorName
        });
        await db.SaveChangesAsync(ct);

        var updatedRows = await db.VariableMeasurements
            .Where(x => x.UploadBatchId == uploadBatchId && x.PartProcessCharacteristicId == req.PartProcessCharacteristicId)
            .OrderBy(x => x.SampleNo).ToListAsync(ct);
        foreach (var row in updatedRows) await spcService.CalculateVariableAsync(row, ct);
        await tx.CommitAsync(ct);

        var updatedIds = updatedRows.Select(x => x.Id).ToList();
        var alerts = await db.AlertEvents
            .Where(x => x.VariableMeasurementId.HasValue && updatedIds.Contains(x.VariableMeasurementId.Value) && x.Status != "Superseded")
            .OrderByDescending(x => x.OccurredAt).ToListAsync(ct);
        return Ok(new { batch = new { id = uploadBatchId, uploadBatchId, req.BatchNo, req.MeasuredAt, operatorName = req.OperatorName, values = updatedRows.Select(x => new { x.Id, x.SampleNo, x.MeasuredValue }) }, updated = updatedRows.Count, alerts });
    }

    [HttpPut("items/{id:long}")]
    public async Task<IActionResult> UpdateItem(long id, UpdateManualMeasurementItemReq req, CancellationToken ct = default)
    {
        var row = await db.VariableMeasurements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return NotFound("找不到要修改的量測資料。");
        if (row.SourceType != SourceType.Manual) return Conflict("此資料不是手動錄入來源，禁止修改。");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var before = new { row.MeasuredValue, row.RecheckValue, row.AdjustAction, row.AdjustAmount, row.MeasuredAt, row.Operator };
        db.SpcCalculationResults.RemoveRange(await db.SpcCalculationResults
            .Where(x => x.VariableMeasurementId == id).ToListAsync(ct));
        var oldAlerts = await db.AlertEvents.Where(x => x.VariableMeasurementId == id).ToListAsync(ct);
        foreach (var alert in oldAlerts)
        {
            alert.Status = "Superseded";
            alert.IsAcknowledged = true;
            alert.ClosedAt = DateTime.UtcNow;
            alert.Message += " [量測資料已修改，原警報失效]";
            alert.UpdatedAt = DateTime.UtcNow;
            alert.UpdatedBy = req.OperatorName;
        }

        row.MeasuredValue = req.MeasuredValue;
        row.RecheckValue = req.RecheckValue;
        row.AdjustAction = req.AdjustAction?.Trim();
        row.AdjustAmount = req.AdjustAmount;
        row.MeasuredAt = req.MeasuredAt;
        row.Operator = req.OperatorName?.Trim();
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = req.OperatorName?.Trim();
        var nextAuditRow = (await db.UploadDetails.Where(x => x.UploadBatchId == row.UploadBatchId)
            .MaxAsync(x => (int?)x.RowNo, ct) ?? 0) + 1;
        db.UploadDetails.Add(new UploadDetail
        {
            UploadBatchId = row.UploadBatchId,
            RowNo = nextAuditRow,
            PayloadJson = JsonSerializer.Serialize(new { action = "ManualMeasurementItemUpdated", measurementId = id, changedAt = DateTime.UtcNow, changedBy = req.OperatorName, before, after = req }),
            IsValid = true,
            CreatedBy = req.OperatorName
        });
        await db.SaveChangesAsync(ct);
        await spcService.CalculateVariableAsync(row, ct);
        await tx.CommitAsync(ct);
        return Ok(new { row.Id, row.UploadBatchId, row.MeasuredValue, row.RecheckValue, row.AdjustAction, row.AdjustAmount, row.MeasuredAt, operatorName = row.Operator });
    }
}
