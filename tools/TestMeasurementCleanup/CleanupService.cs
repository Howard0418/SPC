using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace TestMeasurementCleanup;
public static class CleanupGuard
{
 public static void RequireTest(string? environment,string? server,string? database,string? apiEnvironment){if(environment!="test"||server!="172.16.110.16"||database!="PMR_SPC_TEST"||apiEnvironment!="test")throw new InvalidOperationException("Only the confirmed SPC test database is permitted.");}
}
public sealed record CleanupResult(Dictionary<string,int> DeletedByTable,int JobsSuspended);
public static class CleanupService
{
 public static readonly string[] Tables=["VariableMeasurements","AttributeMeasurements","ParticleMeasurements","MeasurementValues","MeasurementBatches","SpcCalculationResults","SpcPointExclusions","SpcPointRemarks","AlertEvents","UploadDetails","UploadErrors","UploadBatches"];
 public static async Task<CleanupResult> ApplyAsync(AppDbContext db,string actor,DateTime at)
 {
  var jobs=await db.ChemicalStatisticsJobs.Where(x=>x.Status!="Completed"&&x.NextAttemptAtUtc!=DateTime.MaxValue).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Failed").SetProperty(x=>x.NextAttemptAtUtc,DateTime.MaxValue).SetProperty(x=>x.LastError,"User-authorized test measurement cleanup; original job retained, measurement archived, retry suspended."));
  var result=new Dictionary<string,int>();
  async Task Clear<T>() where T:BaseEntity{var table=db.Model.FindEntityType(typeof(T))!.GetTableName()!;result[table]=await db.Set<T>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsDeleted,true).SetProperty(x=>x.UpdatedAt,at).SetProperty(x=>x.UpdatedBy,actor));}
  // Bound inserted/deleted trigger work without changing the enclosing atomic transaction.
  var variables=0;
  while(true){var ids=await db.VariableMeasurements.OrderBy(x=>x.Id).Select(x=>x.Id).Take(200).ToListAsync();if(ids.Count==0)break;variables+=await db.VariableMeasurements.Where(x=>ids.Contains(x.Id)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsDeleted,true).SetProperty(x=>x.UpdatedAt,at).SetProperty(x=>x.UpdatedBy,actor));}
  result["VariableMeasurements"]=variables;
  await Clear<AttributeMeasurement>();await Clear<ParticleMeasurement>();await Clear<MeasurementValue>();await Clear<MeasurementBatch>();await Clear<SpcCalculationResult>();await Clear<SpcPointExclusion>();await Clear<SpcPointRemark>();await Clear<AlertEvent>();await Clear<UploadDetail>();await Clear<UploadError>();await Clear<UploadBatch>();
  return new(result,jobs);
 }
}
