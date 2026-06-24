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
        var ruleGroupId = mapping.RuleGroupId ?? chartType.RuleGroupId;

        List<SpcRuleViolation>? violations = null;
        if (ruleGroupId.HasValue && mapping.CL.HasValue && mapping.UCL.HasValue && mapping.LCL.HasValue)
        {
            var rules = await db.SpcRules.AsNoTracking().Where(x => x.RuleGroupId == ruleGroupId.Value && x.IsEnabled).ToListAsync(ct);
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
            RuleGroupId = ruleGroupId,
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
        var ruleGroupId = mapping.RuleGroupId ?? chartType.RuleGroupId;

        var result = new SpcCalculationResult
        {
            UploadBatchId = measurement.UploadBatchId,
            DataCategory = "Attribute",
            AttributeMeasurementId = measurement.Id,
            PartProcessCharacteristicId = mapping.Id,
            ChartTypeId = chartType.Id,
            RuleGroupId = ruleGroupId,
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

    public async Task<ControlChartResult?> GetInteractiveChartAsync(
        int partProcessCharacteristicId,
        Guid? uploadBatchId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? batchNo = null,
        int? partId = null,
        CancellationToken ct = default)
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
            if (!string.IsNullOrWhiteSpace(batchNo)) query = query.Where(x => x.LotNo == batchNo);
            if (partId.HasValue) query = query.Where(x => x.PartId == partId.Value);
            if (startDate.HasValue) query = query.Where(x => x.MeasuredAt >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(x => x.MeasuredAt < endDate.Value.Date.AddDays(1));

            var measurements = await query.OrderBy(x => x.MeasuredAt).Take(1000).ToListAsync(ct);
            if (measurements.Count == 0) return null;
            var excludedUploadBatchIds = await GetExcludedUploadBatchIdsAsync(measurements.Select(x => x.UploadBatchId), ct);
            var measurementIds = measurements.Select(x => x.Id).ToList();
            var alerts = await db.AlertEvents.AsNoTracking()
                .Where(a => a.VariableMeasurementId.HasValue && measurementIds.Contains(a.VariableMeasurementId.Value))
                .ToListAsync(ct);
            var alertLookup = alerts
                .GroupBy(a => a.VariableMeasurementId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.OccurredAt).First());

            var rawPoints = measurements.Select(x =>
            {
                alertLookup.TryGetValue(x.Id, out var alert);
                return new SpcDataPoint
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
                    IsExcluded = excludedUploadBatchIds.Contains(x.UploadBatchId),
                    IsOutOfSpec = (mapping.USL.HasValue && x.MeasuredValue > mapping.USL.Value)
                        || (mapping.LSL.HasValue && x.MeasuredValue < mapping.LSL.Value),
                    IsOutOfControl = alert?.AlertType == AlertType.OutOfControl,
                    RootCause = alert?.RootCause,
                    CorrectiveAction = alert?.CorrectiveAction,
                    VariableMeasurementId = x.Id,
                    AlertId = alert?.Id,
                    AlertStatus = alert?.Status,
                    ResponsibleUser = alert?.ResponsibleUser
                };
            }).ToList();

            ControlChartResult chartResultVal;
            if (chartType.ChartTypeCode == "I_MR" || chartType.ChartTypeCode == "I-MR")
            {
                chartResultVal = ImrChartCalculator.Calculate(rawPoints, limits) with { RawDataPoints = rawPoints };
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
                        IsExcluded = g.Any(m => excludedUploadBatchIds.Contains(m.UploadBatchId)),
                        VariableMeasurementId = first.Id,
                        AlertId = g.Select(m => alertLookup.GetValueOrDefault(m.Id)?.Id).FirstOrDefault(id => id.HasValue),
                        AlertStatus = g.Select(m => alertLookup.GetValueOrDefault(m.Id)?.Status).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
                        RootCause = g.Select(m => alertLookup.GetValueOrDefault(m.Id)?.RootCause).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
                        CorrectiveAction = g.Select(m => alertLookup.GetValueOrDefault(m.Id)?.CorrectiveAction).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
                        ResponsibleUser = g.Select(m => alertLookup.GetValueOrDefault(m.Id)?.ResponsibleUser).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                    };
                }).ToList();

                if (chartType.ChartTypeCode == "XBAR_S" || chartType.ChartTypeCode == "XBAR-S")
                {
                    chartResultVal = XbarSChartCalculator.Calculate(grouped, limits, expectedSampleSizeVal) with { RawDataPoints = rawPoints };
                }
                else
                {
                    var formulaConfigJson = string.IsNullOrWhiteSpace(mapping.FormulaConfigJson)
                        ? chartType.FormulaConfigJson
                        : mapping.FormulaConfigJson;
                    chartResultVal = XbarRChartCalculator.Calculate(grouped, limits, expectedSampleSizeVal, formulaConfigJson) with { RawDataPoints = rawPoints };
                }
            }

            return PopulateNormalityAndCurve(chartResultVal, rawPoints, limits);
        }
        else // Attribute
        {
            var query = db.AttributeMeasurements.AsNoTracking().Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId);
            if (uploadBatchId.HasValue) query = query.Where(x => x.UploadBatchId == uploadBatchId.Value);
            if (!string.IsNullOrWhiteSpace(batchNo)) query = query.Where(x => x.LotNo == batchNo);
            if (partId.HasValue) query = query.Where(x => x.PartId == partId.Value);
            if (startDate.HasValue) query = query.Where(x => x.MeasuredAt >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(x => x.MeasuredAt < endDate.Value.Date.AddDays(1));

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
            var chartRes = ImrChartCalculator.Calculate(points, limits) with { RawDataPoints = points };
            return PopulateNormalityAndCurve(chartRes, points, limits);
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
                CorrectiveAction = x.CorrectiveAction
            })).ToList();
            var chartRes = XbarRChartCalculator.Calculate(subgroups, limits, expectedN) with { RawDataPoints = rawPoints };
            return PopulateNormalityAndCurve(chartRes, rawPoints, limits);
        }
    }

    internal static ControlChartResult PopulateNormalityAndCurve(
        ControlChartResult result,
        List<SpcDataPoint> rawPoints,
        ControlLimits limits)
    {
        var nonExcludedValues = rawPoints
            .Where(x => !x.IsExcluded)
            .Select(x => x.Value)
            .ToList();

        if (nonExcludedValues.Count < 3)
        {
            return result;
        }

        // 1. Calculate Jarque-Bera Normality Test
        double mean = nonExcludedValues.Average();
        double sumSqDiff = nonExcludedValues.Sum(x => Math.Pow(x - mean, 2));
        double variance = sumSqDiff / (nonExcludedValues.Count - 1);
        double stdDev = Math.Sqrt(variance);

        double m2 = 0;
        double m3 = 0;
        double m4 = 0;
        int n = nonExcludedValues.Count;

        foreach (var x in nonExcludedValues)
        {
            double diff = x - mean;
            double diff2 = diff * diff;
            m2 += diff2;
            m3 += diff2 * diff;
            m4 += diff2 * diff2;
        }

        m2 /= n;
        m3 /= n;
        m4 /= n;

        double skewness = 0;
        double kurtosis = 0;
        double jbStatistic = 0;
        double pValue = 1.0;
        bool isNormal = true;
        string? note = null;

        if (m2 < 1e-15 || stdDev < 1e-15)
        {
            note = "數據無變異 (標準差為 0)，無法進行常態性檢定。";
        }
        else
        {
            skewness = m3 / Math.Pow(m2, 1.5);
            kurtosis = m4 / (m2 * m2);
            jbStatistic = (n / 6.0) * (skewness * skewness + Math.Pow(kurtosis - 3.0, 2) / 4.0);
            pValue = Math.Exp(-jbStatistic / 2.0);
            isNormal = pValue >= 0.05;
        }

        var normality = new NormalityTestResult
        {
            TestName = "Jarque-Bera",
            Statistic = jbStatistic,
            PValue = m2 < 1e-15 || stdDev < 1e-15 ? null : pValue,
            Skewness = skewness,
            Kurtosis = kurtosis,
            IsNormal = isNormal,
            Note = note
        };

        // 2. Generate Normal Curve Points
        var curvePoints = new List<NormalCurvePoint>();
        if (stdDev > 1e-15)
        {
            // Replicate frontend binWidth logic to get exact scale
            var numericDomainValues = new List<double>(nonExcludedValues);
            if (limits.LSL.HasValue) numericDomainValues.Add(limits.LSL.Value);
            if (limits.USL.HasValue) numericDomainValues.Add(limits.USL.Value);
            if (limits.Target.HasValue) numericDomainValues.Add(limits.Target.Value);
            if (limits.LCL.HasValue) numericDomainValues.Add(limits.LCL.Value);
            if (limits.CL.HasValue) numericDomainValues.Add(limits.CL.Value);
            if (limits.UCL.HasValue) numericDomainValues.Add(limits.UCL.Value);

            // Also check for dynamically calculated control limits in result.StatControlLimits
            if (result.StatControlLimits != null)
            {
                var type = result.StatControlLimits.GetType();
                var props = type.GetProperties();
                var limitGroupProp = props.FirstOrDefault(p =>
                    p.Name == "iControlLimitsStat" ||
                    p.Name == "xbarControl" ||
                    p.Name == "pControlLimitsStat" ||
                    p.Name == "npControlLimitsStat");

                if (limitGroupProp != null)
                {
                    var limitGroup = limitGroupProp.GetValue(result.StatControlLimits);
                    if (limitGroup != null)
                    {
                        var groupType = limitGroup.GetType();
                        var uclVal = groupType.GetProperty("ucl")?.GetValue(limitGroup) as double?
                                     ?? groupType.GetProperty("Ucl")?.GetValue(limitGroup) as double?;
                        var lclVal = groupType.GetProperty("lcl")?.GetValue(limitGroup) as double?
                                     ?? groupType.GetProperty("Lcl")?.GetValue(limitGroup) as double?;
                        var clVal = groupType.GetProperty("cl")?.GetValue(limitGroup) as double?
                                    ?? groupType.GetProperty("Cl")?.GetValue(limitGroup) as double?;

                        if (uclVal.HasValue) numericDomainValues.Add(uclVal.Value);
                        if (lclVal.HasValue) numericDomainValues.Add(lclVal.Value);
                        if (clVal.HasValue) numericDomainValues.Add(clVal.Value);
                    }
                }
            }

            double domainMin = numericDomainValues.Min();
            double domainMax = numericDomainValues.Max();
            double binWidth = 0;
            if (domainMax > domainMin)
            {
                int preferredBins = 12;
                int binCount = Math.Max(5, Math.Min(preferredBins, (int)Math.Ceiling(Math.Sqrt(n)) + 3));
                binWidth = (domainMax - domainMin) / binCount;
            }

            // Generate 100 points for ECharts line series
            double minVal = nonExcludedValues.Min();
            double maxVal = nonExcludedValues.Max();
            double startX = minVal - 0.5 * (maxVal - minVal);
            double endX = maxVal + 0.5 * (maxVal - minVal);

            // Expand to cover 3 sigma
            startX = Math.Min(startX, mean - 3 * stdDev);
            endX = Math.Max(endX, mean + 3 * stdDev);

            double step = (endX - startX) / 100.0;
            for (int i = 0; i <= 100; i++)
            {
                double x = startX + i * step;
                double pdf = (1.0 / (stdDev * Math.Sqrt(2 * Math.PI))) * Math.Exp(-0.5 * Math.Pow((x - mean) / stdDev, 2));
                double scaledPdf = pdf * n * binWidth;

                curvePoints.Add(new NormalCurvePoint
                {
                    X = x,
                    Pdf = pdf,
                    ScaledPdf = scaledPdf
                });
            }
        }

        return result with { Normality = normality, NormalCurve = curvePoints };
    }

    public async Task<List<MesSpc.Api.Controllers.ChartSummaryDto>> GetChartSummaryListAsync(
        string dimension,
        Guid? uploadBatchId,
        DateTime? startDate,
        DateTime? endDate,
        string? batchNo = null,
        int? partId = null,
        CancellationToken ct = default)
    {
        var normalizedDimension = NormalizeControlScope(dimension);
        var groupName = await db.ControlChartGroups.AsNoTracking()
            .Where(x => x.GroupCode == dimension || x.GroupCode == normalizedDimension)
            .Select(x => x.GroupName)
            .FirstOrDefaultAsync(ct)
            ?? FormatControlScope(normalizedDimension);
        var chartTypeIds = await (
            from chartType in db.ControlChartTypes.AsNoTracking()
            join category in db.ControlChartCategories.AsNoTracking()
                on chartType.ChartCategoryId equals category.Id
            join chartGroup in db.ControlChartGroups.AsNoTracking()
                on category.ChartGroupId equals chartGroup.Id
            where chartGroup.GroupCode == dimension || chartGroup.GroupCode == normalizedDimension
            select chartType.Id
        ).ToListAsync(ct);

        var mappings = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Include(x => x.Machine)
            .Where(x => x.IsEnabled
                && ((x.ChartTypeId.HasValue && chartTypeIds.Contains(x.ChartTypeId.Value))
                    || (chartTypeIds.Count == 0 && x.ControlScope == normalizedDimension)))
            .Where(x => !partId.HasValue || x.PartId == partId.Value)
            .ToListAsync(ct);

        var chartTypes = await db.ControlChartTypes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ChartTypeName, ct);

        var summaries = new List<MesSpc.Api.Controllers.ChartSummaryDto>();
        foreach (var mapping in mappings)
        {
            var result = await GetInteractiveChartAsync(mapping.Id, uploadBatchId, startDate, endDate, batchNo, partId, ct);
            if (result == null || result.RawDataPoints == null) continue;

            var rawPoints = new List<SpcDataPoint>();
            if (result.RawDataPoints is List<SpcDataPoint> varPoints)
            {
                rawPoints.AddRange(varPoints);
            }
            else if (result.RawDataPoints is List<AttributeDataPoint> attrPoints)
            {
                foreach (var a in attrPoints)
                {
                    rawPoints.Add(new SpcDataPoint
                    {
                        MeasuredAt = a.MeasuredAt,
                        IsOutOfSpec = false,
                        IsOutOfControl = a.IsOutOfControl
                    });
                }
            }

            if (rawPoints.Count == 0) continue;

            var includedPoints = rawPoints.Where(x => !x.IsExcluded).ToList();
            if (includedPoints.Count == 0) continue;

            var oosCount = includedPoints.Count(x => x.IsOutOfSpec);
            var totalCount = includedPoints.Count;
            var oosPercentage = totalCount > 0 ? (double)oosCount / totalCount * 100 : 0;

            var latestAlert = includedPoints
                .Where(x => !string.IsNullOrEmpty(x.RootCause)
                    || !string.IsNullOrEmpty(x.CorrectiveAction)
                    || !string.IsNullOrEmpty(x.ResponsibleUser))
                .OrderByDescending(x => x.MeasuredAt)
                .FirstOrDefault();

            var lineParts = new[]
            {
                mapping.Process?.ProcessName,
                mapping.Machine?.MachineName
            }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var lineName = string.Join(" / ", lineParts);

            var calculatedLimits = ReadPrimaryControlLimits(result.StatControlLimits);
            var usesManualLimits = mapping.UCL.HasValue || mapping.LCL.HasValue;
            var calcMethod = usesManualLimits
                ? "自訂輸入 (Manual)"
                : FormatCalculationMethod(calculatedLimits.CalculationMethod, result.ChartType);

            string typeName = result.ChartType;
            if (mapping.ChartTypeId.HasValue && chartTypes.TryGetValue(mapping.ChartTypeId.Value, out var n)) typeName = n;

            summaries.Add(new MesSpc.Api.Controllers.ChartSummaryDto
            {
                PartProcessCharacteristicId = mapping.Id,
                ControlCategory = groupName,
                LineOrProcessName = lineName,
                ChartName = mapping.Characteristic?.CharacteristicName ?? "",
                ChartType = typeName,
                Usl = result.Limits?.USL,
                Lsl = result.Limits?.LSL,
                Ucl = mapping.UCL ?? calculatedLimits.Ucl ?? result.Limits?.UCL,
                Lcl = mapping.LCL ?? calculatedLimits.Lcl ?? result.Limits?.LCL,
                LimitCalculationMethod = calcMethod,
                OosCount = oosCount,
                OosPercentage = Math.Round(oosPercentage, 2),
                Ca = result.Capability?.Ca,
                Pp = result.Capability?.Pp,
                Ppk = result.Capability?.Ppk,
                ResponsibleUser = latestAlert?.ResponsibleUser ?? "",
                Remarks = latestAlert?.CorrectiveAction ?? latestAlert?.RootCause ?? ""
            });
        }
        return summaries.OrderByDescending(x => x.OosPercentage).ThenBy(x => x.LineOrProcessName).ToList();
    }

    private static string NormalizeControlScope(string? dimension)
    {
        return dimension?.Trim().ToUpperInvariant() switch
        {
            "PROC" or "PROCESS" => "PROCESS",
            "CHEM" or "CHEMICAL" => "CHEMICAL",
            "PROD" or "PRODUCT" => "PRODUCT",
            _ => dimension?.Trim().ToUpperInvariant() ?? string.Empty
        };
    }

    private static string FormatControlScope(string? scope)
    {
        return NormalizeControlScope(scope) switch
        {
            "PROCESS" => "製程管制",
            "CHEMICAL" => "藥液管制",
            "PRODUCT" => "產品管制",
            _ => scope ?? string.Empty
        };
    }

    private static string FormatCalculationMethod(string? method, string? chartType)
    {
        return method?.Trim().ToUpperInvariant() switch
        {
            "MR_METHOD" or "MOVING_RANGE_OF_XBAR" => "平均值移動全距法",
            "SIGMA_METHOD" or "SAMPLE_STD_DEV" => "樣本標準差法",
            _ when string.Equals(chartType, "I_MR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(chartType, "I-MR", StringComparison.OrdinalIgnoreCase) => "移動全距法",
            _ => "標準管制圖公式"
        };
    }

    private static (double? Ucl, double? Lcl, string? CalculationMethod) ReadPrimaryControlLimits(object? statControlLimits)
    {
        if (statControlLimits is null) return (null, null, null);

        var primary = GetPropertyValue(statControlLimits, "iControlLimitsStat")
            ?? GetPropertyValue(statControlLimits, "xbarControl")
            ?? GetPropertyValue(statControlLimits, "pControlLimitsStat")
            ?? GetPropertyValue(statControlLimits, "npControlLimitsStat")
            ?? statControlLimits;

        return (
            ReadNullableDouble(primary, "ucl"),
            ReadNullableDouble(primary, "lcl"),
            GetPropertyValue(primary, "calculationMethod")?.ToString());
    }

    private static object? GetPropertyValue(object source, string propertyName)
    {
        return source.GetType()
            .GetProperties()
            .FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.GetValue(source);
    }

    private static double? ReadNullableDouble(object source, string propertyName)
    {
        var value = GetPropertyValue(source, propertyName);
        if (value is null) return null;
        return Convert.ToDouble(value);
    }
}
