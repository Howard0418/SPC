using System.Security.Claims;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit/events")]
public sealed class AuditEventsController(AuditEventQueryService queries, AuditPermissions permissions,
    AuditEventWriter writer, AuthenticationAudit authentication, ILogger<AuditEventsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] AuditEventQuery request, CancellationToken ct)
    {
        if (!await permissions.CanAccessAsync(User, AuditPermissions.Read, ct)) return Forbid();
        try
        {
            var result = await queries.QueryAsync(request, ct);
            await RecordAsync("audit.events.query", result.Items.Count, result.FromUtc, result.ToUtc, request, ct);
            return Ok(new { success = true, data = result });
        }
        catch (AuditQueryValidationException ex) { return BadRequest(new { success = false, code = ex.Code }); }
        catch (Exception) { logger.LogError("AUDIT_QUERY_UNAVAILABLE; trace {TraceId}", HttpContext.TraceIdentifier); return StatusCode(503, new { success = false, code = "AUDIT_QUERY_UNAVAILABLE" }); }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AuditEventQuery request, CancellationToken ct)
    {
        if (!await permissions.CanAccessAsync(User, AuditPermissions.Export, ct)) return Forbid();
        try
        {
            var result = await queries.ExportAsync(request, ct);
            await RecordAsync("audit.events.export", result.Count, result.FromUtc, result.ToUtc, request, ct);
            return File(result.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SPC-Audit-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }
        catch (AuditQueryValidationException ex) { return BadRequest(new { success = false, code = ex.Code }); }
        catch (Exception) { logger.LogError("AUDIT_EXPORT_UNAVAILABLE; trace {TraceId}", HttpContext.TraceIdentifier); return StatusCode(503, new { success = false, code = "AUDIT_EXPORT_UNAVAILABLE" }); }
    }

    private async Task RecordAsync(string code, int count, DateTime from, DateTime to, AuditEventQuery request, CancellationToken ct)
    {
        var actor = await permissions.CurrentAsync(User, ct) ?? throw new AuditEventValidationException();
        var draft = new AuditEventDraft(code, "Succeeded")
        {
            PageKey = "admin.audit.events", TargetType = "AuditLog", TargetId = "Current",
            HttpMethod = "GET", HttpStatus = 200, CorrelationId = HttpContext.TraceIdentifier,
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            Details = new Dictionary<string, object?> { ["FilterFields"] = AuditEventQueryService.FilterFields(request), ["DateFromUtc"] = from, ["DateToUtc"] = to, ["ResultCount"] = count }
        };
        await writer.AppendAsync(draft, AuditActor.User(actor.Id, "Jwt", authentication.SessionHash(User.FindFirstValue("jti"))), ct);
    }
}
