namespace MesSpc.Api.Domain.Entities;

public sealed class AuditArchiveBatch
{
    public long Id { get; internal set; }
    public Guid BatchId { get; internal set; }
    public string Environment { get; internal set; } = "";
    public DateTime StartedAtUtc { get; internal set; }
    public DateTime CompletedAtUtc { get; internal set; }
    public DateTime RangeFromUtc { get; internal set; }
    public DateTime RangeToUtc { get; internal set; }
    public int RecordCount { get; internal set; }
    public string Checksum { get; internal set; } = "";
    public string EncryptionKeyId { get; internal set; } = "";
    public string Status { get; internal set; } = "Verified";
}

public sealed class AuditEventArchive
{
    public long Id { get; internal set; }
    public long OriginalEventId { get; internal set; }
    public Guid EventId { get; internal set; }
    public long ArchiveBatchId { get; internal set; }
    public DateTime OccurredAtUtc { get; internal set; }
    public string Environment { get; internal set; } = "";
    public string Category { get; internal set; } = "";
    public string EventCode { get; internal set; } = "";
    public string? PageKey { get; internal set; }
    public string? TargetType { get; internal set; }
    public string? TargetId { get; internal set; }
    public byte[] Nonce { get; internal set; } = [];
    public byte[] Ciphertext { get; internal set; } = [];
    public byte[] Tag { get; internal set; } = [];
    public string PayloadHash { get; internal set; } = "";
}

public sealed class AuditLegalHold
{
    public long Id { get; internal set; }
    public Guid HoldId { get; internal set; }
    public string Environment { get; internal set; } = "";
    public DateTime DateFromUtc { get; internal set; }
    public DateTime DateToUtc { get; internal set; }
    public string? EventCode { get; internal set; }
    public string? PageKey { get; internal set; }
    public string? TargetType { get; internal set; }
    public string? TargetId { get; internal set; }
    public string ReasonCode { get; internal set; } = "";
    public int CreatedByOperatorId { get; internal set; }
    public DateTime CreatedAtUtc { get; internal set; }
    public int? ReleasedByOperatorId { get; internal set; }
    public DateTime? ReleasedAtUtc { get; internal set; }
}
