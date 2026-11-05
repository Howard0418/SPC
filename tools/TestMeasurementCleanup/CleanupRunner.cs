using System.Data;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace TestMeasurementCleanup;
internal static class CleanupRunner
{
 private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
 public static async Task RunAsync(string[] args)
 {
  if(args.Length is not(4 or 5)||args[3] is not("plan" or "apply" or "verify" or "gate-test"))throw new ArgumentException("TEST_CONFIG PROD_CONFIG OUTPUT_DIR plan|apply|verify|gate-test [TEST_BACKUP_CONFIG]");
  if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows actor required");
  Config Load(string p,string env,string database){using var d=JsonDocument.Parse(File.ReadAllText(p));var b=new SqlConnectionStringBuilder(d.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!.Replace("Trust Server Certificate=","TrustServerCertificate="));if(d.RootElement.GetProperty("AppEnvironment").GetString()!=env||b.DataSource!="172.16.110.16"||b.InitialCatalog!=database)throw new InvalidOperationException("Configuration target mismatch");return new(env,b.ConnectionString);}
  var tc=Load(args[0],"test","PMR_SPC_TEST");var pc=Load(args[1],"production","PMR_SPC_2026");
  AppDbContext Open(Config c)=>new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(c.Connection).Options);
  await using var test=Open(tc);await using var prod=Open(pc);test.Database.SetCommandTimeout(120);Directory.CreateDirectory(args[2]);var mode=args[3];var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
  using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};var api=await http.GetStringAsync("https://SPCTEST.pmr.com.tw/api/version");using var version=JsonDocument.Parse(api);await http.GetStringAsync("https://SPCTEST.pmr.com.tw/api/health");var b=new SqlConnectionStringBuilder(tc.Connection);CleanupGuard.RequireTest(tc.Environment,b.DataSource,b.InitialCatalog,version.RootElement.GetProperty("environment").GetString());
  await test.Database.OpenConnectionAsync();await prod.Database.OpenConnectionAsync();
  foreach(var pair in new[]{(test,"PMR_SPC_TEST"),(prod,"PMR_SPC_2026")}){await using var c=pair.Item1.Database.GetDbConnection().CreateCommand();c.CommandText="SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";await using var r=await c.ExecuteReaderAsync();await r.ReadAsync();if(r.GetString(0)!=pair.Item2||r.GetString(1)!="172.16.110.16")throw new InvalidOperationException("Actual database mismatch");}
  async Task<string> Gate(AppDbContext db)
  {
   await using var maintenance=db==test&&args.Length==5?new SqlConnection(Load(args[4],"test","PMR_SPC_TEST").Connection):null;
   if(maintenance!=null)await maintenance.OpenAsync();await using var c=(maintenance??db.Database.GetDbConnection()).CreateCommand();
   c.CommandText="SELECT COALESCE(OBJECT_DEFINITION(object_id),CONCAT(name,'|',CONVERT(nvarchar(33),modify_date,126),'|',is_disabled)) FROM sys.triggers WHERE name='TR_ChemicalMeasurementGate'";
   var result=Convert.ToString(await c.ExecuteScalarAsync());if(string.IsNullOrWhiteSpace(result))throw new InvalidOperationException("Missing or inaccessible gate");return result;
  }
  var testGate=await Gate(test);var productionGate=await Gate(prod);
  string OriginalGate()
  {
   if(!testGate.Contains(LegacyArchiveGate.Marker))return testGate;
   foreach(var path in Directory.GetFiles(args[2],"*-gate-after.sql"))if(LegacyArchiveGate.Normalize(File.ReadAllText(path))==LegacyArchiveGate.Normalize(testGate))return File.ReadAllText(path.Replace("-gate-after.sql","-gate-before.sql"));
   throw new InvalidOperationException("Patched gate has no matching original definition");
  }
  if(mode=="gate-test")
  {
   if(args.Length!=5)throw new InvalidOperationException("Explicit test maintenance configuration required");
   var n=await GateSqlRehearsal.RunAsync(Load(args[4],"test","PMR_SPC_TEST").Connection,OriginalGate());
   if(await Gate(test)!=testGate||await Gate(prod)!=productionGate)throw new InvalidOperationException("Live gate changed");
   await File.WriteAllTextAsync(Path.Combine(args[2],"gate-test.json"),JsonSerializer.Serialize(new{checks=n,passed=true,testAndProductionGateUnchanged=true,originalGateHash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(testGate))),verifiedAtUtc=DateTime.UtcNow},Json));Console.WriteLine($"Isolated SQL archive gate checks passed: {n}");return;
  }
  using var approved=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[2],"approved-lines.json")));var workbook=approved.RootElement.GetProperty("workbook").GetString()!;var workbookHash=approved.RootElement.GetProperty("sha256").GetString()!;
  void CheckWorkbook(){using var f=new FileStream(workbook,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);if(Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant()!=workbookHash)throw new InvalidOperationException("Approved workbook changed");}CheckWorkbook();if(approved.RootElement.GetProperty("count").GetInt32()!=48||approved.RootElement.GetProperty("column").GetString()!="G")throw new InvalidOperationException("Approved source contract changed");
  var before=await DatabaseInventory.ReadAsync(test);var formalBefore=await DatabaseInventory.ReadAsync(prod);var targetNames=CleanupService.Tables.ToHashSet();if(targetNames.Any(t=>!before.Any(s=>s.Table==t&&s.Active.HasValue)))throw new InvalidOperationException("Cleanup schema differs");
  var ids=new Dictionary<string,List<string>>();foreach(var table in CleanupService.Tables){await using var c=test.Database.GetDbConnection().CreateCommand();c.CommandText=$"SELECT CONVERT(nvarchar(100),[{(table=="UploadBatches"?"UploadBatchId":"Id")}]) FROM dbo.[{table}] WHERE IsDeleted=0";await using var r=await c.ExecuteReaderAsync();var list=new List<string>();while(await r.ReadAsync())list.Add(r.GetString(0));ids[table]=list;}
  var jobsBefore=await test.ChemicalStatisticsJobs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
  await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-before.json"),JsonSerializer.Serialize(new{mode,sourceWorkbookHash=workbookHash,before,formalBefore,ids,jobsBefore},Json));
  Console.WriteLine($"{mode}: active measurement/import rows={before.Where(x=>targetNames.Contains(x.Table)).Sum(x=>x.Active??0)}; variable={before.Single(x=>x.Table=="VariableMeasurements").Active}; particles={before.Single(x=>x.Table=="ParticleMeasurements").Active}");
  CleanupResult? cleared=null;
  if(mode=="apply")
  {
   if(ids.Values.Sum(x=>x.Count)==0)throw new InvalidOperationException("Already empty; use verify");
   // Maintenance performs backup and the authorized gate patch; data cleanup uses the runtime identity.
   var backupConfig=args.Length==5?Load(args[4],"test","PMR_SPC_TEST"):tc;
   await using(var backupConnection=new SqlConnection(backupConfig.Connection))
   {
    await backupConnection.OpenAsync();await using var c=backupConnection.CreateCommand();c.CommandTimeout=600;
    c.CommandText="SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
    await using(var r=await c.ExecuteReaderAsync()){await r.ReadAsync();if(r.GetString(0)!="PMR_SPC_TEST"||r.GetString(1)!="172.16.110.16")throw new InvalidOperationException("Backup database mismatch");}
    c.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";var dir=Convert.ToString(await c.ExecuteScalarAsync())!;
    var backup=dir.TrimEnd('\\')+"\\PMR_SPC_TEST_CLEAR_"+stamp+".bak";
    c.CommandText="BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@p WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@p WITH CHECKSUM";c.Parameters.AddWithValue("@p",backup);await c.ExecuteNonQueryAsync();
    await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-backup.json"),JsonSerializer.Serialize(new{database="PMR_SPC_TEST",backup,verified=true,copyOnly=true,checksum=true,maintenanceIdentity=args.Length==5},Json));
    {
     using var proof=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[2],"gate-test.json")));if(!proof.RootElement.GetProperty("passed").GetBoolean()||proof.RootElement.GetProperty("checks").GetInt32()!=10||proof.RootElement.GetProperty("originalGateHash").GetString()!=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(testGate)))||DateTime.UtcNow-proof.RootElement.GetProperty("verifiedAtUtc").GetDateTime()>TimeSpan.FromHours(1))throw new InvalidOperationException("Missing or stale SQL rehearsal proof");
     c.Parameters.Clear();c.CommandText="SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.VariableMeasurements') AND system_type_id<>189 ORDER BY column_id";var columns=new List<string>();await using(var r=await c.ExecuteReaderAsync()){while(await r.ReadAsync())columns.Add(r.GetString(0));}
     var original=OriginalGate();var changed=LegacyArchiveGate.Build(original,columns);await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-gate-before.sql"),original);await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-gate-after.sql"),changed);
     if(LegacyArchiveGate.Normalize(testGate)!=LegacyArchiveGate.Normalize(changed)){c.CommandText=changed;await c.ExecuteNonQueryAsync();}
     if(LegacyArchiveGate.Normalize(await Gate(test))!=LegacyArchiveGate.Normalize(changed))throw new InvalidOperationException("Test gate patch mismatch");
    }
   }
   await using var tx=await test.Database.BeginTransactionAsync(IsolationLevel.Serializable);var lockedBefore=await DatabaseInventory.ReadAsync(test);
   if(JsonSerializer.Serialize(lockedBefore.Where(x=>x.Table!="ChemicalStatisticsJobs"))!=JsonSerializer.Serialize(before.Where(x=>x.Table!="ChemicalStatisticsJobs")))throw new InvalidOperationException("Test data changed during backup; rerun to capture current IDs");
   cleared=await CleanupService.ApplyAsync(test,WindowsIdentity.GetCurrent().Name,DateTime.UtcNow);var afterInside=await DatabaseInventory.ReadAsync(test);
   foreach(var state in lockedBefore){var a=afterInside.Single(x=>x.Table==state.Table);if(targetNames.Contains(state.Table)){if(a.Active!=0||a.Total!=state.Total||cleared.DeletedByTable[state.Table]!=state.Active)throw new InvalidOperationException("Cleanup count mismatch");}else if(state.Table!="ChemicalStatisticsJobs"&&a!=state)throw new InvalidOperationException("Protected test table changed: "+state.Table);}
   var jobsAfter=await test.ChemicalStatisticsJobs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();var originalJobs=jobsBefore.ToDictionary(x=>x.Id);if(jobsAfter.Count!=jobsBefore.Count||jobsAfter.Any(j=>!originalJobs.TryGetValue(j.Id,out var old)||j.VariableMeasurementId!=old.VariableMeasurementId||j.ReceiptId!=old.ReceiptId))throw new InvalidOperationException("Job identities changed");if(await test.ChemicalStatisticsJobs.AnyAsync(x=>x.Status!="Completed"&&(x.NextAttemptAtUtc==null||x.NextAttemptAtUtc<=DateTime.UtcNow)))throw new InvalidOperationException("Retryable jobs remain");CheckWorkbook();await tx.CommitAsync();
  }
  var after=await DatabaseInventory.ReadAsync(test);var formalAfter=await DatabaseInventory.ReadAsync(prod);if(JsonSerializer.Serialize(formalBefore)!=JsonSerializer.Serialize(formalAfter)||await Gate(prod)!=productionGate)throw new InvalidOperationException("Production changed during test cleanup");
  var remaining=after.Where(x=>targetNames.Contains(x.Table)).Sum(x=>x.Active??0);if((mode is "apply" or "verify")&&remaining!=0)throw new InvalidOperationException("Remaining active data");
  var spc=new SpcService(test,null!,new ConfigurationBuilder().Build());int charts=0;foreach(var id in await test.PartProcessCharacteristics.AsNoTracking().Where(x=>x.IsEnabled&&x.ChartTypeId!=null).Select(x=>x.Id).ToListAsync()){var chart=await spc.GetInteractiveChartAsync(id,null,null,null);if(mode!="plan"&&chart is not null)throw new InvalidOperationException("Chart still has data");charts++;}
  var health=await http.GetStringAsync("https://SPCTEST.pmr.com.tw/api/health");await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-"+mode+".json"),JsonSerializer.Serialize(new{mode,cleared,remaining,chartsChecked=charts,before,after,formalUnchanged=true,apiVersion=JsonSerializer.Deserialize<object>(api),health},Json));Console.WriteLine($"{mode}: remaining={remaining}; chartsChecked={charts}; protected test tables and production unchanged");
 }
 private sealed record Config(string Environment,string Connection);
}
