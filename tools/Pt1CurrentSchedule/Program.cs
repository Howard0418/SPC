using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Pt1CurrentSchedule;
using TestMeasurementCleanup;

if(args.Length!=4||args[3] is not("plan" or "apply" or "verify"))throw new ArgumentException("TEST_CONFIG PROD_CONFIG EVIDENCE_DIR plan|apply|verify");
if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows actor required");
int[] ids=[2440,2441,2443,2444];var expected=new Dictionary<int,(string Tank,string Name)>{[2440]=("脫脂","硫酸"),[2441]=("粗化","銅離子"),[2443]=("酸洗","鹽酸"),[2444]=("酸洗","銅離子")};
const string task="SPC-CHEM-F-CONSISTENCY-1103-TASK-001-PT1-GAP";
var start=new DateTime(2026,11,2,16,0,0,DateTimeKind.Utc);var json=new JsonSerializerOptions{WriteIndented=true};
var mode=args[3];Directory.CreateDirectory(args[2]);var stamp=TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow,"Taipei Standard Time").ToString("yyyyMMdd-HHmmss");
string Load(string path,string environment,string database)
{
 using var d=JsonDocument.Parse(File.ReadAllText(path));var b=new SqlConnectionStringBuilder(d.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!.Replace("Trust Server Certificate=","TrustServerCertificate="));
 if(d.RootElement.GetProperty("AppEnvironment").GetString()!=environment||b.InitialCatalog!=database||b.DataSource!="172.16.110.16")throw new InvalidOperationException("Configuration target mismatch");return b.ConnectionString;
}
var testConnection=Load(args[0],"test","PMR_SPC_TEST");var productionConnection=Load(args[1],"production","PMR_SPC_2026");
AppDbContext Open(string c)=>new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(c).Options);
await using var source=Open(testConnection);await using var target=Open(productionConnection);await source.Database.OpenConnectionAsync();await target.Database.OpenConnectionAsync();
foreach(var pair in new[]{(source,"PMR_SPC_TEST"),(target,"PMR_SPC_2026")}){await using var c=pair.Item1.Database.GetDbConnection().CreateCommand();c.CommandText="SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";await using var r=await c.ExecuteReaderAsync();await r.ReadAsync();if(r.GetString(0)!=pair.Item2||r.GetString(1)!="172.16.110.16")throw new InvalidOperationException("Actual database target mismatch");}
using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};var api=await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/version");using(var v=JsonDocument.Parse(api)){if(v.RootElement.GetProperty("environment").GetString()!="production")throw new InvalidOperationException("Actual API target mismatch");}await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/health");
bool SameJson(string? a,string? b)=>JsonNode.DeepEquals(JsonNode.Parse(a??"{}"),JsonNode.Parse(b??"{}"));
var sourceBefore=await DatabaseInventory.ReadAsync(source);var before=await DatabaseInventory.ReadAsync(target);
async Task<List<Item>> ReadPlan()
{
 var result=new List<Item>();
 foreach(var id in ids)
 {
  async Task<PartProcessCharacteristic> Mapping(AppDbContext db)
  {
   var p=await db.PartProcessCharacteristics.AsNoTracking().Include(x=>x.Machine).Include(x=>x.Tank).Include(x=>x.Characteristic).SingleAsync(x=>x.Id==id);
   if(!p.IsEnabled||p.ControlScope!="CHEM"||p.Machine?.MachineCode!="PT1"||p.Tank?.TankName!=expected[id].Tank||p.Characteristic?.CharacteristicName!=expected[id].Name)throw new InvalidOperationException("Approved business identity changed");return p;
  }
  var s=await Mapping(source);var t=await Mapping(target);if(s.Unit!=t.Unit||!SameJson(s.ChemicalAnalysisConfigJson,t.ChemicalAnalysisConfigJson))throw new InvalidOperationException("Source and production masters differ");
  var sp=await source.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x=>x.PartProcessCharacteristicId==id);
  var se=await source.ChemicalFormulaEffectivities.AsNoTracking().SingleAsync(x=>x.ScheduleRevisionId==sp.ActiveScheduleRevisionId&&x.PartProcessCharacteristicId==id&&x.EffectiveFromUtc==start&&x.EffectiveUntilUtcExclusive==null);
  if(!await source.ChemicalFormulaScheduleRevisions.AnyAsync(x=>x.Id==sp.ActiveScheduleRevisionId&&x.PublishedAtUtc!=null))throw new InvalidOperationException("Source schedule not published");
  var sf=await source.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x=>x.Id==se.FormulaVersionId&&x.PartProcessCharacteristicId==id);if(!SameJson(sf.ConfigJson,t.ChemicalAnalysisConfigJson))throw new InvalidOperationException("Source effective formula differs from approved master");
  var sourceF=await source.ChemicalFTableVersionHistories.AsNoTracking().SingleAsync(x=>x.Id==se.FTableHistoryId);var fSignature=SchedulePlan.FSignature(sourceF.CellsJson);
  if(fSignature!="B10=1;B3=0.99;B4=1;B5=1;B6=1;B7=1;B8=1;B9=1")throw new InvalidOperationException("Approved F values changed");
  var formula=(await target.ChemicalAnalysisFormulaVersions.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==id).OrderByDescending(x=>x.VersionNo).ToListAsync()).FirstOrDefault(x=>SameJson(x.ConfigJson,sf.ConfigJson))??throw new InvalidOperationException("No matching existing production formula history");
  var f=(await target.ChemicalFTableVersionHistories.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()).FirstOrDefault(x=>SchedulePlan.FSignature(x.CellsJson)==fSignature)??throw new InvalidOperationException("No matching existing production F history");
  var policy=await target.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x=>x.PartProcessCharacteristicId==id);var effects=await target.ChemicalFormulaEffectivities.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==id&&x.ScheduleRevisionId==policy.ActiveScheduleRevisionId).OrderBy(x=>x.EffectiveFromUtc).ToListAsync();
  var old=effects.Select(x=>new Slice(x.EffectiveFromUtc,x.EffectiveUntilUtcExclusive,x.FormulaVersionId,x.FTableHistoryId)).ToList();var merged=SchedulePlan.Merge(old,start,formula.Id,f.Id);
  if(old.Count!=(merged.Changed?19:20)||old.Count(x=>x.FromUtc<start)!=19)throw new InvalidOperationException("Reviewed historical period count changed");
  result.Add(new(id,policy.ActiveScheduleRevisionId!.Value,policy.IsEnabled,formula.Id,f.Id,s.ChemicalAnalysisConfigJson!,sp.ActiveScheduleRevisionId!.Value,se.FormulaVersionId,sourceF.Id,merged));
 }
 return result;
}
string Serialize(object value)=>JsonSerializer.Serialize(value);
var plan=await ReadPlan();var changed=plan.Count(x=>x.Schedule.Changed);if(changed is not(0 or 4))throw new InvalidOperationException("Partial schedule state; manual review required");
var sourceHash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Serialize(plan.Select(x=>new{x.Id,x.Config,x.SourceRevision,x.SourceFormula,x.SourceF})))));
var oldRevisions=await target.ChemicalFormulaScheduleRevisions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();var maxRevision=oldRevisions.Max(x=>x.Id);var oldEffects=await target.ChemicalFormulaEffectivities.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();var maxEffect=oldEffects.Max(x=>x.Id);var oldPolicies=await target.ChemicalFormulaPolicies.AsNoTracking().OrderBy(x=>x.PartProcessCharacteristicId).ToListAsync();
await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-before.json"),JsonSerializer.Serialize(new{task,mode,sourceHash,plan,before,oldRevisions,oldEffects,oldPolicies},json));Console.WriteLine($"{mode}: pending={changed}; oldPeriodsPerItem=19; formulas={string.Join(',',plan.Select(x=>x.Formula))}; F={string.Join(',',plan.Select(x=>x.F))}");
var protection=new EphemeralDataProtectionProvider();ChemicalFormulaScheduleService Service(AppDbContext db)=>new(db,protection,TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
var actor=WindowsIdentity.GetCurrent().Name;int previewChecks=0;
async Task VerifyPreviews()
{
 foreach(var item in plan)
 {
  var active=await target.ChemicalFormulaPolicies.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==item.Id).Select(x=>x.ActiveScheduleRevisionId).SingleAsync();
  var historical=await target.ChemicalFormulaEffectivities.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==item.Id&&x.ScheduleRevisionId==active&&x.EffectiveFromUtc<start).OrderBy(x=>x.EffectiveFromUtc).Select(x=>new Slice(x.EffectiveFromUtc,x.EffectiveUntilUtcExclusive,x.FormulaVersionId,x.FTableHistoryId)).ToListAsync();
  if(!historical.SequenceEqual(item.Schedule.Periods.Where(x=>x.FromUtc<start)))throw new InvalidOperationException("Active historical periods changed");
  try{await Service(target).PreviewAsync(item.Id,new("2026-11-02T23:59:59+08:00","1","1",task+"-boundary-check","Initial"),actor,default);throw new InvalidOperationException("Unapproved prior gap was filled");}
  catch(ArgumentException e)when(e.Message.StartsWith("量測日期沒有適用公式")){previewChecks++;}
 }
 foreach(var item in plan)foreach(var date in new[]{"2026-11-03T00:00:00+08:00","2026-11-05T12:00:00+08:00","2026-12-01T12:00:00+08:00"})foreach(var input in new[]{"0","1","6.5","10"})foreach(var phase in new[]{"Initial","Recheck"})
 {
  var request=new ChemicalPortalPreviewRequest(item.Id,date,input,"1","OPEN","GENERAL",phase);
  ChemicalPortalMeasurementService Portal(AppDbContext db)=>new(db,Service(db),null!,NullLogger<ChemicalPortalMeasurementService>.Instance);
  var a=await Portal(source).PreviewAsync(request,actor,default);var b=await Portal(target).PreviewAsync(request,actor,default);if(Serialize(a.Result)!=Serialize(b.Result))throw new InvalidOperationException("Source/production preview differs");previewChecks++;
 }
}
async Task Protect()
{
 var after=await DatabaseInventory.ReadAsync(target);foreach(var b in before)if(b.Table is not("ChemicalFormulaPolicies" or "ChemicalFormulaScheduleRevisions" or "ChemicalFormulaEffectivities")&&after.Single(x=>x.Table==b.Table)!=b)throw new InvalidOperationException("Protected production table changed: "+b.Table);
 if(Serialize(oldRevisions)!=Serialize(await target.ChemicalFormulaScheduleRevisions.AsNoTracking().Where(x=>x.Id<=maxRevision).OrderBy(x=>x.Id).ToListAsync())||Serialize(oldEffects)!=Serialize(await target.ChemicalFormulaEffectivities.AsNoTracking().Where(x=>x.Id<=maxEffect).OrderBy(x=>x.Id).ToListAsync()))throw new InvalidOperationException("Existing history changed");
 var policies=await target.ChemicalFormulaPolicies.AsNoTracking().OrderBy(x=>x.PartProcessCharacteristicId).ToListAsync();if(policies.Count!=oldPolicies.Count||Serialize(policies.Where(x=>!ids.Contains(x.PartProcessCharacteristicId)))!=Serialize(oldPolicies.Where(x=>!ids.Contains(x.PartProcessCharacteristicId)))||plan.Any(i=>policies.Single(x=>x.PartProcessCharacteristicId==i.Id).IsEnabled!=i.Enabled))throw new InvalidOperationException("Non-target policy or enable flag changed");
 if(await target.ChemicalFormulaScheduleRevisions.CountAsync()!=oldRevisions.Count+(mode=="apply"?changed:0)||await target.ChemicalFormulaEffectivities.CountAsync()!=oldEffects.Count+(mode=="apply"?changed*20:0))throw new InvalidOperationException("Unexpected added history count");
}
if(mode=="apply"&&changed>0)
{
 using(var approved=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[2],"reviewed-plan.json")))){if(approved.RootElement.GetProperty("sourceHash").GetString()!=sourceHash)throw new InvalidOperationException("Source plan changed");}
 await using(var c=target.Database.GetDbConnection().CreateCommand())
 {
  c.CommandTimeout=600;c.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";var dir=Convert.ToString(await c.ExecuteScalarAsync())!;var backup=dir.TrimEnd('\\')+"\\PMR_SPC_2026_PT1_SCHEDULE_"+stamp+".bak";
  c.CommandText="BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";var p=c.CreateParameter();p.ParameterName="@path";p.Value=backup;c.Parameters.Add(p);await c.ExecuteNonQueryAsync();await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-backup.json"),JsonSerializer.Serialize(new{database="PMR_SPC_2026",backup,verified=true,copyOnly=true,checksum=true},json));
 }
 await using var tx=await target.Database.BeginTransactionAsync(IsolationLevel.Serializable);if(Serialize(plan)!=Serialize(await ReadPlan()))throw new InvalidOperationException("Concurrent schedule change");
 foreach(var item in plan)
 {
  string Date(DateTime at)=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(at,DateTimeKind.Utc),TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
  var periods=item.Schedule.Periods.Select(x=>new ChemicalSchedulePeriod(x.FormulaId,x.FId,Date(x.FromUtc),x.UntilUtc.HasValue?Date(x.UntilUtc.Value.AddTicks(-1)):null)).ToArray();
  await Service(target).PublishAsync(item.Id,new(item.Revision,task+" / 使用者授權PT1四項11/03起依已確認測試公式與F；保留十月歷史",periods),actor,default);
 }
 target.ChangeTracker.Clear();await VerifyPreviews();await Protect();if(Serialize(sourceBefore)!=Serialize(await DatabaseInventory.ReadAsync(source)))throw new InvalidOperationException("Test source changed");await tx.CommitAsync();
}
else if(mode=="verify"||changed==0){if(changed!=0)throw new InvalidOperationException("Current schedule still missing");await VerifyPreviews();await Protect();}
else{await File.WriteAllTextAsync(Path.Combine(args[2],"reviewed-plan.json"),JsonSerializer.Serialize(new{sourceHash,plan},json));await Protect();}
var final=await ReadPlan();var remaining=final.Count(x=>x.Schedule.Changed);if(mode!="plan"&&remaining!=0)throw new InvalidOperationException("Post-write schedule differs");if(Serialize(sourceBefore)!=Serialize(await DatabaseInventory.ReadAsync(source)))throw new InvalidOperationException("Test source changed");
var health=await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/health");await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-"+mode+".json"),JsonSerializer.Serialize(new{task,mode,changed,remaining,previewChecks,plan,final,sourceHash,protectedDataUnchanged=true,sourceUnchanged=true,apiVersion=JsonSerializer.Deserialize<object>(api),health},json));Console.WriteLine($"{mode}: addedRevisions={(mode=="apply"?changed:0)}; remaining={remaining}; previewChecks={previewChecks}; protected=unchanged");
record Item(int Id,long Revision,bool Enabled,long Formula,long F,string Config,long SourceRevision,long SourceFormula,long SourceF,MergedSchedule Schedule);
