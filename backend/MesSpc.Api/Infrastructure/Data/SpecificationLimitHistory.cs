using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MesSpc.Api.Domain.Entities;
namespace MesSpc.Api.Infrastructure.Data;
public partial class AppDbContext
{
    private sealed record SpecChange(EntityEntry Entry,string Kind,string Action,string? Previous,string? Current);
    private List<SpecChange> PendingSpecificationChanges()
    {
        ChangeTracker.DetectChanges();
        if(ChangeTracker.Entries<SpecificationLimitVersion>().Any(e=>e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("規格版本歷史不可修改或刪除。");
        var changes=new List<SpecChange>();
        foreach(var e in ChangeTracker.Entries().Where(e=>e.Entity is PartProcessCharacteristic or ControlLimitSegment))
        {
            if(e.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))continue;
            var segment=e.Entity is ControlLimitSegment;
            var names=segment?new[]{"USL","LSL","TargetValue","StartDate","EndDate","Note"}:new[]{"USL","LSL","TargetValue"};
            string Snapshot(PropertyValues values)=>JsonSerializer.Serialize(names.ToDictionary(n=>n,n=>values[n]));
            if(e.State==EntityState.Added && !segment && names.All(n=>e.CurrentValues[n] is null))continue;
            if(e.State==EntityState.Modified && !names.Any(n=>!Equals(e.OriginalValues[n],e.CurrentValues[n])))continue;
            changes.Add(new(e,segment?"Segment":"Base",e.State==EntityState.Added?"Create":e.State==EntityState.Deleted?"Delete":"Update",
                e.State==EntityState.Added?null:Snapshot(e.OriginalValues),e.State==EntityState.Deleted?null:Snapshot(e.CurrentValues)));
        }
        return changes;
    }
    private static string[] SpecificationFields(SpecChange c) => c.Kind == "Segment"
        ? ["USL", "LSL", "TargetValue", "StartDate", "EndDate", "Note"] : ["USL", "LSL", "TargetValue"];
    private static void VerifyOriginal(SpecChange change, PropertyValues? persisted)
    {
        if (persisted is null || SpecificationFields(change).Any(n => !Equals(change.Entry.OriginalValues[n], persisted[n])))
            throw new DbUpdateConcurrencyException("規格已被其他使用者更新，請重新載入後再儲存。");
    }
    private static int PpcId(SpecChange c)=>c.Entry.Entity is PartProcessCharacteristic p?p.Id:((ControlLimitSegment)c.Entry.Entity).PartProcessCharacteristicId;
    private void AddSpecificationVersions(IEnumerable<SpecChange> changes,Dictionary<int,int> last)
    {
        var actor=httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        actor=string.IsNullOrWhiteSpace(actor)?"System":actor.Trim();
        foreach(var c in changes)
        {
            var id=PpcId(c);last.TryGetValue(id,out var version);last[id]=version+1;
            SpecificationLimitVersions.Add(new(){PartProcessCharacteristicId=id,VersionNo=version+1,SourceKind=c.Kind,
                SourceSegmentId=c.Entry.Entity is ControlLimitSegment s?s.Id:null,ChangeType=c.Action,
                PreviousSnapshotJson=c.Previous,SnapshotJson=c.Current,ChangedAt=DateTime.UtcNow,ChangedBy=actor});
        }
    }
    private async Task<int> SaveWithSpecificationHistoryAsync(bool accept,CancellationToken ct)
    {
        var changes=PendingSpecificationChanges();
        if(changes.Count==0){HandleAuditFields();return await base.SaveChangesAsync(accept,ct);}
        if(!accept)throw new InvalidOperationException("規格異動需接受追蹤狀態才能原子保存版本。");
        var own=Database.IsRelational() && Database.CurrentTransaction is null;
        await using var transaction=own?await Database.BeginTransactionAsync(ct):null;
        try
        {
            if(Database.IsSqlServer())foreach(var id in changes.Select(PpcId).Where(id=>id>0).Distinct().Order())
                await Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}",ct);
            foreach(var c in changes.Where(c=>c.Action != "Create")) VerifyOriginal(c, await c.Entry.GetDatabaseValuesAsync(ct));
            HandleAuditFields();var count=await base.SaveChangesAsync(true,ct);
            var ids=changes.Select(PpcId).Distinct().ToArray();
            var maxima=await SpecificationLimitVersions.Where(x=>ids.Contains(x.PartProcessCharacteristicId))
                .GroupBy(x=>x.PartProcessCharacteristicId).Select(g=>new{Id=g.Key,Version=g.Max(x=>x.VersionNo)}).ToDictionaryAsync(x=>x.Id,x=>x.Version,ct);
            AddSpecificationVersions(changes,maxima);await base.SaveChangesAsync(true,ct);
            if(transaction is not null)await transaction.CommitAsync(ct);
            return count;
        }
        catch
        {
            if(Database.CurrentTransaction is { } active)await active.RollbackAsync(CancellationToken.None);
            ChangeTracker.Clear();throw;
        }
    }
    private int SaveWithSpecificationHistory(bool accept)
    {
        var changes=PendingSpecificationChanges();
        if(changes.Count==0){HandleAuditFields();return base.SaveChanges(accept);}
        if(!accept)throw new InvalidOperationException("規格異動需接受追蹤狀態才能原子保存版本。");
        using var transaction=Database.IsRelational() && Database.CurrentTransaction is null?Database.BeginTransaction():null;
        try
        {
            if(Database.IsSqlServer())foreach(var id in changes.Select(PpcId).Where(id=>id>0).Distinct().Order())
                Database.ExecuteSqlInterpolated($"SELECT Id FROM PartProcessCharacteristics WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}");
            foreach(var c in changes.Where(c=>c.Action != "Create")) VerifyOriginal(c, c.Entry.GetDatabaseValues());
            HandleAuditFields();var count=base.SaveChanges(true);
            var ids=changes.Select(PpcId).Distinct().ToArray();
            var maxima=SpecificationLimitVersions.Where(x=>ids.Contains(x.PartProcessCharacteristicId))
                .GroupBy(x=>x.PartProcessCharacteristicId).Select(g=>new{Id=g.Key,Version=g.Max(x=>x.VersionNo)}).ToDictionary(x=>x.Id,x=>x.Version);
            AddSpecificationVersions(changes,maxima);base.SaveChanges(true);transaction?.Commit();return count;
        }
        catch {Database.CurrentTransaction?.Rollback();ChangeTracker.Clear();throw;}
    }
}
