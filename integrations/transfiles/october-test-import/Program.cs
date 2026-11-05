using System.Data;
using System.Globalization;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

// Operator-run integration runner, test-only. Never generates HTTP credentials or changes roles.
var mode=args.ElementAtOrDefault(0)??"--pure";
var root=args.ElementAtOrDefault(1)??throw new ArgumentException("Supply an evidence directory outside Git.");
var manifest=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")))!;
var rows=manifest["rows"]!.AsArray().Where(x=>(string?)x!["status"]=="validated").Select(x=>x!).ToList();
var month=(string?)manifest["summary"]!["month"];
if(rows.Count==0 || month is not ("2026-09" or "2026-10"))throw new Exception("Reviewed month candidate guard");
var reimportVersion=(int?)manifest["summary"]!["reimportVersion"];
var approvedWorkbookHash=(string?)manifest["summary"]!["approvedWorkbookSha256"];
var marker=args.ElementAtOrDefault(2)??(reimportVersion==2?(month=="2026-09"?"TEST-SEPCHEM-20261105-R2":"TEST-OCTCHEM-20261105-R2"):"TEST-OCTCHEM-20261102");
var trendFixture=marker=="TEST-SEPCHEM-20261102-TREND";
var reimportMarker=month=="2026-09"?"TEST-SEPCHEM-20261105-R2":"TEST-OCTCHEM-20261105-R2";
if(reimportVersion==2 && (approvedWorkbookHash?.Length!=64 || marker!=reimportMarker))throw new Exception("Approved reimport marker/hash mismatch");
if(reimportVersion!=2 && (month=="2026-10" ? marker is not ("TEST-OCTCHEM-20261102" or "TEST-OCTCHEM-20261102-SECONDARY") : marker is not ("TEST-SEPCHEM-20261102" or "TEST-SEPCHEM-20261102-TREND")))throw new Exception("Month/isolated marker mismatch");
if(trendFixture != ((string?)manifest["summary"]!["purpose"]=="trend-ui"))throw new Exception("Trend purpose/marker mismatch");
var monthName=month=="2026-09"?"九月":"十月";
var jsonOptions=new JsonSerializerOptions{WriteIndented=true};
string Text(JsonNode r,string key)=>(string?)r[key]??"";
decimal Dec(JsonNode r,string key)=>decimal.Parse(Text(r,key),CultureInfo.InvariantCulture);
string Config(JsonNode r)=>JsonSerializer.Serialize(new{enabled=true,version="2",primaryInputLabel="【報表日測試】原始滴定值",concentrationFormula=Text(r,"expression"),decimalPlaces=8});
string Cells(JsonNode r)=>JsonSerializer.Serialize(r["fCells"]!.AsObject().Select(x=>new ChemicalFTableHistoryCell("F!"+x.Key,"Excel "+x.Key,decimal.Parse((string)x.Value!,CultureInfo.InvariantCulture))));
string GroupKey(JsonNode r)=>JsonSerializer.Serialize(new{line=Text(r,"line"),slot=Text(r,"slot"),item=Text(r,"analysisItem"),formula=Text(r,"expression"),f=Cells(r),lower=Text(r,"lower"),upper=Text(r,"upper")});
if(trendFixture && (rows.Count>31 || rows.Select(GroupKey).Distinct().Count()!=1 || rows.Any(r=>Text(r,"line")!="DV" || Text(r,"slot")!="顯影") || !rows.Any(r=>r["recheckPrimary"] is not null)))throw new Exception("Reviewed trend group guard");
var serverChecks=0;
foreach(var r in rows){
 if(!Text(r,"fileDay").StartsWith(month+"-") || Text(r,"fileHash").Length!=64)throw new Exception("Source guard");
 foreach(var phase in new[]{("primary","excel"),("recheckPrimary","recheckExcel")}){
  if(r[phase.Item1] is null)continue;
  var calc=ChemicalFormulaCalculation.Calculate(Config(r),Text(r,phase.Item1),(string?)r[phase.Item1=="primary"?"secondary":"recheckSecondary"],Cells(r),Dec(r,"lower"),Dec(r,"upper"),(Dec(r,"lower")+Dec(r,"upper"))/2);
  if(Math.Abs(decimal.Parse(calc.Raw,CultureInfo.InvariantCulture)-Dec(r,phase.Item2))>0.00000001m)throw new Exception("Server/Excel mismatch: "+Text(r,"sheet")+"/"+r["sourceRow"]);
  serverChecks++;
 }
}
Console.WriteLine($"SPC engine/Excel checks passed: {serverChecks}; candidates={rows.Count}");
File.WriteAllText(Path.Combine(root,"server-pure.json"),JsonSerializer.Serialize(new{serverChecks,candidates=rows.Count,month,databaseWritten=false},jsonOptions));
if(mode=="--pure")return;
if(mode is not ("--preflight" or "--apply" or "--verify" or "--statistics" or "--align-units" or "--align-trend-statistics"))throw new ArgumentException("Unknown mode");
if(mode=="--align-trend-statistics" && !trendFixture)throw new Exception("Trend repair scope guard");
var cfg=new ConfigurationBuilder().AddJsonFile("D:/SPC/release/test/backend/appsettings.json").Build();
var connection=cfg.GetConnectionString("SqlServer")??throw new Exception("Missing test config");
var cs=new SqlConnectionStringBuilder(connection);
if(cfg["AppEnvironment"]!="test" || cs.InitialCatalog!="PMR_SPC_TEST" || cs.DataSource!="172.16.110.16")throw new Exception("TEST DB environment guard");
using var http=new HttpClient();
var version=JsonNode.Parse(await http.GetStringAsync("https://spctest.pmr.com.tw/api/version"))!;
if((string?)version["environment"]!="test")throw new Exception("TEST API guard");
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
var writerConnection=Environment.GetEnvironmentVariable("SPC_CHEMICAL_DB_CONNECTION",EnvironmentVariableTarget.User)
 ??Environment.GetEnvironmentVariable("SPC_CHEMICAL_DB_CONNECTION")??throw new Exception("Missing low-privilege integrated writer config");
var writerCs=new SqlConnectionStringBuilder(writerConnection);
if(!writerCs.IntegratedSecurity || writerCs.InitialCatalog!="PMR_SPC_TEST" || !(new[]{"172.16.110.16","PMR-MESSQL"}).Contains(writerCs.DataSource,StringComparer.OrdinalIgnoreCase))throw new Exception("TEST integrated writer guard");
await using var writer=new SqlConnection(writerConnection);await writer.OpenAsync();
using(var actual=writer.CreateCommand()){
 actual.CommandText="SELECT CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
 if((string?)await actual.ExecuteScalarAsync()!="172.16.110.16")throw new Exception("Integrated writer actual SQL host mismatch");
}
using var actorCommand=writer.CreateCommand();actorCommand.CommandText="SELECT USER_NAME()";
var principal=(string)(await actorCommand.ExecuteScalarAsync())!;
var actor=principal.Split('\\').Last().Split('@').First();
if(!OperatingSystem.IsWindows())throw new Exception("Windows integrated operator required");
if(!WindowsIdentity.GetCurrent().Name.EndsWith("\\"+actor,StringComparison.OrdinalIgnoreCase))throw new Exception("Real Windows actor guard");
using var permission=writer.CreateCommand();permission.CommandText="SELECT HAS_PERMS_BY_NAME('dbo.CommitChemicalMeasurementWithTrace','OBJECT','EXECUTE')";
if(Convert.ToInt32(await permission.ExecuteScalarAsync())!=1)throw new Exception("Writer execute permission unavailable");
var groups=rows.GroupBy(GroupKey).OrderBy(x=>x.Key).ToList();
Console.WriteLine($"TEST target verified; real Windows integrated writer; groups={groups.Count}");
if(mode=="--preflight")return;
var protection=new EphemeralDataProtectionProvider();
var svc=new ChemicalFormulaScheduleService(db,protection,TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
var existingMeasurementMax=await db.VariableMeasurements.MaxAsync(x=>(long?)x.Id)??0;
async Task<object> ExistingMeasurementFingerprint(){
 await using var check=new SqlConnection(connection);await check.OpenAsync();using var command=check.CreateCommand();
 command.CommandText="SELECT COUNT_BIG(*) n,CHECKSUM_AGG(BINARY_CHECKSUM(Id,PartProcessCharacteristicId,MeasuredValue,RecheckValue,MeasuredAt,ChemicalReceiptId,ChemicalSourceKeyHash,IsDeleted)) c FROM VariableMeasurements WHERE Id<=@max";
 command.Parameters.AddWithValue("@max",existingMeasurementMax);await using var reader=await command.ExecuteReaderAsync();await reader.ReadAsync();
 return new{max=existingMeasurementMax,count=reader.GetInt64(0),checksum=reader.IsDBNull(1)?(int?)null:reader.GetInt32(1)};
}
async Task<string> Snapshot()=>JsonSerializer.Serialize(new{
 Ppcs=await db.PartProcessCharacteristics.AsNoTracking().Where(x=>!db.Processes.Any(p=>p.Id==x.ProcessId&&p.ProcessCode==marker)).OrderBy(x=>x.Id).ToListAsync(),
 Policies=await db.ChemicalFormulaPolicies.AsNoTracking().Where(x=>!db.PartProcessCharacteristics.Any(p=>p.Id==x.PartProcessCharacteristicId&&db.Processes.Any(y=>y.Id==p.ProcessId&&y.ProcessCode==marker))).OrderBy(x=>x.PartProcessCharacteristicId).ToListAsync(),
 ActiveF=await db.ChemicalFTableVersions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Id).ToListAsync(),ExistingMeasurements=await ExistingMeasurementFingerprint()});
var before=await Snapshot();
var resultPath=Path.Combine(root,"import-result.json");
if(mode=="--apply"){
 foreach(var source in manifest["files"]!.AsArray()){
  var current=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes((string)source!["file"]!)));
  if(!current.Equals((string?)source["sha256"],StringComparison.OrdinalIgnoreCase))throw new Exception("Source changed since validation");
 }
 // A server-side COPY_ONLY backup is mandatory before the first isolated write.
 await using(var backupConnection=new SqlConnection(connection)){
  await backupConnection.OpenAsync();using var cmd=backupConnection.CreateCommand();
  cmd.CommandText="SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
  var directory=(string?)await cmd.ExecuteScalarAsync()??throw new Exception("No server backup directory");
  var path=directory.TrimEnd('\\','/')+"\\PMR_SPC_TEST_"+(month=="2026-09"?"SEPCHEM_":"OCTCHEM_")+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".bak";
  cmd.CommandTimeout=180;cmd.CommandText="BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@p WITH COPY_ONLY,CHECKSUM,INIT";cmd.Parameters.AddWithValue("@p",path);await cmd.ExecuteNonQueryAsync();
  cmd.CommandText="RESTORE VERIFYONLY FROM DISK=@p WITH CHECKSUM";await cmd.ExecuteNonQueryAsync();
  File.WriteAllText(Path.Combine(root,"backup.json"),JsonSerializer.Serialize(new{server="172.16.110.16",database="PMR_SPC_TEST",path,verified=true},jsonOptions));
 }
}
var process=await db.Processes.SingleOrDefaultAsync(x=>x.ProcessCode==marker);
if(process is null){
 if(mode!="--apply")throw new Exception("Fixture absent");
 await using var tx=await db.Database.BeginTransactionAsync();
 process=new MesSpc.Api.Domain.Entities.Process{ProcessCode=marker,ProcessName="【"+monthName+(trendFixture?"趨勢驗收測試":"隔離測試")+"】Excel藥液對帳",ControlScope="CHEM",Description="Report-day fixture only; not actual sampling timestamps; record-only prevents operational alerts"};db.Processes.Add(process);await db.SaveChangesAsync();
 var chart=await db.ControlChartTypes.AsNoTracking().FirstAsync(x=>x.IsEnabled&&x.DataCategory=="Variable");
 int index=0;
 foreach(var group in groups){
  index++;var r=group.First();var code=marker+"-"+index.ToString("D3");
  var label=Text(r,"line")+"/"+Text(r,"slot")+"/"+Text(r,"analysisItem").Replace("\n"," ");
  var ch=new QualityCharacteristic{CharacteristicCode=code,CharacteristicName="【"+monthName+"測試】"+label,ControlScope="CHEM",DecimalPlaces=8,InputMode="RECORD_ONLY",IsSpcEnabled=false};db.QualityCharacteristics.Add(ch);
  var fv=new ChemicalFTableVersion{VersionCode=code,DisplayName="【"+monthName+"測試】"+label,IsActive=false,SourceName=Path.GetFileName(Text(r,"file")),Note="Isolated Excel snapshot; not global active F"};db.ChemicalFTableVersions.Add(fv);await db.SaveChangesAsync();
  foreach(var cell in r["fCells"]!.AsObject())db.Add(new ChemicalFTableCell{VersionId=fv.Id,SheetName="F",CellAddress=cell.Key,NormalizedCellAddress=ChemicalFTableService.NormalizeCellAddress("F!"+cell.Key),StandardSolution="Excel "+cell.Key,NumericValue=decimal.Parse((string)cell.Value!,CultureInfo.InvariantCulture),RawValue=(string)cell.Value!});
  var fh=new ChemicalFTableVersionHistory{VersionNo=(await db.ChemicalFTableVersionHistories.MaxAsync(x=>(int?)x.VersionNo)??0)+1,FTableVersionId=fv.Id,VersionCode=code,DisplayName=fv.DisplayName,CellsJson=Cells(r),ChangedBy=actor,Reason="Approved isolated "+month+" report-day fixture",ChangeType="TestFixture"};db.Add(fh);await db.SaveChangesAsync();
  var line=Text(r,"line");var machine=line is "N1" or "N2"?await db.Machines.AsNoTracking().Where(x=>x.MachineCode==line).Select(x=>(int?)x.Id).SingleAsync():null;
  var ppc=new PartProcessCharacteristic{ControlScope="CHEM",ProcessId=process.Id,MachineId=machine,CharacteristicId=ch.Id,Unit=Text(r,"unit"),LSL=(double)Dec(r,"lower"),USL=(double)Dec(r,"upper"),TargetValue=(double)((Dec(r,"lower")+Dec(r,"upper"))/2),DisplayMode=trendFixture?"TREND_CHART":"CONTROL_CHART",ChartTypeId=chart.Id,ChemicalAnalysisConfigJson=Config(r),IsEnabled=true,IsRequired=false};db.Add(ppc);await db.SaveChangesAsync();
  await new ChemicalAnalysisFormulaVersionService(db).RecordChangeAsync(ppc.Id,null,ppc.ChemicalAnalysisConfigJson,actor,month+" isolated source formula; no business policy change",default);
  var formula=await db.ChemicalAnalysisFormulaVersions.SingleAsync(x=>x.PartProcessCharacteristicId==ppc.Id);
  foreach(var cell in r["fCells"]!.AsObject())db.Add(new ChemicalFTableReference{VersionId=fv.Id,PartProcessCharacteristicId=ppc.Id,CellAddress=cell.Key,NormalizedCellAddress=ChemicalFTableService.NormalizeCellAddress("F!"+cell.Key),SourceFormula=Text(r,"formula"),SourceSheet=Text(r,"sheet"),SourceRow=(int)r["sourceRow"]!,ReferenceContext=month+" isolated fixture"});await db.SaveChangesAsync();
  var periods=group.Select(x=>Text(x,"fileDay")).Distinct().Order().Select(day=>new ChemicalSchedulePeriod(formula.Id,fh.Id,day,day)).ToArray();
  await svc.PublishAsync(ppc.Id,new ChemicalPublishSchedule(null,"Report-day fixture only, not business effectiveness",periods),actor,default);
 }
 if(before!=await Snapshot())throw new Exception("Business definition snapshot changed");
 await tx.CommitAsync();Console.WriteLine($"Isolated definitions created: {groups.Count}");
}
var ppcs=await db.PartProcessCharacteristics.AsNoTracking().Where(x=>x.ProcessId==process.Id).OrderBy(x=>x.Id).ToListAsync();
if(mode=="--align-trend-statistics"){
 if(ppcs.Count!=1 || ppcs[0].DisplayMode!="TREND_CHART" || await db.QualityCharacteristics.AnyAsync(x=>x.Id==ppcs[0].CharacteristicId&&x.InputMode!="RECORD_ONLY"))throw new Exception("Trend fixture repair guard");
 if(ppcs[0].ChartTypeId.HasValue)throw new Exception("Trend fixture already has statistics configuration");
 var chartId=await db.ControlChartTypes.AsNoTracking().Where(x=>x.IsEnabled&&x.DataCategory=="Variable").Select(x=>x.Id).FirstAsync();
 var owned=await db.PartProcessCharacteristics.SingleAsync(x=>x.Id==ppcs[0].Id&&x.ProcessId==process.Id);owned.ChartTypeId=chartId;await db.SaveChangesAsync();
 if(before!=await Snapshot())throw new Exception("Business definition changed");Console.WriteLine("Isolated trend statistics configuration aligned; measurements unchanged");return;
}
if(mode=="--align-units"){
 var codes=await db.QualityCharacteristics.AsNoTracking().Where(x=>x.CharacteristicCode.StartsWith(marker+"-")).ToDictionaryAsync(x=>x.CharacteristicCode,x=>x.Id);
 var changes=0;var index=0;
 foreach(var group in groups){
  index++;var existing=ppcs.Single(x=>x.CharacteristicId==codes[marker+"-"+index.ToString("D3")]);var expected=Text(group.First(),"unit");
  if(string.IsNullOrWhiteSpace(expected))throw new Exception("Source unit is missing");
  if(existing.Unit==expected)continue;
  var owned=await db.PartProcessCharacteristics.SingleAsync(x=>x.Id==existing.Id&&x.ProcessId==process.Id);owned.Unit=expected;changes++;
 }
 await db.SaveChangesAsync();if(before!=await Snapshot())throw new Exception("Business definition changed");
 Console.WriteLine($"Isolated source units aligned: {changes}");return;
}
if(mode=="--statistics"){
 var onlyIds=ppcs.Select(x=>x.Id).ToArray();
 if(await db.QualityCharacteristics.AnyAsync(x=>ppcs.Select(p=>p.CharacteristicId).Contains(x.Id)&&x.InputMode!="RECORD_ONLY"))throw new Exception("Record-only notification isolation guard");
 var noMail=new NoOutboundEmail();
 var processor=new ChemicalStatisticsProcessor(db,new SpcService(db,noMail,cfg),NullLogger<ChemicalStatisticsProcessor>.Instance);
 var work=await db.ChemicalStatisticsJobs.AsNoTracking().Where(x=>x.Status!="Completed"&&db.VariableMeasurements.Any(v=>v.Id==x.VariableMeasurementId&&onlyIds.Contains(v.PartProcessCharacteristicId))).Select(x=>x.Id).ToListAsync();
 var done=0;foreach(var id in work){await processor.ProcessOneAsync(id,default);if(++done%100==0)Console.WriteLine($"Isolated statistics checked {done}/{work.Count}");}
 if(noMail.Attempts!=0)throw new Exception("Unexpected notification attempted (blocked by test sink)");
 var state=await db.ChemicalStatisticsJobs.AsNoTracking().Where(x=>db.VariableMeasurements.Any(v=>v.Id==x.VariableMeasurementId&&onlyIds.Contains(v.PartProcessCharacteristicId))).GroupBy(x=>x.Status).Select(x=>new{status=x.Key,count=x.Count()}).ToListAsync();
 File.WriteAllText(Path.Combine(root,"statistics-result.json"),JsonSerializer.Serialize(new{marker,jobs=state,notificationAttempts=noMail.Attempts,businessDefinitionsUnchanged=before==await Snapshot()},jsonOptions));
 Console.WriteLine(JsonSerializer.Serialize(state));return;
}
var byCode=await db.QualityCharacteristics.AsNoTracking().Where(x=>x.CharacteristicCode.StartsWith(marker+"-")).ToDictionaryAsync(x=>x.CharacteristicCode,x=>x.Id);
var outputs=new List<object>();var groupIndex=0;var checkedCount=0;var created=0;var repeated=0;
foreach(var group in groups){
 groupIndex++;var ppc=ppcs.Single(x=>x.CharacteristicId==byCode[marker+"-"+groupIndex.ToString("D3")]);
 if(ppc.ChemicalAnalysisConfigJson!=Config(group.First()))throw new Exception("Fixture rerun definition conflict");
 foreach(var r in group){
  var sourceKey=JsonSerializer.Serialize(new{kind="TransFilesChemical-v1",fileHash=Text(r,"fileHash"),sheet=Text(r,"sheet"),row=(int)r["sourceRow"]!,samplingPhase=Text(r,"samplingPhase"),samplingStage=Text(r,"samplingStage"),fixture=marker,timeBasis="report-day test only"});
  var input=new ChemicalPreviewInput(Text(r,"fileDay")+(month=="2026-09"?"T12:00:00+08:00":"T00:00:00+08:00"),Text(r,"primary"),(string?)r["secondary"],sourceKey);
  var preview=await svc.PreviewAsync(ppc.Id,input,actor,default);
  if(ppc.Unit!=Text(r,"unit"))throw new Exception("Isolated source unit mismatch");
  if(Math.Abs(decimal.Parse(preview.Result.Raw,CultureInfo.InvariantCulture)-Dec(r,"excel"))>0.00000001m)throw new Exception("DB preview mismatch");
  ChemicalPreviewResult? recheck=null;ChemicalPreviewInput? reInput=null;
  if(r["recheckPrimary"] is not null){reInput=input with{Primary=Text(r,"recheckPrimary"),Secondary=(string?)r["recheckSecondary"]};recheck=await svc.PreviewAsync(ppc.Id,reInput,actor,default);if(Math.Abs(decimal.Parse(recheck.Result.Raw,CultureInfo.InvariantCulture)-Dec(r,"recheckExcel"))>0.00000001m)throw new Exception("Recheck preview mismatch");}
  if(mode=="--verify"){
   // Trace projection is the same service used by the website, with no writes.
   var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sourceKey)));
   var vm=await db.VariableMeasurements.AsNoTracking().SingleAsync(x=>x.PartProcessCharacteristicId==ppc.Id&&x.ChemicalSourceKeyHash==hash);
   var trace=await new ChemicalTraceQueryService(db).GetAsync(ppc.Id,null,vm.Id,0,0,default)??throw new Exception("Missing website trace");
   var initial=trace.Rows.Single().Calculations.Single(x=>x.Phase=="Initial"&&x.IsCurrent);
   if(initial.Primary!=Text(r,"primary") || initial.Secondary!=(string?)r["secondary"])throw new Exception("Website original inputs mismatch");
   if(initial.RawResult!=preview.Result.Raw || !JsonNode.DeepEquals(JsonNode.Parse(initial.FormulaConfigJson!),JsonNode.Parse(ppc.ChemicalAnalysisConfigJson!)) || initial.FCells?.Count!=r["fCells"]!.AsObject().Count)throw new Exception("Website evidence mismatch: "+ppc.Id);
   foreach(var cell in r["fCells"]!.AsObject()){
    var saved=initial.FCells!.Single(x=>x.Address.Replace("F!","").Replace("$","")==cell.Key);
    if(decimal.Parse(saved.NumericValue!,CultureInfo.InvariantCulture)!=decimal.Parse((string)cell.Value!,CultureInfo.InvariantCulture))throw new Exception("Website F numeric mismatch");
   }
   if(recheck is not null && trace.Rows.Single().Calculations.Single(x=>x.Phase=="Recheck"&&x.IsCurrent).RawResult!=recheck.Result.Raw)throw new Exception("Website recheck mismatch");
   checkedCount++;continue;
  }
  var receipt=await svc.PrepareAsync(ppc.Id,new ChemicalPrepareRequest(input,preview.Token),actor,default);
  var reReceipt=recheck is null?null:await svc.PrepareAsync(ppc.Id,new ChemicalPrepareRequest(reInput!,recheck.Token),actor,default);
  async Task<(long Id,bool Repeated)> Commit(){
   using var cmd=writer.CreateCommand();cmd.CommandType=CommandType.StoredProcedure;cmd.CommandText="dbo.CommitChemicalMeasurementWithTrace";cmd.CommandTimeout=60;
   cmd.Parameters.AddWithValue("@InitialReceiptId",receipt.Id);cmd.Parameters.AddWithValue("@RecheckReceiptId",(object?)reReceipt?.Id??DBNull.Value);cmd.Parameters.AddWithValue("@Actor",actor);
   await using var reader=await cmd.ExecuteReaderAsync();if(!await reader.ReadAsync())throw new Exception("No commit response");return(Convert.ToInt64(reader["measurementId"]),Convert.ToBoolean(reader["alreadyProcessed"]));
  }
  var committed=await Commit();if(committed.Repeated)repeated++;else created++;
  // Same receipts exercise real idempotence, without preparing redundant evidence.
  var retry=await Commit();if(!retry.Repeated || retry.Id!=committed.Id)throw new Exception("Idempotence failure");
  outputs.Add(new{ppcId=ppc.Id,measurementId=committed.Id,sourceFile=Path.GetFileName(Text(r,"file")),sheet=Text(r,"sheet"),sourceRow=(int)r["sourceRow"]!,excel=Text(r,"excel"),raw=preview.Result.Raw,stored=preview.Result.Stored,judgment=preview.Result.Judgment,recheckRaw=recheck?.Result.Raw,preview.FormulaVersionId,preview.FTableHistoryId,alreadyProcessed=committed.Repeated});
  if((created+repeated)%100==0)Console.WriteLine($"Committed/checked {created+repeated}/{rows.Count}");
  File.WriteAllText(resultPath,JsonSerializer.Serialize(new{marker,processId=process.Id,created,repeated,completed=false,results=outputs},jsonOptions));
 }
}

if(before!=await Snapshot())throw new Exception("Business definitions changed; inspect before declaring complete");
var ids=ppcs.Select(x=>x.Id).ToArray();
var counts=await db.VariableMeasurements.CountAsync(x=>ids.Contains(x.PartProcessCharacteristicId));
var jobs=await db.ChemicalStatisticsJobs.Where(x=>db.VariableMeasurements.Any(v=>v.Id==x.VariableMeasurementId&&ids.Contains(v.PartProcessCharacteristicId))).GroupBy(x=>x.Status).Select(x=>new{status=x.Key,count=x.Count()}).ToListAsync();
if(mode=="--verify"){
 File.WriteAllText(Path.Combine(root,"website-service-verification.json"),JsonSerializer.Serialize(new{marker,checkedCount,measurementCount=counts,jobs,businessDefinitionsUnchanged=true,httpVerification=false},jsonOptions));
 Console.WriteLine($"Website trace service verified {checkedCount}; measurement count={counts}");
}else{
 File.WriteAllText(resultPath,JsonSerializer.Serialize(new{marker,processId=process.Id,created,repeated,completed=true,measurementCount=counts,jobs,businessDefinitionsUnchanged=true,ppcIds=ids,results=outputs},jsonOptions));
 Console.WriteLine($"Import completed: new={created}, existing={repeated}, count={counts}; business definitions unchanged");
}

sealed class NoOutboundEmail:IEmailNotificationService{
 public int Attempts{get;private set;}
 Task<bool> Block(){Attempts++;return Task.FromException<bool>(new InvalidOperationException("Test runner blocks all outbound mail"));}
 public Task<bool> SendAlertEmailAsync(AlertEvent a,string b,string c,SmtpSettingsOverride? d=null,string? e=null)=>Block();
 public Task<bool> SendTestEmailAsync(string a,SmtpSettingsOverride? b=null,string? c=null)=>Block();
 public Task<bool> SendHtmlEmailAsync(string a,string b,string c,string d,SmtpSettingsOverride? e=null,string? f=null,string? g=null)=>Block();
 public Task<bool> SendReportEmailAsync(string a,string b,string c,string d,byte[] e,string f,string? g=null)=>Block();
}
