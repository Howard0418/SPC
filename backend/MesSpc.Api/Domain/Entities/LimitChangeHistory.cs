namespace MesSpc.Api.Domain.Entities;

/// <summary>Immutable header for one atomic six-limit batch change.</summary>
public sealed class LimitChangeBatch
{
    public Guid Id { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public ICollection<LimitChangeItem> Items { get; set; } = new List<LimitChangeItem>();
}

/// <summary>Immutable before/after values for one PPC in a limit change batch.</summary>
public sealed class LimitChangeItem
{
    public long Id { get; set; }
    public Guid LimitChangeBatchId { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public double? PreviousLsl { get; set; }
    public double? PreviousTarget { get; set; }
    public double? PreviousUsl { get; set; }
    public double? PreviousLcl { get; set; }
    public double? PreviousCl { get; set; }
    public double? PreviousUcl { get; set; }
    public double? NewLsl { get; set; }
    public double? NewTarget { get; set; }
    public double? NewUsl { get; set; }
    public double? NewLcl { get; set; }
    public double? NewCl { get; set; }
    public double? NewUcl { get; set; }
    public LimitChangeBatch? LimitChangeBatch { get; set; }
}
