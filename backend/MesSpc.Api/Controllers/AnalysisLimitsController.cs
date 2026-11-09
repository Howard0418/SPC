using MesSpc.Api.Services;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/analysis-limits")]
[Authorize(Roles = "Admin," + UserRoles.Editor)]
public sealed class AnalysisLimitsController(LimitBatchService service, ILogger<AnalysisLimitsController> logger) : ControllerBase
{
    /// <summary>Validates an atomic batch of specification/control limit edits without writing data.</summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview(LimitBatchPreviewRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.PreviewAsync(request, Actor(), ct);
            return Ok(new { success = true, data = result, message = result.IsValid ? "預檢通過。" : "預檢未通過，請依逐列提示修正。" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { success = false, data = (object?)null, message = exception.Message });
        }
    }

    /// <summary>Applies a previously validated limit batch atomically and records immutable before/after history.</summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(LimitBatchApplyRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.ApplyAsync(request, Actor(), ct);
            return Ok(new { success = true, data = result, message = "界限批次已儲存。" });
        }
        catch (LimitBatchConflictException exception)
        {
            logger.LogWarning("Limit batch apply rejected for {Actor}: {Reason}", Actor(), exception.Message);
            return Conflict(new { success = false, data = (object?)null, message = exception.Message });
        }
    }

    private string Actor() => User.Identity?.Name?.Trim() is { Length: > 0 } name ? name : "System";
}
