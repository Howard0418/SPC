using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

[ApiController]
[Route("api/v1/manual-measurements")]
public class ManualMeasurementsV1Controller(AppDbContext db, SpcService spcService) : ControllerBase
{
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
                PartId = mapping.PartId,
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
}
