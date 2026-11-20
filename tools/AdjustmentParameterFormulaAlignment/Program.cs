using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using AdjustmentParameterFormulaAlignment;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

if (args.Length != 2 || args[1] is not ("plan" or "apply" or "verify"))
    throw new ArgumentException("TEST_CONFIG plan|apply|verify");
const string TaskId = "SPC-CHEM-ADJUST-ALIGN-1120-TASK-005";
const string ExpectedBeforeSnapshot = "2BFDF6A54D170FA2DE110D61C7FD95F443BEBE6D7D6C26F5EB8606F7D2D6D858";
const string VerifiedBackup = @"C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_TEST_ADJUST_PARAMS_20261120-171552.bak";
const string BackupConfirmedAt = "2026-11-20T20:38:53+08:00";
var mode = args[1];

var config = JsonNode.Parse(await File.ReadAllTextAsync(args[0]))!.AsObject();
if (config["AppEnvironment"]?.GetValue<string>() != "test")
    throw new InvalidOperationException("Only the test environment is allowed.");
var connectionString = config["ConnectionStrings"]?["SqlServer"]?.GetValue<string>()
    ?? throw new InvalidOperationException("Missing SQL connection string.");
var target = new SqlConnectionStringBuilder(connectionString);
if (target.DataSource != "172.16.110.16" || target.InitialCatalog != "PMR_SPC_TEST")
    throw new InvalidOperationException("Unexpected SQL target.");

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
await using var db = new AppDbContext(options);
var rows = await db.PartProcessCharacteristics.AsNoTracking()
    .Where(x => x.ControlScope == "CHEM" && x.IsEnabled)
    .OrderBy(x => x.Id)
    .Select(x => new
    {
        x.Id,
        Machine = x.Machine == null ? "" : x.Machine.MachineCode,
        Tank = x.Tank == null ? "" : x.Tank.TankName,
        Item = x.Characteristic == null ? "" : x.Characteristic.CharacteristicName,
        x.TargetValue,
        Config = x.ChemicalAnalysisConfigJson ?? "{}"
    }).ToListAsync();

var preview = new List<object>();
var candidates = new List<Candidate>();
var alignedIds = new List<int>();
var counts = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var row in rows)
{
    var root = JsonNode.Parse(row.Config)?.AsObject() ?? new JsonObject();
    var capacity = Decimal(root, "tankCapacityLiters");
    var addition = Decimal(root, "additionControlValue");
    var dilution = Decimal(root, "dilutionControlValue");
    var adjustment = FormulaParameterAlignment.AlignFormula(Text(root, "adjustmentFormula"), capacity, addition, dilution);
    var amount = FormulaParameterAlignment.AlignFormula(Text(root, "adjustmentAmountFormula"), capacity, addition, dilution);
    var blocked = new[] { adjustment, amount }.FirstOrDefault(x => x.Status is FormulaAlignmentStatus.MissingParameter or FormulaAlignmentStatus.Ambiguous);
    var status = blocked?.Status.ToString() ??
        (adjustment.Status == FormulaAlignmentStatus.Update || amount.Status == FormulaAlignmentStatus.Update ? "Update" :
         adjustment.Status == FormulaAlignmentStatus.NoFormula && amount.Status == FormulaAlignmentStatus.NoFormula ? "NoFormula" : "AlreadyAligned");
    counts[status] = counts.GetValueOrDefault(status) + 1;
    if (status == "AlreadyAligned") alignedIds.Add(row.Id);
    if (status == "Update")
    {
        var candidate = root.DeepClone().AsObject();
        candidate["adjustmentFormula"] = adjustment.Formula;
        candidate["adjustmentAmountFormula"] = amount.Formula;
        var calculationCandidate = candidate.DeepClone().AsObject();
        calculationCandidate["enabled"] = true;
        calculationCandidate["concentrationFormula"] = "Primary";
        foreach (var probe in ProbeValues(addition, dilution))
            _ = ChemicalFormulaCalculation.Calculate(calculationCandidate.ToJsonString(), probe, null, "[]", null, null,
                row.TargetValue.HasValue ? Convert.ToDecimal(row.TargetValue.Value) : null);
        candidates.Add(new Candidate(row.Id, row.Config, candidate.ToJsonString()));
    }
    if (status != "NoFormula" && status != "AlreadyAligned")
        preview.Add(new
        {
            row.Id, row.Machine, row.Tank, row.Item, capacity, addition, dilution, status,
            reason = blocked?.Reason,
            adjustment = new { before = Text(root, "adjustmentFormula"), after = adjustment.Formula, status = adjustment.Status.ToString(), adjustment.Reason },
            amount = new { before = Text(root, "adjustmentAmountFormula"), after = amount.Formula, status = amount.Status.ToString(), amount.Reason }
        });
}

var snapshot = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', rows.Select(x => $"{x.Id}|{x.Config}")))));
Console.WriteLine(JsonSerializer.Serialize(new { total = rows.Count, snapshot, counts, preview }, new JsonSerializerOptions { WriteIndented = true }));
if (mode == "plan") return;
if (mode == "verify" && candidates.Count != 0)
    throw new InvalidOperationException($"Alignment remains pending for {candidates.Count} rows.");
if (mode == "verify")
{
    if (alignedIds.Count != 91) throw new InvalidOperationException($"Expected 91 aligned rows, got {alignedIds.Count}.");
    var currentConfigs = rows.Where(x => alignedIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Config);
    var latestVersions = await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
        .Where(x => alignedIds.Contains(x.PartProcessCharacteristicId))
        .GroupBy(x => x.PartProcessCharacteristicId)
        .Select(group => group.OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.Id).First())
        .ToListAsync();
    if (latestVersions.Count != 91 || latestVersions.Any(x => currentConfigs[x.PartProcessCharacteristicId] != x.ConfigJson))
        throw new InvalidOperationException("Current config does not match the latest formula version.");
    var taskVersions = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().CountAsync(x => x.ChangedBy == TaskId);
    var taskAudits = await db.AuditEvents.AsNoTracking().CountAsync(x => x.CorrelationId == TaskId && x.EventCode == "configuration.formula.update");
    if (taskVersions != 91 || taskAudits != 91)
        throw new InvalidOperationException($"Expected 91 task versions/audits, got {taskVersions}/{taskAudits}.");
    var sample = JsonNode.Parse(currentConfigs[2425])!.AsObject();
    var sampleAdjustment = Text(sample, "adjustmentFormula") ?? "";
    var sampleAmount = Text(sample, "adjustmentAmountFormula") ?? "";
    if (!sampleAdjustment.Contains("Concentration<ADDITION_CONTROL_VALUE", StringComparison.Ordinal) ||
        !sampleAdjustment.Contains("Concentration>DILUTION_CONTROL_VALUE", StringComparison.Ordinal) ||
        sampleAmount.Split("TANK_CAPACITY_LITERS", StringSplitOptions.None).Length - 1 != 3 ||
        !sampleAmount.Contains("/1000", StringComparison.Ordinal) || sampleAmount.Contains("/TANK_CAPACITY_LITERS", StringComparison.Ordinal))
        throw new InvalidOperationException("PPC 2425 sample formula did not preserve the reviewed parameter contract.");
    Console.WriteLine($"verify: aligned=91; versions={taskVersions}; audits={taskAudits}; sample2425=passed; pending=0");
    return;
}
if (candidates.Count == 0)
{
    Console.WriteLine("apply: NoChange");
    return;
}
if (candidates.Count != 91 || snapshot != ExpectedBeforeSnapshot)
    throw new InvalidOperationException($"Reviewed preview drifted: updates={candidates.Count}; snapshot={snapshot}.");

await db.Database.OpenConnectionAsync();
var connection = db.Database.GetDbConnection();
Console.WriteLine($"backup confirmed: {VerifiedBackup}; confirmedAt={BackupConfirmedAt}");

var beforeSchedules = await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaScheduleRevisions ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)");
var beforeEffectivities = await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaEffectivities ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)");
var beforeF = await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)");
var beforeMeasurements = await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM VariableMeasurements ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)");
var beforeVersions = await db.ChemicalAnalysisFormulaVersions.LongCountAsync();
var beforeAudits = await db.AuditEvents.LongCountAsync();

await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
{
    await db.Database.ExecuteSqlRawAsync("SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE ControlScope='CHEM' AND IsEnabled=1");
    db.ChangeTracker.Clear();
    var lockedRows = await db.PartProcessCharacteristics.Where(x => candidates.Select(c => c.PpcId).Contains(x.Id)).ToDictionaryAsync(x => x.Id);
    if (lockedRows.Count != candidates.Count || lockedRows.Any(x => x.Value.ChemicalAnalysisConfigJson != candidates.Single(c => c.PpcId == x.Key).BeforeConfig))
        throw new InvalidOperationException("A reviewed formula changed concurrently.");
    var auditConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppEnvironment"] = "test" }).Build();
    var audit = new AuditEventWriter(db, options, auditConfiguration, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
    var versions = new ChemicalAnalysisFormulaVersionService(db);
    foreach (var candidate in candidates)
    {
        var mapping = lockedRows[candidate.PpcId];
        var result = await versions.RecordChangeAsync(candidate.PpcId, candidate.BeforeConfig, candidate.AfterConfig,
            TaskId, "添加／稀釋門檻與槽容改用調整參數頁具名參數", default);
        var version = result.Version ?? throw new InvalidOperationException($"PPC {candidate.PpcId} formula version was not created.");
        mapping.ChemicalAnalysisConfigJson = version.ConfigJson;
        mapping.UpdatedAt = DateTime.UtcNow;
        mapping.UpdatedBy = TaskId;
        await audit.StageAsync(new AuditEventDraft("configuration.formula.update", "Succeeded")
        {
            ReasonCode = "adjustment-parameter-link",
            PageKey = "chemical.analysis.overview",
            TargetType = "PartProcessCharacteristic",
            TargetId = candidate.PpcId.ToString(),
            CorrelationId = TaskId,
            HttpMethod = "PUT",
            HttpStatus = 200,
            Details = new Dictionary<string, object?> { ["VersionId"] = version.Id, ["ChangedFields"] = new[] { "adjustmentFormula", "adjustmentAmountFormula" } }
        }, AuditActor.Service(TaskId));
        await db.SaveChangesAsync();
    }
    await transaction.CommitAsync();
}

db.ChangeTracker.Clear();
var versionDelta = await db.ChemicalAnalysisFormulaVersions.LongCountAsync() - beforeVersions;
var auditDelta = await db.AuditEvents.LongCountAsync() - beforeAudits;
if (versionDelta != 91 || auditDelta != 91) throw new InvalidOperationException($"Expected 91 versions/audits, got {versionDelta}/{auditDelta}.");
if (beforeSchedules != await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaScheduleRevisions ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)") ||
    beforeEffectivities != await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaEffectivities ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)") ||
    beforeF != await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)") ||
    beforeMeasurements != await Fingerprint("SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM VariableMeasurements ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)"))
    throw new InvalidOperationException("Protected schedule, F, or measurement data changed.");
Console.WriteLine($"applied: updates=91; versions=+{versionDelta}; audits=+{auditDelta}; protected=unchanged");

async Task<string> Fingerprint(string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.CommandTimeout = 300;
    return Convert.ToString(await command.ExecuteScalarAsync())!;
}

static string? Text(JsonObject root, string name) => root[name]?.GetValue<string>();
static decimal? Decimal(JsonObject root, string name) => root[name] is null ? null : root[name]!.GetValue<decimal?>();
static IEnumerable<string> ProbeValues(decimal? addition, decimal? dilution)
{
    if (addition.HasValue) yield return ChemicalFormulaCalculation.Canonical(addition.Value * 0.99m);
    if (addition.HasValue && dilution.HasValue) yield return ChemicalFormulaCalculation.Canonical((addition.Value + dilution.Value) / 2m);
    if (dilution.HasValue) yield return ChemicalFormulaCalculation.Canonical(dilution.Value * 1.01m);
}
sealed record Candidate(int PpcId, string BeforeConfig, string AfterConfig);
