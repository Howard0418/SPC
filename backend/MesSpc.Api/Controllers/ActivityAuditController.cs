using MesSpc.Api.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit/activity")]
public sealed class ActivityAuditController(NavigationReadAudit audit, ILogger<ActivityAuditController> logger) : ControllerBase
{
    /// <summary>Appends a constrained best-effort navigation or read summary for the current authenticated user.</summary>
    [HttpPost]
    public async Task<IActionResult> Record([FromBody] ActivityAuditRequest request, CancellationToken ct)
    {
        try
        {
            await audit.RecordAsync(HttpContext, request with { CorrelationId = HttpContext.TraceIdentifier }, ct);
            return NoContent();
        }
        catch (AuditEventValidationException)
        {
            return BadRequest(new { success = false, code = "AUDIT_ACTIVITY_INVALID", message = "活動紀錄格式不正確。" });
        }
        catch (Exception)
        {
            logger.LogError("NAVIGATION_READ_AUDIT_UNAVAILABLE; trace {TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(503, new { success = false, code = "AUDIT_ACTIVITY_UNAVAILABLE", message = "活動紀錄暫時無法保存。" });
        }
    }
}
