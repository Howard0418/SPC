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

            var catVar = new ControlChartCategory { ChartGroupId = grp.Id, CategoryCode = "VAR", CategoryName = "計量型管制圖" };
            var catAttr = new ControlChartCategory { ChartGroupId = grp.Id, CategoryCode = "ATTR", CategoryName = "計數型管制圖" };
            db.ControlChartCategories.AddRange(catVar, catAttr);
            db.SaveChanges();

            db.ControlChartTypes.AddRange(
                new ControlChartType { ChartCategoryId = catVar.Id, ChartTypeCode = "XBAR_R", ChartTypeName = "平均數-全距圖", DataCategory = "Variable", RequiredSampleSize = 5 },
                new ControlChartType { ChartCategoryId = catVar.Id, ChartTypeCode = "I_MR", ChartTypeName = "單值-移動全距圖", DataCategory = "Variable", RequiredSampleSize = 1 },
                new ControlChartType { ChartCategoryId = catAttr.Id, ChartTypeCode = "P", ChartTypeName = "不良率圖", DataCategory = "Attribute" },
                new ControlChartType { ChartCategoryId = catAttr.Id, ChartTypeCode = "NP", ChartTypeName = "不良品數圖", DataCategory = "Attribute" },
                new ControlChartType { ChartCategoryId = catAttr.Id, ChartTypeCode = "C", ChartTypeName = "缺點數圖", DataCategory = "Attribute" },
                new ControlChartType { ChartCategoryId = catAttr.Id, ChartTypeCode = "U", ChartTypeName = "單位缺點數圖", DataCategory = "Attribute" }
            );
            db.SaveChanges();
        }

        if (!db.SpcRuleGroups.Any())
        {
            var ruleGrp = new SpcRuleGroup { RuleGroupCode = "WE", RuleGroupName = "Western Electric Rules (西方電氣規則)" };
            db.SpcRuleGroups.Add(ruleGrp);
            db.SaveChanges();

            db.SpcRules.AddRange(
                new SpcRule { RuleGroupId = ruleGrp.Id, RuleCode = "Rule1_Over3Sigma", RuleName = "單點超出 3 Sigma 界限", Priority = 10 },
                new SpcRule { RuleGroupId = ruleGrp.Id, RuleCode = "Rule2_9SameSide", RuleName = "連續 9 點同側", Priority = 20 },
                new SpcRule { RuleGroupId = ruleGrp.Id, RuleCode = "Rule3_6Trend", RuleName = "連續 6 點穩定上升或下降", Priority = 30 },
                new SpcRule { RuleGroupId = ruleGrp.Id, RuleCode = "Rule4_14Alternating", RuleName = "連續 14 點上下交替", Priority = 40 }
            );
            db.SaveChanges();
        }

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
            var weRuleGrp = db.SpcRuleGroups.First(x => x.RuleGroupCode == "WE");

            var char1 = new QualityCharacteristic { CharacteristicCode = "LEN-001", CharacteristicName = "模組長度", DataCategory = "Variable", Unit = "mm", DefaultChartTypeId = xbarType.Id };
            var char2 = new QualityCharacteristic { CharacteristicCode = "DEF-001", CharacteristicName = "表面刮傷率", DataCategory = "Attribute", Unit = "%", DefaultChartTypeId = pType.Id };
            db.QualityCharacteristics.AddRange(char1, char2);
            db.SaveChanges();

            db.PartProcessCharacteristics.AddRange(
                new PartProcessCharacteristic
                {
                    PartId = part1.Id, ProcessId = proc1.Id, CharacteristicId = char1.Id,
                    USL = 10.2, TargetValue = 10.0, LSL = 9.8,
                    UCL = 10.15, CL = 10.0, LCL = 9.85,
                    SampleSize = 5, ChartTypeId = xbarType.Id, RuleGroupId = weRuleGrp.Id, IsRequired = true, IsEnabled = true
                },
                new PartProcessCharacteristic
                {
                    PartId = part2.Id, ProcessId = proc2.Id, CharacteristicId = char2.Id,
                    USL = 0.05, TargetValue = 0.01, LSL = 0,
                    UCL = 0.03, CL = 0.01, LCL = 0,
                    SampleSize = 1, ChartTypeId = pType.Id, RuleGroupId = weRuleGrp.Id, IsRequired = true, IsEnabled = true
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
                    PartId = ppc1.PartId, ProcessId = ppc1.ProcessId, MachineId = db.Machines.First(m => m.ProcessId == ppc1.ProcessId).Id,
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
                    PartId = ppc2.PartId, ProcessId = ppc2.ProcessId, MachineId = db.Machines.First(m => m.ProcessId == ppc2.ProcessId).Id,
                    CharacteristicId = ppc2.CharacteristicId, PartProcessCharacteristicId = ppc2.Id,
                    LotNo = "L-2026-002", SampleNo = i + 1, InspectedQty = inspected, DefectQty = defects,
                    DefectCount = defects, UnitCount = inspected, MeasuredAt = time, Operator = "OP-02"
                });
            }
            db.AttributeMeasurements.AddRange(attrList);
            db.SaveChanges();
        }
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
}
