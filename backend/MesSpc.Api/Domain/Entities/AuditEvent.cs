namespace MesSpc.Api.Domain.Entities;

/// <summary>Append-only activity evidence; deliberately independent of soft-deleted business entities.</summary>
public sealed class AuditEvent
{
    public long Id { get; internal set; }
    public Guid EventId { get; internal set; }
    public Guid? ClientEventId { get; internal set; }
    public int SchemaVersion { get; internal set; } = 1;
    public DateTime OccurredAtUtc { get; internal set; }
    public DateTime ReceivedAtUtc { get; internal set; }
    public string Environment { get; internal set; } = "";
    public string Category { get; internal set; } = "";
    public string EventCode { get; internal set; } = "";
    public string Outcome { get; internal set; } = "";
    public string? ReasonCode { get; internal set; }
    public string ActorType { get; internal set; } = "";
    public string ActorAssurance { get; internal set; } = "";
    public int? ActorOperatorId { get; internal set; }
    public string? ActorAccountSnapshot { get; internal set; }
    public string? ActorOperatorCodeSnapshot { get; internal set; }
    public string? ActorDisplayNameSnapshot { get; internal set; }
    public string? ActorRoleSnapshot { get; internal set; }
    public string? ActorServiceCode { get; internal set; }
    public string? AttemptedAccountHash { get; internal set; }
    public string? SessionIdHash { get; internal set; }
    public string? CorrelationId { get; internal set; }
    public string? TraceId { get; internal set; }
    public string? PageKey { get; internal set; }
    public string? RouteTemplate { get; internal set; }
    public string? TargetType { get; internal set; }
    public string? TargetId { get; internal set; }
    public string? HttpMethod { get; internal set; }
    public int? HttpStatus { get; internal set; }
    public string Source { get; internal set; } = "";
    public string DetailsJson { get; internal set; } = "{}";
    // Content comparison for retry conflicts only; not an integrity/authenticity guarantee.
    public string DeduplicationHash { get; internal set; } = "";
}
