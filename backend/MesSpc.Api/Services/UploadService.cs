using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class UploadService(AppDbContext db, SpcService spcService)
{
    public async Task<UploadBatch> CreateVariableBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, CancellationToken ct = default)
    {
        var batch = new UploadBatch
        {
            UploadType = "Variable",
            SourceType = sourceType,
            ImportStatus = "PreviewReady",
            CreatedBy = createdBy,
            OriginalFileName = fileName
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        await BuildStagingAsync(batch, rows, "Variable", ct);
        return batch;
    }

    public async Task<UploadBatch> CreateAttributeBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, CancellationToken ct = default)
    {
        var batch = new UploadBatch
        {
            UploadType = "Attribute",
            SourceType = sourceType,
            ImportStatus = "PreviewReady",
            CreatedBy = createdBy,
            OriginalFileName = fileName
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        await BuildStagingAsync(batch, rows, "Attribute", ct);
        return batch;
    }

    public async Task<object?> GetPreviewAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        if (batch is null) return null;
        var details = await db.UploadDetails.Where(x => x.UploadBatchId == uploadBatchId).OrderBy(x => x.RowNo).Take(200).ToListAsync(ct);
        var errors = await db.UploadErrors.Where(x => x.UploadBatchId == uploadBatchId).OrderBy(x => x.RowNo).ToListAsync(ct);
        return new { batch, details, errors };
    }

    public async Task<object?> ConfirmAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        if (batch is null) return null;
        if (batch.ImportStatus is "Imported" or "Importing") return new { batch, message = "Batch already processed." };

        batch.ImportStatus = "Importing";
        await db.SaveChangesAsync(ct);

        var validDetails = await db.UploadDetails
            .Where(x => x.UploadBatchId == uploadBatchId && x.IsValid)
            .OrderBy(x => x.RowNo)
            .ToListAsync(ct);

        var imported = 0;
        var spcCount = 0;
        var alertCount = 0;

        foreach (var detail in validDetails)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string?>>(detail.PayloadJson) ?? [];
            var resolved = await ResolveReferencesAsync(payload, ct);
            if (resolved is null) continue;
            var ctx = resolved.Value;

            if (batch.UploadType == "Variable")
            {
                if (!double.TryParse(Get(payload, "MeasuredValue"), out var measuredValue))
                {
                    continue;
                }
                var vm = new VariableMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part.Id,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    LotNo = Get(payload, "LotNo"),
                    SerialNo = Get(payload, "SerialNo"),
                    SampleNo = TryInt(Get(payload, "SampleNo"), 1),
                    MeasuredValue = measuredValue,
                    MeasuredAt = TryDateTime(Get(payload, "MeasuredAt"), DateTime.UtcNow),
                    Operator = Get(payload, "Operator")
                };
                db.VariableMeasurements.Add(vm);
                await db.SaveChangesAsync(ct);
                imported++;
                var result = await spcService.CalculateVariableAsync(vm, ct);
                if (result is not null)
                {
                    spcCount++;
                    if (result.IsOutOfControl || result.IsOutOfSpec) alertCount++;
                }
            }
            else
            {
                var am = new AttributeMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part.Id,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    LotNo = Get(payload, "LotNo"),
                    SampleNo = TryInt(Get(payload, "SampleNo"), 1),
                    InspectedQty = TryNullableInt(Get(payload, "InspectedQty")),
                    DefectQty = TryNullableInt(Get(payload, "DefectQty")),
                    DefectCount = TryNullableInt(Get(payload, "DefectCount")),
                    UnitCount = TryNullableInt(Get(payload, "UnitCount")),
                    MeasuredAt = TryDateTime(Get(payload, "MeasuredAt"), DateTime.UtcNow),
                    Operator = Get(payload, "Operator")
                };
                db.AttributeMeasurements.Add(am);
                await db.SaveChangesAsync(ct);
                imported++;
                var result = await spcService.CalculateAttributeAsync(am, ct);
                if (result is not null)
                {
                    spcCount++;
                    if (result.IsOutOfControl || result.IsOutOfSpec) alertCount++;
                }
            }
        }

        batch.ImportStatus = "Imported";
        batch.ConfirmedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new { batch, imported, spcCount, alertCount };
    }

    public async Task<bool> DeleteBatchAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        if (batch is null) return false;
        db.UploadBatches.Remove(batch);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task BuildStagingAsync(UploadBatch batch, IEnumerable<Dictionary<string, string?>> rows, string expectedDataCategory, CancellationToken ct)
    {
        var rowNo = 0;
        foreach (var row in rows)
        {
            rowNo++;
            var detail = new UploadDetail { UploadBatchId = batch.UploadBatchId, RowNo = rowNo, PayloadJson = JsonSerializer.Serialize(row), IsValid = true };
            db.UploadDetails.Add(detail);
            await db.SaveChangesAsync(ct);

            var errors = await ValidateRowAsync(row, expectedDataCategory, ct);
            if (errors.Count > 0)
            {
                detail.IsValid = false;
                foreach (var e in errors)
                {
                    db.UploadErrors.Add(new UploadError
                    {
                        UploadBatchId = batch.UploadBatchId,
                        UploadDetailId = detail.Id,
                        RowNo = rowNo,
                        FieldName = e.Field,
                        ErrorCode = e.Code,
                        ErrorMessage = e.Message
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        batch.TotalRows = rowNo;
        batch.ValidRows = await db.UploadDetails.CountAsync(x => x.UploadBatchId == batch.UploadBatchId && x.IsValid, ct);
        batch.ErrorRows = rowNo - batch.ValidRows;
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<(string Field, string Code, string Message)>> ValidateRowAsync(Dictionary<string, string?> row, string expectedDataCategory, CancellationToken ct)
    {
        var errors = new List<(string Field, string Code, string Message)>();
        var part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(row, "PartNo"), ct);
        if (part is null) errors.Add(("PartNo", "PART_NOT_FOUND", "PartNo does not exist."));
        var process = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == Get(row, "ProcessCode"), ct);
        if (process is null) errors.Add(("ProcessCode", "PROCESS_NOT_FOUND", "ProcessCode does not exist."));
        var machine = await db.Machines.FirstOrDefaultAsync(x => x.MachineCode == Get(row, "MachineCode"), ct);
        if (machine is null) errors.Add(("MachineCode", "MACHINE_NOT_FOUND", "MachineCode does not exist."));
        var characteristic = await db.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == Get(row, "CharacteristicCode"), ct);
        if (characteristic is null) errors.Add(("CharacteristicCode", "CHAR_NOT_FOUND", "CharacteristicCode does not exist."));
        if (characteristic is not null && !string.Equals(characteristic.DataCategory, expectedDataCategory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(("DataCategory", "INVALID_DATA_CATEGORY", $"Characteristic data category should be {expectedDataCategory}."));
        }

        if (part is not null && process is not null && characteristic is not null)
        {
            var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.PartId == part.Id && x.ProcessId == process.Id && x.CharacteristicId == characteristic.Id && x.IsEnabled, ct);
            if (mapping is null)
            {
                errors.Add(("PartProcessCharacteristic", "MAPPING_NOT_FOUND", "Part + Process + Characteristic mapping does not exist."));
            }
        }

        return errors;
    }

    private async Task<(Part Part, Process Process, Machine Machine, QualityCharacteristic Characteristic, PartProcessCharacteristic Mapping)?> ResolveReferencesAsync(Dictionary<string, string?> payload, CancellationToken ct)
    {
        var part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(payload, "PartNo"), ct);
        var process = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == Get(payload, "ProcessCode"), ct);
        var machine = await db.Machines.FirstOrDefaultAsync(x => x.MachineCode == Get(payload, "MachineCode"), ct);
        var characteristic = await db.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == Get(payload, "CharacteristicCode"), ct);
        if (part is null || process is null || machine is null || characteristic is null) return null;
        var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.PartId == part.Id && x.ProcessId == process.Id && x.CharacteristicId == characteristic.Id && x.IsEnabled, ct);
        if (mapping is null) return null;
        return (part, process, machine, characteristic, mapping);
    }

    private static string? Get(Dictionary<string, string?> row, string key) => row.TryGetValue(key, out var value) ? value : null;
    private static int TryInt(string? raw, int fallback) => int.TryParse(raw, out var value) ? value : fallback;
    private static int? TryNullableInt(string? raw) => int.TryParse(raw, out var value) ? value : null;
    private static DateTime TryDateTime(string? raw, DateTime fallback) => DateTime.TryParse(raw, out var value) ? value : fallback;
}
