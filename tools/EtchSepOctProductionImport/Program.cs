using EtchProduction;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Explicitly authorized SPC production insert-only import. No Portal connection or master mutation.
var mode=args.ElementAtOrDefault(0)??"--preflight";
if(mode is not ("--preflight" or "--apply" or "--verify"))throw new ArgumentException("Unknown mode");
var root=Path.GetFullPath(args.ElementAtOrDefault(1)??throw new ArgumentException("Evidence directory required"));
var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};
using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")));
var source=manifest.RootElement.GetProperty("source").GetString()!;
var sourceHash=manifest.RootElement.GetProperty("sha256").GetString()!;
void CheckSource(){if(!Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).Equals(sourceHash,StringComparison.OrdinalIgnoreCase))throw new Exception("Source fingerprint changed");}
CheckSource();
ProductionGuard.RequireApplyConfirmation(mode,args,sourceHash);
var reports=manifest.RootElement.GetProperty("reports").Deserialize<List<EtchReportRequest>>(json)!;
string Key(EtchReportRequest r)=>$"ETCH:{r.ReportDate:yyyy-MM-dd}:{r.LineCode}";
if(reports.Count==0||reports.Any(r=>r.ReportDate.Year!=2026||r.ReportDate.Month is not (9 or 10))||reports.Select(Key).Distinct().Count()!=reports.Count)throw new Exception("September/October scope or duplicate guard");
foreach(var r in reports)EtchReportSyncService.Validate(r);
var cfg=new ConfigurationBuilder().AddJsonFile("D:/SPC/release/production/backend/appsettings.json").Build();
var connection=cfg.GetConnectionString("SqlServer")!;var cs=new SqlConnectionStringBuilder(connection);
ProductionGuard.RequireDatabase(cfg["AppEnvironment"],connection);
using var http=new HttpClient();var version=JsonDocument.Parse(await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/version"));
if(version.RootElement.GetProperty("environment").GetString()!="production")throw new Exception("Production API guard");
if(!OperatingSystem.IsWindows())throw new Exception("Actual Windows operator required");
var actor=WindowsIdentity.GetCurrent().Name;
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
var svc=new EtchReportSyncService(db);
var max=await db.VariableMeasurements.IgnoreQueryFilters().MaxAsync(x=>(long?)x.Id)??0;
async Task<string> Fingerprint(){
 await using var c=new SqlConnection(connection);await c.OpenAsync();using var cmd=c.CreateCommand();
 cmd.CommandText="SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,PartProcessCharacteristicId,MeasuredValue,RecheckValue,MeasuredAt,ChemicalReceiptId,ChemicalSourceKeyHash,IsDeleted,UploadBatchId,SourceReference,SampleNo)) FROM VariableMeasurements WHERE Id<=@max";cmd.Parameters.AddWithValue("@max",max);
 await using var reader=await cmd.ExecuteReaderAsync();await reader.ReadAsync();return JsonSerializer.Serialize(new{max,count=reader.GetInt64(0),checksum=reader.IsDBNull(1)?(int?)null:reader.GetInt32(1)});
}
async Task<string> Masters()=>JsonSerializer.Serialize(new{ppcs=await db.PartProcessCharacteristics.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),policies=await db.ChemicalFormulaPolicies.AsNoTracking().OrderBy(x=>x.PartProcessCharacteristicId).ToListAsync(),activeF=await db.ChemicalFTableVersions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Id).ToListAsync()});
var before=await Fingerprint();var masters=await Masters();
var skippedExisting=new List<object>();
var ready=new List<EtchReportRequest>();var preview=new List<object>();var blocked=new List<object>();
foreach(var r in reports){
 var reportKey=Key(r);
 if(mode!="--verify" && await db.VariableMeasurements.IgnoreQueryFilters().AsNoTracking().AnyAsync(x=>x.SourceReference==reportKey)){skippedExisting.Add(new{key=Key(r),reason="Already exists (including archived); never reimport or overwrite",count=await db.VariableMeasurements.IgnoreQueryFilters().CountAsync(x=>x.SourceReference==reportKey)});continue;}
 try{
  if(mode=="--verify"){await svc.ResolveMappingsAsync(r.LineCode);ready.Add(r);preview.Add(new{key=Key(r),verificationOnly=true});}
  else {var p=await svc.PreviewAsync(r);ready.Add(r);preview.Add(new{key=Key(r),r.ReportDate,r.LineCode,p.Unchanged,p.PointCount});}
 }
 catch(InvalidOperationException e){db.ChangeTracker.Clear();blocked.Add(new{key=Key(r),reason=e.Message});}
}
File.WriteAllText(Path.Combine(root,"preview.json"),JsonSerializer.Serialize(new{environment="production",sourceHash,reports=reports.Count,ready=ready.Count,skippedExisting,blocked,preview,existingFingerprint=before},json));
Console.WriteLine($"Production preflight: reports={reports.Count}; ready={ready.Count}; existing skipped={skippedExisting.Count}; blocked={blocked.Count}");
if(mode=="--preflight"){if(before!=await Fingerprint()||masters!=await Masters())throw new Exception("Preview changed data");return;}
if(mode=="--apply"){
 if(ready.Count==0)throw new Exception("No ready reports");CheckSource();
 await using var c=new SqlConnection(connection);await c.OpenAsync();using var cmd=c.CreateCommand();cmd.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";var dir=(string)(await cmd.ExecuteScalarAsync())!;
 var backup=Path.Combine(dir,"PMR_SPC_2026_ETCH_SEP_OCT_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".bak");cmd.CommandTimeout=180;cmd.CommandText="BACKUP DATABASE [PMR_SPC_2026] TO DISK=@p WITH COPY_ONLY,CHECKSUM,INIT";cmd.Parameters.AddWithValue("@p",backup);await cmd.ExecuteNonQueryAsync();cmd.CommandText="RESTORE VERIFYONLY FROM DISK=@p WITH CHECKSUM";await cmd.ExecuteNonQueryAsync();
 File.WriteAllText(Path.Combine(root,"backup.json"),JsonSerializer.Serialize(new{database="PMR_SPC_2026",backup,verified=true,before},json));
}
var output=new List<object>();var created=0;var unchanged=0;var checkedPoints=0;
foreach(var r in ready){
 EtchSyncResult? saved=null;
 if(mode=="--apply"){
  saved=await svc.SyncAsync(r,actor,false);if(saved.Unchanged)unchanged++;else created++;
  var retry=await svc.SyncAsync(r,actor,false);if(!retry.Unchanged||retry.BatchId!=saved.BatchId)throw new Exception("Idempotence failed");
 }
 var (machine,maps)=await svc.ResolveMappingsAsync(r.LineCode);
 var mapIds=maps.Select(x=>x.Id).ToArray();var key=Key(r);
 var rows=await db.VariableMeasurements.AsNoTracking().Where(x=>mapIds.Contains(x.PartProcessCharacteristicId)&&x.SourceReference==key).ToListAsync();
 if(rows.Count!=r.Points.Count+1)throw new Exception("Stored subgroup count mismatch: "+key);
 if(rows.Any(x=>x.MeasuredAt.Date!=r.ReportDate || x.MachineId!=machine.Id || x.ProcessId!=machine.ProcessId))throw new Exception("Stored line/date mismatch: "+key);
 var (ops,length)=EtchReportSyncService.Rule(r.LineCode);
 foreach(var m in maps){
  var code=m.Characteristic!.CharacteristicCode;
  var expected=code=="ETCH_RATE"?new Dictionary<int,decimal>{{1,r.Points.Average(p=>p.BeforeValue-p.AfterValue)*r.LineSpeed/length}}:
   r.Points.Where(p=>p.Side==(code=="ETCH_A_AVG"?"A":"B")).ToDictionary(p=>(p.RepeatNo-1)*ops+p.OpNo,p=>p.BeforeValue-p.AfterValue);
  var actual=rows.Where(x=>x.PartProcessCharacteristicId==m.Id).ToDictionary(x=>x.SampleNo,x=>x.MeasuredValue);
  if(actual.Count!=expected.Count||expected.Any(x=>!actual.ContainsKey(x.Key)||Math.Abs((decimal)actual[x.Key]-x.Value)>0.00000001m))throw new Exception("Stored value mismatch: "+key);
  checkedPoints+=actual.Count;
 }
 output.Add(new{key,r.ReportDate,r.LineCode,batchId=rows[0].UploadBatchId,unchanged=saved?.Unchanged??true,points=rows.Count,ids=rows.Select(x=>x.Id).ToArray(),ppcIds=mapIds,storedRows=rows.Select(x=>new{x.Id,x.PartProcessCharacteristicId,x.SampleNo,x.MeasuredValue})});
 if(output.Count%20==0)Console.WriteLine($"Verified {output.Count}/{ready.Count}");
 File.WriteAllText(Path.Combine(root,mode=="--apply"?"import-result.json":"verification.json"),JsonSerializer.Serialize(new{completed=false,created,unchanged,checkedPoints,skippedExisting,blocked,results=output},json));
 db.ChangeTracker.Clear();
}
CheckSource();if(before!=await Fingerprint()||masters!=await Masters())throw new Exception("Existing data or business masters changed");
File.WriteAllText(Path.Combine(root,mode=="--apply"?"import-result.json":"verification.json"),JsonSerializer.Serialize(new{completed=true,created,unchanged,checkedPoints,sourceUnchanged=true,existingDataUnchanged=true,mastersUnchanged=true,outboundNotifications=0,skippedExisting,blocked,results=output},json));
Console.WriteLine($"Complete: created reports={created}; unchanged={unchanged}; reconciled points incl rates={checkedPoints}; existing data unchanged");
