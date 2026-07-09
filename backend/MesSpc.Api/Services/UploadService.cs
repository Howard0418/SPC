using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Services.Security;

namespace MesSpc.Api.Services;

public class UploadService(AppDbContext db, SpcService spcService)
{
    public async Task<UploadBatch> CreateVariableBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, string? fileHash = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(fileHash))
        {
            var isDuplicate = await db.UploadBatches.AnyAsync(x => x.FileHash == fileHash && (x.ImportStatus == "Imported" || x.ImportStatus == "Importing"), ct);
            if (isDuplicate) throw new InvalidOperationException("DUPLICATE_FILE");

            var staleBatches = await db.UploadBatches.Where(x => x.FileHash == fileHash && x.ImportStatus == "PreviewReady").ToListAsync(ct);
            foreach (var b in staleBatches)
            {
                var errors = db.UploadErrors.Where(e => e.UploadBatchId == b.UploadBatchId);
                var details = db.UploadDetails.Where(d => d.UploadBatchId == b.UploadBatchId);
                db.UploadErrors.RemoveRange(errors);
                db.UploadDetails.RemoveRange(details);
                db.UploadBatches.Remove(b);
            }
            if (staleBatches.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        var batch = new UploadBatch
        {
            UploadType = "Variable",
            SourceType = sourceType,
            ImportStatus = "PreviewReady",
            CreatedBy = createdBy,
            OriginalFileName = fileName,
            FileHash = fileHash
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        await BuildStagingAsync(batch, rows, "Variable", ct);
        return batch;
    }

    public async Task<UploadBatch> CreateAttributeBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, string? fileHash = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(fileHash))
        {
            var isDuplicate = await db.UploadBatches.AnyAsync(x => x.FileHash == fileHash && (x.ImportStatus == "Imported" || x.ImportStatus == "Importing"), ct);
            if (isDuplicate) throw new InvalidOperationException("DUPLICATE_FILE");

            var staleBatches = await db.UploadBatches.Where(x => x.FileHash == fileHash && x.ImportStatus == "PreviewReady").ToListAsync(ct);
            foreach (var b in staleBatches)
            {
                var errors = db.UploadErrors.Where(e => e.UploadBatchId == b.UploadBatchId);
                var details = db.UploadDetails.Where(d => d.UploadBatchId == b.UploadBatchId);
                db.UploadErrors.RemoveRange(errors);
                db.UploadDetails.RemoveRange(details);
                db.UploadBatches.Remove(b);
            }
            if (staleBatches.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        var batch = new UploadBatch
        {
            UploadType = "Attribute",
            SourceType = sourceType,
            ImportStatus = "PreviewReady",
            CreatedBy = createdBy,
            OriginalFileName = fileName,
            FileHash = fileHash
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
        if (batch.ImportStatus == "Imported") return new { batch, message = "Batch already processed." };

        if (batch.ImportStatus != "Importing")
        {
            batch.ImportStatus = "Importing";
            await db.SaveChangesAsync(ct);
        }

        var validDetails = await db.UploadDetails
            .Where(x => x.UploadBatchId == uploadBatchId && x.IsValid)
            .OrderBy(x => x.RowNo)
            .ToListAsync(ct);

        await EnsureImportedOperatorsAsync(validDetails, ct);

        var alreadyImported = batch.UploadType == "Variable"
            ? await db.VariableMeasurements.CountAsync(x => x.UploadBatchId == uploadBatchId, ct)
            : await db.AttributeMeasurements.CountAsync(x => x.UploadBatchId == uploadBatchId, ct);

        if (alreadyImported >= validDetails.Count)
        {
            batch.ImportStatus = "Imported";
            batch.ConfirmedAt ??= DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new { batch, imported = 0, spcCount = 0, alertCount = 0, resumed = true, alreadyImported };
        }

        var imported = 0;
        var spcCount = 0;
        var alertCount = 0;

        foreach (var detail in validDetails.Skip(alreadyImported))
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
                var measuredAt = TryDateTime(Get(payload, "MeasuredAt"), DateTime.UtcNow);
                var sampleNo = TryInt(Get(payload, "SampleNo"), 1);

                var existingVm = await db.VariableMeasurements.FirstOrDefaultAsync(x => 
                    x.PartProcessCharacteristicId == ctx.Mapping.Id && 
                    x.MeasuredAt == measuredAt && 
                    x.LotNo == Get(payload, "LotNo") &&
                    x.SampleNo == sampleNo, ct);

                if (existingVm != null)
                {
                    var oldAlerts = db.AlertEvents.Where(a => a.VariableMeasurementId == existingVm.Id);
                    db.AlertEvents.RemoveRange(oldAlerts);
                    var oldCalcs = db.SpcCalculationResults.Where(c => c.VariableMeasurementId == existingVm.Id);
                    db.SpcCalculationResults.RemoveRange(oldCalcs);
                    db.VariableMeasurements.Remove(existingVm);
                    await db.SaveChangesAsync(ct);
                }

                var vm = new VariableMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part?.Id ?? 0,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    LotNo = Get(payload, "LotNo"),
                    SerialNo = Get(payload, "SerialNo"),
                    LineId = ctx.Tank?.LineId,
                    TankId = ctx.Tank?.Id,
                    SampleNo = sampleNo,
                    MeasuredValue = measuredValue,
                    MeasuredAt = measuredAt,
                    Operator = Get(payload, "Operator"),
                    RecheckValue = double.TryParse(Get(payload, "RecheckValue"), out var rVal) ? rVal : null,
                    AdjustAction = Get(payload, "AdjustAction"),
                    AdjustAmount = double.TryParse(Get(payload, "AdjustAmount"), out var aVal) ? aVal : null
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
                var measuredAt = TryDateTime(Get(payload, "MeasuredAt"), DateTime.UtcNow);
                var sampleNo = TryInt(Get(payload, "SampleNo"), 1);

                var existingAm = await db.AttributeMeasurements.FirstOrDefaultAsync(x => 
                    x.PartProcessCharacteristicId == ctx.Mapping.Id && 
                    x.MeasuredAt == measuredAt && 
                    x.LotNo == Get(payload, "LotNo") &&
                    x.SampleNo == sampleNo, ct);

                if (existingAm != null)
                {
                    var oldAlerts = db.AlertEvents.Where(a => a.AttributeMeasurementId == existingAm.Id);
                    db.AlertEvents.RemoveRange(oldAlerts);
                    var oldCalcs = db.SpcCalculationResults.Where(c => c.AttributeMeasurementId == existingAm.Id);
                    db.SpcCalculationResults.RemoveRange(oldCalcs);
                    db.AttributeMeasurements.Remove(existingAm);
                    await db.SaveChangesAsync(ct);
                }

                var am = new AttributeMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part?.Id ?? 0,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    LotNo = Get(payload, "LotNo"),
                    LineId = ctx.Tank?.LineId,
                    TankId = ctx.Tank?.Id,
                    SampleNo = sampleNo,
                    InspectedQty = TryNullableInt(Get(payload, "InspectedQty")),
                    DefectQty = TryNullableInt(Get(payload, "DefectQty")),
                    DefectCount = TryNullableInt(Get(payload, "DefectCount")),
                    UnitCount = TryNullableInt(Get(payload, "UnitCount")),
                    MeasuredAt = measuredAt,
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

    private async Task EnsureImportedOperatorsAsync(IEnumerable<UploadDetail> validDetails, CancellationToken ct)
    {
        var operatorCodes = validDetails
            .Select(x => JsonSerializer.Deserialize<Dictionary<string, string?>>(x.PayloadJson) ?? [])
            .Select(x => Get(x, "Operator")?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var operatorCode in operatorCodes)
        {
            var code = operatorCode!;
            var exists = await db.Operators.AnyAsync(x => x.OperatorCode == code, ct);
            if (exists) continue;

            var user = ImportedOperatorFactory.Create(code);
            if (await db.Operators.AnyAsync(x => x.Username == code, ct)) user.Username = null;
            db.Operators.Add(user);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> DeleteBatchAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        if (batch is null) return false;
        var errors = db.UploadErrors.Where(x => x.UploadBatchId == uploadBatchId);
        db.UploadErrors.RemoveRange(errors);
        var details = db.UploadDetails.Where(x => x.UploadBatchId == uploadBatchId);
        db.UploadDetails.RemoveRange(details);
        db.UploadBatches.Remove(batch);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureMasterDataAsync(IEnumerable<Dictionary<string, string?>> rows, string expectedDataCategory, CancellationToken ct)
    {
        var chartTypeMeta = await db.ControlChartTypes.FirstOrDefaultAsync(x => x.ChartTypeCode == "XBAR_R" || x.ChartTypeCode == "I_MR" || x.DataCategory == expectedDataCategory, ct);

        foreach (var r in rows)
        {
            var partNo = Get(r, "PartNo");
            var scope = ResolveScope(r);
            var procCode = Get(r, "ProcessCode");
            var machCode = Get(r, "MachineCode");
            var charCode = Get(r, "CharacteristicCode");
            var charName = Get(r, "CharacteristicName") ?? charCode;

            if (scope == "CHEMICAL")
            {
                continue;
            }

            if (scope == "PRODUCT" && string.IsNullOrWhiteSpace(partNo)) continue;
            if (string.IsNullOrWhiteSpace(procCode) || string.IsNullOrWhiteSpace(charCode)) continue;

            if (string.IsNullOrWhiteSpace(machCode)) machCode = $"{procCode}-M01";

            Part? part = null;
            if (scope == "PRODUCT")
            {
                part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == partNo, ct);
                if (part is null)
                {
                    part = new Part { PartNo = partNo!, PartName = partNo!, IsEnabled = true };
                    db.Parts.Add(part);
                    await db.SaveChangesAsync(ct);
                }
            }

            var proc = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == procCode, ct);
            if (proc is null)
            {
                proc = new Process { ProcessCode = procCode, ProcessName = procCode, IsEnabled = true };
                db.Processes.Add(proc);
                await db.SaveChangesAsync(ct);
            }

            var mach = await db.Machines.FirstOrDefaultAsync(x => x.MachineCode == machCode, ct);
            if (mach is null)
            {
                mach = new Machine { MachineCode = machCode, MachineName = machCode, ProcessId = proc.Id, IsEnabled = true };
                db.Machines.Add(mach);
                await db.SaveChangesAsync(ct);
            }

            var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == mach.MachineCode, ct);
            if (line is null)
            {
                var factory = await db.Factories.FirstOrDefaultAsync(ct);
                if (factory is null)
                {
                    var plant = await db.Plants.FirstOrDefaultAsync(ct);
                    if (plant is null)
                    {
                        plant = new Plant { PlantCode = "PLT-01", PlantName = "Main Plant" };
                        db.Plants.Add(plant);
                        await db.SaveChangesAsync(ct);
                    }

                    factory = new Factory { FactoryCode = "FAC-01", FactoryName = "Main Factory", PlantId = plant.Id };
                    db.Factories.Add(factory);
                    await db.SaveChangesAsync(ct);
                }

                line = new ProductionLine
                {
                    LineCode = mach.MachineCode,
                    LineName = mach.MachineName,
                    FactoryId = factory.Id,
                    IsActive = mach.IsEnabled
                };
                db.ProductionLines.Add(line);
                await db.SaveChangesAsync(ct);
            }

            var chr = await db.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == charCode, ct);
            if (chr is null)
            {
                chr = new QualityCharacteristic
                {
                    CharacteristicCode = charCode,
                    CharacteristicName = charName ?? "",
                    DataCategory = expectedDataCategory,
                    DefaultChartTypeId = chartTypeMeta?.Id,
                    IsEnabled = true,
                    IsSpcEnabled = true
                };
                db.QualityCharacteristics.Add(chr);
                await db.SaveChangesAsync(ct);
            }

            int? mappingPartId = scope == "PRODUCT" ? part!.Id : null;
            var map = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x =>
                x.ControlScope == scope &&
                x.PartId == mappingPartId &&
                x.ProcessId == proc.Id &&
                x.CharacteristicId == chr.Id, ct);
            double.TryParse(Get(r, "USL"), out var uslVal);
            double.TryParse(Get(r, "LSL"), out var lslVal);

            if (map is null)
            {
                map = new PartProcessCharacteristic
                {
                    ControlScope = scope,
                    PartId = mappingPartId,
                    ProcessId = proc.Id,
                    MachineId = null,
                    TankId = null,
                    CharacteristicId = chr.Id,
                    USL = uslVal > 0 || lslVal > 0 ? uslVal : null,
                    LSL = uslVal > 0 || lslVal > 0 ? lslVal : null,
                    SampleSize = expectedDataCategory == "Variable" ? 1 : 1,
                    ChartTypeId = chartTypeMeta?.Id,
                    IsRequired = true,
                    IsEnabled = true
                };
                db.PartProcessCharacteristics.Add(map);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task BuildStagingAsync(UploadBatch batch, IEnumerable<Dictionary<string, string?>> rows, string expectedDataCategory, CancellationToken ct)
    {
        await EnsureMasterDataAsync(rows, expectedDataCategory, ct);

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
        var scope = ResolveScope(row);

        if (string.Equals(expectedDataCategory, "Variable", StringComparison.OrdinalIgnoreCase))
        {
            var measuredValue = Get(row, "MeasuredValue");
            if (string.IsNullOrWhiteSpace(measuredValue))
            {
                errors.Add(("MeasuredValue", "MEASURED_VALUE_REQUIRED", "MeasuredValue is required."));
            }
            else if (!double.TryParse(measuredValue, out _))
            {
                errors.Add(("MeasuredValue", "INVALID_MEASURED_VALUE", "MeasuredValue must be numeric."));
            }
        }
        else if (string.Equals(expectedDataCategory, "Attribute", StringComparison.OrdinalIgnoreCase))
        {
            AddIntegerErrorIfInvalid("InspectedQty", "INSPECTED_QTY_REQUIRED");
            AddIntegerErrorIfInvalid("DefectQty", "INVALID_DEFECT_QTY", required: false);
            AddIntegerErrorIfInvalid("DefectCount", "INVALID_DEFECT_COUNT", required: false);
            AddIntegerErrorIfInvalid("UnitCount", "INVALID_UNIT_COUNT", required: false);
        }

        Part? part = null;
        if (scope == "PRODUCT")
        {
            part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(row, "PartNo"), ct);
            if (part is null) errors.Add(("PartNo", "PART_NOT_FOUND", "Product control rows require an existing PartNo."));
        }
        var process = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == Get(row, "ProcessCode"), ct);
        if (process is null) errors.Add(("ProcessCode", "PROCESS_NOT_FOUND", "ProcessCode does not exist."));
        var machine = await db.Machines.FirstOrDefaultAsync(x => x.MachineCode == Get(row, "MachineCode"), ct);
        if (machine is null) errors.Add(("MachineCode", "MACHINE_NOT_FOUND", "MachineCode does not exist."));
        else if (process is not null && machine.ProcessId != process.Id)
        {
            errors.Add(("MachineCode", "MACHINE_PROCESS_MISMATCH", "MachineCode does not belong to the specified ProcessCode."));
        }

        Tank? tank = null;
        if (scope == "CHEMICAL")
        {
            var tankName = Get(row, "TankCode");
            if (string.IsNullOrWhiteSpace(tankName))
            {
                errors.Add(("TankCode", "TANK_REQUIRED", "Chemical control rows require TankCode/槽位."));
            }
            else if (machine is not null)
            {
                tank = await FindTankAsync(machine, tankName, ct);
                if (tank is null) errors.Add(("TankCode", "TANK_NOT_FOUND", "TankCode/槽位 does not exist for the specified MachineCode."));
            }
        }

        var characteristic = await db.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == Get(row, "CharacteristicCode"), ct);
        if (characteristic is null) errors.Add(("CharacteristicCode", "CHAR_NOT_FOUND", "CharacteristicCode does not exist."));
        if (characteristic is not null && !string.Equals(characteristic.DataCategory, expectedDataCategory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(("DataCategory", "INVALID_DATA_CATEGORY", $"Characteristic data category should be {expectedDataCategory}."));
        }

        if ((scope != "PRODUCT" || part is not null) && process is not null && characteristic is not null)
        {
            int? mappingPartId = scope == "PRODUCT" ? part!.Id : null;
            var mappingQuery = db.PartProcessCharacteristics.Where(x =>
                    x.ControlScope == scope &&
                    x.PartId == mappingPartId &&
                    x.ProcessId == process.Id &&
                    x.CharacteristicId == characteristic.Id &&
                    x.IsEnabled);

            if (scope == "CHEMICAL")
            {
                if (machine is not null) mappingQuery = mappingQuery.Where(x => x.MachineId == machine.Id);
                if (tank is not null) mappingQuery = mappingQuery.Where(x => x.TankId == tank.Id);
            }
            else
            {
                mappingQuery = mappingQuery.Where(x => x.TankId == null);
            }

            var mapping = await mappingQuery.FirstOrDefaultAsync(ct);
            if (mapping is null)
            {
                errors.Add(("PartProcessCharacteristic", "MAPPING_NOT_FOUND", scope == "CHEMICAL"
                    ? "Chemical master data does not exist for ProcessCode + MachineCode + TankCode + CharacteristicCode."
                    : "ControlScope + optional Part + Process + Characteristic mapping does not exist."));
            }
        }

        return errors;

        void AddIntegerErrorIfInvalid(string field, string code, bool required = true)
        {
            var raw = Get(row, field);
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (required) errors.Add((field, code, $"{field} is required."));
                return;
            }

            if (!int.TryParse(raw, out _))
            {
                errors.Add((field, code, $"{field} must be an integer."));
            }
        }
    }

    private async Task<(Part? Part, Process Process, Machine Machine, Tank? Tank, QualityCharacteristic Characteristic, PartProcessCharacteristic Mapping)?> ResolveReferencesAsync(Dictionary<string, string?> payload, CancellationToken ct)
    {
        var scope = ResolveScope(payload);
        Part? part = null;
        if (scope == "PRODUCT")
        {
            part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(payload, "PartNo"), ct);
        }
        var process = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == Get(payload, "ProcessCode"), ct);
        var machine = await db.Machines.FirstOrDefaultAsync(x => x.MachineCode == Get(payload, "MachineCode"), ct);
        var characteristic = await db.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == Get(payload, "CharacteristicCode"), ct);
        if ((scope == "PRODUCT" && part is null) || process is null || machine is null || characteristic is null) return null;
        var tank = scope == "CHEMICAL" ? await FindTankAsync(machine, Get(payload, "TankCode"), ct) : null;
        if (scope == "CHEMICAL" && tank is null) return null;
        int? mappingPartId = scope == "PRODUCT" ? part!.Id : null;
        var mappingQuery = db.PartProcessCharacteristics.Where(x =>
                x.ControlScope == scope &&
                x.PartId == mappingPartId &&
                x.ProcessId == process.Id &&
                x.CharacteristicId == characteristic.Id &&
                x.IsEnabled);
        if (scope == "CHEMICAL")
        {
            mappingQuery = mappingQuery.Where(x => x.MachineId == machine.Id && x.TankId == tank!.Id);
        }
        else
        {
            mappingQuery = mappingQuery.Where(x => x.TankId == null);
        }

        var mapping = await mappingQuery.FirstOrDefaultAsync(ct);
        if (mapping is null) return null;
        return (part, process, machine, tank, characteristic, mapping);
    }

    private static string ResolveScope(Dictionary<string, string?> row)
    {
        var raw = Get(row, "ControlScope") ?? Get(row, "管制類型");
        var normalized = (raw ?? "").Trim().ToUpperInvariant();
        if (normalized is "PROCESS" or "PROC" or "製程" or "製程管制") return "PROCESS";
        if (normalized is "CHEMICAL" or "CHEM" or "藥水" or "藥液" or "藥水管制" or "藥液管制") return "CHEMICAL";
        if (normalized is "PRODUCT" or "PROD" or "產品" or "產品管制") return "PRODUCT";
        return string.IsNullOrWhiteSpace(Get(row, "PartNo")) ? "PROCESS" : "PRODUCT";
    }

    private static string? Get(Dictionary<string, string?> row, string key)
    {
        if (row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val)) return val;
        var altKey = key.ToLowerInvariant() switch
        {
            "partno" => "料號",
            "controlscope" => "管制類型",
            "processcode" => "製程",
            "machinecode" => "機台",
            "tankcode" => "槽位",
            "characteristiccode" => "檢驗項目",
            "characteristicname" => "檢驗項目名稱",
            "usl" => "上限",
            "lsl" => "下限",
            "measuredvalue" => "測量值",
            "measuredat" => "日期",
            "operator" => "作業員",
            "lotno" => "lot",
            "serialno" => "工單",
            "sampleno" => "樣本編號",
            "inspectedqty" => "總數",
            "defectqty" => "不良數",
            "defectcount" => "缺點數",
            "unitcount" => "單位數",
            "recheckvalue" => "複驗",
            "adjustaction" => "調整",
            "adjustamount" => "調整量",
            _ => null
        };
        if (altKey != null && row.TryGetValue(altKey, out val) && !string.IsNullOrWhiteSpace(val)) return val;
        var matchedKey = row.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase) || (altKey != null && k.Equals(altKey, StringComparison.OrdinalIgnoreCase)));
        if (matchedKey != null && row.TryGetValue(matchedKey, out val) && !string.IsNullOrWhiteSpace(val)) return val;
        return null;
    }
    private static int TryInt(string? raw, int fallback) => int.TryParse(raw, out var value) ? value : fallback;
    private static int? TryNullableInt(string? raw) => int.TryParse(raw, out var value) ? value : null;
    private static DateTime TryDateTime(string? raw, DateTime fallback) => DateTime.TryParse(raw, out var value) ? value : fallback;

    private async Task<Tank?> FindTankAsync(Machine machine, string? tankNameOrCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tankNameOrCode)) return null;
        var raw = tankNameOrCode.Trim();
        var prefixedCode = raw.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"{machine.MachineCode}-{raw}";

        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == machine.MachineCode, ct);
        if (line is null) return null;

        return await db.Tanks.FirstOrDefaultAsync(x =>
            x.LineId == line.Id &&
            (x.TankCode == raw || x.TankCode == prefixedCode || x.TankName == raw), ct);
    }
}
