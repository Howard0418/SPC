using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using TestMeasurementCleanup;
public class TestMeasurementCleanupTests
{
 [Theory][InlineData("production","172.16.110.16","PMR_SPC_2026","production")][InlineData("test","172.16.110.16","PMR_SPC_2026","test")][InlineData("test","other","PMR_SPC_TEST","test")][InlineData("test","172.16.110.16","PMR_SPC_TEST","production")]
 public void RejectsWrongTarget(string e,string s,string d,string a)=>Assert.Throws<InvalidOperationException>(()=>CleanupGuard.RequireTest(e,s,d,a));
 [Fact]public void AcceptsOnlyTest()=>CleanupGuard.RequireTest("test","172.16.110.16","PMR_SPC_TEST","test");
 [Fact]public async Task ClearsAllMeasurementAndImportTypesButKeepsMastersAndEvidence()
 {
  await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
  db.Processes.Add(new(){ProcessCode="KEEP",ProcessName="KEEP"});db.ChemicalFTableVersions.Add(new(){VersionCode="KEEP",DisplayName="KEEP"});
  db.VariableMeasurements.Add(new(){Id=1,MeasuredValue=5});db.AttributeMeasurements.Add(new(){Id=1});db.ParticleMeasurements.Add(new(){Id=1,Location="A"});db.MeasurementBatches.Add(new(){Id=1,BatchNo="A"});db.MeasurementValues.Add(new(){Id=1,BatchId=1});db.UploadBatches.Add(new(){UploadBatchId=Guid.NewGuid()});db.UploadDetails.Add(new(){Id=1});db.UploadErrors.Add(new(){Id=1});db.SpcCalculationResults.Add(new(){Id=1});db.SpcPointExclusions.Add(new(){Id=1});db.SpcPointRemarks.Add(new(){Id=1});db.AlertEvents.Add(new(){Id=1});
  db.VariableMeasurements.Add(new(){Id=2,MeasuredValue=9,IsDeleted=true,UpdatedBy="previous archive"});
  var receipt=Guid.NewGuid();db.ChemicalCalculationReceipts.Add(new(){Id=receipt,CreatedAtUtc=DateTime.UtcNow,ExpiresAtUtc=DateTime.UtcNow.AddHours(1)});db.ChemicalCalculationTraces.Add(new(){Id=1,ReceiptId=receipt,VariableMeasurementId=1,Phase="Initial",VersionNo=1});db.ChemicalStatisticsJobs.AddRange(new ChemicalStatisticsJob{Id=Guid.NewGuid(),ReceiptId=receipt,VariableMeasurementId=1,Status="Pending"},new ChemicalStatisticsJob{Id=Guid.NewGuid(),ReceiptId=receipt,VariableMeasurementId=2,Status="Completed"});await db.SaveChangesAsync();db.ChangeTracker.Clear();
  var result=await CleanupService.ApplyAsync(db,"operator",DateTime.UtcNow);
  Assert.All(result.DeletedByTable.Values,n=>Assert.Equal(1,n));Assert.Equal(1,result.JobsSuspended);Assert.Equal(0,await db.VariableMeasurements.CountAsync());Assert.Equal(2,await db.VariableMeasurements.IgnoreQueryFilters().CountAsync());Assert.Equal("previous archive",await db.VariableMeasurements.IgnoreQueryFilters().Where(x=>x.Id==2).Select(x=>x.UpdatedBy).SingleAsync());Assert.Equal(1,await db.Processes.CountAsync());Assert.Equal(1,await db.ChemicalFTableVersions.CountAsync());Assert.Equal(1,await db.ChemicalCalculationReceipts.CountAsync());Assert.Equal(1,await db.ChemicalCalculationTraces.CountAsync());Assert.Equal(1,await db.ChemicalStatisticsJobs.CountAsync(x=>x.Status=="Completed"));Assert.Equal(DateTime.MaxValue,await db.ChemicalStatisticsJobs.Where(x=>x.Status=="Failed").Select(x=>x.NextAttemptAtUtc).SingleAsync());
  var again=await CleanupService.ApplyAsync(db,"operator",DateTime.UtcNow);Assert.All(again.DeletedByTable.Values,n=>Assert.Equal(0,n));Assert.Equal(0,again.JobsSuspended);
 }
 [Fact]public async Task TransactionRollbackRestoresMeasurement()
 {
  await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();db.VariableMeasurements.Add(new(){MeasuredValue=7});await db.SaveChangesAsync();await using(var tx=await db.Database.BeginTransactionAsync()){await CleanupService.ApplyAsync(db,"operator",DateTime.UtcNow);Assert.Equal(0,await db.VariableMeasurements.CountAsync());await tx.RollbackAsync();}Assert.Equal(1,await db.VariableMeasurements.CountAsync());
 }
}
