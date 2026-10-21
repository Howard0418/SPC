using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

// Explicit opt-in; no migrations, deletion, authentication/role changes or real mail.
if(args.Length!=2 || args[0]!="--write-test-demo") throw new ArgumentException("Usage: --write-test-demo <test appsettings path>");
var config=new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[1])).Build();
var connection=config.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Missing test connection");
var builder=new SqlConnectionStringBuilder(connection);
if(builder.InitialCatalog!="PMR_SPC_TEST") throw new InvalidOperationException("Only PMR_SPC_TEST is allowed");
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
if(await db.Database.SqlQueryRaw<string>("SELECT DB_NAME() AS Value").SingleAsync()!="PMR_SPC_TEST") throw new InvalidOperationException("Wrong actual database");
const string code="DEMO-FORMULA-20261021";
const string actor="DEMO-FORMULA-20261021";
var mail=new NoMail();
var spc=new SpcService(db,mail,new ConfigurationBuilder().Build());
var schedule=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
var service=new ChemicalPortalMeasurementService(db,schedule,new UploadService(db,spc),NullLogger<ChemicalPortalMeasurementService>.Instance);
var process=await db.Processes.SingleOrDefaultAsync(x=>x.ProcessCode==code);
PartProcessCharacteristic ppc;
if(process is null){
 await using var tx=await db.Database.BeginTransactionAsync();
 var chart=await db.ControlChartTypes.FirstAsync(x=>x.IsEnabled && (x.ChartTypeCode=="I_MR" || x.ChartTypeCode=="I-MR"));
 process=new Process{ProcessCode=code,ProcessName="【測試可清除】日期公式示範",ControlScope="CHEM",Description=code};
 var qc=new QualityCharacteristic{CharacteristicCode=code,CharacteristicName="【測試】跨日期濃度",ControlScope="CHEM",InputMode="FORMULA",DecimalPlaces=2};
 var line=new ProductionLine{FactoryId=await db.Factories.Select(x=>x.Id).FirstAsync(),LineCode=code,LineName="【測試可清除】日期公式",Description=code};
 db.AddRange(process,qc,line);await db.SaveChangesAsync();
 var machine=new Machine{MachineCode=code,MachineName="【測試】日期公式機台",ProcessId=process.Id};
 var tank=new Tank{LineId=line.Id,TankCode=code,TankName="【測試】公式示範槽",Description=code};
 db.AddRange(machine,tank);await db.SaveChangesAsync();
 ppc=new PartProcessCharacteristic{ProcessId=process.Id,MachineId=machine.Id,TankId=tank.Id,CharacteristicId=qc.Id,ControlScope="CHEM",ChartTypeId=chart.Id,Unit="demo",SampleSize=1,
  ChemicalAnalysisConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*3\",\"decimalPlaces\":2}",LSL=1.5,USL=4.2,LCL=1.8,CL=3,UCL=4};
 db.Add(ppc);await db.SaveChangesAsync();
 db.AddRange(new ChemicalAnalysisFormulaVersion{PartProcessCharacteristicId=ppc.Id,VersionNo=1,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\",\"decimalPlaces\":2}",Reason="DEMO-DOC-A：10/10～10/15，原始值×2",ChangedBy=actor},
  new ChemicalAnalysisFormulaVersion{PartProcessCharacteristicId=ppc.Id,VersionNo=2,ConfigJson=ppc.ChemicalAnalysisConfigJson,Reason="DEMO-DOC-B：10/16起，原始值×3",ChangedBy=actor});
 db.AddRange(new ControlLimitSegment{PartProcessCharacteristicId=ppc.Id,StartDate=new(2026,10,9,16,0,0,DateTimeKind.Utc),EndDate=new(2026,10,15,15,59,59,DateTimeKind.Utc),LSL=1.5,USL=3,LCL=1.8,CL=2.3,UCL=2.8,Note=code+" A段：公式×2；規格1.5～3"},
  new ControlLimitSegment{PartProcessCharacteristicId=ppc.Id,StartDate=new(2026,10,15,16,0,0,DateTimeKind.Utc),EndDate=new(2026,10,21,15,59,59,DateTimeKind.Utc),LSL=2.5,USL=4.2,LCL=2.8,CL=3.4,UCL=4,Note=code+" B段：公式×3；規格2.5～4.2"});
 await db.SaveChangesAsync();await tx.CommitAsync();
}else ppc=await db.PartProcessCharacteristics.SingleAsync(x=>x.ProcessId==process.Id && x.ControlScope=="CHEM");
if(!await db.ChemicalFormulaPolicies.AnyAsync(x=>x.PartProcessCharacteristicId==ppc.Id && x.ActiveScheduleRevisionId!=null)){
 var versions=await db.ChemicalAnalysisFormulaVersions.Where(x=>x.PartProcessCharacteristicId==ppc.Id).OrderBy(x=>x.VersionNo).ToListAsync();
 await schedule.PublishAsync(ppc.Id,new(null,code,[new(versions[0].Id,null,"2026-10-10","2026-10-15"),new(versions[1].Id,null,"2026-10-16",null)]),actor,default);
}
var machineCode=await db.Machines.Where(x=>x.Id==ppc.MachineId).Select(x=>x.MachineCode).SingleAsync();
for(var day=10;day<=21;day++){
 var at=$"2026-10-{day:00}T09:10:00+08:00";
 var primary=day is 15 or 16 ? "1.2345" : day==21 ? "2.0000" : (1.10m+(day%4)*0.02m).ToString("0.0000",CultureInfo.InvariantCulture);
 var recheck=day is 15 or 16 ? "1.2345" : day==21 ? "1.6000" : (decimal.Parse(primary,CultureInfo.InvariantCulture)+0.01m).ToString("0.0000",CultureInfo.InvariantCulture);
 var requestId=new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(code+"|"+day))[..16]);
 if(await db.UploadBatches.AnyAsync(x=>x.UploadBatchId==requestId))continue;
 var input=new ChemicalPortalPreviewRequest(ppc.Id,at,primary,null,"OPEN","GENERAL");
 var preview=await service.PreviewAsync(input,actor,default);var again=await service.PreviewAsync(input with{Primary=recheck},actor,default);
 var row=new Dictionary<string,string?>{{"PpcId",ppc.Id.ToString()},{"ControlScope","CHEM"},{"ProcessCode",code},{"MachineCode",machineCode},{"TankCode",code},{"CharacteristicCode",code},{"MeasuredAt",at},{"TitrationValue",primary},{"MeasuredValue","0"},{"RecheckTitrationValue",recheck},{"RecheckValue","0"},{"ChemicalPreviewToken",preview.Token},{"ChemicalRecheckPreviewToken",again.Token},{"SampleNo","1"},{"SamplingPhase","OPEN"},{"SamplingStage","GENERAL"},{"Operator",actor}};
 await service.ConfirmAsync(new(requestId,[row]),actor,default);
 Console.WriteLine($"DAY {day}: v{preview.FormulaVersionNo} {primary} => {preview.Result}");
}
var measurements=await db.VariableMeasurements.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==ppc.Id).OrderBy(x=>x.MeasuredAt).Select(x=>new{x.Id,x.MeasuredAt,x.MeasuredValue,x.RecheckValue}).ToListAsync();
var traceCount=await db.ChemicalCalculationTraces.CountAsync(x=>db.VariableMeasurements.Any(v=>v.Id==x.VariableMeasurementId && v.PartProcessCharacteristicId==ppc.Id));
var statistics=await db.SpcCalculationResults.Where(x=>x.PartProcessCharacteristicId==ppc.Id).Select(x=>new{x.VariableMeasurementId,x.IsOutOfSpec,x.IsOutOfControl}).ToListAsync();
var chartResult=await spc.GetInteractiveChartAsync(ppc.Id,null,new DateTime(2026,10,10),new DateTime(2026,10,21));
if(measurements.Count!=12 || traceCount!=24 || statistics.Count!=12 || chartResult is null)throw new InvalidOperationException("Demo verification incomplete");
var query=new ChemicalTraceQueryService(db);
foreach(var point in measurements){
 var saved=await query.GetAsync(ppc.Id,null,point.Id,0,0,default) ?? throw new InvalidOperationException("Missing saved evidence");
 if(saved.Rows.Single().Calculations.Count!=2)throw new InvalidOperationException("Missing initial/recheck evidence");
}
if(measurements[5].MeasuredValue!=2.47 || measurements[6].MeasuredValue!=3.7 || statistics.Count(x=>x.IsOutOfSpec && x.IsOutOfControl)!=1)throw new InvalidOperationException("Unexpected formula or anomaly");
Console.WriteLine(JsonSerializer.Serialize(new{marker=code,ppcId=ppc.Id,processId=process.Id,ppc.MachineId,ppc.TankId,ppc.CharacteristicId,traceCount,measurements,statistics,mailSuppressed=mail.Suppressed,chart=chartResult},new JsonSerializerOptions{WriteIndented=true}));

sealed class NoMail:IEmailNotificationService{
 public int Suppressed{get;private set;}
 public Task<bool> SendAlertEmailAsync(AlertEvent a,string e,string n,SmtpSettingsOverride? o=null){Suppressed++;return Task.FromResult(false);}
 public Task<bool> SendTestEmailAsync(string e,SmtpSettingsOverride? o=null)=>throw new NotSupportedException();
 public Task<bool> SendHtmlEmailAsync(string e,string n,string s,string h,SmtpSettingsOverride? o=null)=>throw new NotSupportedException();
 public Task<bool> SendReportEmailAsync(string e,string n,string s,string h,byte[] b,string f)=>throw new NotSupportedException();
}
