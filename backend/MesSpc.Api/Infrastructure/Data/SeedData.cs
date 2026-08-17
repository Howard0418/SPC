using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (!db.FormulaDefinitions.Any())
        {
            db.FormulaDefinitions.AddRange(
                New("AVG", "Average", "AVG(values)"),
                New("STDEV", "Standard Deviation", "STDEV(values)"),
                New("RANGE", "Range", "MAX(values)-MIN(values)"),
                New("MAX", "Max", "MAX(values)"),
                New("MIN", "Min", "MIN(values)"),
                New("COUNT", "Count", "COUNT(values)"),
                New("SUM", "Sum", "SUM(values)"),
                New("CP", "Process Capability", "(USL-LSL)/(6*STDEV(values))"),
                New("CPK", "Process Capability Index", "MIN((USL-AVG(values))/(3*STDEV(values)),(AVG(values)-LSL)/(3*STDEV(values)))")
            );
            db.SaveChanges();
        }

        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { ProductCode = "P-1001", ProductName = "Housing A" },
                new Product { ProductCode = "P-1002", ProductName = "Bracket B" }
            );
            db.SaveChanges();
        }

        if (!db.Stations.Any())
        {
            db.Stations.AddRange(
                new Station { StationCode = "ST-01", StationName = "Incoming QC" },
                new Station { StationCode = "ST-02", StationName = "Assembly Line 1" }
            );
            db.SaveChanges();
        }

        if (!db.InspectionItems.Any())
        {
            db.InspectionItems.AddRange(
                new InspectionItem
                {
                    ItemCode = "LEN-001",
                    ItemName = "Length",
                    DataType = DataType.Numeric,
                    Unit = "mm",
                    Usl = 10.2,
                    Lsl = 9.8,
                    Ucl = 10.1,
                    Lcl = 9.9,
                    TargetValue = 10.0,
                    IsSpcEnabled = true
                },
                new InspectionItem
                {
                    ItemCode = "WID-001",
                    ItemName = "Width",
                    DataType = DataType.Numeric,
                    Unit = "mm",
                    Usl = 5.2,
                    Lsl = 4.8,
                    Ucl = 5.1,
                    Lcl = 4.9,
                    TargetValue = 5.0,
                    IsSpcEnabled = true
                }
            );
            db.SaveChanges();
        }

        if (!db.ProductStationItems.Any())
        {
            var product = db.Products.OrderBy(p => p.Id).First();
            var station = db.Stations.OrderBy(s => s.Id).First();
            var items = db.InspectionItems.Select(x => x.Id).ToList();
            db.ProductStationItems.AddRange(items.Select(itemId => new ProductStationItem
            {
                ProductId = product.Id,
                StationId = station.Id,
                InspectionItemId = itemId,
                SampleSize = 5,
                IsActive = true
            }));
            db.SaveChanges();
        }

        // --- New Refactored Enterprise & SPC Seeding ---
        if (!db.ControlChartGroups.Any())
        {
            var grp = new ControlChartGroup { GroupCode = "STD", GroupName = "標準品質管制圖" };
            db.ControlChartGroups.Add(grp);
            db.SaveChanges();

            db.ControlChartTypes.AddRange(
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "XBAR_R", ChartTypeName = "平均數-全距圖", DataCategory = "Variable", RequiredSampleSize = 5 },
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "I_MR", ChartTypeName = "單值-移動全距圖", DataCategory = "Variable", RequiredSampleSize = 1 },
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "P", ChartTypeName = "不良率圖", DataCategory = "Attribute" },
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "NP", ChartTypeName = "不良品數圖", DataCategory = "Attribute" },
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "C", ChartTypeName = "缺點數圖", DataCategory = "Attribute" },
                new ControlChartType { ChartGroupId = grp.Id, ChartTypeCode = "U", ChartTypeName = "單位缺點數圖", DataCategory = "Attribute" }
            );
            db.SaveChanges();
        }

        // Existing databases may predate Xbar-S. Keep the fixed algorithm catalog complete.
        if (!db.ControlChartTypes.Any(x => x.ChartTypeCode == "XBAR_S"))
        {
            var controlGroup = db.ControlChartGroups
                .OrderByDescending(x => x.GroupCode == "STD")
                .ThenBy(x => x.Id)
                .First(x => x.GroupType == "CONTROL_CHART");
            db.ControlChartTypes.Add(new ControlChartType
            {
                ChartGroupId = controlGroup.Id,
                ChartTypeCode = "XBAR_S",
                ChartTypeName = "平均數-標準差圖",
                DataCategory = "Variable",
                RequiredSampleSize = 10,
                IsEnabled = true
            });
            db.SaveChanges();
        }

        var ruleGrp = EnsureDefaultSpcRules(db);
        foreach (var chartType in db.ControlChartTypes.ToList())
            chartType.RuleGroupId = ruleGrp.Id;
        foreach (var item in db.PartProcessCharacteristics.Where(x => x.RuleGroupId != null).ToList())
            item.RuleGroupId = null;
        db.SaveChanges();
        db.SaveChanges();

        if (!db.Parts.Any())
        {
            var part1 = new Part { PartNo = "P-1001", PartName = "高頻通訊模組 A", Specification = "Spec-1001" };
            var part2 = new Part { PartNo = "P-1002", PartName = "精密電路基板 B", Specification = "Spec-1002" };
            db.Parts.AddRange(part1, part2);
            db.SaveChanges();

            var proc1 = new Process { ProcessCode = "ST-01", ProcessName = "第一道雷射切割" };
            var proc2 = new Process { ProcessCode = "ST-02", ProcessName = "自動光學檢驗 AOI" };
            db.Processes.AddRange(proc1, proc2);
            db.SaveChanges();

            var mach1 = new Machine { MachineCode = "M-01", MachineName = "雷射切割機 #1", ProcessId = proc1.Id, Status = "Running" };
            var mach2 = new Machine { MachineCode = "M-02", MachineName = "雷射切割機 #2", ProcessId = proc1.Id, Status = "Running" };
            var mach3 = new Machine { MachineCode = "M-03", MachineName = "AOI 檢驗站 #1", ProcessId = proc2.Id, Status = "Running" };
            db.Machines.AddRange(mach1, mach2, mach3);
            db.SaveChanges();

            var xbarType = db.ControlChartTypes.First(x => x.ChartTypeCode == "XBAR_R");
            var pType = db.ControlChartTypes.First(x => x.ChartTypeCode == "P");
            var char1 = new QualityCharacteristic { CharacteristicCode = "LEN-001", CharacteristicName = "模組長度", DataCategory = "Variable", DefaultChartTypeId = xbarType.Id };
            var char2 = new QualityCharacteristic { CharacteristicCode = "DEF-001", CharacteristicName = "表面刮傷率", DataCategory = "Attribute", DefaultChartTypeId = pType.Id };
            db.QualityCharacteristics.AddRange(char1, char2);
            db.SaveChanges();

            db.PartProcessCharacteristics.AddRange(
                new PartProcessCharacteristic
                {
                    PartId = part1.Id, ProcessId = proc1.Id, CharacteristicId = char1.Id,
                    USL = 10.2, TargetValue = 10.0, LSL = 9.8,
                    UCL = 10.15, CL = 10.0, LCL = 9.85,
                    SampleSize = 5, ChartTypeId = xbarType.Id, IsRequired = true, IsEnabled = true
                },
                new PartProcessCharacteristic
                {
                    PartId = part2.Id, ProcessId = proc2.Id, CharacteristicId = char2.Id,
                    USL = 0.05, TargetValue = 0.01, LSL = 0,
                    UCL = 0.03, CL = 0.01, LCL = 0,
                    SampleSize = 1, ChartTypeId = pType.Id, IsRequired = true, IsEnabled = true
                }
            );
            db.SaveChanges();
        }

        if (!db.VariableMeasurements.Any() && db.PartProcessCharacteristics.Any(x => x.Part!.PartNo == "P-1001"))
        {
            var ppc1 = db.PartProcessCharacteristics.Include(x => x.Part).First(x => x.Part!.PartNo == "P-1001");
            var batchId = Guid.NewGuid();
            db.UploadBatches.Add(new UploadBatch
            {
                UploadBatchId = batchId, UploadType = "Variable", SourceType = "Api", ImportStatus = "Confirmed", ConfirmedAt = DateTime.UtcNow, TotalRows = 25, ValidRows = 25, ErrorRows = 0
            });
            db.SaveChanges();

            var rand = new Random(42);
            var startTime = DateTime.UtcNow.AddDays(-5);
            var varList = new List<VariableMeasurement>();
            for (int i = 0; i < 25; i++)
            {
                var subgroupIdx = i / 5;
                var time = startTime.AddHours(subgroupIdx * 4).AddMinutes((i % 5) * 10);
                var val = 10.0 + (rand.NextDouble() * 0.25 - 0.12);
                if (i == 18) val = 10.25; // Intentional Out of Spec & Control point
                varList.Add(new VariableMeasurement
                {
                    UploadBatchId = batchId,
                    PartId = ppc1.PartId ?? 0, ProcessId = ppc1.ProcessId, MachineId = db.Machines.First(m => m.ProcessId == ppc1.ProcessId).Id,
                    CharacteristicId = ppc1.CharacteristicId, PartProcessCharacteristicId = ppc1.Id,
                    LotNo = "L-2026-001", SerialNo = $"SN-{i:D4}", SampleNo = (i % 5) + 1,
                    MeasuredValue = Math.Round(val, 3), MeasuredAt = time, Operator = "OP-01"
                });
            }
            db.VariableMeasurements.AddRange(varList);
            db.SaveChanges();
        }

        if (!db.AttributeMeasurements.Any() && db.PartProcessCharacteristics.Any(x => x.Part!.PartNo == "P-1002"))
        {
            var ppc2 = db.PartProcessCharacteristics.Include(x => x.Part).First(x => x.Part!.PartNo == "P-1002");
            var batchId = Guid.NewGuid();
            db.UploadBatches.Add(new UploadBatch
            {
                UploadBatchId = batchId, UploadType = "Attribute", SourceType = "Api", ImportStatus = "Confirmed", ConfirmedAt = DateTime.UtcNow, TotalRows = 15, ValidRows = 15, ErrorRows = 0
            });
            db.SaveChanges();

            var rand = new Random(101);
            var startTime = DateTime.UtcNow.AddDays(-5);
            var attrList = new List<AttributeMeasurement>();
            for (int i = 0; i < 15; i++)
            {
                var time = startTime.AddHours(i * 6);
                var inspected = 500;
                var defects = rand.Next(1, 10);
                if (i == 12) defects = 22; // Intentional Out of Control point
                attrList.Add(new AttributeMeasurement
                {
                    UploadBatchId = batchId,
                    PartId = ppc2.PartId ?? 0, ProcessId = ppc2.ProcessId, MachineId = db.Machines.First(m => m.ProcessId == ppc2.ProcessId).Id,
                    CharacteristicId = ppc2.CharacteristicId, PartProcessCharacteristicId = ppc2.Id,
                    LotNo = "L-2026-002", SampleNo = i + 1, InspectedQty = inspected, DefectQty = defects,
                    DefectCount = defects, UnitCount = inspected, MeasuredAt = time, Operator = "OP-02"
                });
            }
            db.AttributeMeasurements.AddRange(attrList);
            db.SaveChanges();
        }

        SeedTraceabilityData(db);
    }

    private static void SeedTraceabilityData(AppDbContext db)
    {
        if (db.LotMasters.Any(x => x.LotNo == "L-2026-001")) return;

        var wo = new WorkOrder { WorkOrderNo = "WO-2026-001", ProductId = db.Products.First().Id, PlannedQty = 1000, Status = "Active", PlannedStartTime = DateTime.UtcNow };
        db.WorkOrders.Add(wo);
        db.SaveChanges();

        var rootLot1 = new LotMaster { LotNo = "L-2026-001", SubLotNo = "00", WorkOrderId = wo.Id, CurrentQty = 300, Status = "Active" };
        var rootLot2 = new LotMaster { LotNo = "L-2026-002", SubLotNo = "00", WorkOrderId = wo.Id, CurrentQty = 500, Status = "Active" };
        db.LotMasters.AddRange(rootLot1, rootLot2);
        db.SaveChanges();

        var childLot1A = new LotMaster { LotNo = "L-2026-001-A", SubLotNo = "01", WorkOrderId = wo.Id, ParentLotId = rootLot1.Id, CurrentQty = 200, Status = "Active" };
        db.LotMasters.Add(childLot1A);
        db.SaveChanges();

        db.LotSplitHistories.Add(new LotSplitHistory { SourceLotId = rootLot1.Id, TargetLotId = childLot1A.Id, SplitQty = 200, SplitTime = DateTime.UtcNow.AddDays(-3), SplitOperator = "OP-01", SplitReason = "Quality Rework" });
        db.SaveChanges();

        var line = new ProductionLine { LineCode = "LINE-A", LineName = "FPC 鍍銅產線 A" };
        db.ProductionLines.Add(line);
        db.SaveChanges();

        var tank1 = new Tank { TankCode = "T-01", TankName = "預浸槽", LineId = line.Id };
        var tank2 = new Tank { TankCode = "T-02", TankName = "鍍銅主槽", LineId = line.Id };
        db.Tanks.AddRange(tank1, tank2);
        db.SaveChanges();

        var slot1 = new Slot { SlotCode = "S-01-A", TankId = tank1.Id, SequenceNo = 1 };
        var slot2 = new Slot { SlotCode = "S-02-A", TankId = tank2.Id, SequenceNo = 2 };
        db.Slots.AddRange(slot1, slot2);
        db.SaveChanges();

        var entry1 = DateTime.UtcNow.AddDays(-4);
        db.LotSlotHistories.AddRange(
            new LotSlotHistory { LotId = rootLot1.Id, SlotId = slot1.Id, EntryTime = entry1, ExitTime = entry1.AddMinutes(30), Operator = "Auto" },
            new LotSlotHistory { LotId = rootLot1.Id, SlotId = slot2.Id, EntryTime = entry1.AddMinutes(35), ExitTime = entry1.AddMinutes(120), Operator = "Auto" },
            new LotSlotHistory { LotId = childLot1A.Id, SlotId = slot2.Id, EntryTime = entry1.AddDays(1), ExitTime = entry1.AddDays(1).AddMinutes(60), Operator = "OP-02" }
        );
        db.SaveChanges();

        // Update existing measurements with Traceability metadata
        var vars = db.VariableMeasurements.Where(v => v.LotNo == "L-2026-001").ToList();
        foreach (var v in vars) { v.LineId = line.Id; v.TankId = tank2.Id; v.SlotId = slot2.Id; }
        db.SaveChanges();
    }

    private static FormulaDefinition New(string code, string name, string expr) =>
        new()
        {
            FormulaCode = code,
            DisplayName = name,
            Expression = expr,
            IsBuiltIn = true,
            IsActive = true
        };

    private static SpcRuleGroup EnsureDefaultSpcRules(AppDbContext db)
    {
        var ruleGrp = db.SpcRuleGroups.FirstOrDefault(x => x.RuleGroupCode == "WE");
        if (ruleGrp is null)
        {
            ruleGrp = new SpcRuleGroup
            {
                RuleGroupCode = "WE",
                RuleGroupName = "Western Electric Rules (西方電氣規則)",
                Description = "系統預設 8 大 SPC 管制規則庫。",
                IsEnabled = true
            };
            db.SpcRuleGroups.Add(ruleGrp);
            db.SaveChanges();
        }
        else
        {
            ruleGrp.RuleGroupName = "Western Electric Rules (西方電氣規則)";
            ruleGrp.Description = "系統預設 8 大 SPC 管制規則庫。";
            ruleGrp.IsEnabled = true;
        }

        var templates = new (string Code, string Name, string? Config, int Priority)[]
        {
            ("Rule1_Over3Sigma", "規則 1：單點超出 3 Sigma 管制界限", null, 10),
            ("Rule2_9SameSide", "規則 2：連續 9 點落在中心線同一側", "{\"RequiredPoints\":9}", 20),
            ("Rule3_6Trend", "規則 3：連續 6 點持續上升或下降", "{\"RequiredPoints\":6}", 30),
            ("Rule4_14Alternating", "規則 4：連續 14 點上下交替", "{\"RequiredPoints\":14}", 40),
            ("Rule5_2Of3Over2Sigma", "規則 5：連續 3 點中有 2 點超出 2 Sigma 且同側", null, 50),
            ("Rule6_4Of5Over1Sigma", "規則 6：連續 5 點中有 4 點超出 1 Sigma 且同側", null, 60),
            ("Rule7_15Within1Sigma", "規則 7：連續 15 點落在中心線 1 Sigma 內", null, 70),
            ("Rule8_8Outside1Sigma", "規則 8：連續 8 點落在中心線兩側且都超出 1 Sigma", null, 80)
        };

        var existingRules = db.SpcRules.Where(x => x.RuleGroupId == ruleGrp.Id).ToList();
        foreach (var template in templates)
        {
            var rule = existingRules.FirstOrDefault(x => x.RuleCode == template.Code);
            if (rule is null)
            {
                db.SpcRules.Add(new SpcRule
                {
                    RuleGroupId = ruleGrp.Id,
                    RuleCode = template.Code,
                    RuleName = template.Name,
                    RuleConfigJson = template.Config,
                    Priority = template.Priority,
                    IsEnabled = true
                });
                continue;
            }

            rule.RuleName = template.Name;
            rule.RuleConfigJson = template.Config;
            rule.Priority = template.Priority;
            rule.IsEnabled = true;
        }

        db.SaveChanges();
        return ruleGrp;
    }

    private static void EnsureDefaultChartTypeRules(AppDbContext db, ControlChartType chartType, int defaultRuleGroupId)
    {
        const string chartTypeRuleGroupCodePrefix = "CT_RULES_";
        var ruleGroupCode = $"{chartTypeRuleGroupCodePrefix}{chartType.Id}";
        var ruleGroup = db.SpcRuleGroups.FirstOrDefault(x => x.RuleGroupCode == ruleGroupCode);
        if (ruleGroup is null)
        {
            ruleGroup = new SpcRuleGroup
            {
                RuleGroupCode = ruleGroupCode,
                RuleGroupName = $"{chartType.ChartTypeName} 管制規則",
                Description = $"管制圖小分類 {chartType.ChartTypeCode} 專用的管制規則預設設定。",
                IsEnabled = true
            };
            db.SpcRuleGroups.Add(ruleGroup);
            db.SaveChanges();
        }
        else
        {
            ruleGroup.RuleGroupName = $"{chartType.ChartTypeName} 管制規則";
            ruleGroup.Description = $"管制圖小分類 {chartType.ChartTypeCode} 專用的管制規則預設設定。";
            ruleGroup.IsEnabled = true;
        }

        var templates = db.SpcRules
            .Where(x => x.RuleGroupId == defaultRuleGroupId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToList();
        var firstRuleCode = templates.FirstOrDefault()?.RuleCode;
        var existingRules = db.SpcRules.Where(x => x.RuleGroupId == ruleGroup.Id).ToList();

        foreach (var template in templates)
        {
            var rule = existingRules.FirstOrDefault(x => x.RuleCode == template.RuleCode);
            var isDefaultSelected = string.Equals(template.RuleCode, firstRuleCode, StringComparison.OrdinalIgnoreCase);
            if (rule is null)
            {
                db.SpcRules.Add(new SpcRule
                {
                    RuleGroupId = ruleGroup.Id,
                    RuleCode = template.RuleCode,
                    RuleName = template.RuleName,
                    RuleConfigJson = template.RuleConfigJson,
                    Priority = template.Priority,
                    IsEnabled = isDefaultSelected
                });
                continue;
            }

            rule.RuleName = template.RuleName;
            rule.RuleConfigJson = template.RuleConfigJson;
            rule.Priority = template.Priority;
            rule.IsEnabled = isDefaultSelected;
        }

        chartType.RuleGroupId = ruleGroup.Id;
    }
}
