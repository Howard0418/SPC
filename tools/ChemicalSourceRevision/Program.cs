using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using ChemicalSourceRevision;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

if(args.Length!=4||args[3] is not("plan" or "apply" or "verify"))throw new ArgumentException("CONFIG MANIFEST OUTPUT plan|apply|verify");
if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows actor required");
const string task="SPC-CHEM-SOURCE-PRECEDENCE-20261105";
const string manifestHash="d0660f258a2bc3f6bf6d3eeb8859c86064197386a5820cab15298e0f73ac604f";
var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};
if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))).ToLowerInvariant()!=manifestHash)throw new InvalidOperationException("Reviewed manifest changed");
var source=JsonSerializer.Deserialize<List<Revision>>(File.ReadAllText(args[1]),json)!;
var ids=source.Select(x=>x.Id).ToArray();
if(!ids.Order().SequenceEqual(new long[]{78769,78771,78772,79045,79046,79047,79048,79125}))throw new InvalidOperationException("Reviewed eight IDs changed");
void CheckFiles(){foreach(var s in source.DistinctBy(x=>x.File)){using var f=new FileStream(s.File,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);if(Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant()!=s.FileHash)throw new InvalidOperationException("Live Excel source changed");}}
CheckFiles();
var cfg=new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[0])).Build();var cs=new SqlConnectionStringBuilder(cfg.GetConnectionString("SqlServer")!.Replace("Trust Server Certificate=","TrustServerCertificate="));
if(cfg["AppEnvironment"]!="production"||cs.DataSource!="172.16.110.16"||cs.InitialCatalog!="PMR_SPC_2026")throw new InvalidOperationException("Production guard");
using var http=new HttpClient();using var version=JsonDocument.Parse(await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/version"));if(version.RootElement.GetProperty("environment").GetString()!="production")throw new InvalidOperationException("API environment mismatch");await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/health");
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);await db.Database.OpenConnectionAsync();
async Task<string> Fingerprint(string table,string where=""){await using var c=db.Database.GetDbConnection().CreateCommand();c.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();c.CommandText=$"SELECT CONCAT(COUNT_BIG(*),':',CHECKSUM_AGG(BINARY_CHECKSUM(*))) FROM dbo.[{table}] {where}";return Convert.ToString(await c.ExecuteScalarAsync())!;}
string excluded="WHERE Id NOT IN ("+string.Join(',',ids)+")";
async Task<Dictionary<string,string>> Protected(){var a=new Dictionary<string,string>{{"NonTargetMeasurements",await Fingerprint("VariableMeasurements",excluded)}};foreach(var t in new[]{"PartProcessCharacteristics","ChemicalAnalysisFormulaVersions","ChemicalFTableVersions","ChemicalFTableCells","ChemicalFTableVersionHistories","ChemicalFormulaPolicies","ChemicalFormulaScheduleRevisions","ChemicalFormulaEffectivities","ChemicalCalculationReceipts","ChemicalCalculationTraces"})a[t]=await Fingerprint(t);return a;}
var mappings=await db.PartProcessCharacteristics.AsNoTracking().Include(x=>x.Process).Include(x=>x.Part).Include(x=>x.Machine).Include(x=>x.Tank).Include(x=>x.Slot).Include(x=>x.Characteristic).Where(x=>source.Select(y=>y.PpcId).Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
var blocked=new List<string>();var rows=await db.VariableMeasurements.Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
foreach(var s in source){if(!rows.TryGetValue(s.Id,out var r))throw new InvalidOperationException("Missing reviewed measurement");var m=mappings[s.PpcId];var key=string.Join('|',m.ControlScope,m.Process?.ProcessCode,m.Part?.PartNo,m.Machine?.MachineCode,m.Tank?.TankCode,m.Slot?.SlotCode,m.Characteristic?.CharacteristicCode,m.Unit);if(key!=s.BusinessKey||!m.IsEnabled||r.PortalDailyDate?.ToString("yyyy-MM-dd")!=s.Day||r.MeasuredAt!=s.MeasuredAt)throw new InvalidOperationException("Business/day/time drift");
 try{RevisionGuard.RequireLegacy(r.PartProcessCharacteristicId,s.PpcId,r.ChemicalReceiptId.HasValue,await db.ChemicalCalculationTraces.AnyAsync(x=>x.VariableMeasurementId==r.Id),await db.ChemicalFormulaPolicies.AnyAsync(x=>x.PartProcessCharacteristicId==s.PpcId&&x.IsEnabled),!string.IsNullOrEmpty(r.ChemicalSourceKeyHash));}catch(InvalidOperationException e){blocked.Add($"{s.Id}: {e.Message}");}
 RevisionGuard.RequireValue(s.After);if(s.Expression!=""){var formulaJson=JsonSerializer.Serialize(new{enabled=true,concentrationFormula=s.Expression,decimalPlaces=8});var f=JsonSerializer.Serialize(s.FCells.Select(c=>new ChemicalFTableHistoryCell(c.Key,"source snapshot",c.Value)));var result=ChemicalFormulaCalculation.Calculate(formulaJson,s.Primary,null,f,null,null,null);if(!RevisionGuard.Same(double.Parse(result.Stored,CultureInfo.InvariantCulture),s.After))throw new InvalidOperationException("SPC engine/Excel mismatch");}
 if(r.SourceReference==task&&RevisionGuard.Same(r.MeasuredValue,s.After)&&r.RecheckValue==s.AfterRecheck)continue;
 if(!RevisionGuard.Same(r.MeasuredValue,s.Before)||r.RecheckValue!=s.BeforeRecheck)throw new InvalidOperationException("Current values drifted");
}
var pending=source.Where(s=>rows[s.Id].SourceReference!=task||!RevisionGuard.Same(rows[s.Id].MeasuredValue,s.After)||rows[s.Id].RecheckValue!=s.AfterRecheck).ToList();
Directory.CreateDirectory(args[2]);var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");var before=await Protected();var count=await db.VariableMeasurements.LongCountAsync();var snapshot=rows.Values.ToDictionary(x=>x.Id,x=>JsonSerializer.Serialize(x,json));
await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-before.json"),JsonSerializer.Serialize(new{mode=args[3],pending=pending.Count,blocked,before,snapshot,source},json));Console.WriteLine($"{args[3]}: pending={pending.Count}; blocked={blocked.Count}");
if(args[3]=="apply"){
 if(blocked.Count>0||pending.Count!=8)throw new InvalidOperationException("Reviewed apply scope changed or has pinned evidence");
 await using(var c=db.Database.GetDbConnection().CreateCommand()){c.CommandTimeout=600;c.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";var dir=Convert.ToString(await c.ExecuteScalarAsync())!;var backup=dir.TrimEnd('\\')+"\\SPC_source_revision_"+stamp+".bak";c.CommandText="BACKUP DATABASE [PMR_SPC_2026] TO DISK=@p WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@p WITH CHECKSUM";var p=c.CreateParameter();p.ParameterName="@p";p.Value=backup;c.Parameters.Add(p);await c.ExecuteNonQueryAsync();await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-backup.json"),JsonSerializer.Serialize(new{backup,verified=true},json));}
 CheckFiles();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var actor=WindowsIdentity.GetCurrent().Name;var now=DateTime.UtcNow;var batchId=Guid.NewGuid();
 foreach(var r in rows.Values){var current=await db.Entry(r).GetDatabaseValuesAsync();if(current is null||!((byte[])current["RowVersion"]!).SequenceEqual(r.RowVersion!))throw new InvalidOperationException("Concurrent measurement update");}
 db.UploadBatches.Add(new(){UploadBatchId=batchId,UploadType="Variable",SourceType="TransFilesChemicalRevision",ImportStatus="Confirmed",OriginalFileName=task,FileHash=manifestHash,TotalRows=8,ValidRows=8,ConfirmedAt=now,CreatedAt=now,CreatedBy=actor});
 foreach(var s in pending){var r=rows[s.Id];db.UploadDetails.Add(new(){UploadBatchId=batchId,RowNo=pending.IndexOf(s)+1,IsValid=true,CreatedAt=now,CreatedBy=actor,PayloadJson=JsonSerializer.Serialize(new{task,authority=s.Authority,source=s,before=JsonSerializer.Deserialize<object>(snapshot[s.Id]),after=new{value=s.After,recheck=s.AfterRecheck},note="Legacy source revision with original formula/F snapshot; no invented sampling time or receipt"},json)});r.MeasuredValue=s.After;r.RecheckValue=s.AfterRecheck;r.AdjustAction=null;r.AdjustAmount=null;r.UploadBatchId=batchId;r.SourceType=SourceType.Excel;r.SourceReference=task;r.UpdatedAt=now;r.UpdatedBy=actor;}
 var oldResults=await db.SpcCalculationResults.Where(x=>x.VariableMeasurementId.HasValue&&ids.Contains(x.VariableMeasurementId.Value)).ToListAsync();foreach(var r in oldResults){r.IsDeleted=true;r.UpdatedAt=now;r.UpdatedBy=actor;}
 var oldAlerts=await db.AlertEvents.Where(x=>x.VariableMeasurementId.HasValue&&ids.Contains(x.VariableMeasurementId.Value)).ToListAsync();foreach(var a in oldAlerts){a.IsDeleted=true;a.Status="Closed";a.ClosedAt=now;a.RootCause="使用者核准後檔取代前檔";a.UpdatedAt=now;a.UpdatedBy=actor;}
 await db.SaveChangesAsync();var mail=new SuppressedMail();var spc=new SpcService(db,mail,cfg);int calculated=0;foreach(var s in pending)if(await spc.CalculateVariableAsync(rows[s.Id],default,true) is not null)calculated++;if(mail.Attempts!=0)throw new InvalidOperationException("Notification attempted");
 var inside=await Protected();if(before.Any(x=>inside[x.Key]!=x.Value)||count!=await db.VariableMeasurements.LongCountAsync())throw new InvalidOperationException("Protected state changed");CheckFiles();await tx.CommitAsync();Console.WriteLine($"updated=8; statistics={calculated}; missingChartType={8-calculated}; notifications=0");
}
db.ChangeTracker.Clear();var final=await db.VariableMeasurements.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToListAsync();var remaining=source.Count(s=>!final.Any(r=>r.Id==s.Id&&r.SourceReference==task&&RevisionGuard.Same(r.MeasuredValue,s.After)&&r.RecheckValue==s.AfterRecheck&&r.MeasuredAt==s.MeasuredAt));var after=await Protected();if(before.Any(x=>after[x.Key]!=x.Value)||count!=await db.VariableMeasurements.LongCountAsync())throw new InvalidOperationException("Protected state changed");if(args[3]!="plan"&&remaining!=0)throw new InvalidOperationException("Remaining source differences");
await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-"+args[3]+".json"),JsonSerializer.Serialize(new{mode=args[3],remaining,count,before,after,blocked,final},json));Console.WriteLine($"remaining={remaining}; total={count}; protected=unchanged");
record Revision(long Id,int PpcId,string Day,string BusinessKey,double Before,double? BeforeRecheck,double After,double? AfterRecheck,DateTime MeasuredAt,string File,string FileName,string FileHash,string Sheet,string Cell,int Row,string SourceFormula,string Expression,string Primary,Dictionary<string,decimal> FCells,string Authority);
sealed class SuppressedMail:IEmailNotificationService{public int Attempts;Task<bool> Block(){Attempts++;return Task.FromResult(true);}public Task<bool> SendAlertEmailAsync(AlertEvent a,string b,string c,SmtpSettingsOverride? d=null,string? e=null)=>Block();public Task<bool> SendTestEmailAsync(string a,SmtpSettingsOverride? b=null,string? c=null)=>Block();public Task<bool> SendHtmlEmailAsync(string a,string b,string c,string d,SmtpSettingsOverride? e=null,string? f=null,string? g=null)=>Block();public Task<bool> SendReportEmailAsync(string a,string b,string c,string d,byte[] e,string f,string? g=null)=>Block();}
