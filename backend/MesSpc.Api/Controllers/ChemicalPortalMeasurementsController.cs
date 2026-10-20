using MesSpc.Api.Services;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chemical-portal-measurements")]
public sealed class ChemicalPortalMeasurementsController(ChemicalPortalMeasurementService service,ILogger<ChemicalPortalMeasurementsController> logger):ControllerBase
{
    private string Actor=>User.Identity?.Name??User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??"";
    /// <summary>Published schedule identifiers for Portal master-data decoration; does not activate legacy write gates.</summary>
    [HttpGet("schedules")]
    public Task<IActionResult> Schedules(CancellationToken ct)=>Execute(()=>service.GetSchedulesAsync(ct));
    /// <summary>Calculate from raw inputs and measurement date using only server-maintained formula history.</summary>
    [HttpPost("preview")]
    public Task<IActionResult> Preview(ChemicalPortalPreviewRequest request,CancellationToken ct)=>Execute(async()=>{
        var value=await service.PreviewAsync(request,Actor,ct);
        return (object)new{concentration=value.Result.Stored,rawConcentration=value.Result.Raw,judgment=value.Result.Judgment,
            adjustment=value.Result.Adjustment,adjustmentAmount=value.Result.AdjustmentAmount,previewToken=value.Token,
            value.RevisionId,value.FormulaVersionId,value.FormulaVersionNo,value.FTableHistoryId,value.ExpiresAtUtc};
    });
    /// <summary>Atomically confirm a Portal daily batch, preserving initial/recheck trace and statistics.</summary>
    [HttpPost("confirm")]
    [Authorize(Roles="Admin,"+UserRoles.Editor)]
    [RequestSizeLimit(1024*1024)]
    public Task<IActionResult> Confirm(ChemicalPortalConfirmRequest request,CancellationToken ct)=>Execute(async()=>(object)await service.ConfirmAsync(request,Actor,ct));
    private async Task<IActionResult> Execute(Func<Task<object>> work)
    {
        try{return Ok(new{success=true,data=await work(),message=""});}
        catch(ArgumentException ex){return BadRequest(new{success=false,data=(object?)null,message=ex.Message});}
        catch(ChemicalPreviewConflictException ex){return Conflict(new{success=false,data=(object?)null,message=ex.Message});}
        catch(Exception ex) when(ex is SqlException{Number:1205 or 1222 or 51200} || ex is DbUpdateException)
        {logger.LogWarning(ex,"Portal chemical transaction rejected");return Conflict(new{message="資料或公式版本已衝突，整批未完成；請重新載入並預覽後再確認。"});}
    }
}
