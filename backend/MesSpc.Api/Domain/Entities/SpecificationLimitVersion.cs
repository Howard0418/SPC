namespace MesSpc.Api.Domain.Entities;
/// <summary>Append-only engineering specification snapshots; preserved when the source segment is removed.</summary>
public sealed class SpecificationLimitVersion
{
    public long Id { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public int VersionNo { get; set; }
    public string SourceKind { get; set; } = "Base";
    public int? SourceSegmentId { get; set; }
    public string ChangeType { get; set; } = "Update";
    public string? PreviousSnapshotJson { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = "System";
}
