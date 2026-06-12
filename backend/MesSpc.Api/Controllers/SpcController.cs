using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc")]
public class SpcController(SpcService spcService, AppDbContext db) : ControllerBase
{
    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] int ppcId,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            return BadRequest("Start date cannot be later than end date.");
        }

        var chart = await spcService.GetInteractiveChartAsync(ppcId, uploadBatchId, startDate, endDate);
        if (chart is null) return NotFound("Chart data not found or invalid part process characteristic.");
        return Ok(chart);
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
