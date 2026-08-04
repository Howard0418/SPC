using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class XbarSChartCalculator
{
    public static ControlChartResult Calculate(
        List<Subgroup> subgroups,
        ControlLimits configuredLimits,
        int expectedSampleSize,
        IReadOnlySet<string>? enabledRuleCodes = null)
    {
        if (subgroups.Count == 0)
        {
            return new ControlChartResult
            {
                ChartType = "XBAR_S",
                Limits = configuredLimits,
                ChartData = new { points = Array.Empty<object>() },
                SecondaryChartData = new { points = Array.Empty<object>() }
            };
        }

        var nRef = expectedSampleSize > 0 ? expectedSampleSize : subgroups.First().N;
        if (nRef < 2) nRef = subgroups.First().N;

        var includedSubgroups = subgroups.Where(x => !x.IsExcluded).ToList();
        var validSubgroups = includedSubgroups.Where(x => x.N == nRef).ToList();
        if (validSubgroups.Count == 0 && includedSubgroups.Count > 0)
        {
            validSubgroups = includedSubgroups;
            nRef = validSubgroups.First().N;
        }

        if (validSubgroups.Count == 0)
        {
            return EmptyExcludedResult(subgroups, configuredLimits, nRef);
        }

        if (nRef < 2 || !SpcConstants.TryGetXbarSFactors(nRef, out var a3, out var b3, out var b4))
        {
            return EmptyExcludedResult(subgroups, configuredLimits, nRef, "子組大小無法解析 Xbar-S 常數，管制線未重新估算。");
        }

        var xDoubleBar = validSubgroups.Average(x => x.Mean);
        var sBar = validSubgroups.Average(StandardDeviation);
        var uclXbar = xDoubleBar + a3 * sBar;
        var lclXbar = xDoubleBar - a3 * sBar;
        var uclS = b4 * sBar;
        var lclS = b3 * sBar;

        var evalPoints = subgroups.Where(x => !x.IsExcluded)
            .Select(x => new SpcDataPoint 
            { 
                MeasuredAt = x.MeasuredAt, 
                Value = x.Mean,
                UCL = x.UCL,
                CL = x.CL,
                LCL = x.LCL
            })
            .ToList();
        if (sBar > 0)
        {
            MesSpc.Api.SpcEngine.Rules.WesternElectricRulesValidator.ApplyRules(
                evalPoints,
                new ControlLimits { CL = xDoubleBar, UCL = uclXbar, LCL = lclXbar },
                enabledRuleCodes);
        }
        var evalDict = evalPoints.ToDictionary(x => x.MeasuredAt);

        var xbarPoints = new List<object>();
        var sPoints = new List<object>();
        foreach (var x in subgroups)
        {
            var p = evalDict.GetValueOrDefault(x.MeasuredAt);
            var xbar = x.Mean;
            var s = StandardDeviation(x);
            var oos = (configuredLimits.USL.HasValue && xbar > configuredLimits.USL.Value) ||
                      (configuredLimits.LSL.HasValue && xbar < configuredLimits.LSL.Value);

            double? activeUcl = x.UCL ?? configuredLimits.UCL ?? uclXbar;
            double? activeLcl = x.LCL ?? configuredLimits.LCL ?? lclXbar;
            double? activeCl = x.CL ?? configuredLimits.CL ?? xDoubleBar;

            var oocConfigured = (activeUcl.HasValue && xbar > activeUcl.Value) || 
                                (activeLcl.HasValue && xbar < activeLcl.Value);
            var oocXbar = (p?.IsOutOfControl ?? false) || oocConfigured;
            var oocS = !x.IsExcluded && (s > uclS || s < lclS);
            var outOfSpec = !x.IsExcluded && (x.OutOfSpec || oos);
            var outOfControl = !x.IsExcluded && (oocXbar || oocS);

            xbarPoints.Add(new
            {
                x.MeasuredAt,
                xbar,
                stdDev = s,
                n = x.N,
                outOfSpec,
                outOfControl,
                outOfControlXbar = oocXbar,
                outOfControlS = oocS,
                violatedRules = p?.ViolatedRules ?? new List<string>(),
                uclStat = activeUcl,
                lclStat = activeLcl,
                clStat = activeCl,
                lotNo = x.LotNo,
                serialNo = x.SerialNo,
                @operator = x.Operator,
                isExcluded = x.IsExcluded,
                rootCause = x.RootCause,
                correctiveAction = x.CorrectiveAction,
                measurementBatchId = x.MeasurementBatchId,
                variableMeasurementId = x.VariableMeasurementId,
                alertId = x.AlertId,
                alertStatus = x.AlertStatus,
                responsibleUser = x.ResponsibleUser
            });
            sPoints.Add(new { x.MeasuredAt, value = s, outOfControl = oocS, isExcluded = x.IsExcluded });
        }

        var capability = ProcessCapabilityCalculator.Calculate(validSubgroups, configuredLimits.USL, configuredLimits.LSL);

        return new ControlChartResult
        {
            ChartType = "XBAR_S",
            Limits = configuredLimits,
            StatControlLimits = new
            {
                xbarControl = new { cl = xDoubleBar, ucl = uclXbar, lcl = lclXbar, n = nRef, a3, sBar, xDoubleBar },
                sControl = new { cl = sBar, ucl = uclS, lcl = lclS, n = nRef, b3, b4, sBar }
            },
            SubgroupSize = nRef,
            SubgroupSizeNote = subgroups.Any(x => x.N != nRef)
                ? "部分批次子組大小與基準 n 不一致，已改用可解析之 n 或納入全部子組；建議每批同子組數。"
                : null,
            ChartData = new { points = xbarPoints },
            SecondaryChartData = new { points = sPoints },
            Capability = capability
        };
    }

    private static ControlChartResult EmptyExcludedResult(List<Subgroup> subgroups, ControlLimits configuredLimits, int nRef, string? note = null)
    {
        var xbarPoints = subgroups.Select(x => new
        {
            x.MeasuredAt,
            xbar = x.Mean,
            stdDev = StandardDeviation(x),
            n = x.N,
            outOfSpec = false,
            outOfControl = false,
            outOfControlXbar = false,
            outOfControlS = false,
            violatedRules = new List<string>(),
            lotNo = x.LotNo,
            serialNo = x.SerialNo,
            @operator = x.Operator,
            isExcluded = x.IsExcluded,
            rootCause = x.RootCause,
            correctiveAction = x.CorrectiveAction,
            measurementBatchId = x.MeasurementBatchId,
            variableMeasurementId = x.VariableMeasurementId,
            alertId = x.AlertId,
            alertStatus = x.AlertStatus,
            responsibleUser = x.ResponsibleUser
        }).ToList();

        var sPoints = subgroups.Select(x => new
        {
            x.MeasuredAt,
            value = StandardDeviation(x),
            outOfControl = false,
            isExcluded = x.IsExcluded
        }).ToList();

        return new ControlChartResult
        {
            ChartType = "XBAR_S",
            Limits = configuredLimits,
            StatControlLimits = new { xbarControl = (object?)null, sControl = (object?)null },
            SubgroupSize = nRef,
            SubgroupSizeNote = note ?? "全部子組皆已標記為不列入計算，管制線未重新估算。",
            ChartData = new { points = xbarPoints },
            SecondaryChartData = new { points = sPoints },
            Capability = null
        };
    }

    private static double StandardDeviation(Subgroup subgroup)
    {
        if (subgroup.Values.Count < 2) return 0;
        var mean = subgroup.Values.Average();
        var variance = subgroup.Values.Sum(x => Math.Pow(x - mean, 2)) / (subgroup.Values.Count - 1);
        return Math.Sqrt(variance);
    }
}
