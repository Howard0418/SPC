using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class ImrChartCalculator
{
    // Constants for MR chart with subgroup size = 2
    private const double D2 = 1.128;
    private const double D3 = 0.0;
    private const double D4 = 3.267;

    public static ControlChartResult Calculate(List<SpcDataPoint> rawData, ControlLimits configuredLimits)
    {
        var values = rawData.Select(x => x.Value).ToList();
        
        var iBar = values.Count > 0 ? values.Average() : (double?)null;
        var mrValues = new List<double>();
        for (var i = 1; i < values.Count; i++)
        {
            mrValues.Add(Math.Abs(values[i] - values[i - 1]));
        }
        var mrBar = mrValues.Count > 0 ? mrValues.Average() : (double?)null;

        var sigma = (mrBar.HasValue && D2 > 0) ? (mrBar.Value / D2) : (double?)null;
        var iUclStat = (iBar.HasValue && sigma.HasValue) ? (iBar.Value + 3 * sigma.Value) : (double?)null;
        var iLclStat = (iBar.HasValue && sigma.HasValue) ? (iBar.Value - 3 * sigma.Value) : (double?)null;

        var mrUclStat = mrBar.HasValue ? D4 * mrBar.Value : (double?)null;
        var mrLclStat = mrBar.HasValue ? D3 * mrBar.Value : (double?)null;

        var iPoints = new List<object>();
        foreach (var p in rawData)
        {
            var v = p.Value;
            var oos = (configuredLimits.USL.HasValue && v > configuredLimits.USL.Value) || 
                      (configuredLimits.LSL.HasValue && v < configuredLimits.LSL.Value);
            var oocConfigured = (configuredLimits.UCL.HasValue && v > configuredLimits.UCL.Value) || 
                                (configuredLimits.LCL.HasValue && v < configuredLimits.LCL.Value);

            iPoints.Add(new
            {
                measuredAt = p.MeasuredAt,
                value = v,
                outOfSpec = oos,
                outOfControl = oocConfigured,
                outOfControlStat = (iUclStat.HasValue && v > iUclStat.Value) || (iLclStat.HasValue && v < iLclStat.Value)
            });
        }

        var mrPoints = new List<object>();
        for (var i = 1; i < values.Count; i++)
        {
            var mr = Math.Abs(values[i] - values[i - 1]);
            var outOfControl = (mrUclStat.HasValue && mr > mrUclStat.Value) || (mrLclStat.HasValue && mr < mrLclStat.Value);
            mrPoints.Add(new { index = i + 1, value = mr, outOfControl });
        }

        return new ControlChartResult
        {
            ChartType = "I-MR",
            Limits = configuredLimits,
            StatControlLimits = new
            {
                iControlLimitsStat = new { cl = iBar, ucl = iUclStat, lcl = iLclStat },
                mrControlLimitsStat = new { cl = mrBar, ucl = mrUclStat, lcl = mrLclStat }
            },
            ChartData = new { points = iPoints },
            SecondaryChartData = new { points = mrPoints },
            SubgroupSize = 1
        };
    }
}
