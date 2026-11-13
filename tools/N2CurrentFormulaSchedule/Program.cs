using System.Data;
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Pt1CurrentSchedule;

if (args.Length is < 3 or > 4 || args[2] is not ("plan" or "apply" or "verify") ||
    (args.Length == 4 && args[3] is not ("chloride" or "p400")))
    throw new ArgumentException("TEST_CONFIG EVIDENCE_DIR plan|apply|verify [chloride|p400]");

const long FHistoryId = 248;
var startUtc = new DateTime(2026, 11, 6, 16, 0, 0, DateTimeKind.Utc);
var measuredAt = "2026-11-12T00:00:00+08:00";
var profile = args.Length == 4 ? args[3] : "chloride";
var taskId = profile == "p400"
    ? "SPC-CHEM-N2-P400-SCHEDULE-1113-TASK-001"
    : "SPC-CHEM-N2-CURRENT-SCHEDULE-1113-TASK-001";
var primary = profile == "p400" ? "0.17" : "1";
var expected = profile == "p400"
    ? new Dictionary<int, Expected> { [2515] = new("催化", "CHAR-000847", 1157, 4) }
    : new Dictionary<int, Expected>
    {
        [2516] = new("催化", "CHAR-000827", 1158, 4),
        [2518] = new("促進", "CHAR-000827", 796, 3)
    };
var mode = args[2];
var json = new JsonSerializerOptions { WriteIndented = true };
Directory.CreateDirectory(args[1]);
var stamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Taipei Standard Time").ToString("yyyyMMdd-HHmmss");

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
AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);
await using var db = Open();
await db.Database.OpenConnectionAsync();
await using (var command = db.Database.GetDbConnection().CreateCommand())
{
    command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16")
        throw new InvalidOperationException("Actual SQL target mismatch.");
}

var protector = new EphemeralDataProtectionProvider();
ChemicalFormulaScheduleService Service() => new(db, protector, TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);

async Task<List<PlanItem>> ReadPlan()
{
    db.ChangeTracker.Clear();
    var activeF = await db.ChemicalFTableVersionHistories.AsNoTracking()
        .Include(x => x.FTableVersion)
        .SingleAsync(x => x.Id == FHistoryId);
    if (activeF.FTableVersion is not { IsActive: true } || string.IsNullOrWhiteSpace(activeF.CellsJson))
        throw new InvalidOperationException("Approved active F history changed.");

    var result = new List<PlanItem>();
    foreach (var (id, identity) in expected)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .SingleAsync(x => x.Id == id);
        if (!mapping.IsEnabled || mapping.ControlScope != "CHEM" || mapping.Machine?.MachineCode != "N2" ||
            mapping.Tank?.TankName != identity.Tank || mapping.Characteristic?.CharacteristicCode != identity.CharacteristicCode)
            throw new InvalidOperationException($"PPC {id} business identity changed.");

        var formula = await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == id)
            .OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.Id)
            .FirstAsync();
        if (formula.Id != identity.FormulaId || formula.VersionNo != identity.VersionNo)
            throw new InvalidOperationException($"PPC {id} latest formula changed; review is required.");

        var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x => x.PartProcessCharacteristicId == id);
        if (policy.ActiveScheduleRevisionId is not long revisionId ||
            !await db.ChemicalFormulaScheduleRevisions.AsNoTracking().AnyAsync(x => x.Id == revisionId && x.PublishedAtUtc != null))
            throw new InvalidOperationException($"PPC {id} has no published active schedule.");
        var effects = await db.ChemicalFormulaEffectivities.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == id && x.ScheduleRevisionId == revisionId)
            .OrderBy(x => x.EffectiveFromUtc).ThenBy(x => x.Id).ToListAsync();
        var old = effects.Select(x => new Slice(DateTime.SpecifyKind(x.EffectiveFromUtc, DateTimeKind.Utc),
            x.EffectiveUntilUtcExclusive.HasValue ? DateTime.SpecifyKind(x.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc) : null,
            x.FormulaVersionId, x.FTableHistoryId)).ToList();
        var merged = SchedulePlan.Merge(old, startUtc, formula.Id, activeF.Id);
        if (merged.Changed && (old.Count != 28 || old.Max(x => x.UntilUtc) != startUtc))
            throw new InvalidOperationException($"PPC {id} reviewed schedule boundary changed.");
        result.Add(new(id, identity.Tank, revisionId, policy.IsEnabled, formula.Id, formula.VersionNo, activeF.Id, merged));
    }
    return result;
}

static string Serialize(object value) => JsonSerializer.Serialize(value);
static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

async Task<string> Fingerprint(string table)
{
    var allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "VariableMeasurements", "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalStatisticsJobs",
        "ChemicalAnalysisFormulaVersions", "ChemicalFTableVersions", "ChemicalFTableCells", "ChemicalFTableVersionHistories",
        "PartProcessCharacteristics", "ControlLimitSegments"
    };
    if (!allowed.Contains(table)) throw new InvalidOperationException("Table not allow-listed.");
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM dbo.[{table}] ORDER BY (SELECT NULL) FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)";
    command.CommandTimeout = 120;
    return Convert.ToString(await command.ExecuteScalarAsync())!;
}

async Task<Dictionary<string, string>> ProtectedFingerprints()
{
    var names = new[] { "VariableMeasurements", "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalStatisticsJobs",
        "ChemicalAnalysisFormulaVersions", "ChemicalFTableVersions", "ChemicalFTableCells", "ChemicalFTableVersionHistories",
        "PartProcessCharacteristics", "ControlLimitSegments" };
    var result = new Dictionary<string, string>();
    foreach (var name in names) result[name] = await Fingerprint(name);
    return result;
}

var initialPlan = await ReadPlan();
var pending = initialPlan.Count(x => x.Schedule.Changed);
if (pending != 0 && pending != expected.Count) throw new InvalidOperationException("Partial target state requires manual review.");
var sourceHash = Hash(Serialize(initialPlan));
var beforeProtected = await ProtectedFingerprints();
var oldRevisions = await db.ChemicalFormulaScheduleRevisions.AsNoTracking()
    .Where(x => expected.Keys.Contains(x.PartProcessCharacteristicId)).OrderBy(x => x.Id).ToListAsync();
var oldEffects = await db.ChemicalFormulaEffectivities.AsNoTracking()
    .Where(x => expected.Keys.Contains(x.PartProcessCharacteristicId)).OrderBy(x => x.Id).ToListAsync();
var oldPolicies = await db.ChemicalFormulaPolicies.AsNoTracking().OrderBy(x => x.PartProcessCharacteristicId).ToListAsync();
var maxRevisionId = oldRevisions.Max(x => x.Id);
var maxEffectId = oldEffects.Max(x => x.Id);

var reviewedPath = Path.Combine(args[1], "reviewed-plan.json");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{mode}-before.json"),
    JsonSerializer.Serialize(new { taskId, profile, mode, sourceHash, pending, initialPlan, beforeProtected }, json));
Console.WriteLine($"{mode}: pending={pending}; formulas={string.Join(',', initialPlan.Select(x => $"{x.Id}:v{x.VersionNo}/{x.FormulaId}"))}; F={FHistoryId}; start={startUtc:o}");

async Task Verify(bool expectApplied)
{
    db.ChangeTracker.Clear();
    foreach (var item in await ReadPlan())
    {
        if (expectApplied && item.Schedule.Changed) throw new InvalidOperationException("Current schedule is still missing.");
        var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x => x.PartProcessCharacteristicId == item.Id);
        var match = await db.ChemicalFormulaEffectivities.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == item.Id && x.ScheduleRevisionId == policy.ActiveScheduleRevisionId &&
                x.EffectiveFromUtc <= new DateTime(2026, 11, 11, 16, 0, 0) &&
                (x.EffectiveUntilUtcExclusive == null || new DateTime(2026, 11, 11, 16, 0, 0) < x.EffectiveUntilUtcExclusive))
            .SingleAsync();
        if (match.FormulaVersionId != item.FormulaId || match.FTableHistoryId != FHistoryId)
            throw new InvalidOperationException("Effective formula or F history mismatch.");
        var input = new ChemicalPreviewInput(measuredAt, primary, null, taskId + "-verify", "Initial");
        var preview = await Service().PreviewAsync(item.Id, input, Environment.UserName, default);
        if (preview.FormulaVersionId != item.FormulaId || preview.FTableHistoryId != FHistoryId)
            throw new InvalidOperationException("Preview did not use the expected latest version.");

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var ownedTransaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Prepare verification requires a transaction.");
        var savepoint = $"verify_prepare_{item.Id}";
        await transaction.CreateSavepointAsync(savepoint);
        try
        {
            var receipt = await Service().PrepareAsync(item.Id, new(input, preview.Token), Environment.UserName, default);
            if (receipt.PartProcessCharacteristicId != item.Id || receipt.EffectivityId != match.Id)
                throw new InvalidOperationException("Prepare did not use the expected schedule period.");
        }
        finally
        {
            await transaction.RollbackToSavepointAsync(savepoint);
            db.ChangeTracker.Clear();
            if (ownedTransaction is not null) await ownedTransaction.RollbackAsync();
        }
    }
}

if (mode == "plan")
{
    await File.WriteAllTextAsync(reviewedPath, JsonSerializer.Serialize(new { taskId, profile, sourceHash, initialPlan }, json));
}
else if (mode == "apply" && pending > 0)
{
    if (!File.Exists(reviewedPath)) throw new InvalidOperationException("Reviewed plan is missing.");
    using (var reviewed = JsonDocument.Parse(await File.ReadAllTextAsync(reviewedPath)))
        if (reviewed.RootElement.GetProperty("sourceHash").GetString() != sourceHash)
            throw new InvalidOperationException("Reviewed plan changed.");

    await using (var command = db.Database.GetDbConnection().CreateCommand())
    {
        command.CommandTimeout = 600;
        command.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";
        var directory = Convert.ToString(await command.ExecuteScalarAsync())!;
        var backup = directory.TrimEnd('\\') + $"\\PMR_SPC_TEST_N2_{profile.ToUpperInvariant()}_CURRENT_SCHEDULE_{stamp}.bak";
        command.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@path"; parameter.Value = backup; command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
        await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-backup.json"),
            JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", backup, copyOnly = true, checksum = true, verifyOnly = true }, json));
    }

    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    if (Serialize(initialPlan) != Serialize(await ReadPlan())) throw new InvalidOperationException("Schedule changed concurrently.");
    foreach (var item in initialPlan)
    {
        var currentPolicy = await db.ChemicalFormulaPolicies.SingleAsync(x => x.PartProcessCharacteristicId == item.Id);
        if (currentPolicy.ActiveScheduleRevisionId != item.RevisionId)
            throw new InvalidOperationException("Active schedule changed concurrently.");
        var revisionNo = (await db.ChemicalFormulaScheduleRevisions
            .Where(x => x.PartProcessCharacteristicId == item.Id).MaxAsync(x => (int?)x.RevisionNo) ?? 0) + 1;
        var revision = new ChemicalFormulaScheduleRevision
        {
            PartProcessCharacteristicId = item.Id,
            RevisionNo = revisionNo,
            CreatedAtUtc = DateTime.UtcNow,
            Actor = Environment.UserName,
            Reason = taskId + " / 使用者指定自2026-11-07起套用最近公式版本"
        };
        db.Add(revision);
        await db.SaveChangesAsync();
        db.AddRange(item.Schedule.Periods.Select(x => new ChemicalFormulaEffectivity
        {
            PartProcessCharacteristicId = item.Id,
            ScheduleRevisionId = revision.Id,
            FormulaVersionId = x.FormulaId,
            FTableHistoryId = x.FId,
            EffectiveFromUtc = x.FromUtc,
            EffectiveUntilUtcExclusive = x.UntilUtc
        }));
        await db.SaveChangesAsync();
        revision.PublishedAtUtc = DateTime.UtcNow;
        currentPolicy.ActiveScheduleRevisionId = revision.Id;
        await db.SaveChangesAsync();
    }
    db.ChangeTracker.Clear();
    await Verify(true);
    if (!beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
        throw new InvalidOperationException("Protected business data changed.");
    var preservedRevisions = await db.ChemicalFormulaScheduleRevisions.AsNoTracking()
        .Where(x => expected.Keys.Contains(x.PartProcessCharacteristicId) && x.Id <= maxRevisionId).OrderBy(x => x.Id).ToListAsync();
    var preservedEffects = await db.ChemicalFormulaEffectivities.AsNoTracking()
        .Where(x => expected.Keys.Contains(x.PartProcessCharacteristicId) && x.Id <= maxEffectId).OrderBy(x => x.Id).ToListAsync();
    if (Serialize(oldRevisions) != Serialize(preservedRevisions) || Serialize(oldEffects) != Serialize(preservedEffects))
        throw new InvalidOperationException("Existing schedule history changed.");
    var policies = await db.ChemicalFormulaPolicies.AsNoTracking().OrderBy(x => x.PartProcessCharacteristicId).ToListAsync();
    if (Serialize(oldPolicies.Where(x => !expected.ContainsKey(x.PartProcessCharacteristicId))) !=
        Serialize(policies.Where(x => !expected.ContainsKey(x.PartProcessCharacteristicId))) ||
        initialPlan.Any(x => policies.Single(p => p.PartProcessCharacteristicId == x.Id).IsEnabled != x.PolicyEnabled))
        throw new InvalidOperationException("Non-target policy or enabled flag changed.");
    await transaction.CommitAsync();
}
else
{
    if (pending != 0) throw new InvalidOperationException("Schedule remains pending; apply is required.");
    await Verify(true);
}

db.ChangeTracker.Clear();
var finalPlan = await ReadPlan();
var remaining = finalPlan.Count(x => x.Schedule.Changed);
if (mode != "plan" && remaining != 0) throw new InvalidOperationException("Post-write schedule verification failed.");
if (mode != "plan" && !beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
    throw new InvalidOperationException("Protected data fingerprint changed.");
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var health = await http.GetStringAsync("https://spctest.pmr.com.tw/api/health");
var version = await http.GetStringAsync("https://spctest.pmr.com.tw/api/version");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{mode}.json"),
    JsonSerializer.Serialize(new { taskId, profile, mode, pending, remaining, sourceHash, initialPlan, finalPlan, protectedDataUnchanged = true,
        apiHealth = JsonSerializer.Deserialize<object>(health), apiVersion = JsonSerializer.Deserialize<object>(version) }, json));
Console.WriteLine($"{mode}: remaining={remaining}; protected=unchanged; health={health}; version={version}");

sealed record Expected(string Tank, string CharacteristicCode, long FormulaId, int VersionNo);
sealed record PlanItem(int Id, string Tank, long RevisionId, bool PolicyEnabled, long FormulaId, int VersionNo, long FId, MergedSchedule Schedule);
