using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;

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
