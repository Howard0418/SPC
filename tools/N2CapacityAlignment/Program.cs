using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Auditing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using N2CapacityAlignment;

if (args.Length != 2 || args[1] is not ("plan" or "apply" or "verify"))
    throw new ArgumentException("TEST_CONFIG plan|apply|verify");

const string TaskId = "SPC-CHEM-ADJUST-ALIGN-1120-TASK-004";
const string Actor = "SPC-CHEM-ADJUST-ALIGN-1120-TASK-004";
// The reviewed snapshot moved only because PPC 2587 was saved from dilution 23 to 22
// after the first preflight. That conflicting row is outside this seven-row plan and
// remains protected; no other drift was accepted.
const string ExpectedBeforeSnapshot = "FCE7DFFC1BCBAC559D4686CA2A9308EDFEC161E7DD1C564BD82CCC7A8B91170B";
var mode = args[1];

string LoadConnection(string path)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    if (document.RootElement.GetProperty("AppEnvironment").GetString() != "test")
        throw new InvalidOperationException("Only the test environment is allowed.");
    var raw = document.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!
        .Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.OrdinalIgnoreCase);
    var builder = new SqlConnectionStringBuilder(raw);
    if (builder.DataSource != "172.16.110.16" || builder.InitialCatalog != "PMR_SPC_TEST")
        throw new InvalidOperationException("Unexpected SQL target.");
    return builder.ConnectionString;
}

var connectionString = LoadConnection(args[0]);
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
await using var db = new AppDbContext(options);
await db.Database.OpenConnectionAsync();
await using (var command = db.Database.GetDbConnection().CreateCommand())
{
    command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16")
        throw new InvalidOperationException("Actual SQL target mismatch.");
}

static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
static bool JsonEquals(string left, string right) => JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));

async Task<string> QueryHash(string sql)
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = sql;
    command.CommandTimeout = 300;
    return Convert.ToString(await command.ExecuteScalarAsync())!;
}

async Task<string> ActiveSnapshot()
{
    var rows = await db.PartProcessCharacteristics.AsNoTracking()
        .Where(x => x.ControlScope == "CHEM" && x.IsEnabled && x.MachineId != null)
        .OrderBy(x => x.Id).Select(x => new { x.Id, Config = x.ChemicalAnalysisConfigJson ?? "" }).ToListAsync();
    return Hash(string.Join('\n', rows.Select(x => $"{x.Id}|{x.Config}")));
}

async Task<Dictionary<string, string>> ProtectedFingerprints()
{
    const string ids = "2512,2514,2515,2516,2519,2550,2594";
    var queries = new Dictionary<string, string>
    {
        ["OtherPpc"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM PartProcessCharacteristics WHERE Id NOT IN ({ids}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherFormulaVersions"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalAnalysisFormulaVersions WHERE PartProcessCharacteristicId NOT IN ({ids}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Policies"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaPolicies ORDER BY PartProcessCharacteristicId FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Revisions"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaScheduleRevisions ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Effectivities"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFormulaEffectivities ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["FHistory"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Measurements"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM VariableMeasurements ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)"
    };
    var result = new Dictionary<string, string>();
    foreach (var query in queries) result[query.Key] = await QueryHash(query.Value);
    return result;
}

async Task<List<ItemState>> ReadStates()
{
    db.ChangeTracker.Clear();
    var result = new List<ItemState>();
    foreach (var definition in AlignmentPlan.Definitions)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .SingleAsync(x => x.Id == definition.PpcId);
        if (!mapping.IsEnabled || mapping.ControlScope != "CHEM" || mapping.Machine?.MachineCode != definition.Machine ||
            mapping.Tank?.TankName != definition.Tank || mapping.Characteristic?.CharacteristicName != definition.Characteristic)
            throw new InvalidOperationException($"PPC {definition.PpcId} identity changed.");
        var aligned = AlignmentPlan.AlignConfig(mapping.ChemicalAnalysisConfigJson!, definition);
        var latest = await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == definition.PpcId).OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.Id).FirstAsync();
        result.Add(new(definition.PpcId, mapping.ChemicalAnalysisConfigJson!, aligned.ConfigJson, aligned.Changed,
            latest.Id, latest.VersionNo, latest.ConfigJson, definition.AfterFormulaHash));
    }
    return result;
}

void VerifyCalculations(IEnumerable<ItemState> states)
{
    foreach (var state in states)
    {
        var definition = AlignmentPlan.Definitions.Single(x => x.PpcId == state.PpcId);
        var root = JsonNode.Parse(state.CandidateConfig)!.AsObject();
        root["enabled"] = true;
        root["concentrationFormula"] = "Primary";
        var result = ChemicalFormulaCalculation.Calculate(root.ToJsonString(), definition.SampleConcentration, null, "[]", null, null, null);
        if (result.AdjustmentAmount != definition.ExpectedAdjustmentAmount)
            throw new InvalidOperationException($"PPC {state.PpcId} calculation differs: {result.AdjustmentAmount}");
    }
}

var initial = await ReadStates();
var pending = initial.Count(x => x.Pending);
if (pending is not (0 or 7)) throw new InvalidOperationException("Alignment state is partial.");
VerifyCalculations(initial);
Console.WriteLine($"{mode}: pending={pending}; snapshot={await ActiveSnapshot()}");
foreach (var state in initial) Console.WriteLine($"PPC {state.PpcId}: {(state.Pending ? "Update" : "NoChange")}; v{state.LatestVersionNo}; candidate={state.AfterFormulaHash[..12]}");

if (mode == "plan") return;

var beforeProtected = await ProtectedFingerprints();
var beforeVersionCount = await db.ChemicalAnalysisFormulaVersions.LongCountAsync();
var beforeAuditCount = await db.AuditEvents.LongCountAsync();

if (mode == "apply" && pending == 7)
{
    if (await ActiveSnapshot() != ExpectedBeforeSnapshot)
        throw new InvalidOperationException("Active CHEM snapshot changed from the reviewed before state.");
    await using (var command = db.Database.GetDbConnection().CreateCommand())
    {
        command.CommandTimeout = 600;
        command.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";
        var directory = Convert.ToString(await command.ExecuteScalarAsync())!;
        var stamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Taipei Standard Time").ToString("yyyyMMdd-HHmmss");
        var backup = directory.TrimEnd('\\') + $"\\PMR_SPC_TEST_N2_CAPACITY_{stamp}.bak";
        command.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@path"; parameter.Value = backup; command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"backup={backup}; verify=passed");
    }

    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    foreach (var definition in AlignmentPlan.Definitions)
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={definition.PpcId}");
    if ((await ActiveSnapshot()) != ExpectedBeforeSnapshot || (await ReadStates()).Count(x => x.Pending) != 7)
        throw new InvalidOperationException("Reviewed source changed concurrently.");

    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppEnvironment"] = "test" }).Build();
    var writer = new AuditEventWriter(db, options, configuration, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
    foreach (var definition in AlignmentPlan.Definitions)
    {
        var mapping = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == definition.PpcId);
        var previous = mapping.ChemicalAnalysisConfigJson!;
        var candidate = AlignmentPlan.AlignConfig(previous, definition);
        var recorded = await new ChemicalAnalysisFormulaVersionService(db).RecordChangeAsync(definition.PpcId, previous, candidate.ConfigJson,
            Actor, TaskId + " / 使用者核准採N2槽容", default);
        var version = recorded.Version ?? throw new InvalidOperationException($"PPC {definition.PpcId} version was not created.");
        mapping.ChemicalAnalysisConfigJson = version.ConfigJson;
        mapping.UpdatedAt = DateTime.UtcNow;
        mapping.UpdatedBy = Actor;
        await writer.StageAsync(new AuditEventDraft("configuration.formula.update", "Succeeded")
        {
            ReasonCode = "approved-matrix",
            PageKey = "chemical.analysis.overview",
            TargetType = "PartProcessCharacteristic",
            TargetId = definition.PpcId.ToString(),
            CorrelationId = TaskId,
            HttpMethod = "PUT",
            HttpStatus = 200,
            Details = new Dictionary<string, object?>
            {
                ["VersionId"] = version.Id,
                ["ChangedFields"] = new[] { "adjustmentAmountFormula" },
                ["CanonicalHash"] = Hash(version.ConfigJson)
            }
        }, AuditActor.Service(Actor));
        await db.SaveChangesAsync();
    }
    await transaction.CommitAsync();
}
else if (mode == "apply" && pending == 0)
{
    Console.WriteLine("apply: NoChange");
}
else if (mode == "verify" && pending != 0)
{
    throw new InvalidOperationException("Alignment remains pending.");
}

db.ChangeTracker.Clear();
var final = await ReadStates();
if (final.Any(x => x.Pending)) throw new InvalidOperationException("Post-apply state is incomplete.");
VerifyCalculations(final);
foreach (var state in final)
{
    var definition = AlignmentPlan.Definitions.Single(x => x.PpcId == state.PpcId);
    if (!JsonEquals(state.CurrentConfig, state.LatestVersionConfig) || state.LatestVersionNo <= initial.Single(x => x.PpcId == state.PpcId).LatestVersionNo)
        throw new InvalidOperationException($"PPC {state.PpcId} current config/version mismatch.");
    if (AlignmentPlan.Hash(JsonNode.Parse(state.CurrentConfig)!["adjustmentAmountFormula"]!.GetValue<string>()) != definition.AfterFormulaHash)
        throw new InvalidOperationException($"PPC {state.PpcId} formula hash mismatch.");
}
if (!beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
    throw new InvalidOperationException("Protected data fingerprint changed.");
var versionDelta = await db.ChemicalAnalysisFormulaVersions.LongCountAsync() - beforeVersionCount;
var auditDelta = await db.AuditEvents.LongCountAsync() - beforeAuditCount;
if (mode == "apply" && pending == 7 && (versionDelta != 7 || auditDelta != 7))
    throw new InvalidOperationException($"Expected seven versions/audits, got {versionDelta}/{auditDelta}.");
if (pending == 0 && (versionDelta != 0 || auditDelta != 0)) throw new InvalidOperationException("NoChange wrote data.");
Console.WriteLine($"verified: versions=+{versionDelta}; audits=+{auditDelta}; protected=unchanged; schedules=unchanged");

sealed record ItemState(int PpcId, string CurrentConfig, string CandidateConfig, bool Pending,
    long LatestVersionId, int LatestVersionNo, string LatestVersionConfig, string AfterFormulaHash);
