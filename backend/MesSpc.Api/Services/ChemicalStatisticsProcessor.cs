using System.Data;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Services;

/// <summary>Transactional, retryable consumer for direct SQL imports. Statistics are never recomputed by the client.</summary>
public sealed class ChemicalStatisticsProcessor(AppDbContext db,SpcService spc,ILogger<ChemicalStatisticsProcessor> logger)
{
    public async Task<bool> ProcessOneAsync(Guid id,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            if(db.Database.IsSqlServer())
            {
                var resource="ChemicalStatistics:"+id.ToString("N");
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 51200,'Statistics job busy.',1;",ct);
            }
            var job=await db.ChemicalStatisticsJobs.SingleOrDefaultAsync(x=>x.Id==id,ct);
            if(job is null || job.Status=="Completed" || job.NextAttemptAtUtc>DateTime.UtcNow){await tx.CommitAsync(ct);return false;}
            var vm=await db.VariableMeasurements.SingleAsync(x=>x.Id==job.VariableMeasurementId,ct);
            if(db.Database.IsSqlServer())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={vm.PartProcessCharacteristicId}",ct);
            if(vm.ChemicalReceiptId!=job.ReceiptId)throw new InvalidOperationException("Measurement changed before statistics completed.");
            job.Status="Processing";job.Attempts++;await db.SaveChangesAsync(ct);
            if(await spc.CalculateVariableAsync(vm,ct,true) is null)throw new InvalidOperationException("Missing enabled chart configuration.");
            job.Status="Completed";job.NextAttemptAtUtc=null;job.LastError=null;
            var batch=await db.UploadBatches.SingleAsync(x=>x.UploadBatchId==vm.UploadBatchId,ct);
            batch.ImportStatus="Imported";await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);await tx.DisposeAsync();
            try{await spc.NotifyCommittedVariableBatchAsync(batch.UploadBatchId,ct);}
            catch(Exception ex){logger.LogWarning(ex,"Committed direct batch notification failed {BatchId}",batch.UploadBatchId);}
            return true;
        }
        catch(OperationCanceledException){await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
        catch(Exception ex)
        {
            await tx.RollbackAsync(CancellationToken.None);await tx.DisposeAsync();db.ChangeTracker.Clear();
            logger.LogError(ex,"Chemical statistics job failed {JobId}",id);
            await using var failureTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            if(db.Database.IsSqlServer())
            {
                var resource="ChemicalStatistics:"+id.ToString("N");
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 51200,'Statistics job busy.',1;",ct);
            }
            var job=await db.ChemicalStatisticsJobs.SingleOrDefaultAsync(x=>x.Id==id,ct);
            if(job is not null && job.Status!="Completed")
            {job.Attempts++;job.Status="Failed";job.LastError="統計未完成，請檢查管制圖設定或服務紀錄後等待重試。";job.NextAttemptAtUtc=DateTime.UtcNow.AddMinutes(Math.Min(30,job.Attempts));await db.SaveChangesAsync(ct);}
            await failureTx.CommitAsync(ct);
            return false;
        }
    }
}

public sealed class ChemicalStatisticsWorker(IServiceScopeFactory scopes,ILogger<ChemicalStatisticsWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ids=await db.ChemicalStatisticsJobs.AsNoTracking().Where(x=>x.Status!="Completed" && (x.NextAttemptAtUtc==null || x.NextAttemptAtUtc<=DateTime.UtcNow))
                    .OrderBy(x=>x.CreatedAtUtc).Select(x=>x.Id).Take(20).ToListAsync(ct);
                foreach(var id in ids)await scope.ServiceProvider.GetRequiredService<ChemicalStatisticsProcessor>().ProcessOneAsync(id,ct);
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Chemical statistics queue unavailable");}
            await Task.Delay(TimeSpan.FromSeconds(15),ct);
        }
    }
}
