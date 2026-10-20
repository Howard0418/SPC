using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<ChemicalFormulaScheduleRevision> ChemicalFormulaScheduleRevisions => Set<ChemicalFormulaScheduleRevision>();
    public DbSet<ChemicalFormulaEffectivity> ChemicalFormulaEffectivities => Set<ChemicalFormulaEffectivity>();
    public DbSet<ChemicalFormulaPolicy> ChemicalFormulaPolicies => Set<ChemicalFormulaPolicy>();
    public DbSet<ChemicalCalculationReceipt> ChemicalCalculationReceipts => Set<ChemicalCalculationReceipt>();
    public DbSet<ChemicalCalculationTrace> ChemicalCalculationTraces => Set<ChemicalCalculationTrace>();
    public DbSet<ChemicalStatisticsJob> ChemicalStatisticsJobs => Set<ChemicalStatisticsJob>();

    private static void ConfigureChemicalTrace(ModelBuilder b)
    {
        b.Entity<ChemicalAnalysisFormulaVersion>().HasAlternateKey(x => new { x.Id, x.PartProcessCharacteristicId });
        b.Entity<ChemicalAnalysisFormulaVersion>().ToTable(t => t.HasTrigger("TR_ChemicalFormulaPinned"));
        b.Entity<ChemicalFTableVersionHistory>().ToTable(t => t.HasTrigger("TR_ChemicalFHistoryPinned"));
        var revision = b.Entity<ChemicalFormulaScheduleRevision>();
        revision.ToTable("ChemicalFormulaScheduleRevisions", t => { t.HasComment("Immutable published formula schedule revision sets."); t.HasTrigger("TR_ChemicalScheduleFrozen"); t.HasCheckConstraint("CK_ChemicalSchedule_Revision", "[RevisionNo] > 0"); });
        revision.HasAlternateKey(x => new { x.Id, x.PartProcessCharacteristicId });
        revision.HasIndex(x => new { x.PartProcessCharacteristicId, x.RevisionNo }).IsUnique();
        revision.Property(x => x.Actor).HasMaxLength(100); revision.Property(x => x.Reason).HasMaxLength(500);
        revision.HasOne<PartProcessCharacteristic>().WithMany().HasForeignKey(x => x.PartProcessCharacteristicId).OnDelete(DeleteBehavior.NoAction);
        var period = b.Entity<ChemicalFormulaEffectivity>();
        period.ToTable("ChemicalFormulaEffectivities", t => { t.HasComment("Half-open UTC intervals referencing existing formula and F snapshots."); t.HasTrigger("TR_ChemicalEffectivityIntegrity"); t.HasCheckConstraint("CK_ChemicalEffectivity_Range", "[EffectiveUntilUtcExclusive] IS NULL OR [EffectiveFromUtc] < [EffectiveUntilUtcExclusive]"); });
        period.HasAlternateKey(x => new { x.Id, x.PartProcessCharacteristicId });
        period.HasIndex(x => new { x.ScheduleRevisionId, x.EffectiveFromUtc }).IsUnique();
        period.HasOne<ChemicalFormulaScheduleRevision>().WithMany().HasForeignKey(x => new { x.ScheduleRevisionId, x.PartProcessCharacteristicId }).HasPrincipalKey(x => new { x.Id, x.PartProcessCharacteristicId }).OnDelete(DeleteBehavior.NoAction);
        period.HasOne<ChemicalAnalysisFormulaVersion>().WithMany().HasForeignKey(x => new { x.FormulaVersionId, x.PartProcessCharacteristicId }).HasPrincipalKey(x => new { x.Id, x.PartProcessCharacteristicId }).OnDelete(DeleteBehavior.NoAction);
        period.HasOne<ChemicalFTableVersionHistory>().WithMany().HasForeignKey(x => x.FTableHistoryId).OnDelete(DeleteBehavior.NoAction);
        var policy = b.Entity<ChemicalFormulaPolicy>();
        policy.ToTable("ChemicalFormulaPolicies", t => { t.HasComment("Per-PPC opt-in; existing PPCs are not enabled or backfilled."); t.HasTrigger("TR_ChemicalPolicyIntegrity"); t.HasCheckConstraint("CK_ChemicalPolicy_Active", "[IsEnabled] = 0 OR [ActiveScheduleRevisionId] IS NOT NULL"); });
        policy.HasKey(x => x.PartProcessCharacteristicId);
        policy.HasOne<PartProcessCharacteristic>().WithMany().HasForeignKey(x => x.PartProcessCharacteristicId).OnDelete(DeleteBehavior.NoAction);
        policy.HasOne<ChemicalFormulaScheduleRevision>().WithMany().HasForeignKey(x => new { x.ActiveScheduleRevisionId, x.PartProcessCharacteristicId }).HasPrincipalKey(x => new { x.Id, x.PartProcessCharacteristicId }).OnDelete(DeleteBehavior.NoAction);
        var receipt = b.Entity<ChemicalCalculationReceipt>();
        receipt.ToTable("ChemicalCalculationReceipts", t => { t.HasComment("Immutable server-prepared evidence; canonical values and F snapshots in payload."); t.HasTrigger("TR_ChemicalReceiptImmutable"); t.HasCheckConstraint("CK_ChemicalReceipt_Expiry", "[CreatedAtUtc] < [ExpiresAtUtc]"); });
        receipt.HasOne<ChemicalFormulaEffectivity>().WithMany().HasForeignKey(x => new { x.EffectivityId, x.PartProcessCharacteristicId }).HasPrincipalKey(x => new { x.Id, x.PartProcessCharacteristicId }).OnDelete(DeleteBehavior.NoAction);
        receipt.Property(x => x.InputHash).HasMaxLength(64); receipt.Property(x => x.SourceKeyHash).HasMaxLength(64);
        receipt.Property(x => x.Actor).HasMaxLength(100); receipt.Property(x => x.EngineVersion).HasMaxLength(100); receipt.Property(x => x.PrecisionPolicy).HasMaxLength(100);
        receipt.HasIndex(x => new { x.PartProcessCharacteristicId, x.SourceKeyHash });
        var measurement = b.Entity<VariableMeasurement>();
        measurement.ToTable(t => t.HasTrigger("TR_ChemicalMeasurementGate"));
        measurement.Property(x => x.ChemicalSourceKeyHash).HasMaxLength(64);
        measurement.HasOne<ChemicalCalculationReceipt>().WithMany().HasForeignKey(x => x.ChemicalReceiptId).OnDelete(DeleteBehavior.NoAction);
        measurement.HasIndex(x => new { x.PartProcessCharacteristicId, x.ChemicalSourceKeyHash }).IsUnique().HasFilter("[ChemicalSourceKeyHash] IS NOT NULL");
        var trace = b.Entity<ChemicalCalculationTrace>();
        trace.ToTable("ChemicalCalculationTraces", t => { t.HasComment("Append-only phase versions; legacy measurements have no invented trace."); t.HasTrigger("TR_ChemicalTraceIntegrity"); t.HasCheckConstraint("CK_ChemicalTrace_Phase", "[Phase] IN ('Initial','Recheck') AND [VersionNo] > 0"); });
        trace.Property(x => x.Phase).HasMaxLength(16); trace.Property(x => x.RawResultCanonical).HasMaxLength(100); trace.Property(x => x.StoredResultCanonical).HasMaxLength(100);
        trace.HasOne<VariableMeasurement>().WithMany().HasForeignKey(x => x.VariableMeasurementId).OnDelete(DeleteBehavior.NoAction);
        trace.HasOne<ChemicalCalculationReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.NoAction);
        trace.HasIndex(x => new { x.VariableMeasurementId, x.Phase, x.VersionNo }).IsUnique();
        trace.HasIndex(x => new { x.ReceiptId, x.Phase }).IsUnique();
        var job = b.Entity<ChemicalStatisticsJob>();
        job.ToTable("ChemicalStatisticsJobs", t => { t.HasComment("Transactional statistics outbox, consumer delivered separately."); t.HasTrigger("TR_ChemicalJobIntegrity"); t.HasCheckConstraint("CK_ChemicalJob_State", "[Attempts] >= 0 AND [Status] IN ('Pending','Processing','Completed','Failed')"); });
        job.Property(x => x.Status).HasMaxLength(16); job.Property(x => x.LastError).HasMaxLength(1000);
        job.HasOne<VariableMeasurement>().WithMany().HasForeignKey(x => x.VariableMeasurementId).OnDelete(DeleteBehavior.NoAction);
        job.HasOne<ChemicalCalculationReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.NoAction);
        job.HasIndex(x => new { x.VariableMeasurementId, x.ReceiptId }).IsUnique();
        job.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
    }

    private void ValidateChemicalImmutableEntries()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(e => e.Entity is ChemicalCalculationReceipt or ChemicalCalculationTrace
            && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("計算憑證及追溯紀錄不可修改或刪除。");
    }
}
