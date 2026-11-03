using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

// One-time, test-only repair. Default is SELECT-only; no measurement or global F writes.
if(args.Length is <3 or >4 || args.Length==4 && args[3] is not ("--apply" or "--verify"))
    throw new ArgumentException("config.json manifest.json report.json [--apply|--verify]");
var jsonOptions=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};
using var config=JsonDocument.Parse(File.ReadAllText(args[0]));
var connection=config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!;
var cs=new SqlConnectionStringBuilder(connection.Replace("Trust Server Certificate=","TrustServerCertificate="));
if(config.RootElement.GetProperty("AppEnvironment").GetString()!="test" || cs.InitialCatalog!="PMR_SPC_TEST" || cs.DataSource!="172.16.110.16")
    throw new InvalidOperationException("This repair is restricted to the verified SPC test database.");
var manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(args[1]),jsonOptions)!;
if(manifest.SourceHash!="df036aa58ad6358ad858d7a1f45256d887480f14de9337d2b8b21e37ff2dac1b" || manifest.EffectiveDate!="2026-11-03" || manifest.Rows.Count!=87 || manifest.Rows.Select(x=>x.PpcId).Distinct().Count()!=87)
    throw new InvalidOperationException("Manifest does not match the reviewed workbook / repair scope.");
var apply=args.Length==4 && args[3]=="--apply";
var verify=args.Length==4 && args[3]=="--verify";
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
await db.Database.OpenConnectionAsync();
await using(var command=db.Database.GetDbConnection().CreateCommand())
{
    command.CommandText="SELECT DB_NAME()";
    if((string?)await command.ExecuteScalarAsync()!="PMR_SPC_TEST")throw new InvalidOperationException("Actual DB mismatch.");
}
var actor="SPC-CHEM-F-1103";
var reason="SPC-CHEM-F-CONSISTENCY-1103-TASK-001 / 指定Excel原公式、係數與初複驗；僅2026-11-03起測試，不回填歷史";
var ids=manifest.Rows.Select(x=>x.PpcId).ToArray();
var mappings=await db.PartProcessCharacteristics.Include(x=>x.Machine).Include(x=>x.Characteristic).Where(x=>ids.Contains(x.Id)).ToListAsync();
var histories=await db.ChemicalFTableVersionHistories.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync();
var required=manifest.FCells.Where(x=>x.Value.ValueKind==JsonValueKind.Number).ToDictionary(x=>"F!"+x.Key,x=>x.Value.GetDecimal(),StringComparer.OrdinalIgnoreCase);
var f=histories.FirstOrDefault(h=>{
    var cells=JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(h.CellsJson,jsonOptions) ?? [];
    return required.All(k=>cells.Any(c=>ChemicalFTableService.NormalizeCellAddress("F!"+c.CellAddress.Replace("F!","",StringComparison.OrdinalIgnoreCase))==k.Key && c.NumericValue==k.Value));
});
var appendSnapshot=f is null;
f ??=new ChemicalFTableVersionHistory {
    VersionCode="EXCEL-20261103-df036aa58ad6",DisplayName="指定藥液日報表F快照（測試修正）",
    CellsJson=JsonSerializer.Serialize(required.Select(x=>new ChemicalFTableHistoryCell(x.Key,null,x.Value))),
    VersionNo=(histories.Select(x=>x.VersionNo).DefaultIfEmpty().Max())+1,
    ChangeType="ExcelSnapshot",ChangedBy=actor,Reason=reason
}; // Immutable evidence only: no active F version / cells are changed.
var backups=new List<object>();var blocked=new List<Blocked>();var changes=new List<object>();
foreach(var r in manifest.Rows)
{
    var expected=JsonNode.Parse(r.ExpectedConfig)!.AsObject();var candidate=JsonNode.Parse(r.Config)!.AsObject();
    expected["enabled"]=true;
    expected["concentrationFormula"]=candidate["concentrationFormula"]!.DeepClone();
    expected["recheckConcentrationFormula"]=candidate["recheckConcentrationFormula"]!.DeepClone();
    if(!JsonNode.DeepEquals(expected,candidate))throw new InvalidOperationException("Repair may only change concentration formulas and enable them.");
    var concentrationOnly=candidate.DeepClone().AsObject();concentrationOnly["adjustmentFormula"]=null;concentrationOnly["adjustmentAmountFormula"]=null;
    foreach(var phase in new[]{"Initial","Recheck"})ChemicalFormulaCalculation.Calculate(concentrationOnly.ToJsonString(),"10","1",f.CellsJson,null,null,null,phase);
    var m=mappings.SingleOrDefault(x=>x.Id==r.PpcId);
    if(m is null || !m.IsEnabled || m.ControlScope!="CHEM" || m.Machine?.MachineCode!=r.MachineCode || m.TankId!=r.TankId || m.Characteristic?.CharacteristicCode!=r.CharacteristicCode)
        throw new InvalidOperationException($"Identity changed: {r.PpcId}");
    if(m.ChemicalAnalysisConfigJson!=r.ExpectedConfig && m.ChemicalAnalysisConfigJson!=r.Config)
        throw new InvalidOperationException($"Config changed since read-only inventory: {r.PpcId}");
    var policy=await db.ChemicalFormulaPolicies.AsNoTracking().SingleOrDefaultAsync(x=>x.PartProcessCharacteristicId==r.PpcId);
    if(policy?.ActiveScheduleRevisionId is not null && m.ChemicalAnalysisConfigJson!=r.Config)
        throw new InvalidOperationException($"Existing business schedule requires separate review: {r.PpcId}");
    backups.Add(new {r.PpcId,Config=m.ChemicalAnalysisConfigJson,Policy=policy,r.Sources});
    try {
        foreach(var phase in new[]{"Initial","Recheck"})
            ChemicalFormulaCalculation.Calculate(r.Config,"10","1",f.CellsJson,(decimal?)m.LSL,(decimal?)m.USL,(decimal?)m.TargetValue,phase);
    }catch(ArgumentException ex){blocked.Add(new(r.PpcId,ex.Message));}
}
var blockedIds=blocked.Select(x=>x.PpcId).ToHashSet();
// Save the complete before image before opening a write transaction.
var beforeFile=Path.ChangeExtension(args[2],"before.json");
if(!File.Exists(beforeFile))File.WriteAllText(beforeFile,JsonSerializer.Serialize(new{Environment="test",manifest.SourceHash,manifest.EffectiveDate,fTableHistoryId=f.Id,backups},jsonOptions));
if(apply)
{
    await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    var versions=new ChemicalAnalysisFormulaVersionService(db);
    var schedule=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
    if(appendSnapshot){db.ChemicalFTableVersionHistories.Add(f);await db.SaveChangesAsync();}
    foreach(var r in manifest.Rows)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={r.PpcId}");
        var m=mappings.Single(x=>x.Id==r.PpcId);await db.Entry(m).ReloadAsync();
        if(m.ChemicalAnalysisConfigJson!=r.ExpectedConfig && m.ChemicalAnalysisConfigJson!=r.Config)throw new InvalidOperationException("Concurrent config change.");
        var policy=await db.ChemicalFormulaPolicies.SingleOrDefaultAsync(x=>x.PartProcessCharacteristicId==r.PpcId);
        if(policy?.ActiveScheduleRevisionId is not null && m.ChemicalAnalysisConfigJson!=r.Config)
            throw new InvalidOperationException("Concurrent schedule change; no overwrite allowed.");
        if(m.ChemicalAnalysisConfigJson==r.Config && (blockedIds.Contains(r.PpcId) || policy?.ActiveScheduleRevisionId is not null && policy.IsEnabled)){changes.Add(new{r.PpcId,Status="AlreadyApplied"});continue;}
        var changed=await versions.RecordChangeAsync(r.PpcId,m.ChemicalAnalysisConfigJson,r.Config,actor,reason,default);
        m.ChemicalAnalysisConfigJson=r.Config;m.UpdatedAt=DateTime.UtcNow;m.UpdatedBy=actor;await db.SaveChangesAsync();
        if(blockedIds.Contains(r.PpcId)){changes.Add(new{r.PpcId,Status="ConcentrationCorrectedButScheduleQuarantined"});continue;}
        var versionId=changed.Version?.Id ?? await db.ChemicalAnalysisFormulaVersions.Where(x=>x.PartProcessCharacteristicId==r.PpcId).OrderByDescending(x=>x.VersionNo).Select(x=>x.Id).FirstAsync();
        var revision=await schedule.PublishAsync(r.PpcId,new(policy?.ActiveScheduleRevisionId,reason,[new(versionId,f.Id,manifest.EffectiveDate,null)]),actor,default);
        var enabled=await db.ChemicalFormulaPolicies.SingleAsync(x=>x.PartProcessCharacteristicId==r.PpcId);
        enabled.IsEnabled=true;await db.SaveChangesAsync(); // Untraced legacy imports cannot bypass the approved formula/F.
        changes.Add(new{r.PpcId,Status="Applied",FormulaVersionId=versionId,RevisionId=revision});
    }
    await tx.CommitAsync();
}
var previews=new List<object>();
if(verify)
{
    var schedule=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
    foreach(var row in manifest.Rows)
    {
        var m=mappings.Single(x=>x.Id==row.PpcId);
        if(m.ChemicalAnalysisConfigJson!=row.Config)throw new InvalidOperationException("Post-repair config mismatch.");
        var policy=await db.ChemicalFormulaPolicies.AsNoTracking().SingleOrDefaultAsync(x=>x.PartProcessCharacteristicId==row.PpcId);
        if(blockedIds.Contains(row.PpcId)){if(policy?.ActiveScheduleRevisionId is not null)throw new InvalidOperationException("Quarantine changed.");continue;}
        if(policy is not {IsEnabled:true,ActiveScheduleRevisionId:not null})throw new InvalidOperationException("Date contract not enabled.");
        foreach(var phase in new[]{"Initial","Recheck"})
        {
            var result=await schedule.PreviewAsync(row.PpcId,new("2026-11-03T12:00:00+08:00","10","1","verification-only",phase),actor,default);
            if(result.FTableHistoryId!=f.Id)throw new InvalidOperationException("F snapshot mismatch.");
            previews.Add(new {row.PpcId,Phase=phase,result.Result,result.FormulaVersionId,result.FTableHistoryId});
        }
    }
}
File.WriteAllText(args[2],JsonSerializer.Serialize(new{Mode=apply?"APPLIED":verify?"VERIFIED_SELECT_ONLY":"SELECT_ONLY",Environment="test",manifest.SourceHash,manifest.EffectiveDate,FTableHistoryId=f.Id,appendSnapshot,Count=manifest.Rows.Count,blocked,changes,previews},jsonOptions));
Console.WriteLine($"{(apply?"APPLIED":"SELECT_ONLY")}: {manifest.Rows.Count} test configs; immutable F history {f.Id}; measurements and active F untouched.");

record Manifest(string SourceHash,string EffectiveDate,Dictionary<string,JsonElement> FCells,List<Row> Rows);
record Row(int PpcId,string MachineCode,int TankId,string CharacteristicCode,string ExpectedConfig,string Config,List<string> Sources);
record Blocked(int PpcId,string Reason);
