using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Calculators;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class SpcService(AppDbContext db, IEmailNotificationService emailService, IConfiguration config)
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

        List<SpcRuleViolation>? violations = null;
        if (mapping.RuleGroupId.HasValue && mapping.CL.HasValue && mapping.UCL.HasValue && mapping.LCL.HasValue)
        {
            var rules = await db.SpcRules.AsNoTracking().Where(x => x.RuleGroupId == mapping.RuleGroupId.Value && x.IsEnabled).ToListAsync(ct);
            if (rules.Count > 0)
            {
                var lastMeasurements = await db.VariableMeasurements.AsNoTracking()
                    .Where(x => x.PartProcessCharacteristicId == mapping.Id)
                    .OrderByDescending(x => x.MeasuredAt)
                    .Take(30)
                    .Select(x => x.MeasuredValue)
                    .ToListAsync(ct);
                
                lastMeasurements.Insert(0, measurement.MeasuredValue);
                lastMeasurements.Reverse();

                violations = SpcRuleEngine.EvaluateRules(lastMeasurements, rules, mapping.CL.Value, mapping.UCL.Value, mapping.LCL.Value);
                if (violations.Count > 0)
                {
                    isOutOfControl = true;
                }
            }
        }
        
        string? violatedRulesJson = violations?.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(violations) : null;

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
            IsOutOfControl = isOutOfControl,
            ViolatedRulesJson = violatedRulesJson
        };
        db.SpcCalculationResults.Add(result);
        await db.SaveChangesAsync(ct);

        if (isOutOfSpec || isOutOfControl)
        {
            var alertMessage = isOutOfSpec
                ? $"Variable measurement violates spec limit. Value={measurement.MeasuredValue}"
                : $"Variable measurement violates control limit. Value={measurement.MeasuredValue}";

            if (violations?.Count > 0)
            {
                alertMessage = "SPC Rules Violated: " + string.Join(", ", violations.Select(x => x.RuleName));
            }

            var alert = new AlertEvent
            {
                OccurredAt = DateTime.UtcNow,
                PartId = measurement.PartId,
                ProcessId = measurement.ProcessId,
                CharacteristicId = measurement.CharacteristicId,
                UploadBatchId = measurement.UploadBatchId,
                VariableMeasurementId = measurement.Id,
                ActualValue = measurement.MeasuredValue,
                AlertType = isOutOfSpec ? AlertType.OutOfSpec : AlertType.OutOfControl,
                Message = alertMessage
            };
            db.AlertEvents.Add(alert);
            await db.SaveChangesAsync(ct);

            var defaultEmail = config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw";
            await emailService.SendAlertEmailAsync(alert, defaultEmail, "品管工程師");
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
            var alert = new AlertEvent
            {
                OccurredAt = DateTime.UtcNow,
                PartId = measurement.PartId,
                ProcessId = measurement.ProcessId,
                CharacteristicId = measurement.CharacteristicId,
                UploadBatchId = measurement.UploadBatchId,
                AttributeMeasurementId = measurement.Id,
                ActualValue = statisticValue,
                AlertType = AlertType.OutOfControl,
                Message = $"Attribute measurement violates control limit. Chart={chartType.ChartTypeCode}, Value={statisticValue}"
            };
            db.AlertEvents.Add(alert);
            await db.SaveChangesAsync(ct);

            var defaultEmail = config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw";
            await emailService.SendAlertEmailAsync(alert, defaultEmail, "品管工程師");
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

            var defaultEmail = config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw";
            foreach (var alert in alerts)
            {
                await emailService.SendAlertEmailAsync(alert, defaultEmail, "品管工程師");
            }
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

        var spcPoints = raw.Select(x => new SpcDataPoint { MeasuredAt = x.MeasuredAt, Value = x.Value }).ToList();
        var limits = new ControlLimits
        {
            USL = item.Usl,
            LSL = item.Lsl,
            UCL = item.Ucl,
            LCL = item.Lcl,
            Target = item.TargetValue
        };

        return ImrChartCalculator.Calculate(spcPoints, limits);
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

        var expectedN = psi?.SampleSize ?? 0;
        var subgroups = groupedRaw.Select(g => new Subgroup
        {
            MeasuredAt = g.MeasuredAt,
            Values = g.Values
        }).ToList();
        
        var limits = new ControlLimits
        {
            USL = item.Usl,
            LSL = item.Lsl,
            UCL = item.Ucl,
            LCL = item.Lcl,
            Target = item.TargetValue
        };

        return XbarRChartCalculator.Calculate(subgroups, limits, expectedN);
    }

    public async Task<ControlChartResult?> GetInteractiveChartAsync(int partProcessCharacteristicId, Guid? uploadBatchId, CancellationToken ct = default)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == partProcessCharacteristicId && x.IsEnabled, ct);
        if (mapping is null || !mapping.ChartTypeId.HasValue) return null;

        var chartType = await db.ControlChartTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == mapping.ChartTypeId.Value && x.IsEnabled, ct);
        if (chartType is null) return null;

        var limits = new ControlLimits
        {
            USL = mapping.USL,
            LSL = mapping.LSL,
            Target = mapping.TargetValue,
            UCL = mapping.UCL,
            CL = mapping.CL,
            LCL = mapping.LCL
        };

        if (chartType.DataCategory == "Variable")
        {
            var query = db.VariableMeasurements.AsNoTracking().Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId);
            if (uploadBatchId.HasValue) query = query.Where(x => x.UploadBatchId == uploadBatchId.Value);

            var measurements = await query.OrderBy(x => x.MeasuredAt).Take(1000).ToListAsync(ct);
            if (measurements.Count == 0) return null;
            var excludedUploadBatchIds = await GetExcludedUploadBatchIdsAsync(measurements.Select(x => x.UploadBatchId), ct);

            var rawPoints = measurements.Select(x => new SpcDataPoint
            {
                MeasuredAt = x.MeasuredAt,
                Value = x.MeasuredValue,
                LotNo = x.LotNo,
                SerialNo = x.SerialNo,
                Operator = x.Operator,
                LineId = x.LineId,
                TankId = x.TankId,
                SlotId = x.SlotId,
                SideCode = x.SideCode.ToString(),
                IsExcluded = excludedUploadBatchIds.Contains(x.UploadBatchId)
            }).ToList();

            if (chartType.ChartTypeCode == "I_MR" || chartType.ChartTypeCode == "I-MR")
            {
                return ImrChartCalculator.Calculate(rawPoints, limits) with { RawDataPoints = rawPoints };
            }
            else // XBAR_R / XBAR_S
            {
                var expectedSampleSizeVal = mapping.SampleSize > 0 ? mapping.SampleSize : (chartType.RequiredSampleSize ?? 0) > 0 ? chartType.RequiredSampleSize!.Value : 5;
                var grouped = measurements.GroupBy(x => x.MeasuredAt).Select(g =>
                {
                    var first = g.First();
                    return new Subgroup
                    {
                        MeasuredAt = g.Key,
                        Values = g.Select(m => m.MeasuredValue).ToList(),
                        LotNo = first.LotNo,
                        SerialNo = first.SerialNo,
                        Operator = first.Operator,
                        LineId = first.LineId,
                        TankId = first.TankId,
                        SlotId = first.SlotId,
                        SideCode = first.SideCode.ToString(),
                        IsExcluded = g.Any(m => excludedUploadBatchIds.Contains(m.UploadBatchId))
                    };
                }).ToList();

                if (chartType.ChartTypeCode == "XBAR_S" || chartType.ChartTypeCode == "XBAR-S")
                {
                    return XbarSChartCalculator.Calculate(grouped, limits, expectedSampleSizeVal) with { RawDataPoints = rawPoints };
                }

                return XbarRChartCalculator.Calculate(grouped, limits, expectedSampleSizeVal, chartType.FormulaConfigJson) with { RawDataPoints = rawPoints };
            }
        }
        else // Attribute
        {
            var query = db.AttributeMeasurements.AsNoTracking().Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId);
            if (uploadBatchId.HasValue) query = query.Where(x => x.UploadBatchId == uploadBatchId.Value);

            var measurements = await query.OrderBy(x => x.MeasuredAt).Take(1000).ToListAsync(ct);
            if (measurements.Count == 0) return null;
            var excludedUploadBatchIds = await GetExcludedUploadBatchIdsAsync(measurements.Select(x => x.UploadBatchId), ct);

            var points = measurements.Select(x => new AttributeDataPoint
            {
                MeasuredAt = x.MeasuredAt,
                InspectedQty = x.InspectedQty,
                DefectQty = x.DefectQty,
                DefectCount = x.DefectCount,
                UnitCount = x.UnitCount,
                LotNo = x.LotNo,
                Operator = x.Operator,
                LineId = x.LineId,
                TankId = x.TankId,
                SlotId = x.SlotId,
                SideCode = x.SideCode.ToString(),
                IsExcluded = excludedUploadBatchIds.Contains(x.UploadBatchId)
            }).ToList();

            return AttributeChartCalculator.Calculate(chartType.ChartTypeCode, points, limits) with { RawDataPoints = points };
        }
    }

    private async Task<HashSet<Guid>> GetExcludedUploadBatchIdsAsync(IEnumerable<Guid> uploadBatchIds, CancellationToken ct)
    {
        var ids = uploadBatchIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await db.UploadBatches.AsNoTracking()
            .Where(x => ids.Contains(x.UploadBatchId) && x.IsExcluded)
            .Select(x => x.UploadBatchId)
            .ToHashSetAsync(ct);
    }

    private static AlertEvent NewAlert(MeasurementBatch batch, MeasurementValue value, InspectionItem item, double actual, AlertType type, string msg) =>
        new()
        {
            OccurredAt = DateTime.UtcNow,
            PartId = batch.ProductId,
            ProcessId = batch.StationId,
            CharacteristicId = item.Id,
            MeasurementBatchId = batch.Id,
            ActualValue = actual,
            AlertType = type,
            Message = msg
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

    public async Task<ControlChartResult?> GetInteractiveChartV2Async(int productId, int stationId, int inspectionItemId, string? batchNo = null, CancellationToken ct = default)
    {
        var item = await db.InspectionItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inspectionItemId, ct);
        if (item is null) return null;

        var psi = await db.ProductStationItems.AsNoTracking()
            .Where(x => x.ProductId == productId && x.StationId == stationId && x.InspectionItemId == inspectionItemId && x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        var query = db.MeasurementBatches.Include(x => x.Values).Where(b => b.ProductId == productId && b.StationId == stationId);
        if (!string.IsNullOrWhiteSpace(batchNo)) query = query.Where(x => x.BatchNo == batchNo || x.LotNo == batchNo);

        var groupedRaw = await query
            .OrderBy(x => x.MeasuredAt)
            .Take(1000)
            .Select(b => new
            {
                b.Id,
                b.BatchNo,
                b.MeasuredAt,
                b.LotNo,
                b.SerialNo,
                b.OperatorName,
                b.IsExcluded,
                Values = b.Values.Where(v => v.InspectionItemId == inspectionItemId && v.ValueNumeric.HasValue).Select(v => v.ValueNumeric!.Value).ToList()
            })
            .Where(x => x.Values.Count > 0)
            .ToListAsync(ct);

        if (groupedRaw.Count == 0) return null;

        var batchIds = groupedRaw.Select(x => x.Id).ToList();
        
        // Fetch alerts for these batches to attach RootCause/CorrectiveAction
        var alerts = await db.AlertEvents.AsNoTracking()
            .Where(a => a.MeasurementBatchId.HasValue && batchIds.Contains(a.MeasurementBatchId.Value) && a.CharacteristicId == inspectionItemId)
            .ToListAsync(ct);

        var alertLookup = alerts.GroupBy(a => a.MeasurementBatchId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var subgroups = groupedRaw.Select(g => 
        {
            var batchAlerts = alertLookup.GetValueOrDefault(g.Id) ?? [];
            var outOfSpec = batchAlerts.Any(a => a.AlertType == AlertType.OutOfSpec);
            var outOfControl = batchAlerts.Any(a => a.AlertType == AlertType.OutOfControl);
            var rootCause = batchAlerts.FirstOrDefault(a => !string.IsNullOrEmpty(a.RootCause))?.RootCause;
            var correctiveAction = batchAlerts.FirstOrDefault(a => !string.IsNullOrEmpty(a.CorrectiveAction))?.CorrectiveAction;

            return new Subgroup
            {
                MeasuredAt = g.MeasuredAt,
                Values = g.Values,
                LotNo = g.LotNo ?? g.BatchNo,
                SerialNo = g.SerialNo,
                Operator = g.OperatorName,
                IsExcluded = g.IsExcluded,
                OutOfSpec = outOfSpec,
                OutOfControl = outOfControl,
                RootCause = rootCause,
                CorrectiveAction = correctiveAction,
                MeasurementBatchId = g.Id
            };
        }).ToList();
        
        var limits = new ControlLimits
        {
            USL = item.Usl,
            LSL = item.Lsl,
            UCL = item.Ucl,
            LCL = item.Lcl,
            Target = item.TargetValue
        };

        var expectedN = psi?.SampleSize ?? 5; // Default to 5 if not set

        if (expectedN == 1)
        {
            var points = subgroups.Select(x => new SpcDataPoint
            {
                MeasuredAt = x.MeasuredAt,
                Value = x.Values.First(),
                LotNo = x.LotNo,
                SerialNo = x.SerialNo,
                Operator = x.Operator,
                IsExcluded = x.IsExcluded,
                IsOutOfSpec = x.OutOfSpec,
                IsOutOfControl = x.OutOfControl,
                RootCause = x.RootCause,
                CorrectiveAction = x.CorrectiveAction,
                MeasurementBatchId = x.MeasurementBatchId
            }).ToList();
            return ImrChartCalculator.Calculate(points, limits) with { RawDataPoints = points };
        }
        else
        {
            var rawPoints = subgroups.SelectMany(x => x.Values.Select((val, idx) => new SpcDataPoint
            {
                MeasuredAt = x.MeasuredAt,
                Value = val,
                LotNo = x.LotNo,
                SerialNo = x.SerialNo,
                Operator = x.Operator,
                IsExcluded = x.IsExcluded,
                IsOutOfSpec = x.OutOfSpec,
                IsOutOfControl = x.OutOfControl,
                RootCause = x.RootCause,
                CorrectiveAction = x.CorrectiveAction,
                MeasurementBatchId = x.MeasurementBatchId
            })).ToList();
            return XbarRChartCalculator.Calculate(subgroups, limits, expectedN) with { RawDataPoints = rawPoints };
        }
    }
}
