using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

if (args.Length != 3 || args[2] is not ("plan" or "apply" or "verify"))
    throw new ArgumentException("TEST_CONFIG EVIDENCE_DIR plan|apply|verify");

const string TaskId = "SPC-CHEM-N2-TIN-PRECISION-1113-TASK-001";
const int PpcId = 2518;
const long OldFormulaId = 796;
const int OldFormulaVersion = 3;
const long FHistoryId = 248;
const long OldRevisionId = 1741;
const long MeasurementId = 94084;
const string OldReceiptIdText = "461a0864-3f47-4bfe-a5fc-938b2b273ad2";
const string Actor = "ihao_ting";
var oldReceiptId = Guid.Parse(OldReceiptIdText);
var startUtc = new DateTime(2026, 11, 6, 16, 0, 0, DateTimeKind.Utc);
var measuredAt = "2026-11-12T12:00:00+08:00";
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
ChemicalFormulaScheduleService Schedule() => new(db, protector, TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
static string Serialize(object value) => JsonSerializer.Serialize(value);
static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

static int DecimalPlaces(string config)
{
    using var doc = JsonDocument.Parse(config);
    return doc.RootElement.GetProperty("decimalPlaces").GetInt32();
}

static string WithThreePlaces(string config)
{
    var root = JsonNode.Parse(config)?.AsObject() ?? throw new InvalidOperationException("Formula config is not an object.");
    root["decimalPlaces"] = 3;
    return root.ToJsonString();
}

static JsonNode WithoutPrecision(string config)
{
    var root = JsonNode.Parse(config)?.AsObject() ?? throw new InvalidOperationException("Formula config is not an object.");
    root.Remove("decimalPlaces");
    return root;
}

static bool EqualsWithoutPrecision(string left, string right) =>
    JsonNode.DeepEquals(WithoutPrecision(left), WithoutPrecision(right));

async Task<string> QueryHash(string sql)
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = sql;
    command.CommandTimeout = 120;
    return Convert.ToString(await command.ExecuteScalarAsync())!;
}

async Task<Dictionary<string, string>> ProtectedFingerprints()
{
    var queries = new Dictionary<string, string>
    {
        ["OtherMeasurements"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM VariableMeasurements WHERE Id<>94084 ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherReceipts"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalCalculationReceipts WHERE PartProcessCharacteristicId<>2518 ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherTraces"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalCalculationTraces WHERE VariableMeasurementId<>94084 ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherFormulas"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalAnalysisFormulaVersions WHERE PartProcessCharacteristicId<>2518 ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["F"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherPpc"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM PartProcessCharacteristics WHERE Id<>2518 ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Limits"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ControlLimitSegments ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Jobs"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalStatisticsJobs ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)"
    };
    var result = new Dictionary<string, string>();
    foreach (var item in queries) result[item.Key] = await QueryHash(item.Value);
    return result;
}

async Task<State> ReadState()
{
    db.ChangeTracker.Clear();
    var mapping = await db.PartProcessCharacteristics.AsNoTracking()
        .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic).SingleAsync(x => x.Id == PpcId);
    if (!mapping.IsEnabled || mapping.ControlScope != "CHEM" || mapping.Machine?.MachineCode != "N2" ||
        mapping.Tank?.TankName != "促進" || mapping.Characteristic?.CharacteristicCode != "CHAR-000827")
        throw new InvalidOperationException("PPC identity changed.");
    var formulas = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().Where(x => x.PartProcessCharacteristicId == PpcId)
        .OrderBy(x => x.VersionNo).ThenBy(x => x.Id).ToListAsync();
    var latest = formulas.Last();
    var appliedFormula = latest.VersionNo == 4 && DecimalPlaces(latest.ConfigJson) == 3;
    if (!appliedFormula && (latest.Id != OldFormulaId || latest.VersionNo != OldFormulaVersion || DecimalPlaces(latest.ConfigJson) != 2))
        throw new InvalidOperationException("Latest formula differs from reviewed v3 source.");
    var baseline = formulas.Single(x => x.Id == OldFormulaId && x.VersionNo == OldFormulaVersion);
    if (!EqualsWithoutPrecision(latest.ConfigJson, baseline.ConfigJson))
        throw new InvalidOperationException("Formula changed beyond decimal precision.");
    if (DecimalPlaces(mapping.ChemicalAnalysisConfigJson ?? "{}") != (appliedFormula ? 3 : 2) ||
        !EqualsWithoutPrecision(mapping.ChemicalAnalysisConfigJson ?? "{}", baseline.ConfigJson))
        throw new InvalidOperationException("Current PPC formula config differs from reviewed source.");
    var f = await db.ChemicalFTableVersionHistories.AsNoTracking().Include(x => x.FTableVersion).SingleAsync(x => x.Id == FHistoryId);
    using (var cells = JsonDocument.Parse(f.CellsJson))
    {
        var b9 = cells.RootElement.EnumerateArray().Single(x => x.GetProperty("CellAddress").GetString()!.Replace("$", "") == "F!B9");
        if (b9.GetProperty("NumericValue").GetDecimal() != 1m || f.FTableVersion is not { IsActive: true })
            throw new InvalidOperationException("F248/B9 changed.");
    }
    var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x => x.PartProcessCharacteristicId == PpcId);
    var revision = await db.ChemicalFormulaScheduleRevisions.AsNoTracking().SingleAsync(x => x.Id == policy.ActiveScheduleRevisionId);
    var effects = await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => x.PartProcessCharacteristicId == PpcId && x.ScheduleRevisionId == revision.Id)
        .OrderBy(x => x.EffectiveFromUtc).ThenBy(x => x.Id).ToListAsync();
    var slices = effects.Select(x => new Slice(DateTime.SpecifyKind(x.EffectiveFromUtc, DateTimeKind.Utc),
        x.EffectiveUntilUtcExclusive.HasValue ? DateTime.SpecifyKind(x.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc) : null,
        x.FormulaVersionId, x.FTableHistoryId)).ToList();
    bool scheduleNeedsCorrection;
    if (appliedFormula)
    {
        scheduleNeedsCorrection = SchedulePlan.CorrectCurrent(slices, startUtc, OldFormulaId, FHistoryId, latest.Id).Changed;
    }
    else
    {
        if (slices.LastOrDefault() != new Slice(startUtc, null, OldFormulaId, FHistoryId))
            throw new InvalidOperationException("Current effectiveness differs from reviewed source.");
        scheduleNeedsCorrection = true;
    }
    if (effects.Count != 29 || (!appliedFormula && revision.Id != OldRevisionId))
        throw new InvalidOperationException("Reviewed schedule changed.");
    var measurement = await db.VariableMeasurements.AsNoTracking().SingleAsync(x => x.Id == MeasurementId);
    var traces = await db.ChemicalCalculationTraces.AsNoTracking().Where(x => x.VariableMeasurementId == MeasurementId)
        .OrderBy(x => x.VersionNo).ThenBy(x => x.Id).ToListAsync();
    var oldReceipt = await db.ChemicalCalculationReceipts.AsNoTracking().SingleAsync(x => x.Id == oldReceiptId);
    if (oldReceipt.EffectivityId != 43803 || traces.FirstOrDefault()?.ReceiptId != oldReceiptId || traces.FirstOrDefault()?.VersionNo != 1)
        throw new InvalidOperationException("Original receipt or trace changed.");
    var corrected = appliedFormula && measurement.MeasuredValue == 0.226 && measurement.ChemicalReceiptId != oldReceiptId &&
        traces.Count == 2 && traces[^1].VersionNo == 2 && traces[^1].ReceiptId == measurement.ChemicalReceiptId;
    if (!corrected && (measurement.MeasuredValue != 0.23 || measurement.ChemicalReceiptId != oldReceiptId || traces.Count != 1))
        throw new InvalidOperationException("Measurement correction state is partial or changed.");
    if (appliedFormula != corrected || appliedFormula == scheduleNeedsCorrection)
        throw new InvalidOperationException("Formula, schedule and measurement states are inconsistent.");
    return new(latest.Id, latest.VersionNo, revision.Id, policy.IsEnabled, appliedFormula, scheduleNeedsCorrection,
        measurement.MeasuredValue, measurement.ChemicalReceiptId, traces.Count, oldReceipt.PayloadJson,
        mapping.ChemicalAnalysisConfigJson!, effects.Select(x => x.Id).ToList());
}

var beforeProtected = await ProtectedFingerprints();
var oldFormula = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x => x.Id == OldFormulaId);
var oldRevision = await db.ChemicalFormulaScheduleRevisions.AsNoTracking().SingleAsync(x => x.Id == OldRevisionId);
var oldEffects = await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => x.ScheduleRevisionId == OldRevisionId).OrderBy(x => x.Id).ToListAsync();
var oldReceiptSnapshot = await db.ChemicalCalculationReceipts.AsNoTracking().SingleAsync(x => x.Id == oldReceiptId);
var oldTrace = await db.ChemicalCalculationTraces.AsNoTracking().SingleAsync(x => x.VariableMeasurementId == MeasurementId && x.VersionNo == 1);
var initial = await ReadState();
var pending = initial.Applied ? 0 : 1;
var sourceHash = Hash(Serialize(initial));
var reviewedPath = Path.Combine(args[1], "reviewed-plan.json");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{mode}-before.json"), JsonSerializer.Serialize(new { TaskId, mode, pending, sourceHash, initial, beforeProtected }, json));
Console.WriteLine($"{mode}: pending={pending}; formula=v{initial.FormulaVersion}/{initial.FormulaId}; revision={initial.RevisionId}; value={initial.Value.ToString("0.###", CultureInfo.InvariantCulture)}");

async Task VerifyApplied()
{
    var state = await ReadState();
    if (!state.Applied || state.ScheduleChanged || state.Value != 0.226 || state.TraceCount != 2)
        throw new InvalidOperationException("Correction verification failed.");
    if (Serialize(oldFormula) != Serialize(await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x => x.Id == OldFormulaId)) ||
        Serialize(oldRevision) != Serialize(await db.ChemicalFormulaScheduleRevisions.AsNoTracking().SingleAsync(x => x.Id == OldRevisionId)) ||
        Serialize(oldEffects) != Serialize(await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => x.ScheduleRevisionId == OldRevisionId).OrderBy(x => x.Id).ToListAsync()) ||
        Serialize(oldReceiptSnapshot) != Serialize(await db.ChemicalCalculationReceipts.AsNoTracking().SingleAsync(x => x.Id == oldReceiptId)) ||
        Serialize(oldTrace) != Serialize(await db.ChemicalCalculationTraces.AsNoTracking().SingleAsync(x => x.Id == oldTrace.Id)))
        throw new InvalidOperationException("Original immutable evidence changed.");
    var row = await db.VariableMeasurements.AsNoTracking().SingleAsync(x => x.Id == MeasurementId);
    var presentation = (await new ChemicalHistoryQueryService(db).GetAsync([row]))[MeasurementId];
    if (presentation.EffectiveValue != 0.226 || presentation.InitialJudgment != "OK" || presentation.Judgment != "OK")
        throw new InvalidOperationException("History query does not show corrected effective value.");
    var input = new ChemicalPreviewInput(measuredAt, "0.10", null, "PortalDaily|2518|20261112|OPEN|OPEN", "Initial");
    var preview = await Schedule().PreviewAsync(PpcId, input, Actor, default);
    if (preview.FormulaVersionNo != 4 || preview.FTableHistoryId != FHistoryId || preview.DecimalPlaces != 3 ||
        preview.Result.Raw != "0.2256" || preview.Result.Stored != "0.226")
        throw new InvalidOperationException("Preview is not Excel-equivalent.");
}

if (mode == "plan")
{
    await File.WriteAllTextAsync(reviewedPath, JsonSerializer.Serialize(new { TaskId, sourceHash, initial }, json));
}
else if (mode == "apply" && pending == 1)
{
    if (!File.Exists(reviewedPath)) throw new InvalidOperationException("Reviewed plan is missing.");
    using (var reviewed = JsonDocument.Parse(await File.ReadAllTextAsync(reviewedPath)))
        if (reviewed.RootElement.GetProperty("sourceHash").GetString() != sourceHash) throw new InvalidOperationException("Reviewed plan changed.");
    await using (var command = db.Database.GetDbConnection().CreateCommand())
    {
        command.CommandTimeout = 600;
        command.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";
        var directory = Convert.ToString(await command.ExecuteScalarAsync())!;
        var backup = directory.TrimEnd('\\') + $"\\PMR_SPC_TEST_N2_TIN_PRECISION_{stamp}.bak";
        command.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@path"; parameter.Value = backup; command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
        await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-backup.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", backup, copyOnly = true, checksum = true, verifyOnly = true }, json));
    }
    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={PpcId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM VariableMeasurements WITH(UPDLOCK,HOLDLOCK) WHERE Id={MeasurementId}");
    if (Serialize(initial) != Serialize(await ReadState())) throw new InvalidOperationException("Source state changed concurrently.");
    var mapping = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == PpcId);
    var candidateConfig = WithThreePlaces(mapping.ChemicalAnalysisConfigJson!);
    var formulaResult = await new ChemicalAnalysisFormulaVersionService(db).RecordChangeAsync(PpcId, mapping.ChemicalAnalysisConfigJson,
        candidateConfig, Actor, TaskId + " / Excel三位結果一致：0.10→0.226", default);
    var newFormula = formulaResult.Version ?? throw new InvalidOperationException("New formula version was not created.");
    if (newFormula.VersionNo != 4 || DecimalPlaces(newFormula.ConfigJson) != 3 || !EqualsWithoutPrecision(newFormula.ConfigJson, oldFormula.ConfigJson))
        throw new InvalidOperationException("New formula version differs beyond precision.");
    mapping.ChemicalAnalysisConfigJson = newFormula.ConfigJson;
    mapping.UpdatedAt = DateTime.UtcNow;
    mapping.UpdatedBy = Actor;
    var currentEffects = await db.ChemicalFormulaEffectivities.Where(x => x.ScheduleRevisionId == OldRevisionId).OrderBy(x => x.EffectiveFromUtc).ThenBy(x => x.Id).ToListAsync();
    var corrected = SchedulePlan.CorrectCurrent(currentEffects.Select(x => new Slice(DateTime.SpecifyKind(x.EffectiveFromUtc, DateTimeKind.Utc),
        x.EffectiveUntilUtcExclusive.HasValue ? DateTime.SpecifyKind(x.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc) : null,
        x.FormulaVersionId, x.FTableHistoryId)), startUtc, OldFormulaId, FHistoryId, newFormula.Id);
    var revisionNo = await db.ChemicalFormulaScheduleRevisions.Where(x => x.PartProcessCharacteristicId == PpcId).MaxAsync(x => x.RevisionNo) + 1;
    var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = PpcId, RevisionNo = revisionNo,
        CreatedAtUtc = DateTime.UtcNow, Actor = Actor, Reason = TaskId + " / 修正兩位先取位造成0.230與Excel0.226不一致" };
    db.Add(revision); await db.SaveChangesAsync();
    db.AddRange(corrected.Periods.Select(x => new ChemicalFormulaEffectivity { PartProcessCharacteristicId = PpcId,
        ScheduleRevisionId = revision.Id, FormulaVersionId = x.FormulaId, FTableHistoryId = x.FId,
        EffectiveFromUtc = x.FromUtc, EffectiveUntilUtcExclusive = x.UntilUtc }));
    await db.SaveChangesAsync();
    revision.PublishedAtUtc = DateTime.UtcNow;
    var policy = await db.ChemicalFormulaPolicies.SingleAsync(x => x.PartProcessCharacteristicId == PpcId);
    policy.ActiveScheduleRevisionId = revision.Id;
    await db.SaveChangesAsync();
    var input = new ChemicalPreviewInput(measuredAt, "0.10", null, "PortalDaily|2518|20261112|OPEN|OPEN", "Initial");
    var preview = await Schedule().PreviewAsync(PpcId, input, Actor, default);
    if (preview.Result.Raw != "0.2256" || preview.Result.Stored != "0.226" || preview.DecimalPlaces != 3)
        throw new InvalidOperationException("Corrected preview differs from Excel.");
    var receipt = await Schedule().PrepareAsync(PpcId, new(input, preview.Token), Actor, default);
    var result = ChemicalPortalMeasurementService.Result(receipt);
    var measurement = await db.VariableMeasurements.SingleAsync(x => x.Id == MeasurementId);
    var before = new { measurement.MeasuredValue, measurement.ChemicalReceiptId, measurement.UpdatedAt, measurement.UpdatedBy };
    measurement.MeasuredValue = double.Parse(result.Stored, CultureInfo.InvariantCulture);
    measurement.ChemicalReceiptId = receipt.Id;
    measurement.ChemicalSourceKeyHash = receipt.SourceKeyHash;
    measurement.UpdatedAt = DateTime.UtcNow;
    measurement.UpdatedBy = Actor;
    db.Add(new ChemicalCalculationTrace { VariableMeasurementId = MeasurementId, ReceiptId = receipt.Id, Phase = "Initial", VersionNo = 2,
        RawResultCanonical = result.Raw, StoredResultCanonical = result.Stored, CreatedAtUtc = DateTime.UtcNow });
    var nextRow = (await db.UploadDetails.Where(x => x.UploadBatchId == measurement.UploadBatchId).MaxAsync(x => (int?)x.RowNo) ?? 0) + 1;
    db.Add(new UploadDetail { UploadBatchId = measurement.UploadBatchId, RowNo = nextRow, IsValid = false, CreatedBy = Actor,
        PayloadJson = JsonSerializer.Serialize(new { action = "ControlledMeasurementPrecisionCorrected", taskId = TaskId, recordedAt = DateTime.UtcNow,
            measurementId = MeasurementId, before, after = new { measuredValue = result.Stored, receiptId = receipt.Id, formulaVersionId = newFormula.Id, formulaVersionNo = newFormula.VersionNo } }) });
    await db.SaveChangesAsync();
    await VerifyApplied();
    if (!beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
        throw new InvalidOperationException("Protected data changed.");
    await transaction.CommitAsync();
}
else
{
    if (pending != 0) throw new InvalidOperationException("Correction remains pending.");
    await VerifyApplied();
}

db.ChangeTracker.Clear();
var final = await ReadState();
var remaining = final.Applied ? 0 : 1;
if (mode != "plan" && remaining != 0) throw new InvalidOperationException("Post-write verification failed.");
if (mode != "plan" && !beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
    throw new InvalidOperationException("Protected data fingerprint changed.");
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var health = await http.GetStringAsync("https://spctest.pmr.com.tw/api/health");
var version = await http.GetStringAsync("https://spctest.pmr.com.tw/api/version");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{mode}.json"), JsonSerializer.Serialize(new { TaskId, mode, pending, remaining, sourceHash, initial, final,
    protectedDataUnchanged = true, apiHealth = JsonSerializer.Deserialize<object>(health), apiVersion = JsonSerializer.Deserialize<object>(version) }, json));
Console.WriteLine($"{mode}: remaining={remaining}; formula=v{final.FormulaVersion}/{final.FormulaId}; revision={final.RevisionId}; value={final.Value.ToString("0.000", CultureInfo.InvariantCulture)}; traces={final.TraceCount}; protected=unchanged");

sealed record State(long FormulaId, int FormulaVersion, long RevisionId, bool PolicyEnabled, bool Applied, bool ScheduleChanged,
    double Value, Guid? ReceiptId, int TraceCount, string OriginalReceiptPayload, string CurrentConfig, List<long> EffectIds);
