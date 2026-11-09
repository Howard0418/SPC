using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AuditArchiveBatch> AuditArchiveBatches => Set<AuditArchiveBatch>();
    public DbSet<AuditEventArchive> AuditEventArchives => Set<AuditEventArchive>();
    public DbSet<AuditLegalHold> AuditLegalHolds => Set<AuditLegalHold>();

    private void ConfigureAuditEvents(ModelBuilder builder)
    {
        var entity = builder.Entity<AuditEvent>();
        entity.ToTable("AuditEvents", table =>
        {
            table.HasComment("SPC append-only activity events. Runtime must not update or delete evidence.");
            table.HasCheckConstraint("CK_AuditEvents_Environment", "[Environment] IN ('Test','Production')");
            table.HasCheckConstraint("CK_AuditEvents_Outcome", "[Outcome] IN ('Succeeded','Denied','Failed')");
            table.HasCheckConstraint("CK_AuditEvents_SchemaVersion", "[SchemaVersion] = 1");
            table.HasCheckConstraint("CK_AuditEvents_Actor", "([ActorType]='User' AND [ActorOperatorId] IS NOT NULL AND [ActorOperatorId]>0 AND [ActorAssurance] IN ('Jwt','PortalAssertion')) OR ([ActorType]='Anonymous' AND [ActorOperatorId] IS NULL AND [ActorAssurance]='None' AND [ActorAccountSnapshot] IS NULL AND [ActorOperatorCodeSnapshot] IS NULL AND [ActorDisplayNameSnapshot] IS NULL AND [ActorRoleSnapshot] IS NULL AND [ActorServiceCode] IS NULL) OR ([ActorType]='Service' AND [ActorOperatorId] IS NULL AND [ActorAssurance]='Service' AND [ActorServiceCode] IS NOT NULL)");
            table.HasCheckConstraint("CK_AuditEvents_ClientEvent", "[ClientEventId] IS NULL OR ([EventCode]='navigation.page.enter' AND [ActorType]='User' AND [SessionIdHash] IS NOT NULL)");
            table.HasCheckConstraint("CK_AuditEvents_HttpStatus", "[HttpStatus] IS NULL OR [HttpStatus] BETWEEN 100 AND 599");
            if (Database.IsSqlServer())
            {
                table.UseSqlOutputClause(false);
                table.HasCheckConstraint("CK_AuditEvents_DetailsJson", "ISJSON([DetailsJson])=1 AND DATALENGTH([DetailsJson])<=8192");
            }
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();
        entity.HasIndex(x => x.EventId).IsUnique();
        entity.HasIndex(x => new { x.Environment, x.ActorOperatorId, x.SessionIdHash, x.ClientEventId })
            .IsUnique().HasFilter("[ClientEventId] IS NOT NULL").HasDatabaseName("UX_AuditEvents_ClientRetry");
        entity.HasIndex(x => new { x.Environment, x.OccurredAtUtc, x.Id });
        entity.HasIndex(x => new { x.Environment, x.ActorOperatorId, x.OccurredAtUtc });
        entity.HasIndex(x => new { x.Environment, x.EventCode, x.OccurredAtUtc });
        entity.HasIndex(x => new { x.Environment, x.TargetType, x.TargetId, x.OccurredAtUtc });
        entity.HasIndex(x => new { x.Environment, x.CorrelationId });
        entity.Property(x => x.OccurredAtUtc).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        entity.Property(x => x.ReceivedAtUtc).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var name in new[] { "Environment", "ActorType", "ActorAssurance", "Outcome", "Source", "Category" })
            entity.Property<string>(name).HasMaxLength(32);
        foreach (var name in new[] { "EventCode", "ReasonCode", "PageKey", "ActorServiceCode", "TargetType", "TraceId", "CorrelationId" })
            entity.Property<string>(name).HasMaxLength(100);
        foreach (var name in new[] { "ActorAccountSnapshot", "ActorOperatorCodeSnapshot", "ActorDisplayNameSnapshot", "ActorRoleSnapshot", "TargetId", "RouteTemplate" })
            entity.Property<string>(name).HasMaxLength(200);
        foreach (var name in new[] { "AttemptedAccountHash", "SessionIdHash", "DeduplicationHash" })
            entity.Property<string>(name).HasMaxLength(64);
        entity.Property(x => x.HttpMethod).HasMaxLength(10);
        entity.Property(x => x.DetailsJson).HasMaxLength(4096);
        foreach (var property in entity.Metadata.GetProperties())
            property.SetComment("Audit v1: " + property.Name + ". Append-only; no raw request or secret values.");

        var batch = builder.Entity<AuditArchiveBatch>();
        batch.ToTable("AuditArchiveBatches", t => t.HasComment("Verified audit archive batches; maintenance identity only."));
        batch.HasKey(x => x.Id); batch.HasIndex(x => x.BatchId).IsUnique();
        batch.Property(x => x.Environment).HasMaxLength(32); batch.Property(x => x.Checksum).HasMaxLength(64);
        batch.Property(x => x.EncryptionKeyId).HasMaxLength(100); batch.Property(x => x.Status).HasMaxLength(32);
        foreach (var property in new[] { nameof(AuditArchiveBatch.StartedAtUtc), nameof(AuditArchiveBatch.CompletedAtUtc), nameof(AuditArchiveBatch.RangeFromUtc), nameof(AuditArchiveBatch.RangeToUtc) })
            batch.Property<DateTime>(property).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var archive = builder.Entity<AuditEventArchive>();
        archive.ToTable("AuditEventArchives", t => t.HasComment("Encrypted audit event archive; controlled maintenance deletion only."));
        archive.HasKey(x => x.Id); archive.HasIndex(x => x.OriginalEventId).IsUnique(); archive.HasIndex(x => x.EventId).IsUnique();
        archive.HasIndex(x => new { x.Environment, x.OccurredAtUtc, x.Id });
        archive.Property(x => x.Environment).HasMaxLength(32); archive.Property(x => x.Category).HasMaxLength(32);
        archive.Property(x => x.EventCode).HasMaxLength(100); archive.Property(x => x.PageKey).HasMaxLength(100);
        archive.Property(x => x.TargetType).HasMaxLength(100); archive.Property(x => x.TargetId).HasMaxLength(200);
        archive.Property(x => x.PayloadHash).HasMaxLength(64);
        archive.Property(x => x.OccurredAtUtc).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        archive.HasOne<AuditArchiveBatch>().WithMany().HasForeignKey(x => x.ArchiveBatchId).OnDelete(DeleteBehavior.Restrict);

        var hold = builder.Entity<AuditLegalHold>();
        hold.ToTable("AuditLegalHolds", t => t.HasComment("Legal holds override audit retention and purge."));
        hold.HasKey(x => x.Id); hold.HasIndex(x => x.HoldId).IsUnique(); hold.HasIndex(x => new { x.Environment, x.ReleasedAtUtc, x.DateFromUtc, x.DateToUtc });
        hold.Property(x => x.Environment).HasMaxLength(32); hold.Property(x => x.EventCode).HasMaxLength(100);
        hold.Property(x => x.PageKey).HasMaxLength(100); hold.Property(x => x.TargetType).HasMaxLength(100);
        hold.Property(x => x.TargetId).HasMaxLength(200); hold.Property(x => x.ReasonCode).HasMaxLength(100);
        foreach (var property in new[] { nameof(AuditLegalHold.DateFromUtc), nameof(AuditLegalHold.DateToUtc), nameof(AuditLegalHold.CreatedAtUtc) })
            hold.Property<DateTime>(property).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        hold.Property(x => x.ReleasedAtUtc).HasConversion(value => value,
            value => value == null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private void ValidateAuditImmutableEntries()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditEvent>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit events are append-only.");
        if (ChangeTracker.Entries<AuditEventArchive>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit archives are immutable.");
    }
}
