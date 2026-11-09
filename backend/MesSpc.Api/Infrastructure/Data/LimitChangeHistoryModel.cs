using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public partial class AppDbContext
{
    private void ConfigureLimitChangeHistory(ModelBuilder builder)
    {
        var batch = builder.Entity<LimitChangeBatch>();
        batch.ToTable("LimitChangeBatches", table =>
        {
            table.HasComment("Immutable header for atomic specification/control limit batch edits.");
            table.HasCheckConstraint("CK_LimitChangeBatches_ItemCount", "[ItemCount] BETWEEN 1 AND 500");
        });
        batch.HasKey(x => x.Id);
        batch.Property(x => x.ChangedBy).HasMaxLength(100);
        batch.HasIndex(x => x.ChangedAt);

        var item = builder.Entity<LimitChangeItem>();
        item.ToTable("LimitChangeItems", table => table.HasComment("Immutable per-PPC before/after values for a limit change batch."));
        item.HasKey(x => x.Id);
        item.HasIndex(x => new { x.LimitChangeBatchId, x.PartProcessCharacteristicId }).IsUnique();
        item.HasIndex(x => x.PartProcessCharacteristicId);
        item.HasOne(x => x.LimitChangeBatch).WithMany(x => x.Items)
            .HasForeignKey(x => x.LimitChangeBatchId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ValidateLimitChangeHistoryImmutableEntries()
    {
        if (ChangeTracker.Entries<LimitChangeBatch>().Any(x => x.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<LimitChangeItem>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("界限批次歷史不可修改或刪除。");
        }
    }
}
