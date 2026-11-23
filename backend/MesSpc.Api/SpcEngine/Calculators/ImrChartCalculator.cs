using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class ImrChartCalculator
{
    // Constants for MR chart with subgroup size = 2
    private const double D2 = 1.128;
    private const double D3 = 0.0;
    private const double D4 = 3.267;

    public static ControlChartResult Calculate(
        List<SpcDataPoint> rawData,
        ControlLimits configuredLimits,
        IReadOnlySet<string>? enabledRuleCodes = null,
        string? formulaConfigJson = null)
    {
        var validData = rawData.Where(x => !x.IsExcluded).ToList();
        var values = validData.Select(x => x.Value).ToList();
        
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
        var useSelectedMethodLimits = false;
        object iControlLimitsStat = new { cl = iBar, ucl = iUclStat, lcl = iLclStat };

        string? selectedMethod = null;
        var multiplier = 3.0;
        var mrMultiplier = 2.66;
        if (!string.IsNullOrWhiteSpace(formulaConfigJson))
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(formulaConfigJson);
                if (document.RootElement.TryGetProperty("XbarCalculationMethod", out var methodProperty))
                    selectedMethod = methodProperty.GetString()?.Trim().ToUpperInvariant();
                if (document.RootElement.TryGetProperty("Multiplier", out var multiplierProperty)
                    && multiplierProperty.TryGetDouble(out var parsedMultiplier) && parsedMultiplier > 0)
                    multiplier = parsedMultiplier;
                if (document.RootElement.TryGetProperty("MrMultiplier", out var mrMultiplierProperty)
                    && mrMultiplierProperty.TryGetDouble(out var parsedMrMultiplier) && parsedMrMultiplier > 0)
                    mrMultiplier = parsedMrMultiplier;
            }
            catch (System.Text.Json.JsonException)
            {
                selectedMethod = null;
            }
        }

        if (selectedMethod is "SIGMA_METHOD" or "SAMPLE_STD_DEV" && values.Count >= 2 && iBar.HasValue)
        {
            var sampleSigma = Math.Sqrt(values.Sum(value => Math.Pow(value - iBar.Value, 2)) / (values.Count - 1));
            iUclStat = iBar.Value + multiplier * sampleSigma;
            iLclStat = iBar.Value - multiplier * sampleSigma;
            useSelectedMethodLimits = true;
            iControlLimitsStat = new { cl = iBar, ucl = iUclStat, lcl = iLclStat, calculationMethod = "SIGMA_METHOD", sampleSigma, multiplier };
        }
        else if (selectedMethod is "MOVING_RANGE_OF_XBAR" or "MR_METHOD" && iBar.HasValue && mrBar.HasValue)
        {
            iUclStat = iBar.Value + mrMultiplier * mrBar.Value;
            iLclStat = iBar.Value - mrMultiplier * mrBar.Value;
            useSelectedMethodLimits = true;
            iControlLimitsStat = new { cl = iBar, ucl = iUclStat, lcl = iLclStat, calculationMethod = "MR_METHOD", mrBar, mrMultiplier };
        }

        var mrUclStat = mrBar.HasValue ? D4 * mrBar.Value : (double?)null;
        var mrLclStat = mrBar.HasValue ? D3 * mrBar.Value : (double?)null;

        if (iBar.HasValue && iUclStat.HasValue && iLclStat.HasValue && validData.Count > 0)
        {
            var statLimits = new ControlLimits { CL = iBar, UCL = iUclStat, LCL = iLclStat };
            MesSpc.Api.SpcEngine.Rules.WesternElectricRulesValidator.ApplyRules(validData, statLimits, enabledRuleCodes);
        }
        var iPoints = new List<object>();
        foreach (var p in rawData)
        {
            var v = p.Value;
            var oos = (configuredLimits.USL.HasValue && v > configuredLimits.USL.Value) || 
                      (configuredLimits.LSL.HasValue && v < configuredLimits.LSL.Value);
            var activeUcl = useSelectedMethodLimits ? iUclStat : p.UCL ?? configuredLimits.UCL;
            var activeLcl = useSelectedMethodLimits ? iLclStat : p.LCL ?? configuredLimits.LCL;
            var activeCl = useSelectedMethodLimits ? iBar : p.CL ?? configuredLimits.CL ?? iBar;

            var oocConfigured = (activeUcl.HasValue && v > activeUcl.Value) || 
                                (activeLcl.HasValue && v < activeLcl.Value);
            var oocStat = !p.IsExcluded && p.IsOutOfControl;
            var outOfSpec = !p.IsExcluded && (p.IsOutOfSpec || oos);
            var outOfControl = !p.IsExcluded && (p.IsOutOfControl || oocConfigured || oocStat);

            iPoints.Add(new
            {
                measuredAt = p.MeasuredAt,
                portalDailyDate = p.PortalDailyDate,
                samplingPhase = p.SamplingPhase,
                samplingStage = p.SamplingStage,
                value = v,
                outOfSpec,
                outOfControl,
                outOfControlStat = oocStat,
                violatedRules = p.ViolatedRules ?? new List<string>(),
                uclStat = activeUcl ?? iUclStat,
                lclStat = activeLcl ?? iLclStat,
                clStat = activeCl,
                lotNo = p.LotNo,
                serialNo = p.SerialNo,
                @operator = p.Operator,
                isExcluded = p.IsExcluded,
                rootCause = p.RootCause,
                correctiveAction = p.CorrectiveAction,
                measurementBatchId = p.MeasurementBatchId,
                variableMeasurementId = p.VariableMeasurementId,
                alertId = p.AlertId,
                alertStatus = p.AlertStatus,
                responsibleUser = p.ResponsibleUser
            });
        }

        var mrPoints = new List<object>();
        for (var i = 1; i < rawData.Count; i++)
        {
            var mr = Math.Abs(rawData[i].Value - rawData[i - 1].Value);
            var isExcluded = rawData[i].IsExcluded || rawData[i - 1].IsExcluded;
            var outOfControl = !isExcluded && ((mrUclStat.HasValue && mr > mrUclStat.Value) || (mrLclStat.HasValue && mr < mrLclStat.Value));
            mrPoints.Add(new { index = i + 1, value = mr, outOfControl, isExcluded = rawData[i].IsExcluded });
        }

        var dummySubgroups = validData.Select(x => new Subgroup { MeasuredAt = x.MeasuredAt, Values = [x.Value] }).ToList();
        var capability = ProcessCapabilityCalculator.Calculate(dummySubgroups, configuredLimits.USL, configuredLimits.LSL);

        return new ControlChartResult
        {
            ChartType = "I-MR",
            Limits = configuredLimits,
            StatControlLimits = new
            {
                iControlLimitsStat,
                mrControlLimitsStat = new { cl = mrBar, ucl = mrUclStat, lcl = mrLclStat }
            },
            ChartData = new { points = iPoints },
            SecondaryChartData = new { points = mrPoints },
            SubgroupSize = 1,
            Capability = capability
        };
    }
}
