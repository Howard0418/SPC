using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Stages atomic business evidence or independently persists a standalone event. No public API is exposed.</summary>
public sealed class AuditEventWriter(AppDbContext db, DbContextOptions<AppDbContext> options,
    IConfiguration configuration, TimeProvider clock, ILogger<AuditEventWriter> logger)
{
    /// <summary>Adds evidence to the caller's unit of work without saving or committing it. Client retry keys use AppendAsync.</summary>
    public async Task<AuditEvent> StageAsync(AuditEventDraft draft, AuditActor actor, CancellationToken ct = default)
    {
        AuditEventContract.Require(draft.ClientEventId is null);
        var entry = await BuildAsync(db, draft, actor, ct);
        db.AuditEvents.Add(entry);
        return entry;
    }

    /// <summary>Persists only the event in an independent context; retries are scoped to environment, actor and session.</summary>
    public async Task<AuditEvent> AppendAsync(AuditEventDraft draft, AuditActor actor, CancellationToken ct = default)
    {
        await using var store = new AppDbContext(options);
        var entry = await BuildAsync(store, draft, actor, ct);
        await using var transaction = await store.Database.BeginTransactionAsync(ct);
        try
        {
            if (entry.ClientEventId is not null)
            {
                if (store.Database.IsSqlServer())
                {
                    var key = "SpcAudit:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{entry.Environment}:{entry.ActorOperatorId}:{entry.SessionIdHash}:{entry.ClientEventId}")));
                    await store.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={key}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r<0 THROW 51300,'Audit retry lock unavailable',1;", ct);
                }
                var existing = await store.AuditEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Environment == entry.Environment
                    && x.ActorOperatorId == entry.ActorOperatorId && x.SessionIdHash == entry.SessionIdHash && x.ClientEventId == entry.ClientEventId, ct);
                if (existing is not null)
                {
                    AuditEventContract.Require(existing.DeduplicationHash == entry.DeduplicationHash);
                    await transaction.CommitAsync(ct);
                    return existing;
                }
            }
            store.AuditEvents.Add(entry);
            await store.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return entry;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            // Deliberately omit exception/request/actor details: providers may embed SQL or rejected values.
            logger.LogError("AUDIT_APPEND_FAILED; event code {EventCode}", entry.EventCode);
            throw;
        }
    }

    private async Task<AuditEvent> BuildAsync(AppDbContext store, AuditEventDraft draft, AuditActor actor, CancellationToken ct)
    {
        var category = AuditEventContract.Category(draft.EventCode);
        var environment = configuration["AppEnvironment"]?.ToLowerInvariant() switch
        {
            "test" => "Test", "production" => "Production", _ => throw new AuditEventValidationException()
        };
        AuditEventContract.Require(draft.Outcome is "Succeeded" or "Denied" or "Failed");
        AuditEventContract.Require(draft.HttpStatus is null or >= 100 and <= 599);
        AuditEventContract.Require(draft.HttpMethod is null or "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS");
        AuditEventContract.Code(draft.ReasonCode); AuditEventContract.Code(draft.TargetType);
        AuditEventContract.Code(draft.TargetId, 200); AuditEventContract.Code(draft.CorrelationId); AuditEventContract.Code(draft.TraceId);
        AuditEventContract.HashValue(actor.SessionIdHash); AuditEventContract.HashValue(actor.AttemptedAccountHash);
        AuditEventContract.Require(draft.PageKey is null || AuditEventContract.Pages.ContainsKey(draft.PageKey));
        if (category == "Navigation")
            AuditEventContract.Require(draft.PageKey is not null && draft.Outcome == "Succeeded" && actor.Type == "User");
        if (draft.ClientEventId is not null)
            AuditEventContract.Require(draft.ClientEventId != Guid.Empty && category == "Navigation" && actor.OperatorId > 0 && actor.SessionIdHash is not null);
        var now = clock.GetUtcNow().UtcDateTime;
        var entry = new AuditEvent
        {
            EventId = Guid.NewGuid(), ClientEventId = draft.ClientEventId, OccurredAtUtc = now, ReceivedAtUtc = now,
            Environment = environment, Category = category, EventCode = draft.EventCode, Outcome = draft.Outcome,
            ReasonCode = draft.ReasonCode, ActorType = actor.Type, ActorAssurance = actor.Assurance,
            SessionIdHash = actor.SessionIdHash, AttemptedAccountHash = actor.AttemptedAccountHash,
            CorrelationId = draft.CorrelationId, TraceId = draft.TraceId, PageKey = draft.PageKey,
            RouteTemplate = draft.PageKey is null ? null : AuditEventContract.Pages[draft.PageKey],
            TargetType = draft.TargetType, TargetId = draft.TargetId, HttpMethod = draft.HttpMethod, HttpStatus = draft.HttpStatus,
            Source = category == "Navigation" ? "WebRouter" : actor.Type == "Service" ? "BackgroundJob" : draft.EventCode == "auth.sso.exchange" ? "PortalSso" : "Api",
            DetailsJson = AuditEventContract.SerializeDetails(category, draft)
        };
        if (actor.Type == "User")
        {
            AuditEventContract.Require(actor.OperatorId > 0 && actor.Assurance is "Jwt" or "PortalAssertion" && actor.ServiceCode is null && actor.AttemptedAccountHash is null);
            var user = await store.Operators.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor.OperatorId, ct);
            AuditEventContract.Require(user is not null && (user.IsActive && !user.IsDeleted || actor.Assurance == "PortalAssertion" && draft.Outcome == "Denied"));
            entry.ActorOperatorId = user!.Id;
            entry.ActorAccountSnapshot = Snapshot(user.Username);
            entry.ActorOperatorCodeSnapshot = Snapshot(user.OperatorCode);
            entry.ActorDisplayNameSnapshot = Snapshot(user.OperatorName);
            entry.ActorRoleSnapshot = Snapshot(user.Role);
        }
        else if (actor.Type == "Anonymous")
            AuditEventContract.Require(actor.Assurance == "None" && actor.OperatorId is null && actor.ServiceCode is null && actor.SessionIdHash is null
                && category is "Authentication" or "Security" && draft.Outcome != "Succeeded");
        else if (actor.Type == "Service")
        {
            AuditEventContract.Require(actor.Assurance == "Service" && actor.OperatorId is null && actor.SessionIdHash is null && actor.AttemptedAccountHash is null && actor.ServiceCode is not null);
            AuditEventContract.Code(actor.ServiceCode); entry.ActorServiceCode = actor.ServiceCode;
        }
        else throw new AuditEventValidationException();
        entry.DeduplicationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            entry.Environment, entry.EventCode, entry.Outcome, entry.ReasonCode, entry.ActorType, entry.ActorOperatorId, entry.ActorAssurance,
            entry.ActorServiceCode, entry.SessionIdHash, entry.PageKey, entry.TargetType, entry.TargetId,
            entry.DetailsJson
        }))));
        return entry;
    }

    private static string? Snapshot(string? value)
    {
        AuditEventContract.Require(value is null || value.Length <= 200 && !value.Any(char.IsControl));
        return value;
    }
}
