using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Services.Security;

namespace MesSpc.Api.Services;

public class UploadService(AppDbContext db, SpcService spcService)
{
    public async Task<UploadBatch> CreateVariableBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, string? fileHash = null, CancellationToken ct = default, Guid? uploadBatchId = null)
    {
        if (!string.IsNullOrEmpty(fileHash))
        {
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
            UploadBatchId = uploadBatchId ?? Guid.NewGuid(),
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

    public async Task<UploadBatch> CreateAttributeBatchAsync(IEnumerable<Dictionary<string, string?>> rows, string sourceType, string? createdBy, string? fileName, string? fileHash = null, CancellationToken ct = default, Guid? uploadBatchId = null)
    {
        if (!string.IsNullOrEmpty(fileHash))
        {
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
            UploadBatchId = uploadBatchId ?? Guid.NewGuid(),
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
        var invalidDetails = await db.UploadDetails
            .Where(x => x.UploadBatchId == uploadBatchId)
            .Where(x => !x.IsValid)
            .OrderBy(x => x.RowNo)
            .ToListAsync(ct);
        var firstValidDetail = await db.UploadDetails
            .Where(x => x.UploadBatchId == uploadBatchId && x.IsValid)
            .OrderBy(x => x.RowNo)
            .FirstOrDefaultAsync(ct);
        var details = firstValidDetail is null
            ? invalidDetails
            : invalidDetails.Append(firstValidDetail).ToList();
        var errors = await db.UploadErrors.Where(x => x.UploadBatchId == uploadBatchId).OrderBy(x => x.RowNo).ToListAsync(ct);
        return new { batch, details, errors };
    }

    public async Task<object?> GetProgressAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        return await db.UploadBatches.AsNoTracking()
            .Where(x => x.UploadBatchId == uploadBatchId)
            .Select(x => new
            {
                x.UploadBatchId,
                total = x.TotalRows,
                processed = x.ValidRows + x.ErrorRows,
                valid = x.ValidRows,
                errors = x.ErrorRows,
                x.ImportStatus
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<object?> CreateMissingMappingsAndRevalidateAsync(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FirstOrDefaultAsync(x => x.UploadBatchId == uploadBatchId, ct);
        if (batch is null) return null;
        if (batch.ImportStatus != "PreviewReady")
            throw new InvalidOperationException("只有尚未確認的匯入批次可以補建設定。");

        var missingDetailIds = await db.UploadErrors
            .Where(x => x.UploadBatchId == uploadBatchId &&
                (x.ErrorCode == "MAPPING_NOT_FOUND" || x.ErrorCode == "CHAR_NOT_FOUND") &&
                x.UploadDetailId != null)
            .Select(x => x.UploadDetailId!.Value)
            .Distinct()
            .ToListAsync(ct);
        var details = await db.UploadDetails.Where(x => missingDetailIds.Contains(x.Id)).ToListAsync(ct);
        var created = 0;
        var createdCharacteristics = 0;
        var reusedMappings = 0;
        var skippedMissingMasterData = 0;
        var skippedMissingTank = 0;
        var skippedMissingChartType = 0;
        var createdTanks = 0;
        var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var detail in details)
        {
            var row = JsonSerializer.Deserialize<Dictionary<string, string?>>(detail.PayloadJson) ?? [];
            var scope = ResolveScope(row);
            var group = await FindScopeGroupAsync(scope, ct);
            if (group is null) { skippedMissingMasterData++; continue; }
            var process = await FindProcessAsync(Get(row, "ProcessCode"), ct);
            var machine = await FindMachineAsync(Get(row, "MachineCode"), process, ct);
            if (process is null && machine is not null)
                process = await db.Processes.FirstOrDefaultAsync(x => x.Id == machine.ProcessId, ct);
            if (process is null || (group.RequiresMachine && machine is null)) { skippedMissingMasterData++; continue; }
            var tank = group.RequiresTank && machine is not null
                ? await FindTankAsync(machine, Get(row, "TankCode"), ct)
                : null;
            if (group.RequiresTank && machine is not null && tank is null)
            {
                var tankResult = await CreateTankIfNoSimilarAsync(machine, Get(row, "TankCode"), ct);
                tank = tankResult.Tank;
                if (tankResult.Created) createdTanks++;
            }
            if (group.RequiresTank && tank is null) { skippedMissingTank++; continue; }
            var characteristic = await FindMappedCharacteristicAsync(
                Get(row, "CharacteristicCode"), Get(row, "Unit"), scope,
                process, machine, tank, group.RequiresMachine, group.RequiresTank, ct)
                ?? await FindCharacteristicAsync(Get(row, "CharacteristicCode"), Get(row, "Unit"), scope, ct);
            if (characteristic is null)
            {
                var rawName = Get(row, "CharacteristicCode")?.Trim();
                if (string.IsNullOrWhiteSpace(rawName)) continue;
                var unit = Get(row, "Unit")?.Trim();
                var cleanName = string.Join(" / ", rawName.Replace("\r", "\n")
                    .Split(['\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                var baseCode = !string.IsNullOrWhiteSpace(unit) && !cleanName.Contains($"({unit})", StringComparison.OrdinalIgnoreCase)
                    ? $"{cleanName} ({unit})"
                    : cleanName;
                characteristic = await db.QualityCharacteristics.FirstOrDefaultAsync(x =>
                    x.ControlScope == scope &&
                    (x.CharacteristicCode == baseCode || x.CharacteristicCode == $"{baseCode}_CHEM"), ct);
                if (characteristic is null)
                {
                    var code = baseCode;
                    var suffix = 0;
                    while (await db.QualityCharacteristics.AnyAsync(x => x.CharacteristicCode == code, ct))
                    {
                        suffix++;
                        code = suffix == 1 ? $"{baseCode}_CHEM" : $"{baseCode}_CHEM{suffix}";
                    }
                    characteristic = new QualityCharacteristic
                    {
                        CharacteristicCode = code,
                        CharacteristicName = cleanName,
                        ControlScope = scope,
                        DataCategory = "Variable",
                        Unit = unit,
                        InputMode = "DIRECT",
                        ValueLabel = "量測值",
                        DecimalPlaces = 3,
                        IsSpcEnabled = true,
                        IsEnabled = true
                    };
                    db.QualityCharacteristics.Add(characteristic);
                    await db.SaveChangesAsync(ct);
                    createdCharacteristics++;
                }
            }

            var key = $"{scope}|{process.Id}|{machine?.Id}|{tank?.Id}|{characteristic.Id}";
            if (!handled.Add(key)) continue;
            var exists = await db.PartProcessCharacteristics.AnyAsync(x =>
                x.ControlScope == scope && x.PartId == null && x.ProcessId == process.Id &&
                x.MachineId == (group.RequiresMachine ? machine!.Id : null) &&
                x.TankId == (group.RequiresTank ? tank!.Id : null) &&
                x.CharacteristicId == characteristic.Id && x.IsEnabled, ct);
            if (exists) { reusedMappings++; continue; }

            var template = await db.PartProcessCharacteristics
                .Where(x => x.ControlScope == scope && x.CharacteristicId == characteristic.Id && x.IsEnabled)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);
            var chartType = template?.ChartTypeId is not null
                ? await db.ControlChartTypes.FirstOrDefaultAsync(x => x.Id == template.ChartTypeId, ct)
                : await db.ControlChartTypes
                    .Where(x => x.ChartGroupId == group.Id && x.IsEnabled && x.DataCategory == "Variable")
                    .OrderBy(x => x.ChartTypeCode == "I_MR" ? 0 : x.RequiredSampleSize == 1 ? 1 : 2)
                    .FirstOrDefaultAsync(ct);
            if (chartType is null) { skippedMissingChartType++; continue; }
            characteristic.DefaultChartTypeId ??= chartType.Id;

            db.PartProcessCharacteristics.Add(new PartProcessCharacteristic
            {
                ControlScope = scope,
                PartId = null,
                ProcessId = process.Id,
                MachineId = group.RequiresMachine ? machine!.Id : null,
                TankId = group.RequiresTank ? tank!.Id : null,
                CharacteristicId = characteristic.Id,
                Unit = template?.Unit ?? characteristic.Unit ?? Get(row, "Unit"),
                USL = template?.USL,
                LSL = template?.LSL,
                UCL = template?.UCL,
                CL = template?.CL,
                LCL = template?.LCL,
                TargetValue = template?.TargetValue,
                SampleSize = template?.SampleSize ?? 1,
                DisplayMode = template?.DisplayMode ?? "CONTROL_CHART",
                ChartTypeId = chartType.Id,
                FormulaConfigJson = template?.FormulaConfigJson,
                RuleGroupId = template?.RuleGroupId ?? chartType.RuleGroupId,
                IsRequired = true,
                IsEnabled = true
            });
            await db.SaveChangesAsync(ct);
            created++;
        }

        await RevalidateBatchAsync(batch, ct);
        return new
        {
            createdMappings = created,
            createdCharacteristics,
            reusedMappings,
            skippedMissingMasterData,
            skippedMissingTank,
            skippedMissingChartType,
            createdTanks,
            batch.ValidRows,
            batch.ErrorRows
        };
    }

    private async Task RevalidateBatchAsync(UploadBatch batch, CancellationToken ct)
    {
        var uploadBatchId = batch.UploadBatchId;
        var allDetails = await db.UploadDetails.Where(x => x.UploadBatchId == uploadBatchId).OrderBy(x => x.RowNo).ToListAsync(ct);
        var oldErrors = await db.UploadErrors.Where(x => x.UploadBatchId == uploadBatchId).ToListAsync(ct);
        db.UploadErrors.RemoveRange(oldErrors);
        batch.ImportStatus = "Revalidating";
        batch.TotalRows = allDetails.Count;
        batch.ValidRows = 0;
        batch.ErrorRows = 0;
        await db.SaveChangesAsync(ct);
        var processed = 0;
        foreach (var detail in allDetails)
        {
            var row = JsonSerializer.Deserialize<Dictionary<string, string?>>(detail.PayloadJson) ?? [];
            var validationErrors = await ValidateRowAsync(row, batch.UploadType, ct);
            detail.IsValid = validationErrors.Count == 0;
            if (detail.IsValid)
            {
                var resolved = await ResolveReferencesAsync(row, ct);
                if (resolved is not null)
                {
                    row["ResolvedProcessCode"] = resolved.Value.Process.ProcessCode;
                    row["ResolvedMachineCode"] = resolved.Value.Machine?.MachineCode;
                    row["ResolvedTankCode"] = resolved.Value.Tank?.TankCode;
                    row["ResolvedCharacteristicCode"] = resolved.Value.Characteristic.CharacteristicCode;
                    detail.PayloadJson = JsonSerializer.Serialize(row);
                }
            }
            foreach (var error in validationErrors)
                db.UploadErrors.Add(new UploadError
                {
                    UploadBatchId = uploadBatchId,
                    UploadDetailId = detail.Id,
                    RowNo = detail.RowNo,
                    FieldName = error.Field,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });
            if (detail.IsValid) batch.ValidRows++;
            else batch.ErrorRows++;
            processed++;
            if (processed % 10 == 0) await db.SaveChangesAsync(ct);
        }
        await db.SaveChangesAsync(ct);
        batch.ImportStatus = "PreviewReady";
        await db.SaveChangesAsync(ct);
    }

    public async Task<object?> ConfirmAsync(Guid uploadBatchId, string mode = "upsert", CancellationToken ct = default)
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
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
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
                ApplyImportedSpecification(payload, ctx.Mapping);
                var measuredAt = TryDateTime(Get(payload, "MeasuredAt"), DateTime.UtcNow);
                var sampleNo = TryInt(Get(payload, "SampleNo"), 1);

                var existingVm = await db.VariableMeasurements.FirstOrDefaultAsync(x => 
                    x.PartProcessCharacteristicId == ctx.Mapping.Id && 
                    x.MeasuredAt == measuredAt && 
                    x.LotNo == Get(payload, "LotNo") &&
                    x.SampleNo == sampleNo, ct);

                if (existingVm != null)
                {
                    if (mode == "insertOnly")
                    {
                        skipped++;
                        continue;
                    }
                    var oldAlerts = db.AlertEvents.Where(a => a.VariableMeasurementId == existingVm.Id);
                    db.AlertEvents.RemoveRange(oldAlerts);
                    var oldCalcs = db.SpcCalculationResults.Where(c => c.VariableMeasurementId == existingVm.Id);
                    db.SpcCalculationResults.RemoveRange(oldCalcs);
                    db.VariableMeasurements.Remove(existingVm);
                    await db.SaveChangesAsync(ct);
                    updated++;
                }
                else inserted++;

                var vm = new VariableMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part?.Id ?? 0,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine?.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    WorkOrderNo = Get(payload, "WorkOrderNo"),
                    LotNo = Get(payload, "LotNo"),
                    SerialNo = Get(payload, "SerialNo"),
                    LineId = ctx.Tank?.LineId,
                    TankId = ctx.Tank?.Id,
                    SlotId = ctx.Slot?.Id,
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
                    if (mode == "insertOnly")
                    {
                        skipped++;
                        continue;
                    }
                    var oldAlerts = db.AlertEvents.Where(a => a.AttributeMeasurementId == existingAm.Id);
                    db.AlertEvents.RemoveRange(oldAlerts);
                    var oldCalcs = db.SpcCalculationResults.Where(c => c.AttributeMeasurementId == existingAm.Id);
                    db.SpcCalculationResults.RemoveRange(oldCalcs);
                    db.AttributeMeasurements.Remove(existingAm);
                    await db.SaveChangesAsync(ct);
                    updated++;
                }
                else inserted++;

                var am = new AttributeMeasurement
                {
                    UploadBatchId = batch.UploadBatchId,
                    PartId = ctx.Part?.Id ?? 0,
                    ProcessId = ctx.Process.Id,
                    MachineId = ctx.Machine?.Id,
                    CharacteristicId = ctx.Characteristic.Id,
                    PartProcessCharacteristicId = ctx.Mapping.Id,
                    LotNo = Get(payload, "LotNo"),
                    LineId = ctx.Tank?.LineId,
                    TankId = ctx.Tank?.Id,
                    SlotId = ctx.Slot?.Id,
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
        return new { batch, mode, imported, inserted, updated, skipped, spcCount, alertCount };
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

            if (scope == "CHEM")
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
        var rowList = rows.ToList();
        batch.TotalRows = rowList.Count;
        batch.ValidRows = 0;
        batch.ErrorRows = 0;
        await db.SaveChangesAsync(ct);
        await EnsureMasterDataAsync(rowList, expectedDataCategory, ct);

        var rowNo = 0;
        var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rowList)
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
            else
            {
                var resolved = await ResolveReferencesAsync(row, ct);
                if (resolved is not null)
                {
                    var ctx = resolved.Value;
                    row["ResolvedProcessCode"] = ctx.Process.ProcessCode;
                    row["ResolvedMachineCode"] = ctx.Machine?.MachineCode;
                    row["ResolvedTankCode"] = ctx.Tank?.TankCode;
                    row["ResolvedCharacteristicCode"] = ctx.Characteristic.CharacteristicCode;

                    var measuredAt = TryDateTime(Get(row, "MeasuredAt"), DateTime.UtcNow);
                    var sampleNo = TryInt(Get(row, "SampleNo"), 1);
                    var lotNo = Get(row, "LotNo") ?? "";
                    var duplicateKey = $"{ctx.Mapping.Id}|{measuredAt:O}|{lotNo}|{sampleNo}";
                    var duplicateInFile = !batchKeys.Add(duplicateKey);
                    var duplicateInDatabase = expectedDataCategory == "Variable"
                        ? await db.VariableMeasurements.AnyAsync(x =>
                            x.PartProcessCharacteristicId == ctx.Mapping.Id &&
                            x.MeasuredAt == measuredAt &&
                            (x.LotNo ?? "") == lotNo &&
                            x.SampleNo == sampleNo, ct)
                        : await db.AttributeMeasurements.AnyAsync(x =>
                            x.PartProcessCharacteristicId == ctx.Mapping.Id &&
                            x.MeasuredAt == measuredAt &&
                            (x.LotNo ?? "") == lotNo &&
                            x.SampleNo == sampleNo, ct);
                    row["DuplicateStatus"] = duplicateInFile
                        ? "同一匯入檔內重複"
                        : duplicateInDatabase ? "正式資料已存在" : "";
                    detail.PayloadJson = JsonSerializer.Serialize(row);
                }
            }
            if (detail.IsValid) batch.ValidRows++;
            else batch.ErrorRows++;
            await db.SaveChangesAsync(ct);
        }

        batch.TotalRows = rowNo;
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<(string Field, string Code, string Message)>> ValidateRowAsync(Dictionary<string, string?> row, string expectedDataCategory, CancellationToken ct)
    {
        var errors = new List<(string Field, string Code, string Message)>();
        var scope = ResolveScope(row);
        var scopeGroup = await FindScopeGroupAsync(scope, ct);
        if (scopeGroup is null)
            errors.Add(("ControlScope", "SCOPE_NOT_CONFIGURED", "ControlScope has no enabled control-chart group configuration."));
        var requiresPart = scopeGroup?.RequiresPart ?? scope == "PRODUCT";
        var requiresMachine = scopeGroup?.RequiresMachine ?? true;
        var requiresTank = scopeGroup?.RequiresTank ?? scope == "CHEM";

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
        if (requiresPart)
        {
            part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(row, "PartNo"), ct);
            if (part is null) errors.Add(("PartNo", "PART_NOT_FOUND", "Product control rows require an existing PartNo."));
        }
        var machineCode = Get(row, "MachineCode");
        var process = await FindProcessAsync(Get(row, "ProcessCode"), ct);
        var machine = await FindMachineAsync(machineCode, process, ct);
        if (process is null && machine is not null)
            process = await db.Processes.FirstOrDefaultAsync(x => x.Id == machine.ProcessId, ct);
        if (process is null) errors.Add(("ProcessCode", "PROCESS_NOT_FOUND", "製程代碼／名稱不存在，且無法由線別取得所屬製程。"));
        if (requiresMachine && machine is null) errors.Add(("MachineCode", "MACHINE_NOT_FOUND", "線別代碼／名稱不存在。"));
        else if (process is not null && machine is not null && machine.ProcessId != process.Id)
        {
            errors.Add(("MachineCode", "MACHINE_PROCESS_MISMATCH", "MachineCode does not belong to the specified ProcessCode."));
        }

        Tank? tank = null;
        if (requiresTank)
        {
            var tankName = Get(row, "TankCode");
            if (string.IsNullOrWhiteSpace(tankName))
            {
                errors.Add(("TankCode", "TANK_REQUIRED", "Chemical control rows require TankCode/槽位."));
            }
            else if (machine is not null)
            {
                tank = await FindTankAsync(machine, tankName, ct);
                if (tank is null) errors.Add(("TankCode", "TANK_NOT_FOUND", "此線別底下找不到指定槽位。"));
            }
        }

        var characteristic = await FindMappedCharacteristicAsync(
            Get(row, "CharacteristicCode"), Get(row, "Unit"), scope,
            process, machine, tank, requiresMachine, requiresTank, ct)
            ?? await FindCharacteristicAsync(Get(row, "CharacteristicCode"), Get(row, "Unit"), scope, ct);
        if (characteristic is null) errors.Add(("CharacteristicCode", "CHAR_NOT_FOUND", "管制項目代碼／名稱不存在，請先建立品質特性。"));
        if (characteristic is not null && !string.Equals(characteristic.DataCategory, expectedDataCategory, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(("DataCategory", "INVALID_DATA_CATEGORY", $"Characteristic data category should be {expectedDataCategory}."));
        }

        if ((!requiresPart || part is not null) &&
            (!requiresMachine || machine is not null) &&
            (!requiresTank || tank is not null) &&
            process is not null &&
            characteristic is not null)
        {
            int? mappingPartId = requiresPart ? part!.Id : null;
            var mappingQuery = db.PartProcessCharacteristics.Where(x =>
                    x.ControlScope == scope &&
                    x.PartId == mappingPartId &&
                    x.ProcessId == process.Id &&
                    x.CharacteristicId == characteristic.Id &&
                    x.IsEnabled);

            mappingQuery = mappingQuery.Where(x => x.MachineId == (requiresMachine ? machine!.Id : null));
            mappingQuery = mappingQuery.Where(x => x.TankId == (requiresTank ? tank!.Id : null));

            var mapping = await mappingQuery.FirstOrDefaultAsync(ct);
            if (mapping is null)
            {
                errors.Add(("PartProcessCharacteristic", "MAPPING_NOT_FOUND", "此線別、槽位與管制項目尚未建立品質特性設定。"));
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

    private async Task<(Part? Part, Process Process, Machine? Machine, Tank? Tank, Slot? Slot, QualityCharacteristic Characteristic, PartProcessCharacteristic Mapping)?> ResolveReferencesAsync(Dictionary<string, string?> payload, CancellationToken ct)
    {
        var scope = ResolveScope(payload);
        var scopeGroup = await FindScopeGroupAsync(scope, ct);
        var requiresPart = scopeGroup?.RequiresPart ?? scope == "PRODUCT";
        var requiresMachine = scopeGroup?.RequiresMachine ?? true;
        var requiresTank = scopeGroup?.RequiresTank ?? scope == "CHEM";
        Part? part = null;
        if (requiresPart)
        {
            part = await db.Parts.FirstOrDefaultAsync(x => x.PartNo == Get(payload, "PartNo"), ct);
        }
        var process = await FindProcessAsync(Get(payload, "ProcessCode"), ct);
        var machine = await FindMachineAsync(Get(payload, "MachineCode"), process, ct);
        if (process is null && machine is not null)
            process = await db.Processes.FirstOrDefaultAsync(x => x.Id == machine.ProcessId, ct);
        if ((requiresPart && part is null) || process is null || (requiresMachine && machine is null)) return null;
        var tank = requiresTank && machine is not null ? await FindTankAsync(machine, Get(payload, "TankCode"), ct) : null;
        if (requiresTank && tank is null) return null;
        var characteristic = await FindMappedCharacteristicAsync(
            Get(payload, "CharacteristicCode"), Get(payload, "Unit"), scope,
            process, machine, tank, requiresMachine, requiresTank, ct)
            ?? await FindCharacteristicAsync(Get(payload, "CharacteristicCode"), Get(payload, "Unit"), scope, ct);
        if (characteristic is null) return null;
        Slot? slot = null;
        var slotCode = Get(payload, "SlotCode");
        if (!string.IsNullOrWhiteSpace(slotCode))
        {
            if (tank is null) return null;
            slot = await db.Slots.FirstOrDefaultAsync(x => x.TankId == tank.Id && x.SlotCode == slotCode, ct);
            if (slot is null) return null;
        }
        int? mappingPartId = requiresPart ? part!.Id : null;
        var mappingQuery = db.PartProcessCharacteristics.Where(x =>
                x.ControlScope == scope &&
                x.PartId == mappingPartId &&
                x.ProcessId == process.Id &&
                x.CharacteristicId == characteristic.Id &&
                x.IsEnabled);
        mappingQuery = mappingQuery.Where(x => x.MachineId == (requiresMachine ? machine!.Id : null));
        mappingQuery = mappingQuery.Where(x => x.TankId == (requiresTank ? tank!.Id : null));

        var mapping = slot is null
            ? await mappingQuery.Where(x => x.SlotId == null).FirstOrDefaultAsync(ct)
            : await mappingQuery.Where(x => x.SlotId == slot.Id || x.SlotId == null)
                .OrderByDescending(x => x.SlotId == slot.Id).FirstOrDefaultAsync(ct);
        if (mapping is null) return null;
        return (part, process, machine, tank, slot, characteristic, mapping);
    }

    private static string ResolveScope(Dictionary<string, string?> row)
    {
        var raw = Get(row, "ControlScope") ?? Get(row, "管制類型");
        var normalized = (raw ?? "").Trim().ToUpperInvariant();
        if (normalized is "PROCESS" or "PROC" or "製程" or "製程管制") return "PROCESS";
        if (normalized is "CHEMICAL" or "CHEM" or "藥水" or "藥液" or "藥水管制" or "藥液管制") return "CHEM";
        if (normalized is "PRODUCT" or "PROD" or "產品" or "產品管制") return "PRODUCT";
        if (!string.IsNullOrWhiteSpace(normalized)) return normalized;
        return string.IsNullOrWhiteSpace(Get(row, "PartNo")) ? "PROCESS" : "PRODUCT";
    }

    private Task<ControlChartGroup?> FindScopeGroupAsync(string scope, CancellationToken ct)
        => db.ControlChartGroups.AsNoTracking()
            .Where(x => x.IsEnabled && x.GroupType == "CONTROL_CHART" && x.BusinessScopeCode == scope)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

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
            "specification" => "規格",
            "specificationrange" => "範圍",
            _ => null
        };
        if (altKey != null && row.TryGetValue(altKey, out val) && !string.IsNullOrWhiteSpace(val)) return val;
        var matchedKey = row.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase) || (altKey != null && k.Equals(altKey, StringComparison.OrdinalIgnoreCase)));
        if (matchedKey != null && row.TryGetValue(matchedKey, out val) && !string.IsNullOrWhiteSpace(val)) return val;
        return null;
    }

    private static void ApplyImportedSpecification(
        Dictionary<string, string?> payload,
        PartProcessCharacteristic mapping)
    {
        if (ResolveScope(payload) != "CHEM") return;

        var parsed = SpecificationRangeParser.Parse(
            Get(payload, "Specification"),
            Get(payload, "SpecificationRange"));

        if (parsed.TargetValue.HasValue) mapping.TargetValue = parsed.TargetValue;
        if (parsed.Lsl.HasValue) mapping.LSL = parsed.Lsl;
        if (parsed.Usl.HasValue) mapping.USL = parsed.Usl;
    }
    private static int TryInt(string? raw, int fallback) => int.TryParse(raw, out var value) ? value : fallback;
    private static int? TryNullableInt(string? raw) => int.TryParse(raw, out var value) ? value : null;
    private static DateTime TryDateTime(string? raw, DateTime fallback) => DateTime.TryParse(raw, out var value) ? value : fallback;

    private async Task<Process?> FindProcessAsync(string? codeOrName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codeOrName)) return null;
        var value = codeOrName.Trim();
        var byCode = await db.Processes.FirstOrDefaultAsync(x => x.ProcessCode == value, ct);
        if (byCode is not null) return byCode;
        var byName = await db.Processes.Where(x => x.ProcessName == value).Take(2).ToListAsync(ct);
        if (byName.Count == 1) return byName[0];

        var normalized = NormalizeMasterLookup(value, "PROCESS");
        if (normalized.Length < 2) return null;
        var fuzzyMatches = (await db.Processes.Where(x => x.IsEnabled).ToListAsync(ct))
            .Where(x => IsUniqueFuzzyMatch(
                normalized,
                NormalizeMasterLookup(x.ProcessCode, "PROCESS"),
                NormalizeMasterLookup(x.ProcessName, "PROCESS")))
            .Take(2)
            .ToList();
        return fuzzyMatches.Count == 1 ? fuzzyMatches[0] : null;
    }

    private async Task<Machine?> FindMachineAsync(string? codeOrName, Process? process, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codeOrName)) return null;
        var value = codeOrName.Trim();
        var codeQuery = db.Machines.Where(x => x.MachineCode == value);
        if (process is not null) codeQuery = codeQuery.Where(x => x.ProcessId == process.Id);
        var byCode = await codeQuery.FirstOrDefaultAsync(ct);
        if (byCode is not null) return byCode;

        var legacyCode = $"{value}1";
        if (!value.EndsWith("1", StringComparison.OrdinalIgnoreCase))
        {
            var legacyQuery = db.Machines.Where(x => x.MachineCode == legacyCode);
            if (process is not null) legacyQuery = legacyQuery.Where(x => x.ProcessId == process.Id);
            var legacyMachine = await legacyQuery.FirstOrDefaultAsync(ct);
            if (legacyMachine is not null) return legacyMachine;
        }

        var nameQuery = db.Machines.Where(x => x.MachineName == value);
        if (process is not null) nameQuery = nameQuery.Where(x => x.ProcessId == process.Id);
        var byName = await nameQuery.Take(2).ToListAsync(ct);
        if (byName.Count == 1) return byName[0];

        var normalized = NormalizeMasterLookup(value, "MACHINE");
        if (normalized.Length < 2) return null;
        var fuzzyQuery = db.Machines.Where(x => x.IsEnabled);
        if (process is not null) fuzzyQuery = fuzzyQuery.Where(x => x.ProcessId == process.Id);
        var fuzzyMatches = (await fuzzyQuery.ToListAsync(ct))
            .Where(x => IsUniqueFuzzyMatch(
                normalized,
                NormalizeMasterLookup(x.MachineCode, "MACHINE"),
                NormalizeMasterLookup(x.MachineName, "MACHINE")))
            .Take(2)
            .ToList();
        return fuzzyMatches.Count == 1 ? fuzzyMatches[0] : null;
    }

    private static bool IsUniqueFuzzyMatch(string input, params string[] candidates)
        => candidates.Any(candidate =>
            candidate == input ||
            candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith(candidate, StringComparison.OrdinalIgnoreCase) ||
            candidate.Contains(input, StringComparison.OrdinalIgnoreCase) ||
            input.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeMasterLookup(string? value, string kind)
    {
        var normalized = string.Concat((value ?? "").Trim().ToUpperInvariant().Where(char.IsLetterOrDigit));
        if (kind == "PROCESS")
            normalized = normalized.Replace("製程", "", StringComparison.OrdinalIgnoreCase);
        else
            normalized = normalized
                .Replace("線別", "", StringComparison.OrdinalIgnoreCase)
                .Replace("機台", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('線');
        return normalized;
    }

    private async Task<QualityCharacteristic?> FindCharacteristicAsync(string? codeOrName, string? unit, string scope, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codeOrName)) return null;
        var value = codeOrName.Trim();
        var scopedQuery = db.QualityCharacteristics.Where(x => x.ControlScope == scope && x.IsEnabled);
        var byCode = await scopedQuery.FirstOrDefaultAsync(x => x.CharacteristicCode == value, ct);
        if (byCode is not null) return byCode;
        var cleanName = string.Join(" / ", value.Replace("\r", "\n")
            .Split(['\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var normalizedUnit = NormalizeUnit(unit);
        var importedCode = !string.IsNullOrWhiteSpace(unit) && !cleanName.Contains($"({unit})", StringComparison.OrdinalIgnoreCase)
            ? $"{cleanName} ({unit})"
            : cleanName;
        var importedMatch = await scopedQuery.FirstOrDefaultAsync(x =>
            x.CharacteristicCode == importedCode || x.CharacteristicCode == $"{importedCode}_CHEM", ct);
        if (importedMatch is not null) return importedMatch;
        var byName = await scopedQuery.Where(x => x.CharacteristicName == value).Take(2).ToListAsync(ct);
        if (byName.Count == 1) return byName[0];

        var inputTokens = GetCharacteristicTokens(value);
        var candidates = (await scopedQuery.ToListAsync(ct))
            .Where(x => GetCharacteristicTokens(x.CharacteristicCode)
                    .Concat(GetCharacteristicTokens(x.CharacteristicName))
                    .Any(candidate => inputTokens.Contains(candidate, StringComparer.OrdinalIgnoreCase)))
            .ToList();
        if (candidates.Count == 1) return candidates[0];

        if (!string.IsNullOrWhiteSpace(normalizedUnit))
        {
            var unitMatches = candidates.Where(x =>
                NormalizeUnit(x.Unit) == normalizedUnit ||
                GetParenthesizedUnits(x.CharacteristicCode).Contains(normalizedUnit, StringComparer.OrdinalIgnoreCase) ||
                GetParenthesizedUnits(x.CharacteristicName).Contains(normalizedUnit, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (unitMatches.Count == 1) return unitMatches[0];
        }
        return null;
    }

    private async Task<QualityCharacteristic?> FindMappedCharacteristicAsync(
        string? codeOrName,
        string? unit,
        string scope,
        Process? process,
        Machine? machine,
        Tank? tank,
        bool requiresMachine,
        bool requiresTank,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codeOrName) || process is null ||
            (requiresMachine && machine is null) || (requiresTank && tank is null))
            return null;

        var mappings = await db.PartProcessCharacteristics
            .Include(x => x.Characteristic)
            .Where(x => x.ControlScope == scope && x.ProcessId == process.Id &&
                x.MachineId == (requiresMachine ? machine!.Id : null) &&
                x.TankId == (requiresTank ? tank!.Id : null) &&
                x.IsEnabled && x.Characteristic != null && x.Characteristic.IsEnabled)
            .ToListAsync(ct);
        var inputTokens = ExpandChemicalAliases(GetCharacteristicTokens(codeOrName));
        var candidates = mappings
            .Select(x => x.Characteristic!)
            .Where(x => ExpandChemicalAliases(
                    GetCharacteristicTokens(x.CharacteristicCode)
                        .Concat(GetCharacteristicTokens(x.CharacteristicName))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList())
                .Any(token => inputTokens.Contains(token, StringComparer.OrdinalIgnoreCase)))
            .DistinctBy(x => x.Id)
            .ToList();
        if (candidates.Count == 1) return candidates[0];

        var normalizedUnit = NormalizeUnit(unit);
        if (!string.IsNullOrWhiteSpace(normalizedUnit))
        {
            var unitMatches = candidates.Where(x =>
                NormalizeUnit(x.Unit) == normalizedUnit ||
                GetParenthesizedUnits(x.CharacteristicCode).Contains(normalizedUnit, StringComparer.OrdinalIgnoreCase) ||
                GetParenthesizedUnits(x.CharacteristicName).Contains(normalizedUnit, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (unitMatches.Count == 1) return unitMatches[0];
        }
        return null;
    }

    private static List<string> GetCharacteristicTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        return value.Replace("\r", "\n")
            .Split(['\n', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(line => new[] { line, line.Split('(')[0].Trim() })
            .Select(NormalizeCharacteristicToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ExpandChemicalAliases(List<string> tokens)
    {
        var expanded = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            var aliases = token.ToUpperInvariant() switch
            {
                "H2SO4" => new[] { "硫酸" },
                "HCL" => new[] { "鹽酸" },
                "CU2+" => new[] { "銅離子" },
                "H2O2" => new[] { "雙氧水" },
                "KOH" => new[] { "氫氧化鉀" },
                "CL-" => new[] { "氯離子" },
                "NA2CO3" => new[] { "碳酸鈉" },
                "HNO3" => new[] { "硝酸" },
                "SPS" => new[] { "過硫酸鈉" },
                _ => []
            };
            foreach (var alias in aliases) expanded.Add(NormalizeCharacteristicToken(alias));
        }
        return expanded.ToList();
    }

    private static string NormalizeCharacteristicToken(string value)
        => string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    private static string NormalizeUnit(string? value)
        => string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c))).Trim('(', ')').ToUpperInvariant();

    private static List<string> GetParenthesizedUnits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var matches = System.Text.RegularExpressions.Regex.Matches(value, @"\(([^()]*)\)");
        return matches.Select(x => NormalizeUnit(x.Groups[1].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private async Task<Tank?> FindTankAsync(Machine machine, string? tankNameOrCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tankNameOrCode)) return null;
        var raw = tankNameOrCode.Trim();
        var prefixedCode = raw.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"{machine.MachineCode}-{raw}";

        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == machine.MachineCode, ct);
        if (line is null) return null;

        var exactMatch = await db.Tanks.FirstOrDefaultAsync(x =>
            x.LineId == line.Id &&
            (x.TankCode == raw || x.TankCode == prefixedCode || x.TankName == raw), ct);
        if (exactMatch is not null) return exactMatch;

        var normalizedRaw = NormalizeTankName(raw);
        if (normalizedRaw.Length < 2) return null;
        var tanks = await db.Tanks.Where(x => x.LineId == line.Id && x.IsActive).ToListAsync(ct);
        var fuzzyMatches = tanks.Where(x =>
        {
            var normalizedName = NormalizeTankName(x.TankName);
            var codeWithoutLine = x.TankCode.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
                ? x.TankCode[(machine.MachineCode.Length + 1)..]
                : x.TankCode;
            var normalizedCode = NormalizeTankName(codeWithoutLine);
            return normalizedName == normalizedRaw ||
                   normalizedCode == normalizedRaw ||
                   normalizedName.StartsWith(normalizedRaw, StringComparison.OrdinalIgnoreCase) ||
                   normalizedCode.StartsWith(normalizedRaw, StringComparison.OrdinalIgnoreCase);
        }).Take(2).ToList();
        return fuzzyMatches.Count == 1 ? fuzzyMatches[0] : null;
    }

    private async Task<(Tank? Tank, bool Created)> CreateTankIfNoSimilarAsync(
        Machine machine, string? tankNameOrCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tankNameOrCode)) return (null, false);
        var raw = tankNameOrCode.Trim();
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == machine.MachineCode, ct);
        if (line is null) return (null, false);

        var normalizedRaw = NormalizeTankName(raw);
        var tanks = await db.Tanks.Where(x => x.LineId == line.Id).ToListAsync(ct);
        var similar = tanks.Where(x =>
        {
            var normalizedName = NormalizeTankName(x.TankName);
            var codeWithoutLine = x.TankCode.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
                ? x.TankCode[(machine.MachineCode.Length + 1)..]
                : x.TankCode;
            var normalizedCode = NormalizeTankName(codeWithoutLine);
            return normalizedName.Contains(normalizedRaw, StringComparison.OrdinalIgnoreCase) ||
                   normalizedRaw.Contains(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                   normalizedCode.Contains(normalizedRaw, StringComparison.OrdinalIgnoreCase) ||
                   normalizedRaw.Contains(normalizedCode, StringComparison.OrdinalIgnoreCase);
        }).Take(2).ToList();
        if (similar.Count == 1) return (similar[0], false);
        if (similar.Count > 1) return (null, false);

        var tankCode = raw.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"{machine.MachineCode}-{raw}";
        var existingByCode = await db.Tanks.FirstOrDefaultAsync(x => x.TankCode == tankCode, ct);
        if (existingByCode is not null)
            return existingByCode.LineId == line.Id ? (existingByCode, false) : (null, false);

        var tank = new Tank
        {
            LineId = line.Id,
            TankCode = tankCode,
            TankName = raw,
            Description = "由匯入批次自動建立",
            IsActive = true
        };
        db.Tanks.Add(tank);
        await db.SaveChangesAsync(ct);
        return (tank, true);
    }

    private static string NormalizeTankName(string? value)
    {
        var normalized = string.Concat((value ?? "").Where(c =>
            !char.IsWhiteSpace(c) && c is not '-' and not '_' and not '(' and not ')'))
            .Replace("TANK", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return normalized == "表處" ? "表面處理" : normalized;
    }
}
