using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.Services; // For SpcConstants

namespace MesSpc.Api.SpcEngine.Calculators;

public static class XbarRChartCalculator
{
    public static ControlChartResult Calculate(List<Subgroup> subgroups, ControlLimits configuredLimits, int expectedSampleSize)
    {
        if (subgroups.Count == 0)
        {
            return new ControlChartResult
            {
                ChartType = "XBAR_R",
                Limits = configuredLimits,
                ChartData = new { points = Array.Empty<object>() },
                SecondaryChartData = new { points = Array.Empty<object>() }
            };
        }

        var nRef = expectedSampleSize > 0 ? expectedSampleSize : subgroups.First().N;
        if (nRef < 2) nRef = subgroups.First().N;
        
        var validSubgroups = subgroups.Where(x => x.N == nRef).ToList();
        if (validSubgroups.Count == 0)
        {
            validSubgroups = subgroups;
            nRef = validSubgroups.First().N;
        }

        object? xbarControl = null;
        object? rControl = null;
        double? uclXbar = null;
        double? lclXbar = null;
        double? uclRrange = null;
        double? lclRrange = null;

        if (nRef >= 2 && SpcConstants.TryGetFactors(nRef, out var a2, out var d3, out var d4))
        {
            var meanOfXbar = validSubgroups.Average(x => x.Mean);
            var rBar = validSubgroups.Average(x => x.Range);
            
            uclXbar = meanOfXbar + a2 * rBar;
            lclXbar = meanOfXbar - a2 * rBar;
            uclRrange = d4 * rBar;
            lclRrange = d3 * rBar;
            
            xbarControl = new { cl = meanOfXbar, ucl = uclXbar, lcl = lclXbar, n = nRef, a2, rBar, xDoubleBar = meanOfXbar };
            rControl = new { cl = rBar, ucl = uclRrange, lcl = lclRrange, n = nRef, d3, d4, rBar };
        }

        var spcPoints = validSubgroups.Select(x => new SpcDataPoint
        {
            MeasuredAt = x.MeasuredAt,
            Value = x.Mean
        }).ToList();

        if (uclXbar.HasValue && lclXbar.HasValue && nRef >= 2)
        {
            var statLimits = new ControlLimits { CL = validSubgroups.Average(x => x.Mean), UCL = uclXbar, LCL = lclXbar };
            MesSpc.Api.SpcEngine.Rules.NelsonRulesValidator.ApplyRules(spcPoints, statLimits);
        }

        var xbarPoints = new List<object>();
        var rPoints = new List<object>();
        for (int i = 0; i < validSubgroups.Count; i++)
        {
            var x = validSubgroups[i];
            var p = spcPoints[i];
            var xbar = x.Mean;
            var range = x.Range;
            var oos = (configuredLimits.USL.HasValue && xbar > configuredLimits.USL.Value) || 
                      (configuredLimits.LSL.HasValue && xbar < configuredLimits.LSL.Value);
            var oocStat = p.IsOutOfControl; // Provided by NelsonRulesValidator for Xbar
            var oocR = uclRrange.HasValue && lclRrange.HasValue && (range > uclRrange.Value || range < lclRrange.Value);

            xbarPoints.Add(new
            {
                x.MeasuredAt,
                xbar,
                range,
                n = x.N,
                outOfSpec = oos,
                outOfControl = oocStat || oocR,
                outOfControlXbar = oocStat,
                outOfControlR = oocR,
                violatedRules = p.ViolatedRules
            });
            rPoints.Add(new { x.MeasuredAt, value = range, outOfControl = oocR });
        }

        return new ControlChartResult
        {
            ChartType = "XBAR_R",
            Limits = configuredLimits,
            StatControlLimits = new { xbarControl, rControl },
            SubgroupSize = nRef,
            SubgroupSizeNote = subgroups.Any(x => x.N != nRef)
                ? "部分批次子組大小與基準 n 不一致，已改用可解析之 n 或納入全部子組；建議每批同子組數。"
                : null,
            ChartData = new { points = xbarPoints },
            SecondaryChartData = new { points = rPoints }
        };
    }
}
