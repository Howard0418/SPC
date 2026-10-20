using MesSpc.Api.Services;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/part-process-characteristics/{ppcId:int}/chemical-formula-schedule")]
[Route("api/v1/part-process-characteristics/{ppcId:int}/chemical-formula-schedule")]
public sealed class ChemicalFormulaScheduleController(ChemicalFormulaScheduleService service, ILogger<ChemicalFormulaScheduleController> logger) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>Read the active date schedule. An unenabled policy does not change legacy writes.</summary>
    [HttpGet]
    public Task<IActionResult> Get(int ppcId, CancellationToken ct) => Execute(() => service.GetAsync(ppcId, ct));

    /// <summary>Publish a complete immutable schedule against the revision last read by the editor.</summary>
    [HttpPost]
    [Authorize(Roles="Admin," + UserRoles.Editor)]
    public Task<IActionResult> Publish(int ppcId, ChemicalPublishSchedule request, CancellationToken ct) => Execute(async () =>
        (object)new { revisionId=await service.PublishAsync(ppcId,request,Actor,ct) });

    /// <summary>Preview a measurement using explicit-offset date and raw decimal strings. Does not import.</summary>
    [HttpPost("preview")]
    public Task<IActionResult> Preview(int ppcId, ChemicalPreviewInput request, CancellationToken ct) => Execute(async () =>
        (object)await service.PreviewAsync(ppcId,request,Actor,ct));

    /// <summary>Revalidate a protected preview and prepare a short-lived receipt. Does not write measurement data.</summary>
    [HttpPost("prepare")]
    [Authorize(Roles="Admin," + UserRoles.Editor)]
    public Task<IActionResult> Prepare(int ppcId, ChemicalPrepareRequest request, CancellationToken ct) => Execute(async () => {
        var receipt=await service.PrepareAsync(ppcId,request,Actor,ct);
        return (object)new { receiptId=receipt.Id, receipt.ExpiresAtUtc, receipt.InputHash, receipt.SourceKeyHash,
            message="僅建立計算憑證，尚未匯入量測；直接DB確認程序留TASK-005。" };
    });

    private async Task<IActionResult> Execute(Func<Task<object>> work)
    {
        try { return Ok(new { success=true,data=await work(),message="" }); }
        catch(KeyNotFoundException ex) { return NotFound(new { success=false,data=(object?)null,message=ex.Message }); }
        catch(ArgumentException ex) { return BadRequest(new { success=false,data=(object?)null,message=ex.Message }); }
        catch(ChemicalPreviewConflictException ex) { return Conflict(new { success=false,data=(object?)null,message=ex.Message }); }
        catch(Exception ex) when (ex is SqlException { Number:1205 or 1222 } || ex is DbUpdateException { InnerException: SqlException { Number:1205 or 1222 or 2601 or 2627 } })
        {
            logger.LogWarning(ex,"Chemical schedule concurrency conflict");
            return Conflict(new { success=false,data=(object?)null,message="其他人正在更新排程，請重新載入後再試。" });
        }
    }
}
