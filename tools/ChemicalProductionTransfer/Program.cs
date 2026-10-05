using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

// One-time, bounded transfer. No application host, scheduler, SMTP, or migrations are started.
var root = @"D:\SPC\specs\20260910-chemical-production-transfer";
var apply = args.Contains("--apply");
var sourceBatch = Guid.Parse("79ceeb2e-cfc6-46b2-9032-5cf832a3238b");
var transferBatch = Guid.Parse("5f6964d5-f2ca-4b9c-9506-40613273830e");
var planText = File.ReadAllText(Path.Combine(root,"insert-plan.json"));
var planHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(planText)));
using var plan = JsonDocument.Parse(planText);
var rows = plan.RootElement.EnumerateArray().ToArray();
if (rows.Length != 1269) throw new InvalidOperationException("Expected reviewed 1269-row plan.");
if (apply && !args.Contains("--confirm-database=PMR_SPC_2026")) throw new InvalidOperationException("Missing target guard.");

string Connection(string env, string expected) {
    using var cfg=JsonDocument.Parse(File.ReadAllText($@"D:\SPC\release\{env}\backend\appsettings.json"));
    var cs=cfg.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!;
    var b=new SqlConnectionStringBuilder(cs);
    if(b.DataSource!="172.16.110.16" || b.InitialCatalog!=expected) throw new InvalidOperationException("Unexpected target.");
    return cs;
}
await using var source = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Connection("test","PMR_SPC_TEST")).Options);
await using var target = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Connection("production","PMR_SPC_2026")).Options);
await source.Database.OpenConnectionAsync();
await target.Database.OpenConnectionAsync();
if (await target.UploadBatches.AnyAsync(b=>b.UploadBatchId==transferBatch)) throw new InvalidOperationException("Transfer batch already exists; do not repeat.");
var sourceRecords = await source.VariableMeasurements.AsNoTracking()
    .Where(v=>v.UploadBatchId==sourceBatch).ToDictionaryAsync(v=>v.Id);
var sourceDetails = await source.UploadDetails.AsNoTracking()
    .Where(d=>d.UploadBatchId==sourceBatch && d.IsValid).ToDictionaryAsync(d=>d.Id);
if(sourceRecords.Count!=1269 || sourceDetails.Count!=1269) throw new InvalidOperationException("Source batch changed.");
var sourceBatchInfo=await source.UploadBatches.AsNoTracking().SingleAsync(b=>b.UploadBatchId==sourceBatch);
if(sourceBatchInfo.ImportStatus!="Imported" || sourceBatchInfo.IsExcluded || sourceBatchInfo.IsDeleted || sourceBatchInfo.SourceType!="Api")
    throw new InvalidOperationException("Source batch not eligible.");
var sourceMapIds=sourceRecords.Values.Select(v=>v.PartProcessCharacteristicId).Distinct().ToArray();
if(await source.PartProcessCharacteristics.CountAsync(m=>sourceMapIds.Contains(m.Id) && m.ControlScope=="CHEM")!=sourceMapIds.Length)
    throw new InvalidOperationException("Source contains non-CHEM mappings.");
foreach(var row in rows) {
    var s=row.GetProperty("Source").Deserialize<VariableMeasurement>()!;
    var d=row.GetProperty("SourceDetail").Deserialize<UploadDetail>()!;
    if(s.UploadBatchId!=sourceBatch || !sourceRecords.TryGetValue(s.Id,out var current) ||
       !s.RowVersion!.SequenceEqual(current.RowVersion!) || current.IsDeleted ||
       !sourceDetails.TryGetValue(d.Id,out var cd) || cd.PayloadJson!=d.PayloadJson || cd.IsDeleted)
        throw new InvalidOperationException("Source changed after reviewed inventory.");
    if(s.PartId!=0 || s.ChemicalId!=null || s.SlotId!=null || s.PortalDailyDate!=null || s.SamplingPhase!="GENERAL")
        throw new InvalidOperationException("Unsupported source reference/phase.");
}

// All tracked writes are limited to newly created transfer records. Existing records cannot be updated.
target.SavingChanges += (_,_) => {
    foreach(var e in target.ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)) {
        var valid=e.State==EntityState.Added && e.Entity switch {
            UploadBatch b => b.UploadBatchId==transferBatch,
            UploadDetail d => d.UploadBatchId==transferBatch,
            VariableMeasurement v => v.UploadBatchId==transferBatch,
            SpcCalculationResult c => c.UploadBatchId==transferBatch,
            AlertEvent a => a.UploadBatchId==transferBatch,
            _ => false
        };
        if(!valid) throw new InvalidOperationException($"Disallowed write: {e.Metadata.ClrType.Name} {e.State}");
    }
};

async Task<Dictionary<string,string>> Fingerprints() {
    // Only counts and aggregate checksums leave SQL Server for unrelated tables, never their records.
    var con=target.Database.GetDbConnection();
    await using var names=con.CreateCommand();
    names.Transaction=target.Database.CurrentTransaction?.GetDbTransaction();
    names.CommandText="SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') ORDER BY name";
    var tables=new List<string>();
    await using(var rd=await names.ExecuteReaderAsync()) { while(await rd.ReadAsync()) tables.Add(rd.GetString(0)); }
    var result=new Dictionary<string,string>();
    var allowed=new HashSet<string>{"UploadBatches","UploadDetails","VariableMeasurements","SpcCalculationResults","AlertEvents"};
    foreach(var table in tables) {
        await using var cmd=con.CreateCommand(); cmd.Transaction=target.Database.CurrentTransaction?.GetDbTransaction(); cmd.CommandTimeout=90;
        var quoted=new SqlCommandBuilder().QuoteIdentifier(table);
        cmd.CommandText="SELECT COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(*)),0) FROM dbo."+quoted+
            (allowed.Contains(table)?" WHERE UploadBatchId<>@batch OR UploadBatchId IS NULL":"");
        var p=cmd.CreateParameter();p.ParameterName="@batch";p.Value=transferBatch;cmd.Parameters.Add(p);
        await using var rd=await cmd.ExecuteReaderAsync();await rd.ReadAsync();
        result[table]=$"{rd.GetInt64(0)}:{rd.GetInt32(1)}";
    }
    return result;
}

async Task VerifyTargets() {
    foreach(var group in rows.GroupBy(r=>r.GetProperty("TargetMapping").GetProperty("Id").GetInt32())) {
        var j=group.First().GetProperty("TargetMapping");
        var m=await target.PartProcessCharacteristics.AsNoTracking().Include(m=>m.Process).Include(m=>m.Machine)
            .Include(m=>m.Tank).Include(m=>m.Characteristic).SingleAsync(m=>m.Id==group.Key);
        if(m.ControlScope!="CHEM" || !m.IsEnabled || m.Unit!=j.GetProperty("Unit").GetString() ||
           m.Process?.ProcessCode!=j.GetProperty("ProcessCode").GetString() ||
           m.Machine?.MachineCode!=j.GetProperty("MachineCode").GetString() ||
           m.Tank?.TankCode!=j.GetProperty("TankCode").GetString() ||
           m.Characteristic?.CharacteristicCode!=j.GetProperty("CharacteristicCode").GetString())
            throw new InvalidOperationException("Target mapping changed.");
        var existing=await target.VariableMeasurements.AsNoTracking().Where(v=>v.PartProcessCharacteristicId==m.Id).ToListAsync();
        foreach(var row in group) {
            var s=row.GetProperty("Source").Deserialize<VariableMeasurement>()!;
            if(existing.Any(t => (t.MeasuredAt==s.MeasuredAt && t.SampleNo==s.SampleNo && t.LotNo==s.LotNo) ||
                (t.PortalDailyDate?.Date==s.MeasuredAt.Date && t.SamplingPhase==s.SamplingPhase)))
                throw new InvalidOperationException("New target duplicate detected; regenerate diff.");
        }
    }
}
await VerifyTargets();
var baseline=await Fingerprints();
File.WriteAllText(Path.Combine(root,apply?"apply-before-fingerprints.json":"dry-run-fingerprints.json"),JsonSerializer.Serialize(baseline));
if(!apply) {
    File.WriteAllText(Path.Combine(root,"dry-run.json"),JsonSerializer.Serialize(new {PlanHash=planHash,Count=rows.Length,Source="PMR_SPC_TEST",Target="PMR_SPC_2026",CheckedAt=DateTime.UtcNow,Status="Passed"}));
    Console.WriteLine($"DRY RUN PASSED: {rows.Length} CHEM records, {baseline.Count} table fingerprints, plan {planHash}");
    return;
}
using(var dry=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"dry-run.json")))) {
    if(dry.RootElement.GetProperty("PlanHash").GetString()!=planHash) throw new InvalidOperationException("Plan changed since dry run.");
}
using(var backup=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"backup.json")))) {
    if(backup.RootElement.GetProperty("Database").GetString()!="PMR_SPC_2026" ||
       !backup.RootElement.GetProperty("Verified").GetBoolean() ||
       DateTime.UtcNow-backup.RootElement.GetProperty("VerifiedAtUtc").GetDateTime()>TimeSpan.FromHours(2))
        throw new InvalidOperationException("Missing recent verified backup.");
}
await using var tx=await target.Database.BeginTransactionAsync(IsolationLevel.Serializable);
await VerifyTargets();
baseline=await Fingerprints();
var batch=new UploadBatch {UploadBatchId=transferBatch,UploadType="Variable",SourceType="Api",ImportStatus="Imported",
    OriginalFileName="SPC_TEST_CHEM_2026-08_79ceeb2e_20260910",FileHash=planHash,
    TotalRows=rows.Length,ValidRows=rows.Length,ErrorRows=0,ConfirmedAt=DateTime.UtcNow};
target.UploadBatches.Add(batch);
await target.SaveChangesAsync();
var inserted=new List<(long SourceId,VariableMeasurement Target)>();
var rowNo=0;
foreach(var row in rows) {
    var s=row.GetProperty("Source").Deserialize<VariableMeasurement>()!;
    var sourceId=s.Id;
    var m=row.GetProperty("TargetMapping");
    s.Id=0;s.RowVersion=null;s.UploadBatchId=transferBatch;
    s.PartProcessCharacteristicId=m.GetProperty("Id").GetInt32();
    s.ProcessId=m.GetProperty("ProcessId").GetInt32();s.MachineId=m.GetProperty("MachineId").GetInt32();
    s.TankId=m.GetProperty("TankId").GetInt32();s.LineId=m.GetProperty("TankLineId").GetInt32();
    s.CharacteristicId=m.GetProperty("CharacteristicId").GetInt32();s.UpdatedAt=null;s.UpdatedBy=null;
    var d=row.GetProperty("SourceDetail").Deserialize<UploadDetail>()!;
    var payload=JsonSerializer.Deserialize<Dictionary<string,string?>>(d.PayloadJson)!;
    payload["ResolvedMappingId"]=s.PartProcessCharacteristicId.ToString();
    payload["TransferSourceDatabase"]="PMR_SPC_TEST";payload["TransferSourceBatchId"]=sourceBatch.ToString();
    payload["TransferSourceMeasurementId"]=sourceId.ToString();payload["TransferSourceDetailId"]=d.Id.ToString();
    payload["MeasuredValue"]=s.MeasuredValue.ToString("R",CultureInfo.InvariantCulture);
    payload["RecheckValue"]=s.RecheckValue?.ToString("R",CultureInfo.InvariantCulture);
    payload["AdjustAction"]=s.AdjustAction;payload["AdjustAmount"]=s.AdjustAmount;
    target.UploadDetails.Add(new UploadDetail {UploadBatchId=transferBatch,RowNo=++rowNo,IsValid=true,PayloadJson=JsonSerializer.Serialize(payload)});
    target.VariableMeasurements.Add(s);inserted.Add((sourceId,s));
}
await target.SaveChangesAsync();
Console.WriteLine($"Inserted {inserted.Count} measurements inside uncommitted transaction; calculating with production definitions.");
var mail=new NoEmail();
var spc=new SpcService(target,mail,new ConfigurationBuilder().Build());
var calculated=0;
foreach(var item in inserted.OrderBy(i=>i.Target.MeasuredAt).ThenBy(i=>i.Target.Id)) {
    if(await spc.CalculateVariableAsync(item.Target)!=null) calculated++;
    if(calculated>0 && calculated%250==0) Console.WriteLine($"Calculated {calculated} results.");
}
target.ChangeTracker.Clear();
var actual=await target.VariableMeasurements.AsNoTracking().Where(v=>v.UploadBatchId==transferBatch).ToDictionaryAsync(v=>v.Id);
foreach(var item in inserted) {
    var a=actual[item.Target.Id];var expected=item.Target;
    foreach(var property in typeof(VariableMeasurement).GetProperties().Where(p=>p.PropertyType.IsValueType || p.PropertyType==typeof(string))) {
        if(property.Name is "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy") continue;
        if(!Equals(property.GetValue(a),property.GetValue(expected))) throw new InvalidOperationException($"Value mismatch: {property.Name}");
    }
}
var after=await Fingerprints();
if(baseline.Any(p=>after[p.Key]!=p.Value)) throw new InvalidOperationException("Existing or unrelated data changed; rollback required.");
var detailsCount=await target.UploadDetails.CountAsync(d=>d.UploadBatchId==transferBatch);
if(actual.Count!=1269 || detailsCount!=1269) throw new InvalidOperationException("Count mismatch.");
var alerts=await target.AlertEvents.CountAsync(a=>a.UploadBatchId==transferBatch);
File.WriteAllText(Path.Combine(root,"inserted-ids.json"),JsonSerializer.Serialize(inserted.Select(i=>new {i.SourceId,TargetId=i.Target.Id})));
File.WriteAllText(Path.Combine(root,"transaction-verification.json"),JsonSerializer.Serialize(new {BatchId=transferBatch,Measurements=actual.Count,Details=detailsCount,Calculations=calculated,Alerts=alerts,EmailsSent=0,SuppressedEmails=mail.Suppressed,ProtectedTables=after.Count,ExistingDataUnchanged=true,VerifiedAt=DateTime.UtcNow}));
await tx.CommitAsync();
var committed=await target.VariableMeasurements.AsNoTracking().CountAsync(v=>v.UploadBatchId==transferBatch);
File.WriteAllText(Path.Combine(root,"result.json"),JsonSerializer.Serialize(new {Status="Committed",BatchId=transferBatch,Inserted=committed,Calculations=calculated,Alerts=alerts,EmailsSent=0,PlanHash=planHash,CommittedAt=DateTime.UtcNow}));
Console.WriteLine($"COMMITTED: {committed} measurements, {calculated} calculations, {alerts} alerts, 0 emails. Batch {transferBatch}");

sealed class NoEmail : IEmailNotificationService {
    public int Suppressed {get;private set;}
    public Task<bool> SendAlertEmailAsync(AlertEvent a,string email,string name,SmtpSettingsOverride? settings=null) {Suppressed++;return Task.FromResult(false);}
    public Task<bool> SendTestEmailAsync(string email,SmtpSettingsOverride? settings=null)=>throw new InvalidOperationException("Email prohibited");
    public Task<bool> SendHtmlEmailAsync(string email,string name,string subject,string html,SmtpSettingsOverride? settings=null)=>throw new InvalidOperationException("Email prohibited");
    public Task<bool> SendReportEmailAsync(string email,string name,string subject,string html,byte[] bytes,string filename)=>throw new InvalidOperationException("Email prohibited");
}
