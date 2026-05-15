using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class SpcService(AppDbContext db)
{
    public async Task<SpcCalculationResult?> CalculateVariableAsync(VariableMeasurement measurement, CancellationToken ct = default)
    {
        var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.Id == measurement.PartProcessCharacteristicId && x.IsEnabled, ct);
        if (mapping is null || !mapping.ChartTypeId.HasValue) return null;
        var chartType = await db.ControlChartTypes.FirstOrDefaultAsync(x => x.Id == mapping.ChartTypeId.Value && x.IsEnabled, ct);
        if (chartType is null) return null;

        var isOutOfSpec = (mapping.USL.HasValue && measurement.MeasuredValue > mapping.USL.Value)
                          || (mapping.LSL.HasValue && measurement.MeasuredValue < mapping.LSL.Value);
        var isOutOfControl = (mapping.UCL.HasValue && measurement.MeasuredValue > mapping.UCL.Value)
                             || (mapping.LCL.HasValue && measurement.MeasuredValue < mapping.LCL.Value);

        var result = new SpcCalculationResult
        {
            UploadBatchId = measurement.UploadBatchId,
            DataCategory = "Variable",
            VariableMeasurementId = measurement.Id,
            PartProcessCharacteristicId = mapping.Id,
            ChartTypeId = chartType.Id,
            RuleGroupId = mapping.RuleGroupId,
            StatisticName = chartType.ChartTypeCode,
            StatisticValue = measurement.MeasuredValue,
            USL = mapping.USL,
            LSL = mapping.LSL,
            UCL = mapping.UCL,
            CL = mapping.CL,
            LCL = mapping.LCL,
            IsOutOfSpec = isOutOfSpec,
            IsOutOfControl = isOutOfControl
        };
        db.SpcCalculationResults.Add(result);
        await db.SaveChangesAsync(ct);

        if (isOutOfSpec || isOutOfControl)
        {
            db.AlertEvents.Add(new AlertEvent
            {
                OccurredAt = DateTime.UtcNow,
                ProductId = measurement.PartId,
                StationId = measurement.ProcessId,
                InspectionItemId = measurement.CharacteristicId,
                ActualValue = measurement.MeasuredValue,
                AlertType = isOutOfSpec ? AlertType.OutOfSpec : AlertType.OutOfControl,
                Message = isOutOfSpec
                    ? $"Variable measurement violates spec limit. Value={measurement.MeasuredValue}"
                    : $"Variable measurement violates control limit. Value={measurement.MeasuredValue}"
            });
            await db.SaveChangesAsync(ct);
        }

        return result;
    }

    public async Task<SpcCalculationResult?> CalculateAttributeAsync(AttributeMeasurement measurement, CancellationToken ct = default)
    {
        var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.Id == measurement.PartProcessCharacteristicId && x.IsEnabled, ct);
        if (mapping is null || !mapping.ChartTypeId.HasValue) return null;
        var chartType = await db.ControlChartTypes.FirstOrDefaultAsync(x => x.Id == mapping.ChartTypeId.Value && x.IsEnabled, ct);
        if (chartType is null) return null;

        var statisticValue = CalculateAttributeStatistic(chartType.ChartTypeCode, measurement);
        var isOutOfControl = (mapping.UCL.HasValue && statisticValue.HasValue && statisticValue.Value > mapping.UCL.Value)
                             || (mapping.LCL.HasValue && statisticValue.HasValue && statisticValue.Value < mapping.LCL.Value);

        var result = new SpcCalculationResult
        {
            UploadBatchId = measurement.UploadBatchId,
            DataCategory = "Attribute",
            AttributeMeasurementId = measurement.Id,
            PartProcessCharacteristicId = mapping.Id,
            ChartTypeId = chartType.Id,
            RuleGroupId = mapping.RuleGroupId,
            StatisticName = chartType.ChartTypeCode,
            StatisticValue = statisticValue,
            USL = mapping.USL,
            LSL = mapping.LSL,
            UCL = mapping.UCL,
            CL = mapping.CL,
            LCL = mapping.LCL,
            IsOutOfSpec = false,
            IsOutOfControl = isOutOfControl
        };
        db.SpcCalculationResults.Add(result);
        await db.SaveChangesAsync(ct);

        if (isOutOfControl)
        {
            db.AlertEvents.Add(new AlertEvent
            {
                OccurredAt = DateTime.UtcNow,
                ProductId = measurement.PartId,
                StationId = measurement.ProcessId,
                InspectionItemId = measurement.CharacteristicId,
                ActualValue = statisticValue,
                AlertType = AlertType.OutOfControl,
                Message = $"Attribute measurement violates control limit. Chart={chartType.ChartTypeCode}, Value={statisticValue}"
            });
            await db.SaveChangesAsync(ct);
        }

        return result;
    }

    public async Task<List<AlertEvent>> EvaluateBatchAsync(MeasurementBatch batch, CancellationToken ct = default)
    {
        var alerts = new List<AlertEvent>();
        var itemIds = batch.Values.Select(x => x.InspectionItemId).Distinct().ToList();
        var items = await db.InspectionItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        foreach (var value in batch.Values.Where(x => x.ValueNumeric.HasValue))
        {
            if (!items.TryGetValue(value.InspectionItemId, out var item)) continue;
            var v = value.ValueNumeric!.Value;

            if (item.Usl.HasValue && v > item.Usl.Value)
                alerts.Add(NewAlert(batch, value, item, v, AlertType.OutOfSpec, $"Value {v} > USL {item.Usl}"));
            if (item.Lsl.HasValue && v < item.Lsl.Value)
                alerts.Add(NewAlert(batch, value, item, v, AlertType.OutOfSpec, $"Value {v} < LSL {item.Lsl}"));
            if (item.Ucl.HasValue && v > item.Ucl.Value)
                alerts.Add(NewAlert(batch, value, item, v, AlertType.OutOfControl, $"Value {v} > UCL {item.Ucl}"));
            if (item.Lcl.HasValue && v < item.Lcl.Value)
                alerts.Add(NewAlert(batch, value, item, v, AlertType.OutOfControl, $"Value {v} < LCL {item.Lcl}"));
        }

        if (alerts.Count > 0)
        {
            await db.AlertEvents.AddRangeAsync(alerts, ct);
            await db.SaveChangesAsync(ct);
        }
        return alerts;
    }

    public async Task<object?> GetImrChartAsync(int inspectionItemId, int productId, int stationId, CancellationToken ct = default)
    {
        var item = await db.InspectionItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inspectionItemId, ct);
        if (item is null) return null;

        var raw = await db.MeasurementValues
            .Where(v => v.InspectionItemId == inspectionItemId && v.ValueNumeric.HasValue)
            .Join(db.MeasurementBatches.Where(b => b.ProductId == productId && b.StationId == stationId),
                v => v.BatchId, b => b.Id, (v, b) => new { b.MeasuredAt, Value = v.ValueNumeric!.Value })
            .OrderBy(x => x.MeasuredAt)
            .Take(500)
            .ToListAsync(ct);

        var values = raw.Select(x => x.Value).ToList();

        // I chart based on Individuals (I-MR). Here we compute the statistical control limits from MR.
        // Constants for MR chart with subgroup size = 2:
        //   d2 = 1.128, D3 = 0, D4 = 3.267
        // Reference: common SPC constants table (Montgomery).
        const double d2 = 1.128;
        const double D3 = 0.0;
        const double D4 = 3.267;

        var iBar = values.Count > 0 ? values.Average() : (double?)null;
        var mrValues = new List<double>();
        for (var i = 1; i < values.Count; i++)
        {
            mrValues.Add(Math.Abs(values[i] - values[i - 1]));
        }
        var mrBar = mrValues.Count > 0 ? mrValues.Average() : (double?)null;

        var sigma = (mrBar.HasValue && d2 > 0) ? (mrBar.Value / d2) : (double?)null;
        var iUclStat = (iBar.HasValue && sigma.HasValue) ? (iBar.Value + 3 * sigma.Value) : (double?)null;
        var iLclStat = (iBar.HasValue && sigma.HasValue) ? (iBar.Value - 3 * sigma.Value) : (double?)null;

        var mrUclStat = (mrBar.HasValue) ? (D4 * mrBar.Value) : (double?)null;
        var mrLclStat = (mrBar.HasValue) ? (D3 * mrBar.Value) : (double?)null;

        var iPoints = new List<object>();
        foreach (var p in raw)
        {
            var v = p.Value;
            var oos = (item.Usl.HasValue && v > item.Usl.Value) || (item.Lsl.HasValue && v < item.Lsl.Value);
            var oocConfigured = (item.Ucl.HasValue && v > item.Ucl.Value) || (item.Lcl.HasValue && v < item.Lcl.Value);

            // Keep configured control limits for outOfControl so that AlertEvent generation matches.
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

        var limits = new
        {
            usl = item.Usl,
            lsl = item.Lsl,
            ucl = item.Ucl,
            lcl = item.Lcl,
            target = item.TargetValue
        };

        var statControl = new
        {
            iControlLimitsStat = new { cl = iBar, ucl = iUclStat, lcl = iLclStat },
            mrControlLimitsStat = new { cl = mrBar, ucl = mrUclStat, lcl = mrLclStat }
        };

        return new { chartType = "I-MR", limits, statControl, iChart = new { points = iPoints }, mrChart = new { points = mrPoints } };
    }

    public async Task<object?> GetXbarRChartAsync(int inspectionItemId, int productId, int stationId, CancellationToken ct = default)
    {
        var item = await db.InspectionItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inspectionItemId, ct);
        if (item is null) return null;

        var psi = await db.ProductStationItems.AsNoTracking()
            .Where(x => x.ProductId == productId && x.StationId == stationId && x.InspectionItemId == inspectionItemId && x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        var groupedRaw = await db.MeasurementBatches
            .Where(b => b.ProductId == productId && b.StationId == stationId)
            .Select(b => new
            {
                b.Id,
                b.MeasuredAt,
                Values = b.Values.Where(v => v.InspectionItemId == inspectionItemId && v.ValueNumeric.HasValue).Select(v => v.ValueNumeric!.Value).ToList()
            })
            .Where(x => x.Values.Count > 0)
            .OrderBy(x => x.MeasuredAt)
            .Take(200)
            .ToListAsync(ct);

        if (groupedRaw.Count == 0)
        {
            return new
            {
                chartType = "XBAR_R",
                limits = new { usl = item.Usl, lsl = item.Lsl, ucl = item.Ucl, lcl = item.Lcl, target = item.TargetValue },
                xbarControlLimits = (object?)null,
                rControlLimits = (object?)null,
                subgroupSize = 0,
                subgroupSizeNote = (string?)null,
                xbarChart = new { points = Array.Empty<object>() },
                rChart = new { points = Array.Empty<object>() }
            };
        }

        var nRef = psi?.SampleSize is > 0 and var psN ? psN : groupedRaw.FirstOrDefault()?.Values.Count ?? 0;
        if (nRef < 2) nRef = groupedRaw.FirstOrDefault()?.Values.Count ?? 0;
        var grouped = groupedRaw.Where(x => x.Values.Count == nRef).ToList();
        if (grouped.Count == 0)
        {
            grouped = groupedRaw;
            nRef = grouped.First().Values.Count;
        }

        object? xbarControl = null;
        object? rControl = null;
        double? uclXbar = null;
        double? lclXbar = null;
        double? uclRrange = null;
        double? lclRrange = null;

        if (nRef >= 2 && SpcConstants.TryGetFactors(nRef, out var a2, out var d3, out var d4))
        {
            var xbars = grouped.Select(x => x.Values.Average()).ToList();
            var ranges = grouped.Select(x => x.Values.Max() - x.Values.Min()).ToList();
            var meanOfXbar = xbars.Average();
            var rBar = ranges.Average();
            uclXbar = meanOfXbar + a2 * rBar;
            lclXbar = meanOfXbar - a2 * rBar;
            uclRrange = d4 * rBar;
            lclRrange = d3 * rBar;
            xbarControl = new { cl = meanOfXbar, ucl = uclXbar, lcl = lclXbar, n = nRef, a2, rBar, xDoubleBar = meanOfXbar };
            rControl = new { cl = rBar, ucl = uclRrange, lcl = lclRrange, n = nRef, d3, d4, rBar };
        }

        var xbarPoints = new List<object>();
        var rPoints = new List<object>();
        foreach (var x in grouped)
        {
            var xbar = x.Values.Average();
            var range = x.Values.Max() - x.Values.Min();
            var oos = (item.Usl.HasValue && xbar > item.Usl.Value) || (item.Lsl.HasValue && xbar < item.Lsl.Value);
            var oocStat = uclXbar.HasValue && lclXbar.HasValue && (xbar > uclXbar.Value || xbar < lclXbar.Value);
            var oocR = uclRrange.HasValue && lclRrange.HasValue && (range > uclRrange.Value || range < lclRrange.Value);

            xbarPoints.Add(new
            {
                x.MeasuredAt,
                xbar,
                range,
                n = x.Values.Count,
                outOfSpec = oos,
                outOfControl = oocStat || oocR,
                outOfControlXbar = oocStat,
                outOfControlR = oocR
            });
            rPoints.Add(new { x.MeasuredAt, value = range, outOfControl = oocR });
        }

        var limits = new
        {
            usl = item.Usl,
            lsl = item.Lsl,
            ucl = item.Ucl,
            lcl = item.Lcl,
            target = item.TargetValue
        };

        return new
        {
            chartType = "XBAR_R",
            limits,
            xbarControlLimits = xbarControl,
            rControlLimits = rControl,
            subgroupSize = nRef,
            subgroupSizeNote = groupedRaw.Any(x => x.Values.Count != nRef)
                ? "部分批次子組大小與基準 n 不一致，已改用可解析之 n 或納入全部子組；建議每批同子組數。"
                : null,
            xbarChart = new { points = xbarPoints },
            rChart = new { points = rPoints }
        };
    }

    private static AlertEvent NewAlert(MeasurementBatch batch, MeasurementValue value, InspectionItem item, double actual, AlertType type, string msg) =>
        new()
        {
            OccurredAt = DateTime.UtcNow,
            ProductId = batch.ProductId,
            StationId = batch.StationId,
            InspectionItemId = item.Id,
            ActualValue = actual,
            AlertType = type,
            Message = msg,
            BatchId = batch.Id,
            MeasurementValueId = value.Id
        };

    private static double? CalculateAttributeStatistic(string chartTypeCode, AttributeMeasurement measurement)
    {
        var code = chartTypeCode.Trim().ToUpperInvariant();
        return code switch
        {
            "P" or "P_CHART" or "P-CHART" => measurement.InspectedQty > 0 && measurement.DefectQty.HasValue
                ? (double)measurement.DefectQty.Value / measurement.InspectedQty.Value
                : null,
            "NP" or "NP_CHART" or "NP-CHART" => measurement.DefectQty,
            "C" or "C_CHART" or "C-CHART" => measurement.DefectCount,
            "U" or "U_CHART" or "U-CHART" => measurement.UnitCount > 0 && measurement.DefectCount.HasValue
                ? (double)measurement.DefectCount.Value / measurement.UnitCount.Value
                : null,
            _ => null
        };
    }
}
