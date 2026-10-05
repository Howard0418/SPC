using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using EtchTrialImport;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// September import defaults to test. Production requires explicit environment
// selection and exact database confirmations before --apply is accepted.
var target=ImportTargetOptions.Parse(args);
var month=args.Contains("--month",StringComparer.OrdinalIgnoreCase);
var apply=target.Apply;
var validateBackupOnly=args.Contains("--validate-backup-proof-only",StringComparer.OrdinalIgnoreCase);
if(target.IsProduction && !string.IsNullOrWhiteSpace(target.BackupProofPath))
{
    var proof=ProductionBackupProof.LoadAndValidate(target.BackupProofPath,target,DateTimeOffset.UtcNow);
    Console.WriteLine($"Backup proof valid: Portal={proof.Portal.Database}; SPC={proof.Spc.Database}; CreatedAtUtc={proof.CreatedAtUtc:o}");
    if(validateBackupOnly)return;
}
else if(validateBackupOnly)
{
    throw new InvalidOperationException("--validate-backup-proof-only requires production and --backup-proof.");
}
var manifest=Path.GetFullPath(args.FirstOrDefault(x=>x.EndsWith(".json"))??"release-staging/etch-trial-20260917/input.json");
var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};
using var input=JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
var source=input.RootElement.GetProperty("source").GetString()!;
var expectedHash=input.RootElement.GetProperty("sha256").GetString()!;
if(!Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(source))).Equals(expectedHash,StringComparison.OrdinalIgnoreCase))throw new Exception("Source workbook changed");
var reports=input.RootElement.GetProperty("reports").Deserialize<List<EtchReportRequest>>(json)!;
var allowedLines=new[]{"PT1","PT2","QE1","QE2"};
string Key(EtchReportRequest r)=>EtchImportKey.Report(r.ReportDate,r.LineCode);
if(reports.Count==0 || reports.Any(r=>!allowedLines.Contains(r.LineCode) || r.ReportDate.Year!=2026 || r.ReportDate.Month!=9) || reports.Select(Key).Distinct().Count()!=reports.Count)throw new Exception("Invalid or duplicate September report");
if(!month && (reports.Count!=4 || reports.Any(r=>r.ReportDate!=new DateTime(2026,9,1))))throw new Exception("Additional dates require --month");
if(target.IsProduction)
{
    const string productionSourceHash="cadd52c7d5e4d31188b9ab5cc7aae7faa68970f17a305dbe1ce9a04e1417f43a";
    var lineCounts=reports.GroupBy(x=>x.LineCode).ToDictionary(x=>x.Key,x=>x.Count());
    if(!string.Equals(expectedHash,productionSourceHash,StringComparison.OrdinalIgnoreCase) ||
       reports.Count!=48 || reports.Sum(x=>x.Points.Count)!=3650 ||
       lineCounts.GetValueOrDefault("PT1")!=10 || lineCounts.GetValueOrDefault("PT2")!=12 ||
       lineCounts.GetValueOrDefault("QE1")!=13 || lineCounts.GetValueOrDefault("QE2")!=13)
        throw new Exception("Production manifest differs from the reviewed 48-report inventory.");
}
foreach(var r in reports)EtchReportSyncService.Validate(r);
var config=new ConfigurationBuilder().AddJsonFile(target.SpcConfigPath).Build();
var portalConfig=new ConfigurationBuilder().AddJsonFile(target.PortalConfigPath).Build();
var spcCs=config.GetConnectionString("SqlServer")!;
var portalCs=portalConfig.GetConnectionString(target.PortalConnectionName)!;
target.GuardConnection(spcCs,target.SpcDatabase);
target.GuardConnection(portalCs,target.PortalDatabase);
if(config["AppEnvironment"]!=target.Environment ||
   !string.Equals(portalConfig["EnvironmentSwitch:Target"],target.PortalConnectionName,StringComparison.OrdinalIgnoreCase))
    throw new Exception("Application environment does not match the selected import target.");
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(spcCs).Options);
await db.Database.OpenConnectionAsync();
await using var portal=new SqlConnection(portalCs);await portal.OpenAsync();
var lines=reports.Select(r=>r.LineCode).ToList();
var machines=await db.Machines.Where(m=>lines.Contains(m.MachineCode)).ToListAsync();
if(machines.Count!=4)throw new Exception("Missing or ambiguous machines");
var machineIds=machines.Select(m=>m.Id).ToList();
var chars=await db.QualityCharacteristics.Where(c=>EtchReportSyncService.Codes.Contains(c.CharacteristicCode)).ToListAsync();
var charIds=chars.Select(c=>c.Id).ToList();
var mappings=await db.PartProcessCharacteristics.Where(p=>machineIds.Contains(p.MachineId!.Value)&&charIds.Contains(p.CharacteristicId)&&p.ControlScope=="PROCESS").ToListAsync();
var existingPortal=new Dictionary<string,EtchReportRequest>();
foreach(var r in reports)
{
    using var cmd=new SqlCommand("SELECT r.ReportDate,r.LineCode,r.LineSpeed,r.OperatorName,JSON_QUERY((SELECT p.Side,p.RepeatNo,p.OpNo,p.BeforeValue,p.AfterValue FROM etch_amount_points p WHERE p.ReportId=r.Id ORDER BY p.Side,p.RepeatNo,p.OpNo FOR JSON PATH)) Points FROM etch_amount_reports r WHERE r.ReportDate=@date AND r.LineCode=@line FOR JSON PATH",portal);
    cmd.Parameters.AddWithValue("@date",r.ReportDate);cmd.Parameters.AddWithValue("@line",r.LineCode);
    var chunks=new System.Text.StringBuilder();
    await using(var reader=await cmd.ExecuteReaderAsync())while(await reader.ReadAsync())chunks.Append(reader.GetString(0));
    var old=JsonSerializer.Deserialize<List<EtchReportRequest>>(chunks.Length==0?"[]":chunks.ToString(),json)!;
    if(old.Count>1)throw new Exception("Duplicate Portal report");
    if(old.Count==1)
    {
        var o=old[0];if(o.LineSpeed!=r.LineSpeed || o.OperatorName!=r.OperatorName || !o.Points.OrderBy(p=>p.Side).ThenBy(p=>p.RepeatNo).ThenBy(p=>p.OpNo).SequenceEqual(r.Points))throw new Exception($"Portal content conflict: {r.LineCode}");
        existingPortal[Key(r)]=o;
    }
}
var trialDate=reports.Min(r=>r.ReportDate);var next=reports.Max(r=>r.ReportDate).AddDays(1);var mapIds=mappings.Select(m=>m.Id).ToList();
var expectedKeys=reports.Select(r=>EtchImportKey.SourceReference(r.ReportDate,r.LineCode)).ToHashSet(StringComparer.Ordinal);
var existingSpc=await db.VariableMeasurements.AsNoTracking()
    .Where(v=>v.MeasuredAt>=trialDate && v.MeasuredAt<next && v.SourceReference!=null && v.SourceReference.StartsWith("ETCH:2026-09-"))
    .ToListAsync();
if(existingSpc.Any(v=>v.SourceReference is null || !expectedKeys.Contains(v.SourceReference)))
    throw new Exception("SPC contains an unreviewed September ETCH source key.");
var foreignSpc=await db.VariableMeasurements.AsNoTracking()
    .Where(v=>mapIds.Contains(v.PartProcessCharacteristicId) && v.MeasuredAt>=trialDate && v.MeasuredAt<next &&
              (v.SourceReference==null || !v.SourceReference.StartsWith("ETCH:2026-09-")))
    .AnyAsync();
if(foreignSpc)throw new Exception("SPC same-date foreign-source conflict");

var groupExists=await db.ControlChartGroups.AnyAsync(x=>x.GroupCode=="PROC");
var xbar=await db.ControlChartTypes.AsNoTracking().SingleOrDefaultAsync(x=>x.ChartTypeCode=="XBAR_S");
var imr=await db.ControlChartTypes.AsNoTracking().SingleOrDefaultAsync(x=>x.ChartTypeCode=="I-MR" && x.IsEnabled);
if(!groupExists || imr is null)throw new Exception("Required PROC group or enabled I-MR chart type is missing.");
var masterChanges=new List<object>();
if(xbar is null)masterChanges.Add(new{Action="CreateChartType",Code="XBAR_S"});
foreach(var code in EtchReportSyncService.Codes)
{
    var characteristic=chars.SingleOrDefault(x=>x.CharacteristicCode==code);
    if(characteristic is null)masterChanges.Add(new{Action="CreateCharacteristic",Code=code});
    foreach(var machine in machines)
    {
        var mapping=characteristic is null?null:mappings.SingleOrDefault(x=>x.MachineId==machine.Id&&x.CharacteristicId==characteristic.Id);
        var amount=code is "ETCH_A_AVG" or "ETCH_B_AVG";
        var sampleSize=amount?EtchReportSyncService.Rule(machine.MachineCode).Ops*5:1;
        var chartCode=amount?"XBAR_S":"I-MR";
        if(mapping is null)
            masterChanges.Add(new{Action="CreatePpc",Line=machine.MachineCode,Code=code,SampleSize=sampleSize,ChartType=chartCode});
        else if(mapping.SampleSize!=sampleSize || mapping.ChartTypeId!=(amount?xbar?.Id:imr.Id) || !mapping.IsEnabled)
            masterChanges.Add(new{Action="UpdatePpc",Line=machine.MachineCode,Code=code,SampleSize=sampleSize,ChartType=chartCode,ExistingId=mapping.Id});
    }
}
var plan=new{
    Environment=target.Environment,
    Server=ImportTargetOptions.ExpectedServer,
    PortalDatabase=target.PortalDatabase,
    SpcDatabase=target.SpcDatabase,
    Manifest=manifest,
    SourceHash=expectedHash,
    Reports=reports.Count,
    RawPoints=reports.Sum(r=>r.Points.Count),
    Rates=reports.Count,
    Speeds=reports.Count,
    PortalExisting=existingPortal.Count,
    SpcExistingRows=existingSpc.Count,
    MasterChanges=masterChanges,
    Apply=apply
};
var planPath=Path.Combine(Path.GetDirectoryName(manifest)!,$"master-plan-{target.Environment}.json");
await File.WriteAllTextAsync(planPath,JsonSerializer.Serialize(plan,json));
Console.WriteLine($"Plan: environment={target.Environment}; Portal={target.PortalDatabase}; SPC={target.SpcDatabase}; {reports.Count} reports; {reports.Sum(r=>r.Points.Count)} raw points + {reports.Count} rates + {reports.Count} speeds; Portal existing={existingPortal.Count}, SPC existing={existingSpc.Count}; master changes={masterChanges.Count}; apply={apply}");
Console.WriteLine("Master plan: "+planPath);
if(!apply){Console.WriteLine("DRY-RUN completed. No database writes.");return;}
var runDir=Path.Combine(Path.GetDirectoryName(manifest)!,"run-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(runDir);
await File.WriteAllTextAsync(Path.Combine(runDir,"before.json"),JsonSerializer.Serialize(new{Mappings=mappings,Characteristics=chars,Measurements=existingSpc,PortalReports=existingPortal,Note="Targeted pre-write backup. Other dates are untouched."},json));
File.Copy(manifest,Path.Combine(runDir,"input.json"));
var actor=target.IsProduction?"EtchProductionImport:20260922":month?"Codex:EtchMonth:20260918":"Codex:EtchTrial:20260917";
await using(var tx=await db.Database.BeginTransactionAsync())
{
    var group=await db.ControlChartGroups.SingleAsync(x=>x.GroupCode=="PROC");
    xbar=await db.ControlChartTypes.SingleOrDefaultAsync(x=>x.ChartTypeCode=="XBAR_S");
    if(xbar is null){xbar=new(){ChartGroupId=group.Id,ChartTypeCode="XBAR_S",ChartTypeName="平均值－標準差圖",DataCategory="Variable",CreatedBy=actor};db.ControlChartTypes.Add(xbar);await db.SaveChangesAsync();}
    imr=await db.ControlChartTypes.SingleAsync(x=>x.ChartTypeCode=="I-MR" && x.IsEnabled);
    foreach(var code in EtchReportSyncService.Codes)
    {
        var ch=chars.SingleOrDefault(x=>x.CharacteristicCode==code);
        if(ch is null){ch=new(){CharacteristicCode=code,CharacteristicName=code=="ETCH_LINE_SPEED"?"實際線速":code,ControlScope="PROCESS",DataCategory="Variable",DecimalPlaces=6,CreatedBy=actor};db.QualityCharacteristics.Add(ch);await db.SaveChangesAsync();chars.Add(ch);}
        foreach(var machine in machines)
        {
            var m=mappings.SingleOrDefault(x=>x.MachineId==machine.Id&&x.CharacteristicId==ch.Id);
            var amount=code is "ETCH_A_AVG" or "ETCH_B_AVG";
            if(m is null)
            {
                var targetValue=machine.MachineCode switch{"PT1" or "PT2"=>.3,"QE1"=>1.07,_=>1.38};
                var tolerance=machine.MachineCode switch{"PT1" or "PT2"=>.1,"QE1"=>.21,_=>.23};
                var rate=machine.MachineCode switch{"PT1"=>.52,"PT2"=>.46,"QE1"=>.77,_=>.90};
                m=new(){ControlScope="PROCESS",MachineId=machine.Id,ProcessId=machine.ProcessId,CharacteristicId=ch.Id,Unit=amount?"μm":code=="ETCH_RATE"?"μm/min":"m/min",CreatedBy=actor};
                if(amount){m.TargetValue=targetValue;m.LSL=targetValue-tolerance;m.USL=targetValue+tolerance;}
                else if(code=="ETCH_RATE"){m.TargetValue=rate;m.LSL=rate-(machine.MachineCode=="PT2"?.1:.15);m.USL=rate+(machine.MachineCode=="PT2"?.1:.15);}
                db.PartProcessCharacteristics.Add(m);mappings.Add(m);
            }
            m.SampleSize=amount?EtchReportSyncService.Rule(machine.MachineCode).Ops*5:1;m.ChartTypeId=amount?xbar.Id:imr.Id;m.IsEnabled=true;m.UpdatedBy=actor;m.UpdatedAt=DateTime.UtcNow;
        }
    }
    await db.SaveChangesAsync();await tx.CommitAsync();
}
var results=new List<object>();
foreach(var r in reports)
{
    int reportId;
    await using(var tx=(SqlTransaction)await portal.BeginTransactionAsync(IsolationLevel.Serializable))
    {
        using var find=new SqlCommand("SELECT Id FROM etch_amount_reports WITH(UPDLOCK,HOLDLOCK) WHERE ReportDate=@date AND LineCode=@line",portal,tx);
        find.Parameters.AddWithValue("@date",r.ReportDate);find.Parameters.AddWithValue("@line",r.LineCode);
        var id=await find.ExecuteScalarAsync();
        if(id is not null)
        {
            if(!existingPortal.ContainsKey(Key(r)))throw new Exception("Portal report appeared after preflight; rerun preview");
            reportId=Convert.ToInt32(id);
            using var check=new SqlCommand("SELECT p.Side,p.RepeatNo,p.OpNo,p.BeforeValue,p.AfterValue FROM etch_amount_points p WITH(UPDLOCK,HOLDLOCK) WHERE p.ReportId=@id ORDER BY p.Side,p.RepeatNo,p.OpNo",portal,tx);
            check.Parameters.AddWithValue("@id",reportId);var actual=new List<EtchPoint>();
            await using(var reader=await check.ExecuteReaderAsync())while(await reader.ReadAsync())actual.Add(new(reader.GetString(0),reader.GetInt32(1),reader.GetInt32(2),reader.GetDecimal(3),reader.GetDecimal(4)));
            if(!actual.SequenceEqual(r.Points))throw new Exception("Portal points changed after preflight");
            using var header=new SqlCommand("SELECT COUNT(*) FROM etch_amount_reports WHERE Id=@id AND LineSpeed=@speed AND OperatorName=@op",portal,tx);
            header.Parameters.AddWithValue("@id",reportId);header.Parameters.AddWithValue("@speed",r.LineSpeed);header.Parameters.AddWithValue("@op",r.OperatorName);
            if(Convert.ToInt32(await header.ExecuteScalarAsync())!=1)throw new Exception("Portal header changed after preflight");
        }
        else
        {
            var a=r.Points.Where(p=>p.Side=="A").Select(p=>p.BeforeValue-p.AfterValue).ToList();var b=r.Points.Where(p=>p.Side=="B").Select(p=>p.BeforeValue-p.AfterValue).ToList();var all=a.Concat(b).ToList();
            decimal Range(List<decimal> v)=>v.Max()-v.Min();decimal Uniform(List<decimal> v)=>v.Average()==0?0:1-Range(v)/(2*v.Average());
            var length=EtchReportSyncService.Rule(r.LineCode).Length;var rate=all.Average()*r.LineSpeed/length;var speedRef=r.LineCode switch{"PT1" or "PT2"=>.3m,"QE1"=>1.8m,_=>1.5m};
            using var insert=new SqlCommand("INSERT INTO etch_amount_reports (ReportDate,LineCode,LineSpeed,OperatorName,FormulaVersion,AverageEtch,EtchRate,RecommendedSpeed,ASideAverage,BSideAverage,ASideRange,BSideRange,TotalRange,AUniformity,BUniformity,TotalUniformity,CreatedAt,UpdatedAt,UpdatedBy,SpcSyncStatus) OUTPUT INSERTED.Id VALUES (@date,@line,@speed,@operator,'PD-3-581-06B-2026',@avg,@rate,@recommended,@a,@b,@ar,@br,@tr,@au,@bu,@tu,GETDATE(),GETDATE(),@actor,'Pending')",portal,tx);
            var parameters=new Dictionary<string,object>{{"date",r.ReportDate},{"line",r.LineCode},{"speed",r.LineSpeed},{"operator",r.OperatorName},{"avg",all.Average()},{"rate",rate},{"recommended",length*rate/speedRef},{"a",a.Average()},{"b",b.Average()},{"ar",Range(a)},{"br",Range(b)},{"tr",Range(all)},{"au",Uniform(a)},{"bu",Uniform(b)},{"tu",Uniform(all)},{"actor",actor}};
            foreach(var p in parameters)insert.Parameters.AddWithValue("@"+p.Key,p.Value);
            reportId=Convert.ToInt32(await insert.ExecuteScalarAsync());
            foreach(var p in r.Points)
            {
                using var point=new SqlCommand("INSERT INTO etch_amount_points (ReportId,Side,RepeatNo,OpNo,BeforeValue,AfterValue,EtchAmount) VALUES (@id,@side,@r,@op,@before,@after,@amount)",portal,tx);
                point.Parameters.AddWithValue("@id",reportId);point.Parameters.AddWithValue("@side",p.Side);point.Parameters.AddWithValue("@r",p.RepeatNo);point.Parameters.AddWithValue("@op",p.OpNo);point.Parameters.AddWithValue("@before",p.BeforeValue);point.Parameters.AddWithValue("@after",p.AfterValue);point.Parameters.AddWithValue("@amount",p.BeforeValue-p.AfterValue);await point.ExecuteNonQueryAsync();
            }
        }
        await tx.CommitAsync();
    }
    // Cross-system failure leaves the Portal report Pending, not falsely successful. Strict reruns reuse it.
    var result=await new EtchReportSyncService(db).SyncAsync(r,actor,false);
    using(var status=new SqlCommand("UPDATE etch_amount_reports SET SpcSyncStatus='SUCCESS',SpcSyncedAt=GETDATE(),SpcSyncError=NULL WHERE Id=@id",portal)){status.Parameters.AddWithValue("@id",reportId);await status.ExecuteNonQueryAsync();}
    results.Add(new{r.ReportDate,r.LineCode,PortalReportId=reportId,result.BatchId,result.Unchanged,result.PointCount});
    await File.WriteAllTextAsync(Path.Combine(runDir,"result.json"),JsonSerializer.Serialize(results,json));
}
await File.WriteAllTextAsync(Path.Combine(runDir,"masters-after.json"),JsonSerializer.Serialize(mappings,json));
Console.WriteLine(JsonSerializer.Serialize(results,json));Console.WriteLine("Evidence: "+runDir);
