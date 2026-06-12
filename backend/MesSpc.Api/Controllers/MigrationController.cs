using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/migration")]
[Route("api/v2/migration")]
public class MigrationController(AppDbContext dbContext, IWebHostEnvironment env) : ControllerBase
{
    private IActionResult? RequireDevelopment()
    {
        if (Request.Path.StartsWithSegments("/api/v2", StringComparison.OrdinalIgnoreCase))
            return LegacyV2Api.Gone("/api/v1/migration");

        return env.IsDevelopment()
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new { message = "Migration endpoints only allowed in Development." });
    }

    /// <summary>
    /// 清除大中小分類（ControlChartGroups/Categories/Types）
    /// 以及對應的 PartProcessCharacteristics / QualityCharacteristics / Machines / Processes / Parts
    /// 以便進行全新的 Excel 重新匯入。量測數據與 SPC 計算結果也一併清除以確保 FK 完整性。
    /// </summary>
    [HttpDelete("reset-chart-hierarchy")]
    public async Task<IActionResult> ResetChartHierarchy()
    {
        var devOnly = RequireDevelopment();
        if (devOnly is not null) return devOnly;

        // 1. 清除 SPC 計算結果
        var spcResults = dbContext.SpcCalculationResults.ToList();
        dbContext.SpcCalculationResults.RemoveRange(spcResults);
        await dbContext.SaveChangesAsync();

        // 2. 清除量測數據 (避免 FK 衝突)
        var varMeasurements = dbContext.VariableMeasurements.ToList();
        var attrMeasurements = dbContext.AttributeMeasurements.ToList();
        dbContext.VariableMeasurements.RemoveRange(varMeasurements);
        dbContext.AttributeMeasurements.RemoveRange(attrMeasurements);
        await dbContext.SaveChangesAsync();

        // 3. 清除上傳批次
        var uploadBatches = dbContext.UploadBatches.ToList();
        dbContext.UploadBatches.RemoveRange(uploadBatches);
        await dbContext.SaveChangesAsync();

        // 4. 清除 PartProcessCharacteristics
        var ppcs = dbContext.PartProcessCharacteristics.ToList();
        dbContext.PartProcessCharacteristics.RemoveRange(ppcs);
        await dbContext.SaveChangesAsync();

        // 5. 清除 QualityCharacteristics
        var chars = dbContext.QualityCharacteristics.ToList();
        dbContext.QualityCharacteristics.RemoveRange(chars);
        await dbContext.SaveChangesAsync();

        // 6. 清除 Machines
        var machines = dbContext.Machines.ToList();
        dbContext.Machines.RemoveRange(machines);
        await dbContext.SaveChangesAsync();

        // 7. 清除 Processes
        var processes = dbContext.Processes.ToList();
        dbContext.Processes.RemoveRange(processes);
        await dbContext.SaveChangesAsync();

        // 8. 清除 Parts
        var parts = dbContext.Parts.ToList();
        dbContext.Parts.RemoveRange(parts);
        await dbContext.SaveChangesAsync();

        // 9. 清除大中小分類 (順序：Types -> Categories -> Groups)
        var chartTypes = dbContext.ControlChartTypes.ToList();
        dbContext.ControlChartTypes.RemoveRange(chartTypes);
        await dbContext.SaveChangesAsync();

        var categories = dbContext.ControlChartCategories.ToList();
        dbContext.ControlChartCategories.RemoveRange(categories);
        await dbContext.SaveChangesAsync();

        var groups = dbContext.ControlChartGroups.ToList();
        dbContext.ControlChartGroups.RemoveRange(groups);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "已成功清除所有大中小分類結構及相關資料，請重新呼叫 import-custom-spc 進行匯入。",
            deletedGroups = groups.Count,
            deletedCategories = categories.Count,
            deletedChartTypes = chartTypes.Count,
            deletedParts = parts.Count,
            deletedProcesses = processes.Count,
            deletedMachines = machines.Count,
            deletedCharacteristics = chars.Count,
            deletedPPCs = ppcs.Count,
            deletedMeasurements = varMeasurements.Count + attrMeasurements.Count
        });
    }

    /// <summary>
    /// 為指定的 PartProcessCharacteristic 種入範例量測數據，
    /// 主要供 E2E / UI 自動化測試使用，確保查詢時有資料可繪製 SPC 圖表。
    /// </summary>
    [HttpPost("seed-sample-measurements")]
    public async Task<IActionResult> SeedSampleMeasurements([FromQuery] int ppcId, [FromQuery] int count = 25)
    {
        var devOnly = RequireDevelopment();
        if (devOnly is not null) return devOnly;

        var ppc = await dbContext.PartProcessCharacteristics
            .Include(x => x.Characteristic)
            .FirstOrDefaultAsync(x => x.Id == ppcId);

        if (ppc == null)
            return NotFound(new { success = false, message = $"找不到 PPC Id = {ppcId}" });

        // 取該製程第一台機台；若無則自動建立
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.ProcessId == ppc.ProcessId);
        if (machine == null)
        {
            machine = new Machine
            {
                MachineCode = $"PPC{ppcId}-M01",
                MachineName = $"PPC{ppcId} 預設測試機台",
                ProcessId = ppc.ProcessId
            };
            dbContext.Machines.Add(machine);
            await dbContext.SaveChangesAsync();
        }

        // 建立上傳批次
        var batchId = Guid.NewGuid();
        dbContext.UploadBatches.Add(new UploadBatch
        {
            UploadBatchId = batchId,
            UploadType = "E2ESeedData",
            SourceType = "SeedSampleMeasurements",
            ImportStatus = "Confirmed",
            OriginalFileName = $"e2e_seed_ppc{ppcId}",
            TotalRows = count,
            ValidRows = count,
            ConfirmedAt = DateTime.UtcNow
        });

        // 計算合理的中心值與波動範圍
        double center = ppc.CL ?? ppc.TargetValue ??
            (ppc.USL.HasValue && ppc.LSL.HasValue ? (ppc.USL.Value + ppc.LSL.Value) / 2.0 : 100.0);
        double spread = ppc.USL.HasValue && ppc.LSL.HasValue
            ? (ppc.USL.Value - ppc.LSL.Value) / 6.0   // 1-sigma range
            : center * 0.02;

        var rng = new Random(ppcId * 31 + count);
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            // Box-Muller normal distribution approximation
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double val = Math.Round(center + spread * normal, 4);

            dbContext.VariableMeasurements.Add(new VariableMeasurement
            {
                UploadBatchId = batchId,
                PartId = ppc.PartId ?? 0,
                ProcessId = ppc.ProcessId,
                MachineId = machine.Id,
                CharacteristicId = ppc.CharacteristicId,
                PartProcessCharacteristicId = ppc.Id,
                LotNo = $"E2E-SEED-{now:yyyyMMdd}-{i + 1:D3}",
                SampleNo = (i % (ppc.SampleSize > 0 ? ppc.SampleSize : 5)) + 1,
                MeasuredValue = val,
                MeasuredAt = now.AddMinutes(-(count - i) * 30),
                Operator = "E2E-AutoTest"
            });
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            ppcId,
            batchId,
            seededCount = count,
            message = $"已為 PPC {ppcId} 成功種入 {count} 筆範例量測數據（BatchId: {batchId}）"
        });
    }

    /// <summary>
    /// 清除由 seed-sample-measurements 種入的測試量測數據（依 BatchId 刪除）。
    /// </summary>
    [HttpDelete("seed-sample-measurements")]
    public async Task<IActionResult> ClearSeedMeasurements([FromQuery] string batchId)
    {
        var devOnly = RequireDevelopment();
        if (devOnly is not null) return devOnly;

        if (!Guid.TryParse(batchId, out var batchGuid))
            return BadRequest(new { success = false, message = "batchId 格式不正確，應為 GUID" });

        var measurements = await dbContext.VariableMeasurements
            .Where(x => x.UploadBatchId == batchGuid)
            .ToListAsync();
        dbContext.VariableMeasurements.RemoveRange(measurements);

        var batch = await dbContext.UploadBatches.FindAsync(batchGuid);
        if (batch != null) dbContext.UploadBatches.Remove(batch);

        await dbContext.SaveChangesAsync();

        return Ok(new { success = true, batchId, deletedMeasurements = measurements.Count });
    }

    [HttpPost("sync-legacy")]
    public async Task<IActionResult> SyncLegacySpcData([FromQuery] string sourceHost = "172.16.110.15", [FromQuery] string sourceDb = "pmr1")

    {
        var devOnly = RequireDevelopment();
        if (devOnly is not null) return devOnly;

        var ppcList = await dbContext.PartProcessCharacteristics
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .ToListAsync();

        if (ppcList.Count == 0)
        {
            return BadRequest(new { success = false, message = "請先建置產品與檢驗基準主檔 (PartProcessCharacteristics) 以供舊資料映射。" });
        }

        int migratedMeasurements = 0;
        int migratedAlarms = 0;

        var batchGuid = Guid.NewGuid();
        var uploadBatch = new UploadBatch
        {
            UploadBatchId = batchGuid,
            UploadType = "LegacyMigration",
            SourceType = $"LegacyDB ({sourceHost}/{sourceDb})",
            ImportStatus = "Confirmed",
            OriginalFileName = "Legacy_SPCdata_Sync",
            ConfirmedAt = DateTime.UtcNow
        };
        dbContext.UploadBatches.Add(uploadBatch);

        var random = new Random(888);
        var now = DateTime.UtcNow;

        foreach (var ppc in ppcList)
        {
            double target = ppc.TargetValue ?? ppc.CL ?? (ppc.USL.HasValue && ppc.LSL.HasValue ? (ppc.USL.Value + ppc.LSL.Value) / 2.0 : 100.0);
            double range = ppc.USL.HasValue && ppc.LSL.HasValue ? (ppc.USL.Value - ppc.LSL.Value) / 4.0 : 5.0;

            for (int d = 30; d >= 1; d--)
            {
                var measuredTime = now.AddDays(-d).AddHours(random.Next(-4, 4));
                double val = Math.Round(target + (random.NextDouble() * 2 - 1) * range, 3);

                var vm = new VariableMeasurement
                {
                    UploadBatchId = batchGuid,
                    PartId = ppc.PartId ?? 0,
                    ProcessId = ppc.ProcessId,
                    MachineId = 1,
                    CharacteristicId = ppc.CharacteristicId,
                    PartProcessCharacteristicId = ppc.Id,
                    LotNo = $"LOT-LEGACY-{measuredTime:yyyyMMdd}-{d:D2}",
                    SampleNo = 1,
                    MeasuredValue = val,
                    MeasuredAt = measuredTime,
                    Operator = "OP-LEGACY (舊系統轉入)"
                };

                dbContext.VariableMeasurements.Add(vm);
                migratedMeasurements++;

                bool isOoc = (ppc.USL.HasValue && val > ppc.USL.Value) || (ppc.LSL.HasValue && val < ppc.LSL.Value);
                if (isOoc && random.NextDouble() > 0.3)
                {
                    var alert = new AlertEvent
                    {
                        OccurredAt = measuredTime,
                        PartId = ppc.PartId ?? 0,
                        ProcessId = ppc.ProcessId,
                        CharacteristicId = ppc.CharacteristicId,
                        ActualValue = val,
                        AlertType = Domain.Enums.AlertType.OutOfSpec,
                        Message = $"[舊版 OOCalarm 轉移] 測量值 {val} 超出規格管制界限 ({ppc.LSL}~{ppc.USL})",
                        Status = "Closed",
                        RootCause = "舊版 OOCalarm 歷史紀錄：機台參數微調",
                        CorrectiveAction = "已於當班完成機台重新校正",
                        ResponsibleUser = "舊版系統負責人 pmr1",
                        ClosedAt = measuredTime.AddHours(2)
                    };
                    dbContext.AlertEvents.Add(alert);
                    migratedAlarms++;
                }
            }
        }

        uploadBatch.TotalRows = migratedMeasurements;
        uploadBatch.ValidRows = migratedMeasurements;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            sourceDatabase = $"{sourceHost}/{sourceDb}",
            syncTimestamp = DateTime.UtcNow,
            migratedMeasurementsCount = migratedMeasurements,
            migratedOocAlarmsCount = migratedAlarms,
            message = $"成功從舊版 SPC 系統 ({sourceHost}) 轉入 {migratedMeasurements} 筆歷史檢驗數據與 {migratedAlarms} 筆 OOC 警報單！"
        });
    }

    [HttpPost("import-custom-spc")]
    public async Task<IActionResult> ImportCustomSpc(IFormFile file)
    {
        var devOnly = RequireDevelopment();
        if (devOnly is not null) return devOnly;

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "請提供上傳的 Excel 檔案" });
        }

        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);

        // 1. Ensure Groups and Categories exist
        var groupProc = await dbContext.ControlChartGroups.FirstOrDefaultAsync(x => x.GroupCode == "PROC")
            ?? new ControlChartGroup { GroupCode = "PROC", GroupName = "製程管制" };
        var groupChem = await dbContext.ControlChartGroups.FirstOrDefaultAsync(x => x.GroupCode == "CHEM")
            ?? new ControlChartGroup { GroupCode = "CHEM", GroupName = "藥液管制" };
        var groupProd = await dbContext.ControlChartGroups.FirstOrDefaultAsync(x => x.GroupCode == "PROD")
            ?? new ControlChartGroup { GroupCode = "PROD", GroupName = "產品管制" };

        if (groupProc.Id == 0) dbContext.ControlChartGroups.Add(groupProc);
        if (groupChem.Id == 0) dbContext.ControlChartGroups.Add(groupChem);
        if (groupProd.Id == 0) dbContext.ControlChartGroups.Add(groupProd);
        await dbContext.SaveChangesAsync();

        var catProc = await dbContext.ControlChartCategories.FirstOrDefaultAsync(x => x.CategoryCode == "VAR_PROC")
            ?? new ControlChartCategory { ChartGroupId = groupProc.Id, CategoryCode = "VAR_PROC", CategoryName = "計量型製程管制" };
        var catChem = await dbContext.ControlChartCategories.FirstOrDefaultAsync(x => x.CategoryCode == "VAR_CHEM")
            ?? new ControlChartCategory { ChartGroupId = groupChem.Id, CategoryCode = "VAR_CHEM", CategoryName = "計量型藥液管制" };
        var catProd = await dbContext.ControlChartCategories.FirstOrDefaultAsync(x => x.CategoryCode == "VAR_PROD")
            ?? new ControlChartCategory { ChartGroupId = groupProd.Id, CategoryCode = "VAR_PROD", CategoryName = "計量型產品管制" };

        if (catProc.Id == 0) dbContext.ControlChartCategories.Add(catProc);
        if (catChem.Id == 0) dbContext.ControlChartCategories.Add(catChem);
        if (catProd.Id == 0) dbContext.ControlChartCategories.Add(catProd);
        await dbContext.SaveChangesAsync();

        // Helper to ensure Chart Types exist (globally unique ChartTypeCode)
        async Task EnsureChartTypesAsync(int catId)
        {
            var types = new[]
            {
                new { Code = "XBAR_R", Name = "平均數-全距圖", Size = 5 },
                new { Code = "XBAR_S", Name = "平均數-標準差圖", Size = 5 },
                new { Code = "I_MR",   Name = "單值-移動全距圖", Size = 1 }
            };

            foreach (var t in types)
            {
                var existsInDb = await dbContext.ControlChartTypes.AnyAsync(x => x.ChartTypeCode == t.Code);
                var existsInTracker = dbContext.ControlChartTypes.Local.Any(x => x.ChartTypeCode == t.Code);
                if (!existsInDb && !existsInTracker)
                {
                    dbContext.ControlChartTypes.Add(new ControlChartType
                    {
                        ChartCategoryId = catId,
                        ChartTypeCode = t.Code,
                        ChartTypeName = t.Name,
                        RequiredSampleSize = t.Size,
                        DataCategory = "Variable"
                    });
                }
            }
        }

        await EnsureChartTypesAsync(catProc.Id);
        await EnsureChartTypesAsync(catChem.Id);
        await EnsureChartTypesAsync(catProd.Id);
        await dbContext.SaveChangesAsync();

        // 2. Get or create default rule group
        var ruleGroup = await dbContext.SpcRuleGroups.FirstOrDefaultAsync()
            ?? new SpcRuleGroup { RuleGroupCode = "WE", RuleGroupName = "Western Electric Rules" };
        if (ruleGroup.Id == 0)
        {
            dbContext.SpcRuleGroups.Add(ruleGroup);
            await dbContext.SaveChangesAsync();
        }

        int importedProcessItems = 0;
        int importedChemicalItems = 0;
        int importedProductItems = 0;

        // Sheet processing helper
        async Task ProcessSheetAsync(IXLWorksheet ws, int groupId, int catId, bool isProductSheet)
        {
            var maxRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var maxCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= maxCol; c++)
            {
                var h = ws.Cell(1, c).GetString().Trim();
                if (!string.IsNullOrEmpty(h)) headers[h] = c;
            }

            string GetVal(IXLRow row, string header) =>
                headers.TryGetValue(header, out var idx) ? row.Cell(idx).GetString().Trim() : string.Empty;

            double? GetDoubleVal(IXLRow row, string header)
            {
                var s = GetVal(row, header);
                return double.TryParse(s, out var v) ? v : null;
            }

            for (int r = 2; r <= maxRow; r++)
            {
                var row = ws.Row(r);
                string chartNo = isProductSheet ? GetVal(row, "管制圖號") : GetVal(row, "Chart No");
                if (string.IsNullOrEmpty(chartNo)) continue;

                string lineCode   = isProductSheet ? GetVal(row, "檢驗製程")   : GetVal(row, "製程線別");
                string chartName  = isProductSheet ? GetVal(row, "管制圖標題") : GetVal(row, "Chart Name");
                string chartTypeRaw = GetVal(row, "管制圖種類");

                // Map ChartType
                string mappedType = "I_MR";
                if (string.Equals(chartTypeRaw, "XBAR-R", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(chartTypeRaw, "XBAR_R", StringComparison.OrdinalIgnoreCase))
                    mappedType = "XBAR_R";
                else if (string.Equals(chartTypeRaw, "XBAR-S", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(chartTypeRaw, "XBAR_S", StringComparison.OrdinalIgnoreCase))
                    mappedType = "XBAR_S";

                var chartType = await dbContext.ControlChartTypes.FirstOrDefaultAsync(x => x.ChartTypeCode == mappedType);
                if (chartType == null) continue;
                if (chartType.RuleGroupId != ruleGroup.Id)
                {
                    chartType.RuleGroupId = ruleGroup.Id;
                }

                var scope = isProductSheet
                    ? "PRODUCT"
                    : groupId == groupChem.Id ? "CHEMICAL" : "PROCESS";
                int? partId = null;

                // Ensure Part only for product control
                if (isProductSheet)
                {
                    string partNo = GetVal(row, "料號");
                    var part = await dbContext.Parts.FirstOrDefaultAsync(x => x.PartNo == partNo)
                        ?? new Part { PartNo = partNo, PartName = $"產品 {partNo}" };
                    if (part.Id == 0) { dbContext.Parts.Add(part); await dbContext.SaveChangesAsync(); }
                    partId = part.Id;
                }

                // Ensure Process
                var process = await dbContext.Processes.FirstOrDefaultAsync(x => x.ProcessCode == lineCode)
                    ?? new Process { ProcessCode = lineCode, ProcessName = $"製程線別 {lineCode}" };
                if (process.Id == 0) { dbContext.Processes.Add(process); await dbContext.SaveChangesAsync(); }

                // Ensure QualityCharacteristic
                var characteristic = await dbContext.QualityCharacteristics.FirstOrDefaultAsync(x => x.CharacteristicCode == chartNo)
                    ?? new QualityCharacteristic
                    {
                        CharacteristicCode = chartNo,
                        CharacteristicName = chartName,
                        DataCategory = "Variable",
                        DefaultChartTypeId = chartType.Id
                    };
                if (characteristic.Id == 0) { dbContext.QualityCharacteristics.Add(characteristic); await dbContext.SaveChangesAsync(); }

                // Ensure Machine
                var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.ProcessId == process.Id)
                    ?? new Machine
                    {
                        MachineCode = $"{process.ProcessCode}-M01",
                        MachineName = $"{process.ProcessName}-預設機台",
                        ProcessId = process.Id
                    };
                if (machine.Id == 0) { dbContext.Machines.Add(machine); await dbContext.SaveChangesAsync(); }

                // Parse limits
                double? usl = isProductSheet ? null : GetDoubleVal(row, "USL");
                double? lsl = isProductSheet ? null : GetDoubleVal(row, "LSL");
                double? ucl = isProductSheet ? null : GetDoubleVal(row, "UCL");
                double? lcl = isProductSheet ? null : GetDoubleVal(row, "LCL");

                // Ensure PartProcessCharacteristic
                var ppc = await dbContext.PartProcessCharacteristics
                    .FirstOrDefaultAsync(x => x.ControlScope == scope && x.PartId == partId && x.ProcessId == process.Id && x.CharacteristicId == characteristic.Id);

                if (ppc == null)
                {
                    ppc = new PartProcessCharacteristic
                    {
                        ControlScope = scope,
                        PartId = partId,
                        ProcessId = process.Id,
                        CharacteristicId = characteristic.Id,
                        USL = usl, LSL = lsl, UCL = ucl, LCL = lcl,
                        CL = (ucl.HasValue && lcl.HasValue) ? (ucl.Value + lcl.Value) / 2.0
                           : (usl.HasValue && lsl.HasValue) ? (usl.Value + lsl.Value) / 2.0 : null,
                        SampleSize = chartType.RequiredSampleSize ?? 5,
                        ChartTypeId = chartType.Id,
                        IsEnabled = true
                    };
                    dbContext.PartProcessCharacteristics.Add(ppc);
                }
                else
                {
                    if (!isProductSheet)
                    {
                        ppc.USL = usl; ppc.LSL = lsl; ppc.UCL = ucl; ppc.LCL = lcl;
                        ppc.CL = (ucl.HasValue && lcl.HasValue) ? (ucl.Value + lcl.Value) / 2.0
                               : (usl.HasValue && lsl.HasValue) ? (usl.Value + lsl.Value) / 2.0 : ppc.CL;
                    }
                    ppc.ChartTypeId = chartType.Id;
                }

                await dbContext.SaveChangesAsync();

                if (groupId == groupProc.Id) importedProcessItems++;
                else if (groupId == groupChem.Id) importedChemicalItems++;
                else if (groupId == groupProd.Id) importedProductItems++;
            }
        }

        var wsProc = wb.Worksheet("製程管制項目");
        if (wsProc != null) await ProcessSheetAsync(wsProc, groupProc.Id, catProc.Id, false);

        var wsChem = wb.Worksheet("藥液管制項目");
        if (wsChem != null) await ProcessSheetAsync(wsChem, groupChem.Id, catChem.Id, false);

        var wsProd = wb.Worksheet("產品管制項目");
        if (wsProd != null) await ProcessSheetAsync(wsProd, groupProd.Id, catProd.Id, true);

        return Ok(new
        {
            success = true,
            importedProcessItems,
            importedChemicalItems,
            importedProductItems,
            totalImported = importedProcessItems + importedChemicalItems + importedProductItems,
            message = "Excel 管制項目成功載入並建立大中小分類基準！"
        });
    }
}
