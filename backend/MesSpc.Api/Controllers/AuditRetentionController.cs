using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit/retention")]
public sealed class AuditRetentionController(AuditRetentionService retention, AuditPermissions permissions,
    AuditEventWriter writer, AuthenticationAudit authentication) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (!await permissions.CanAccessAsync(User, AuditPermissions.Evidence, ct)) return Forbid();
        return Ok(new { success = true, data = await retention.PreviewAsync(ct) });
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute(CancellationToken ct)
    {
        var actor = await MaintenanceActorAsync(ct); if (actor is null) return Forbid();
        try
        {
            var result = await retention.ExecuteAsync(ct);
            await RecordAsync("audit.retention.execute", actor.Id, result.Archived, result.Archived > 0 ? result.Checksum : null, null, ct);
            return Ok(new { success = true, data = result });
        }
        catch (AuditRetentionException ex) { return Conflict(new { success = false, code = ex.Code }); }
    }

    [HttpPost("legal-holds")]
    public async Task<IActionResult> CreateHold([FromBody] AuditLegalHoldRequest request, CancellationToken ct)
    {
        var actor = await MaintenanceActorAsync(ct); if (actor is null) return Forbid();
        try
        {
            var hold = await retention.CreateLegalHoldAsync(request, actor.Id, ct);
            await RecordAsync("audit.legal_hold.change", actor.Id, 0, null, hold.HoldId, ct);
            return Ok(new { success = true, data = new { hold.HoldId, hold.DateFromUtc, hold.DateToUtc, hold.ReasonCode } });
        }
        catch (AuditRetentionException ex) { return BadRequest(new { success = false, code = ex.Code }); }
    }

    [HttpPost("legal-holds/{holdId:guid}/release")]
    public async Task<IActionResult> ReleaseHold(Guid holdId, CancellationToken ct)
    {
        var actor = await MaintenanceActorAsync(ct); if (actor is null) return Forbid();
        try
        {
            if (!await retention.ReleaseLegalHoldAsync(holdId, actor.Id, ct)) return NotFound();
            await RecordAsync("audit.legal_hold.change", actor.Id, 0, null, holdId, ct); return NoContent();
        }
        catch (AuditRetentionException ex) { return Conflict(new { success = false, code = ex.Code }); }
    }

    private async Task<Domain.Entities.Operator?> MaintenanceActorAsync(CancellationToken ct)
    {
        if (!await permissions.CanAccessAsync(User, AuditPermissions.Evidence, ct)) return null;
        var actor = await permissions.CurrentAsync(User, ct);
        return actor is not null && permissions.IsSecurityAdministrator(actor.Id) ? actor : null;
    }

    private async Task RecordAsync(string code, int actorId, int count, string? checksum, Guid? holdId, CancellationToken ct)
    {
        var details = new Dictionary<string, object?> { ["RecordCount"] = count };
        if (checksum is not null) details["ArchiveChecksum"] = checksum;
        if (holdId is not null) details["LegalHoldId"] = holdId.Value;
        await writer.AppendAsync(new(code, "Succeeded")
        {
            PageKey = "admin.audit.events", TargetType = holdId is null ? "AuditArchive" : "AuditLegalHold",
            TargetId = holdId?.ToString("D") ?? "Retention", HttpMethod = "POST", HttpStatus = 200,
            CorrelationId = HttpContext.TraceIdentifier, Details = details
        }, AuditActor.User(actorId, "Jwt", authentication.SessionHash(User.FindFirstValue("jti"))), ct);
    }
}
