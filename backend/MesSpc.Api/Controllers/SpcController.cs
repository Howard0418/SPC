using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc")]
public class SpcController(SpcService spcService, AppDbContext db) : ControllerBase
{
    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] int ppcId,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] string? batchNo,
        [FromQuery] int? partId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var useAlternateFilter = !string.IsNullOrWhiteSpace(batchNo) || partId.HasValue;
        var start = useAlternateFilter ? startDate : startDate ?? DateTime.Today.AddMonths(-3);
        var end = useAlternateFilter ? endDate : endDate ?? DateTime.Today;

        if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
        {
            return BadRequest("量測起日不可晚於量測迄日。");
        }

        if (start.HasValue && end.HasValue && (end.Value.Date - start.Value.Date).TotalDays > 93)
        {
            return BadRequest("查詢時間範圍最多不可超過 3 個月。");
        }

        var chart = await spcService.GetInteractiveChartAsync(ppcId, uploadBatchId, start, end, batchNo, partId);
        if (chart is null) return NotFound("Chart data not found or invalid part process characteristic.");
        return Ok(chart);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetChartSummary(
        [FromQuery] string dimension,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] string? batchNo,
        [FromQuery] int? partId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var useAlternateFilter = !string.IsNullOrWhiteSpace(batchNo) || partId.HasValue;
        var start = useAlternateFilter ? startDate : startDate ?? DateTime.Today.AddMonths(-3);
        var end = useAlternateFilter ? endDate : endDate ?? DateTime.Today;

        if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
        {
            return BadRequest("量測起日不可晚於量測迄日。");
        }

        if (start.HasValue && end.HasValue && (end.Value.Date - start.Value.Date).TotalDays > 93)
        {
            return BadRequest("查詢時間範圍最多不可超過 3 個月。");
        }

        var list = await spcService.GetChartSummaryListAsync(dimension, uploadBatchId, start, end, batchNo, partId);
        return Ok(list);
    }

    [HttpPost("exclude-batch/{uploadBatchId:guid}")]
    public async Task<IActionResult> ToggleExcludeUploadBatch(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FindAsync([uploadBatchId], ct);
        if (batch is null) return NotFound("Upload batch not found.");

        batch.IsExcluded = !batch.IsExcluded;
        await db.SaveChangesAsync(ct);

        return Ok(new { batch.UploadBatchId, batch.IsExcluded });
    }

    [HttpPut("control-limits/{ppcId:int}")]
    public async Task<IActionResult> UpdateControlLimits(int ppcId, [FromBody] UpdateControlLimitsReq req, CancellationToken ct = default)
    {
        var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.Id == ppcId && x.IsEnabled, ct);
        if (mapping is null) return NotFound("Part process characteristic not found.");

        mapping.UCL = req.Ucl;
        mapping.CL = req.Cl;
        mapping.LCL = req.Lcl;

        await db.SaveChangesAsync(ct);
        return Ok(new { mapping.Id, mapping.UCL, mapping.CL, mapping.LCL });
    }

    [HttpPost("ocap")]
    public async Task<IActionResult> SaveOcap([FromBody] SaveOcapReq req, CancellationToken ct = default)
    {
        if (req.PpcId <= 0) return BadRequest("PpcId is required.");

        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.PpcId && x.IsEnabled, ct);
        if (mapping is null) return NotFound("Part process characteristic not found.");

        VariableMeasurement? measurement = null;
        if (req.VariableMeasurementId.HasValue)
        {
            measurement = await db.VariableMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == req.VariableMeasurementId.Value, ct);
        }

        AlertEvent? alert = null;
        if (req.AlertId.HasValue)
        {
            alert = await db.AlertEvents.FirstOrDefaultAsync(x => x.Id == req.AlertId.Value, ct);
        }
        else if (req.VariableMeasurementId.HasValue)
        {
            alert = await db.AlertEvents
                .Where(x => x.VariableMeasurementId == req.VariableMeasurementId.Value)
                .OrderByDescending(x => x.OccurredAt)
                .FirstOrDefaultAsync(ct);
        }

        var actualValue = req.ActualValue ?? measurement?.MeasuredValue;
        if (alert is null)
        {
            if (!actualValue.HasValue) return BadRequest("ActualValue or VariableMeasurementId is required when creating OCAP.");

            var isOutOfSpec = (mapping.USL.HasValue && actualValue.Value > mapping.USL.Value)
                || (mapping.LSL.HasValue && actualValue.Value < mapping.LSL.Value);

            alert = new AlertEvent
            {
                OccurredAt = measurement?.MeasuredAt ?? DateTime.UtcNow,
                PartId = measurement?.PartId ?? mapping.PartId ?? 0,
                ProcessId = measurement?.ProcessId ?? mapping.ProcessId,
                CharacteristicId = measurement?.CharacteristicId ?? mapping.CharacteristicId,
                ActualValue = actualValue,
                AlertType = isOutOfSpec ? AlertType.OutOfSpec : AlertType.OutOfControl,
                UploadBatchId = measurement?.UploadBatchId,
                VariableMeasurementId = measurement?.Id ?? req.VariableMeasurementId,
                Status = "Open",
                Message = isOutOfSpec ? "圖表監控 OCAP：量測值超出規格界限" : "圖表監控 OCAP：量測點觸發管制規則或管制界限"
            };
            db.AlertEvents.Add(alert);
        }

        alert.RootCause = JoinOcapText(req.CauseCategory, req.CauseType, req.RootCause);
        alert.CorrectiveAction = JoinOcapText(req.ActionType, null, req.CorrectiveAction);
        alert.ResponsibleUser = req.ResponsibleUser;
        alert.Status = string.IsNullOrWhiteSpace(req.Status) ? alert.Status : req.Status;
        if (string.Equals(alert.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            alert.IsAcknowledged = true;
            alert.ClosedAt ??= DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            alert.Id,
            alert.Status,
            alert.RootCause,
            alert.CorrectiveAction,
            alert.ResponsibleUser,
            alert.IsAcknowledged,
            alert.ClosedAt
        });
    }

    private static string? JoinOcapText(string? prefix, string? type, string? text)
    {
        var parts = new[] { prefix, type, text }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();
        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }
}

public record UpdateControlLimitsReq(double? Ucl, double? Cl, double? Lcl);
public record SaveOcapReq(
    int PpcId,
    long? VariableMeasurementId,
    int? AlertId,
    double? ActualValue,
    string? Status,
    string? CauseCategory,
    string? CauseType,
    string? ActionType,
    string? RootCause,
    string? CorrectiveAction,
    string? ResponsibleUser);

public class ChartSummaryDto
{
    public int PartProcessCharacteristicId { get; set; }
    public string ControlCategory { get; set; } = string.Empty;
    public string LineOrProcessName { get; set; } = string.Empty;
    public string ChartName { get; set; } = string.Empty;
    public string ChartType { get; set; } = string.Empty;
    public double? Usl { get; set; }
    public double? Lsl { get; set; }
    public double? Ucl { get; set; }
    public double? Lcl { get; set; }
    public string LimitCalculationMethod { get; set; } = string.Empty;
    public int OosCount { get; set; }
    public double OosPercentage { get; set; }
    public double? Ca { get; set; }
    public double? Pp { get; set; }
    public double? Ppk { get; set; }
    public string ResponsibleUser { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/formulas")]
public class FormulaController(AppDbContext db, FormulaEngineService formulaEngine) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.FormulaDefinitions.OrderBy(x => x.FormulaCode).ToListAsync());

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Domain.Entities.FormulaDefinition req)
    {
        var x = await db.FormulaDefinitions.FindAsync(id);
        if (x is null) return NotFound();
        x.DisplayName = req.DisplayName;
        x.Expression = req.Expression;
        x.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate(EvaluateFormulaReq req)
    {
        var formula = await db.FormulaDefinitions.FirstOrDefaultAsync(x => x.FormulaCode == req.FormulaCode && x.IsActive);
        if (formula is null) return NotFound($"Formula '{req.FormulaCode}' not found or inactive.");
        var value = formulaEngine.EvaluateByExpression(formula.Expression, req.Values, req.Usl, req.Lsl);
        return Ok(new { req.FormulaCode, formula.Expression, value });
    }

    public record EvaluateFormulaReq(string FormulaCode, List<double> Values, double? Usl, double? Lsl);
}
