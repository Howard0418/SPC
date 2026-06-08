using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class AttributeChartCalculator
{
    public static ControlChartResult Calculate(string chartType, List<AttributeDataPoint> rawData, ControlLimits configuredLimits)
    {
        var code = chartType.Trim().ToUpperInvariant();
        
        return code switch
        {
            "P" or "P_CHART" or "P-CHART" => CalculatePChart(rawData, configuredLimits),
            "NP" or "NP_CHART" or "NP-CHART" => CalculateNpChart(rawData, configuredLimits),
            "C" or "C_CHART" or "C-CHART" => CalculateCChart(rawData, configuredLimits),
            "U" or "U_CHART" or "U-CHART" => CalculateUChart(rawData, configuredLimits),
            _ => throw new NotSupportedException($"Attribute chart type {chartType} is not supported.")
        };
    }

    private static ControlChartResult CalculatePChart(List<AttributeDataPoint> data, ControlLimits configuredLimits)
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

            var outOfControl = !d.IsExcluded && ((ucl.HasValue && p > ucl.Value) || (lcl.HasValue && p < lcl.Value));
            
            if (!d.IsExcluded)
            {
                var spcPoint = new SpcDataPoint { MeasuredAt = d.MeasuredAt, Value = p, IsOutOfControl = outOfControl };
                spcPoints.Add(spcPoint);
                spcPointIndexes.Add(points.Count);
            }
            
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", p },
                { "n", n },
                { "uclStat", ucl },
                { "lclStat", lcl },
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
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = pBar, UCL = staticUcl, LCL = staticLcl });
            
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

    private static ControlChartResult CalculateNpChart(List<AttributeDataPoint> data, ControlLimits configuredLimits)
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
            var outOfControl = (staticUcl.HasValue && np > staticUcl.Value) || (staticLcl.HasValue && np < staticLcl.Value);
            spcPoints.Add(new SpcDataPoint { MeasuredAt = d.MeasuredAt, Value = np, IsOutOfControl = outOfControl });
        }

        if (npBar.HasValue && staticUcl.HasValue && staticLcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = npBar, UCL = staticUcl, LCL = staticLcl });
        }

        var spcByMeasuredAt = spcPoints.ToDictionary(x => x.MeasuredAt);
        foreach (var d in chartData)
        {
            var p = spcByMeasuredAt.GetValueOrDefault(d.MeasuredAt);
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", d.DefectQty!.Value },
                { "n", d.InspectedQty },
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

    private static ControlChartResult CalculateCChart(List<AttributeDataPoint> data, ControlLimits configuredLimits)
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
            var outOfControl = (ucl.HasValue && c > ucl.Value) || (lcl.HasValue && c < lcl.Value);
            spcPoints.Add(new SpcDataPoint { MeasuredAt = d.MeasuredAt, Value = c, IsOutOfControl = outOfControl });
        }

        if (cBar.HasValue && ucl.HasValue && lcl.HasValue)
        {
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = cBar, UCL = ucl, LCL = lcl });
        }

        var spcByMeasuredAt = spcPoints.ToDictionary(x => x.MeasuredAt);
        foreach (var d in chartData)
        {
            var p = spcByMeasuredAt.GetValueOrDefault(d.MeasuredAt);
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", d.DefectCount!.Value },
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

    private static ControlChartResult CalculateUChart(List<AttributeDataPoint> data, ControlLimits configuredLimits)
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

            var outOfControl = !d.IsExcluded && ((ucl.HasValue && u > ucl.Value) || (lcl.HasValue && u < lcl.Value));
            if (!d.IsExcluded)
            {
                spcPoints.Add(new SpcDataPoint { MeasuredAt = d.MeasuredAt, Value = u, IsOutOfControl = outOfControl });
                spcPointIndexes.Add(points.Count);
            }
            
            points.Add(new Dictionary<string, object?>
            {
                { "measuredAt", d.MeasuredAt },
                { "value", u },
                { "n", n },
                { "uclStat", ucl },
                { "lclStat", lcl },
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
            WesternElectricRulesValidator.ApplyRules(spcPoints, new ControlLimits { CL = uBar, UCL = staticUcl, LCL = staticLcl });
            
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
