using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class AttributeChartCalculator
{
    public static ControlChartResult Calculate(
        string chartType,
        List<AttributeDataPoint> rawData,
        ControlLimits configuredLimits,
        IReadOnlySet<string>? enabledRuleCodes = null)
    {
        var code = chartType.Trim().ToUpperInvariant();
        
        return code switch
        {
            "P" or "P_CHART" or "P-CHART" => CalculatePChart(rawData, configuredLimits, enabledRuleCodes),
            "NP" or "NP_CHART" or "NP-CHART" => CalculateNpChart(rawData, configuredLimits, enabledRuleCodes),
            "C" or "C_CHART" or "C-CHART" => CalculateCChart(rawData, configuredLimits, enabledRuleCodes),
            "U" or "U_CHART" or "U-CHART" => CalculateUChart(rawData, configuredLimits, enabledRuleCodes),
            _ => throw new NotSupportedException($"Attribute chart type {chartType} is not supported.")
        };
    }

    private static ControlChartResult CalculatePChart(List<AttributeDataPoint> data, ControlLimits configuredLimits, IReadOnlySet<string>? enabledRuleCodes)
    {
        var chartData = data.Where(d => d.InspectedQty.HasValue && d.InspectedQty > 0 && d.DefectQty.HasValue).ToList();
        var includedData = chartData.Where(d => !d.IsExcluded).ToList();
        
        double? pBar = null;
        double? nBar = null;
        if (includedData.Count > 0)
        {
            double totalDefects = includedData.Sum(d => d.DefectQty!.Value);
            double totalInspected = includedData.Sum(d => d.InspectedQty!.Value);
            pBar = totalDefects / totalInspected;
            nBar = totalInspected / includedData.Count;
        }

        var points = new List<Dictionary<string, object?>>();
        var spcPoints = new List<SpcDataPoint>();
        var spcPointIndexes = new List<int>();
        
        foreach (var d in chartData)
        {
            double n = d.InspectedQty!.Value;
            double p = (double)d.DefectQty!.Value / n;
            
            double? ucl = pBar.HasValue ? pBar.Value + 3 * Math.Sqrt(pBar.Value * (1 - pBar.Value) / n) : null;
            double? lcl = pBar.HasValue ? Math.Max(0, pBar.Value - 3 * Math.Sqrt(pBar.Value * (1 - pBar.Value) / n)) : null;

            var activeUcl = d.UCL ?? configuredLimits.UCL ?? ucl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? lcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? pBar;

            var outOfControl = !d.IsExcluded && ((activeUcl.HasValue && p > activeUcl.Value) || (activeLcl.HasValue && p < activeLcl.Value));
            
            if (!d.IsExcluded)
            {
                var spcPoint = new SpcDataPoint 
                { 
                    MeasuredAt = d.MeasuredAt, 
                    Value = p, 
                    IsOutOfControl = outOfControl,
                    UCL = d.UCL,
                    CL = d.CL,
                    LCL = d.LCL
                };
                spcPoints.Add(spcPoint);
                spcPointIndexes.Add(points.Count);
            }
            
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", p },
                { "n", n },
                { "uclStat", activeUcl },
                { "lclStat", activeLcl },
                { "clStat", activeCl },
                { "outOfControl", outOfControl },
                { "lotNo", d.LotNo },
                { "operator", d.Operator },
                { "isExcluded", d.IsExcluded }
            });
        }

        double? staticUcl = pBar.HasValue && nBar.HasValue ? pBar.Value + 3 * Math.Sqrt(pBar.Value * (1 - pBar.Value) / nBar.Value) : null;
        double? staticLcl = pBar.HasValue && nBar.HasValue ? Math.Max(0, pBar.Value - 3 * Math.Sqrt(pBar.Value * (1 - pBar.Value) / nBar.Value)) : null;

        if (pBar.HasValue && staticUcl.HasValue && staticLcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = pBar, UCL = staticUcl, LCL = staticLcl }, enabledRuleCodes);
            
            for (int i = 0; i < spcPoints.Count; i++)
            {
                var pointIndex = spcPointIndexes[i];
                points[pointIndex]["violatedRules"] = spcPoints[i].ViolatedRules;
                points[pointIndex]["outOfControl"] = spcPoints[i].IsOutOfControl;
            }
        }

        return new ControlChartResult
        {
            ChartType = "P_CHART",
            Limits = configuredLimits,
            StatControlLimits = new { cl = pBar, nBar, ucl = staticUcl, lcl = staticLcl },
            ChartData = new { points }
        };
    }

    private static ControlChartResult CalculateNpChart(List<AttributeDataPoint> data, ControlLimits configuredLimits, IReadOnlySet<string>? enabledRuleCodes)
    {
        var chartData = data.Where(d => d.InspectedQty.HasValue && d.InspectedQty > 0 && d.DefectQty.HasValue).ToList();
        var includedData = chartData.Where(d => !d.IsExcluded).ToList();
        
        double? npBar = null;
        double? nBar = null;
        if (includedData.Count > 0)
        {
            npBar = includedData.Average(d => d.DefectQty!.Value);
            nBar = includedData.Average(d => d.InspectedQty!.Value);
        }

        double? pBar = nBar > 0 ? npBar / nBar : null;
        double? staticUcl = npBar.HasValue && pBar.HasValue ? npBar.Value + 3 * Math.Sqrt(npBar.Value * (1 - pBar.Value)) : null;
        double? staticLcl = npBar.HasValue && pBar.HasValue ? Math.Max(0, npBar.Value - 3 * Math.Sqrt(npBar.Value * (1 - pBar.Value))) : null;

        var spcPoints = new List<SpcDataPoint>();
        var points = new List<Dictionary<string, object?>>();

        foreach (var d in includedData)
        {
            double np = d.DefectQty!.Value;
            var activeUcl = d.UCL ?? configuredLimits.UCL ?? staticUcl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? staticLcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? npBar;

            var outOfControl = (activeUcl.HasValue && np > activeUcl.Value) || (activeLcl.HasValue && np < activeLcl.Value);
            spcPoints.Add(new SpcDataPoint 
            { 
                MeasuredAt = d.MeasuredAt, 
                Value = np, 
                IsOutOfControl = outOfControl,
                UCL = d.UCL,
                CL = d.CL,
                LCL = d.LCL
            });
        }

        if (npBar.HasValue && staticUcl.HasValue && staticLcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = npBar, UCL = staticUcl, LCL = staticLcl }, enabledRuleCodes);
        }

        var spcByMeasuredAt = spcPoints.ToDictionary(x => x.MeasuredAt);
        foreach (var d in chartData)
        {
            var p = spcByMeasuredAt.GetValueOrDefault(d.MeasuredAt);
            var np = d.DefectQty!.Value;
            var activeUcl = d.UCL ?? configuredLimits.UCL ?? staticUcl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? staticLcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? npBar;

            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", np },
                { "n", d.InspectedQty },
                { "uclStat", activeUcl },
                { "lclStat", activeLcl },
                { "clStat", activeCl },
                { "outOfControl", p?.IsOutOfControl ?? false },
                { "violatedRules", p?.ViolatedRules ?? new List<string>() },
                { "lotNo", d.LotNo },
                { "operator", d.Operator },
                { "isExcluded", d.IsExcluded }
            });
        }

        return new ControlChartResult
        {
            ChartType = "NP_CHART",
            Limits = configuredLimits,
            StatControlLimits = new { cl = npBar, nBar, ucl = staticUcl, lcl = staticLcl },
            ChartData = new { points }
        };
    }

    private static ControlChartResult CalculateCChart(List<AttributeDataPoint> data, ControlLimits configuredLimits, IReadOnlySet<string>? enabledRuleCodes)
    {
        var chartData = data.Where(d => d.DefectCount.HasValue).ToList();
        var includedData = chartData.Where(d => !d.IsExcluded).ToList();
        
        double? cBar = includedData.Count > 0 ? includedData.Average(d => d.DefectCount!.Value) : null;
        double? ucl = cBar.HasValue ? cBar.Value + 3 * Math.Sqrt(cBar.Value) : null;
        double? lcl = cBar.HasValue ? Math.Max(0, cBar.Value - 3 * Math.Sqrt(cBar.Value)) : null;

        var spcPoints = new List<SpcDataPoint>();
        var points = new List<Dictionary<string, object?>>();

        foreach (var d in includedData)
        {
            double c = d.DefectCount!.Value;
            var activeUcl = d.UCL ?? configuredLimits.UCL ?? ucl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? lcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? cBar;

            var outOfControl = (activeUcl.HasValue && c > activeUcl.Value) || (activeLcl.HasValue && c < activeLcl.Value);
            spcPoints.Add(new SpcDataPoint 
            { 
                MeasuredAt = d.MeasuredAt, 
                Value = c, 
                IsOutOfControl = outOfControl,
                UCL = d.UCL,
                CL = d.CL,
                LCL = d.LCL
            });
        }

        if (cBar.HasValue && ucl.HasValue && lcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = cBar, UCL = ucl, LCL = lcl }, enabledRuleCodes);
        }

        var spcByMeasuredAt = spcPoints.ToDictionary(x => x.MeasuredAt);
        foreach (var d in chartData)
        {
            var p = spcByMeasuredAt.GetValueOrDefault(d.MeasuredAt);
            var c = d.DefectCount!.Value;
            var activeUcl = d.UCL ?? configuredLimits.UCL ?? ucl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? lcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? cBar;

            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", c },
                { "uclStat", activeUcl },
                { "lclStat", activeLcl },
                { "clStat", activeCl },
                { "outOfControl", p?.IsOutOfControl ?? false },
                { "violatedRules", p?.ViolatedRules ?? new List<string>() },
                { "lotNo", d.LotNo },
                { "operator", d.Operator },
                { "isExcluded", d.IsExcluded }
            });
        }

        return new ControlChartResult
        {
            ChartType = "C_CHART",
            Limits = configuredLimits,
            StatControlLimits = new { cl = cBar, ucl, lcl },
            ChartData = new { points }
        };
    }

    private static ControlChartResult CalculateUChart(List<AttributeDataPoint> data, ControlLimits configuredLimits, IReadOnlySet<string>? enabledRuleCodes)
    {
        var chartData = data.Where(d => d.UnitCount.HasValue && d.UnitCount > 0 && d.DefectCount.HasValue).ToList();
        var includedData = chartData.Where(d => !d.IsExcluded).ToList();
        
        double? uBar = null;
        double? nBar = null;
        if (includedData.Count > 0)
        {
            double totalDefects = includedData.Sum(d => d.DefectCount!.Value);
            double totalUnits = includedData.Sum(d => d.UnitCount!.Value);
            uBar = totalDefects / totalUnits;
            nBar = totalUnits / includedData.Count;
        }

        var points = new List<Dictionary<string, object?>>();
        var spcPoints = new List<SpcDataPoint>();
        var spcPointIndexes = new List<int>();
        
        foreach (var d in chartData)
        {
            double n = d.UnitCount!.Value;
            double u = (double)d.DefectCount!.Value / n;
            
            double? ucl = uBar.HasValue ? uBar.Value + 3 * Math.Sqrt(uBar.Value / n) : null;
            double? lcl = uBar.HasValue ? Math.Max(0, uBar.Value - 3 * Math.Sqrt(uBar.Value / n)) : null;

            var activeUcl = d.UCL ?? configuredLimits.UCL ?? ucl;
            var activeLcl = d.LCL ?? configuredLimits.LCL ?? lcl;
            var activeCl = d.CL ?? configuredLimits.CL ?? uBar;

            var outOfControl = !d.IsExcluded && ((activeUcl.HasValue && u > activeUcl.Value) || (activeLcl.HasValue && u < activeLcl.Value));
            if (!d.IsExcluded)
            {
                spcPoints.Add(new SpcDataPoint 
                { 
                    MeasuredAt = d.MeasuredAt, 
                    Value = u, 
                    IsOutOfControl = outOfControl,
                    UCL = d.UCL,
                    CL = d.CL,
                    LCL = d.LCL
                });
                spcPointIndexes.Add(points.Count);
            }
            
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", u },
                { "n", n },
                { "uclStat", activeUcl },
                { "lclStat", activeLcl },
                { "clStat", activeCl },
                { "outOfControl", outOfControl },
                { "lotNo", d.LotNo },
                { "operator", d.Operator },
                { "isExcluded", d.IsExcluded }
            });
        }

        double? staticUcl = uBar.HasValue && nBar.HasValue ? uBar.Value + 3 * Math.Sqrt(uBar.Value / nBar.Value) : null;
        double? staticLcl = uBar.HasValue && nBar.HasValue ? Math.Max(0, uBar.Value - 3 * Math.Sqrt(uBar.Value / nBar.Value)) : null;

        if (uBar.HasValue && staticUcl.HasValue && staticLcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = uBar, UCL = staticUcl, LCL = staticLcl }, enabledRuleCodes);
            
            for (int i = 0; i < spcPoints.Count; i++)
            {
                var pointIndex = spcPointIndexes[i];
                points[pointIndex]["violatedRules"] = spcPoints[i].ViolatedRules;
                points[pointIndex]["outOfControl"] = spcPoints[i].IsOutOfControl;
            }
        }

        return new ControlChartResult
        {
            ChartType = "U_CHART",
            Limits = configuredLimits,
            StatControlLimits = new { cl = uBar, nBar, ucl = staticUcl, lcl = staticLcl },
            ChartData = new { points }
        };
    }
}
