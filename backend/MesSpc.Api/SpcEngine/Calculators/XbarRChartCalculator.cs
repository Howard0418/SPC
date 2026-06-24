using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.Services; // For SpcConstants

namespace MesSpc.Api.SpcEngine.Calculators;

public static class XbarRChartCalculator
{
    public static ControlChartResult Calculate(List<Subgroup> subgroups, ControlLimits configuredLimits, int expectedSampleSize, string? formulaConfigJson = null)
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
        
        var includedSubgroups = subgroups.Where(x => !x.IsExcluded).ToList();
        var validSubgroups = includedSubgroups.Where(x => x.N == nRef).ToList();
        if (validSubgroups.Count == 0 && includedSubgroups.Count > 0)
        {
            validSubgroups = includedSubgroups;
            nRef = validSubgroups.First().N;
        }
        if (validSubgroups.Count == 0)
        {
            var xbarPointsEmpty = subgroups.Select(x => new
            {
                x.MeasuredAt,
                xbar = x.Mean,
                range = x.Range,
                n = x.N,
                outOfSpec = false,
                outOfControl = false,
                outOfControlXbar = false,
                outOfControlR = false,
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

            var rPointsEmpty = subgroups.Select(x => new
            {
                x.MeasuredAt,
                value = x.Range,
                outOfControl = false,
                isExcluded = x.IsExcluded
            }).ToList();

            return new ControlChartResult
            {
                ChartType = "XBAR_R",
                Limits = configuredLimits,
                StatControlLimits = new { xbarControl = (object?)null, rControl = (object?)null },
                SubgroupSize = nRef,
                SubgroupSizeNote = "全部子組皆已標記為不列入計算，管制線未重新估算。",
                ChartData = new { points = xbarPointsEmpty },
                SecondaryChartData = new { points = rPointsEmpty },
                Capability = null
            };
        }

        bool useMrMethod = false;
        bool useSigmaMethod = false;
        double mrMultiplier = 2.66;
        if (!string.IsNullOrWhiteSpace(formulaConfigJson))
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(formulaConfigJson);
                if (doc.RootElement.TryGetProperty("XbarCalculationMethod", out var methodProp))
                {
                    var methodStr = methodProp.GetString();
                    if (string.Equals(methodStr, "MOVING_RANGE_OF_XBAR", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(methodStr, "MR_METHOD", StringComparison.OrdinalIgnoreCase))
                    {
                        useMrMethod = true;
                    }
                    else if (string.Equals(methodStr, "SIGMA_METHOD", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(methodStr, "SAMPLE_STD_DEV", StringComparison.OrdinalIgnoreCase))
                    {
                        useSigmaMethod = true;
                    }
                }
                if (doc.RootElement.TryGetProperty("MrMultiplier", out var mulProp) && mulProp.TryGetDouble(out var mulVal))
                {
                    mrMultiplier = mulVal;
                }
                else if (doc.RootElement.TryGetProperty("Multiplier", out var sigMulProp) && sigMulProp.TryGetDouble(out var sigMulVal))
                {
                    mrMultiplier = sigMulVal;
                }
            }
            catch { /* fallback to default */ }
        }

        object? xbarControl = null;
        object? rControl = null;
        double? uclXbar = null;
        double? lclXbar = null;
        double? uclRrange = null;
        double? lclRrange = null;

        var meanOfXbar = validSubgroups.Average(x => x.Mean);

        if (useSigmaMethod && validSubgroups.Count >= 2)
        {
            double sXbar = Math.Sqrt(validSubgroups.Sum(g => Math.Pow(g.Mean - meanOfXbar, 2)) / (validSubgroups.Count - 1));
            double multiplier = mrMultiplier > 0 ? mrMultiplier : 3.0;
            uclXbar = meanOfXbar + multiplier * sXbar;
            lclXbar = meanOfXbar - multiplier * sXbar;

            if (nRef >= 2 && SpcConstants.TryGetFactors(nRef, out var _, out var d3, out var d4))
            {
                var rBar = validSubgroups.Average(x => x.Range);
                uclRrange = d4 * rBar;
                lclRrange = d3 * rBar;
                rControl = new { cl = rBar, ucl = uclRrange, lcl = lclRrange, n = nRef, d3, d4, rBar };
            }

            xbarControl = new { cl = meanOfXbar, ucl = uclXbar, lcl = lclXbar, n = nRef, calculationMethod = "SIGMA_METHOD", sXbar, multiplier, xDoubleBar = meanOfXbar };
        }
        else if (useMrMethod && validSubgroups.Count >= 2)
        {
            var mrList = new List<double>();
            for (int i = 1; i < validSubgroups.Count; i++)
            {
                mrList.Add(Math.Abs(validSubgroups[i].Mean - validSubgroups[i - 1].Mean));
            }
            var mrBarXbar = mrList.Average();

            uclXbar = meanOfXbar + mrMultiplier * mrBarXbar;
            lclXbar = meanOfXbar - mrMultiplier * mrBarXbar;

            if (nRef >= 2 && SpcConstants.TryGetFactors(nRef, out var _, out var d3, out var d4))
            {
                var rBar = validSubgroups.Average(x => x.Range);
                uclRrange = d4 * rBar;
                lclRrange = d3 * rBar;
                rControl = new { cl = rBar, ucl = uclRrange, lcl = lclRrange, n = nRef, d3, d4, rBar };
            }

            xbarControl = new { cl = meanOfXbar, ucl = uclXbar, lcl = lclXbar, n = nRef, calculationMethod = "MR_METHOD", mrBarXbar, mrMultiplier, xDoubleBar = meanOfXbar };
        }
        else if (nRef >= 2 && SpcConstants.TryGetFactors(nRef, out var a2, out var d3, out var d4))
        {
            var rBar = validSubgroups.Average(x => x.Range);
            uclXbar = meanOfXbar + a2 * rBar;
            lclXbar = meanOfXbar - a2 * rBar;
            uclRrange = d4 * rBar;
            lclRrange = d3 * rBar;

            xbarControl = new { cl = meanOfXbar, ucl = uclXbar, lcl = lclXbar, n = nRef, a2, rBar, xDoubleBar = meanOfXbar };
            rControl = new { cl = rBar, ucl = uclRrange, lcl = lclRrange, n = nRef, d3, d4, rBar };
        }

        // Only evaluate rules on non-excluded points
        var evalPoints = subgroups.Where(x => !x.IsExcluded).Select(x => new SpcDataPoint { MeasuredAt = x.MeasuredAt, Value = x.Mean }).ToList();
        if (uclXbar.HasValue && lclXbar.HasValue && nRef >= 2 && validSubgroups.Count > 0)
        {
            var statLimits = new ControlLimits { CL = validSubgroups.Average(x => x.Mean), UCL = uclXbar, LCL = lclXbar };
            MesSpc.Api.SpcEngine.Rules.WesternElectricRulesValidator.ApplyRules(evalPoints, statLimits);
        }
        
        // Merge rules back
        var evalDict = evalPoints.ToDictionary(x => x.MeasuredAt);

        var xbarPoints = new List<object>();
        var rPoints = new List<object>();
        for (int i = 0; i < subgroups.Count; i++)
        {
            var x = subgroups[i];
            var p = evalDict.GetValueOrDefault(x.MeasuredAt);
            var xbar = x.Mean;
            var range = x.Range;
            var oos = (configuredLimits.USL.HasValue && xbar > configuredLimits.USL.Value) || 
                      (configuredLimits.LSL.HasValue && xbar < configuredLimits.LSL.Value);
            var oocStat = p?.IsOutOfControl ?? false; 
            var oocR = uclRrange.HasValue && lclRrange.HasValue && (range > uclRrange.Value || range < lclRrange.Value);
            var outOfSpec = !x.IsExcluded && (x.OutOfSpec || oos);
            var outOfControl = !x.IsExcluded && (x.OutOfControl || oocStat || oocR);

            xbarPoints.Add(new
            {
                x.MeasuredAt,
                xbar,
                range,
                n = x.N,
                outOfSpec,
                outOfControl,
                outOfControlXbar = oocStat,
                outOfControlR = !x.IsExcluded && oocR,
                violatedRules = p?.ViolatedRules ?? new List<string>(),
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
            rPoints.Add(new { x.MeasuredAt, value = range, outOfControl = oocR, isExcluded = x.IsExcluded });
        }

        var capability = ProcessCapabilityCalculator.Calculate(validSubgroups, configuredLimits.USL, configuredLimits.LSL);

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
            SecondaryChartData = new { points = rPoints },
            Capability = capability
        };
    }
}
