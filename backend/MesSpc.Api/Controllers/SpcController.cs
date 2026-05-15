using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc")]
public class SpcController(SpcService spcService) : ControllerBase
{
    [HttpGet("chart")]
    public async Task<IActionResult> GetChart([FromQuery] int inspectionItemId, [FromQuery] int productId, [FromQuery] int stationId, [FromQuery] string chartType = "IMR")
    {
        var ct = chartType.ToUpperInvariant();
        if (ct is "IMR" or "I-MR")
        {
            var imr = await spcService.GetImrChartAsync(inspectionItemId, productId, stationId);
            return imr is null ? NotFound() : Ok(imr);
        }

        if (ct is "XBAR_R" or "XBAR-R")
        {
            var xr = await spcService.GetXbarRChartAsync(inspectionItemId, productId, stationId);
            return xr is null ? NotFound() : Ok(xr);
        }

        return BadRequest("chartType must be IMR or XBAR_R");
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
