namespace MesSpc.Api.Domain.Entities;

/// <summary>A revision set; publication freezes its interval rows. No revision is activated by migration.</summary>
public sealed class ChemicalFormulaScheduleRevision
{
    public long Id { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public int RevisionNo { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public string Actor { get; set; } = "System";
    public string Reason { get; set; } = "";
}
public sealed class ChemicalFormulaEffectivity
{
    public long Id { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public long ScheduleRevisionId { get; set; }
    public long FormulaVersionId { get; set; }
    public long? FTableHistoryId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveUntilUtcExclusive { get; set; }
}
public sealed class ChemicalFormulaPolicy
{
    public int PartProcessCharacteristicId { get; set; }
    public long? ActiveScheduleRevisionId { get; set; }
    public bool IsEnabled { get; set; }
}
/// <summary>Immutable server calculation evidence, prepared only after confirmation; consumption is a measurement link.</summary>
public sealed class ChemicalCalculationReceipt
{
    public Guid Id { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public long EffectivityId { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public string Actor { get; set; } = "";
    public string InputHash { get; set; } = "";
    public string SourceKeyHash { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public string PrecisionPolicy { get; set; } = "";
    // Canonical decimal strings, original inputs, F values, results and units retain their full precision in JSON.
    public string PayloadJson { get; set; } = "{}";
}
public sealed class ChemicalCalculationTrace
{
    public long Id { get; set; }
    public long VariableMeasurementId { get; set; }
    public Guid ReceiptId { get; set; }
    public string Phase { get; set; } = "Initial";
    public int VersionNo { get; set; } = 1;
    public string RawResultCanonical { get; set; } = "";
    public string StoredResultCanonical { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class ChemicalStatisticsJob
{
    public Guid Id { get; set; }
    public long VariableMeasurementId { get; set; }
    public Guid ReceiptId { get; set; }
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
}
