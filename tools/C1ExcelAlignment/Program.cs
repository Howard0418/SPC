using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using C1ExcelAlignment;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

if (args.Length != 3 || args[2] is not ("plan" or "apply" or "verify"))
    throw new ArgumentException("TEST_CONFIG EVIDENCE_DIR plan|apply|verify");

const string TaskId = "SPC-CHEM-C1-EXCEL-ALIGN-1113-TASK-001";
const string Actor = "ihao_ting";
const long FHistoryId = 247;
var cutoffUtc = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
const string MeasuredAt = "2026-11-12T12:00:00+08:00";
var definitions = new[]
{
    new Definition(2551, 277, 94098, "鍍銅1", "硫酸", "7.10", "172.2105", "172.211", "添加", "硫酸(50%)：20.7 L", true),
    new Definition(2530, 278, 94099, "鍍銅1", "硫酸銅", "8.50", "53.125", "53.125", "添加", "氧化銅粉：3.6 Kg", false),
    new Definition(2531, 279, 94100, "鍍銅1", "氯離子", "0.198", "30.096", "30.096", "添加", "鹽酸38%：10.2 ml", false),
    new Definition(2553, 280, 94101, "鍍銅2", "硫酸", "6.30", "152.8065", "152.807", "", "", false),
    new Definition(2533, 281, 94102, "鍍銅2", "硫酸銅", "19.60", "122.5", "122.5", "", "", false),
    new Definition(2534, 282, 94103, "鍍銅2", "氯離子", "0.22", "33.44", "33.44", "添加", "鹽酸(38%)：15.8 ml", false)
};
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
static bool JsonEquals(string left, string right) => JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));

async Task<string> QueryHash(string sql)
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = sql;
    command.CommandTimeout = 180;
    return Convert.ToString(await command.ExecuteScalarAsync())!;
}

async Task<Dictionary<string, string>> ProtectedFingerprints()
{
    const string ppcs = "2530,2531,2533,2534,2551,2553";
    const string measurements = "94098,94099,94100,94101,94102,94103";
    var queries = new Dictionary<string, string>
    {
        ["OtherMeasurements"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM VariableMeasurements WHERE Id NOT IN ({measurements}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherReceipts"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalCalculationReceipts WHERE PartProcessCharacteristicId NOT IN ({ppcs}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherTraces"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalCalculationTraces WHERE VariableMeasurementId NOT IN ({measurements}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherFormulas"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalAnalysisFormulaVersions WHERE PartProcessCharacteristicId NOT IN ({ppcs}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["F"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["OtherPpc"] = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM PartProcessCharacteristics WHERE Id NOT IN ({ppcs}) ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)",
        ["Limits"] = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM ControlLimitSegments ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2)"
    };
    var result = new Dictionary<string, string>();
    foreach (var query in queries) result[query.Key] = await QueryHash(query.Value);
    return result;
}

async Task<IReadOnlyList<ItemState>> ReadState()
{
    db.ChangeTracker.Clear();
    var states = new List<ItemState>();
    foreach (var definition in definitions)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .SingleAsync(x => x.Id == definition.PpcId);
        if (!mapping.IsEnabled || mapping.ControlScope != "CHEM" || mapping.Machine?.MachineCode != "C1" ||
            mapping.Tank?.TankName != definition.Tank || mapping.Characteristic?.CharacteristicName != definition.Characteristic)
            throw new InvalidOperationException($"PPC {definition.PpcId} identity changed.");
        var baseline = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x => x.Id == definition.OldFormulaId);
        var desired = AlignmentPlan.AlignConfig(baseline.ConfigJson, definition.TankOneSulfuric);
        var formulaApplied = JsonEquals(mapping.ChemicalAnalysisConfigJson!, desired);
        if (!formulaApplied && !JsonEquals(mapping.ChemicalAnalysisConfigJson!, baseline.ConfigJson))
            throw new InvalidOperationException($"PPC {definition.PpcId} formula changed from reviewed source.");

        var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleAsync(x => x.PartProcessCharacteristicId == definition.PpcId);
        var revision = await db.ChemicalFormulaScheduleRevisions.AsNoTracking().SingleAsync(x => x.Id == policy.ActiveScheduleRevisionId);
        var effects = await db.ChemicalFormulaEffectivities.AsNoTracking()
            .Where(x => x.ScheduleRevisionId == revision.Id).OrderBy(x => x.EffectiveFromUtc).ThenBy(x => x.Id).ToListAsync();
        var slices = effects.Select(x => new FormulaSlice(DateTime.SpecifyKind(x.EffectiveFromUtc, DateTimeKind.Utc),
            x.EffectiveUntilUtcExclusive.HasValue ? DateTime.SpecifyKind(x.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc) : null,
            x.FormulaVersionId, x.FTableHistoryId)).ToList();
        var activeSlice = slices.SingleOrDefault(x => x.FromUtc == cutoffUtc && x.UntilUtc is null && x.FId == FHistoryId);
        ChemicalAnalysisFormulaVersion activeFormula;
        bool scheduleApplied;
        if (formulaApplied)
        {
            if (activeSlice is null) throw new InvalidOperationException($"PPC {definition.PpcId} corrected schedule is missing.");
            activeFormula = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x => x.Id == activeSlice.FormulaId);
            scheduleApplied = JsonEquals(activeFormula.ConfigJson, desired);
        }
        else
        {
            _ = AlignmentPlan.SplitAt(slices, cutoffUtc, definition.OldFormulaId, FHistoryId, long.MaxValue);
            activeFormula = baseline;
            scheduleApplied = false;
        }

        var measurement = await db.VariableMeasurements.AsNoTracking().SingleAsync(x => x.Id == definition.MeasurementId);
        var traces = await db.ChemicalCalculationTraces.AsNoTracking().Where(x => x.VariableMeasurementId == definition.MeasurementId)
            .OrderBy(x => x.VersionNo).ThenBy(x => x.Id).ToListAsync();
        if (traces.Count is < 1 or > 2 || traces[0].VersionNo != 1)
            throw new InvalidOperationException($"Measurement {definition.MeasurementId} trace state changed.");
        var oldReceipt = await db.ChemicalCalculationReceipts.AsNoTracking().SingleAsync(x => x.Id == traces[0].ReceiptId);
        using var oldPayload = JsonDocument.Parse(oldReceipt.PayloadJson);
        var oldPrimary = oldPayload.RootElement.GetProperty("originalInput").GetProperty("Primary").GetString();
        var expectedOldPrimary = definition.PpcId == 2531 ? "0.20" : definition.Primary;
        if (oldPrimary != expectedOldPrimary || measurement.PortalDailyDate != new DateTime(2026, 11, 12) ||
            measurement.SamplingPhase != "OPEN" || measurement.SamplingStage != "GENERAL")
            throw new InvalidOperationException($"Measurement {definition.MeasurementId} source changed.");
        var corrected = formulaApplied && scheduleApplied && traces.Count == 2 && traces[1].VersionNo == 2 &&
            measurement.ChemicalReceiptId == traces[1].ReceiptId && Math.Abs(measurement.MeasuredValue - definition.ExpectedDouble) < 0.0000001;
        if (!corrected && traces.Count != 1)
            throw new InvalidOperationException($"Measurement {definition.MeasurementId} correction is partial.");
        if (formulaApplied != scheduleApplied || formulaApplied != corrected)
            throw new InvalidOperationException($"PPC {definition.PpcId} formula, schedule and measurement states differ.");
        states.Add(new(definition.PpcId, activeFormula.Id, activeFormula.VersionNo, revision.Id, formulaApplied, measurement.MeasuredValue,
            measurement.ChemicalReceiptId, traces.Count, baseline.ConfigJson, oldReceipt.Id, Hash(Serialize(oldReceipt)), Hash(Serialize(traces[0]))));
    }
    return states;
}

async Task VerifyApplied()
{
    var states = await ReadState();
    if (states.Any(x => !x.Applied || x.TraceCount != 2)) throw new InvalidOperationException("Not all corrections are applied.");
    foreach (var definition in definitions)
    {
        var input = new ChemicalPreviewInput(MeasuredAt, definition.Primary, null,
            $"PortalDaily|{definition.PpcId}|20261112|OPEN|GENERAL", "Initial");
        var preview = await Schedule().PreviewAsync(definition.PpcId, input, Actor, default);
        if (preview.DecimalPlaces != 3 || preview.FTableHistoryId != FHistoryId ||
            preview.Result.Raw != definition.Raw || preview.Result.Stored != definition.Stored ||
            preview.Result.Adjustment != definition.Adjustment || preview.Result.AdjustmentAmount != definition.AdjustmentAmount)
            throw new InvalidOperationException($"PPC {definition.PpcId} preview differs from Excel: {Serialize(preview.Result)}");
        var row = await db.VariableMeasurements.AsNoTracking().SingleAsync(x => x.Id == definition.MeasurementId);
        var presentation = (await new ChemicalHistoryQueryService(db).GetAsync([row]))[definition.MeasurementId];
        if (Math.Abs(presentation.EffectiveValue - definition.ExpectedDouble) > 0.0000001 || presentation.Judgment != "OK")
            throw new InvalidOperationException($"PPC {definition.PpcId} history query differs from Excel.");
    }
}

var beforeProtected = await ProtectedFingerprints();
var initial = await ReadState();
var pending = initial.Count(x => !x.Applied);
if (pending is not (0 or 6)) throw new InvalidOperationException("Correction state is partial.");
var sourceHash = Hash(Serialize(initial));
var reviewedPath = Path.Combine(args[1], "reviewed-plan.json");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{args[2]}-before.json"), JsonSerializer.Serialize(new { TaskId, mode = args[2], pending, sourceHash, initial, beforeProtected }, json));
Console.WriteLine($"{args[2]}: pending={pending}; values={string.Join(',', initial.Select(x => x.Value.ToString("0.###", CultureInfo.InvariantCulture)))}");

if (args[2] == "plan")
{
    await File.WriteAllTextAsync(reviewedPath, JsonSerializer.Serialize(new { TaskId, sourceHash, initial }, json));
}
else if (args[2] == "apply" && pending == 6)
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
        var backup = directory.TrimEnd('\\') + $"\\PMR_SPC_TEST_C1_EXCEL_ALIGN_{stamp}.bak";
        command.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@path"; parameter.Value = backup; command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
        await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-backup.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", backup, copyOnly = true, checksum = true, verifyOnly = true }, json));
    }
    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    foreach (var definition in definitions)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={definition.PpcId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM VariableMeasurements WITH(UPDLOCK,HOLDLOCK) WHERE Id={definition.MeasurementId}");
    }
    if (Serialize(initial) != Serialize(await ReadState())) throw new InvalidOperationException("Source state changed concurrently.");

    foreach (var definition in definitions)
    {
        var mapping = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == definition.PpcId);
        var oldConfig = mapping.ChemicalAnalysisConfigJson!;
        var candidate = AlignmentPlan.AlignConfig(oldConfig, definition.TankOneSulfuric);
        var recorded = await new ChemicalAnalysisFormulaVersionService(db).RecordChangeAsync(definition.PpcId, oldConfig, candidate,
            Actor, TaskId + " / Excel三位精度與公式對齊", default);
        var newFormula = recorded.Version ?? throw new InvalidOperationException("New formula version was not created.");
        mapping.ChemicalAnalysisConfigJson = newFormula.ConfigJson;
        mapping.UpdatedAt = DateTime.UtcNow;
        mapping.UpdatedBy = Actor;

        var policy = await db.ChemicalFormulaPolicies.SingleAsync(x => x.PartProcessCharacteristicId == definition.PpcId);
        var oldRevisionId = policy.ActiveScheduleRevisionId!.Value;
        var effects = await db.ChemicalFormulaEffectivities.Where(x => x.ScheduleRevisionId == oldRevisionId)
            .OrderBy(x => x.EffectiveFromUtc).ThenBy(x => x.Id).ToListAsync();
        var split = AlignmentPlan.SplitAt(effects.Select(x => new FormulaSlice(DateTime.SpecifyKind(x.EffectiveFromUtc, DateTimeKind.Utc),
            x.EffectiveUntilUtcExclusive.HasValue ? DateTime.SpecifyKind(x.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc) : null,
            x.FormulaVersionId, x.FTableHistoryId)), cutoffUtc, definition.OldFormulaId, FHistoryId, newFormula.Id);
        var revisionNo = await db.ChemicalFormulaScheduleRevisions.Where(x => x.PartProcessCharacteristicId == definition.PpcId).MaxAsync(x => x.RevisionNo) + 1;
        var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = definition.PpcId, RevisionNo = revisionNo,
            CreatedAtUtc = DateTime.UtcNow, Actor = Actor, Reason = TaskId + " / 2026-11-12起與Excel三位結果一致" };
        db.Add(revision); await db.SaveChangesAsync();
        db.AddRange(split.Periods.Select(x => new ChemicalFormulaEffectivity { PartProcessCharacteristicId = definition.PpcId,
            ScheduleRevisionId = revision.Id, FormulaVersionId = x.FormulaId, FTableHistoryId = x.FId,
            EffectiveFromUtc = x.FromUtc, EffectiveUntilUtcExclusive = x.UntilUtc }));
        await db.SaveChangesAsync();
        revision.PublishedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        policy.ActiveScheduleRevisionId = revision.Id;
        await db.SaveChangesAsync();

        var input = new ChemicalPreviewInput(MeasuredAt, definition.Primary, null,
            $"PortalDaily|{definition.PpcId}|20261112|OPEN|GENERAL", "Initial");
        var preview = await Schedule().PreviewAsync(definition.PpcId, input, Actor, default);
        if (preview.Result.Raw != definition.Raw || preview.Result.Stored != definition.Stored || preview.DecimalPlaces != 3)
            throw new InvalidOperationException($"PPC {definition.PpcId} corrected preview differs from Excel.");
        var receipt = await Schedule().PrepareAsync(definition.PpcId, new(input, preview.Token), Actor, default);
        var result = ChemicalPortalMeasurementService.Result(receipt);
        var measurement = await db.VariableMeasurements.SingleAsync(x => x.Id == definition.MeasurementId);
        var before = new { measurement.MeasuredValue, measurement.AdjustAction, measurement.AdjustAmount, measurement.ChemicalReceiptId };
        measurement.MeasuredValue = double.Parse(result.Stored, CultureInfo.InvariantCulture);
        measurement.AdjustAction = string.IsNullOrEmpty(result.Adjustment) ? null : result.Adjustment;
        measurement.AdjustAmount = string.IsNullOrEmpty(result.AdjustmentAmount) ? null : result.AdjustmentAmount;
        measurement.ChemicalReceiptId = receipt.Id;
        measurement.ChemicalSourceKeyHash = receipt.SourceKeyHash;
        measurement.UpdatedAt = DateTime.UtcNow;
        measurement.UpdatedBy = Actor;
        db.Add(new ChemicalCalculationTrace { VariableMeasurementId = definition.MeasurementId, ReceiptId = receipt.Id, Phase = "Initial", VersionNo = 2,
            RawResultCanonical = result.Raw, StoredResultCanonical = result.Stored, CreatedAtUtc = DateTime.UtcNow });
        var nextRow = (await db.UploadDetails.Where(x => x.UploadBatchId == measurement.UploadBatchId).MaxAsync(x => (int?)x.RowNo) ?? 0) + 1;
        db.Add(new UploadDetail { UploadBatchId = measurement.UploadBatchId, RowNo = nextRow, IsValid = false, CreatedBy = Actor,
            PayloadJson = JsonSerializer.Serialize(new { action = "ControlledC1ExcelAlignment", taskId = TaskId, recordedAt = DateTime.UtcNow,
                measurementId = definition.MeasurementId, before, after = new { primary = definition.Primary, measuredValue = result.Stored,
                    adjustment = result.Adjustment, adjustmentAmount = result.AdjustmentAmount, receiptId = receipt.Id,
                    formulaVersionId = newFormula.Id, formulaVersionNo = newFormula.VersionNo } }) });
        await db.SaveChangesAsync();
    }

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
var remaining = final.Count(x => !x.Applied);
if (args[2] != "plan" && remaining != 0) throw new InvalidOperationException("Post-write verification failed.");
if (args[2] != "plan" && !beforeProtected.OrderBy(x => x.Key).SequenceEqual((await ProtectedFingerprints()).OrderBy(x => x.Key)))
    throw new InvalidOperationException("Protected data fingerprint changed.");
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var health = await http.GetStringAsync("https://spctest.pmr.com.tw/api/health");
var version = await http.GetStringAsync("https://spctest.pmr.com.tw/api/version");
await File.WriteAllTextAsync(Path.Combine(args[1], $"{stamp}-{args[2]}.json"), JsonSerializer.Serialize(new { TaskId, mode = args[2], pending, remaining,
    sourceHash, initial, final, protectedDataUnchanged = true, apiHealth = JsonSerializer.Deserialize<object>(health), apiVersion = JsonSerializer.Deserialize<object>(version) }, json));
Console.WriteLine($"{args[2]}: remaining={remaining}; values={string.Join(',', final.Select(x => x.Value.ToString("0.000", CultureInfo.InvariantCulture)))}; traces={string.Join(',', final.Select(x => x.TraceCount))}; protected=unchanged");

sealed record Definition(int PpcId, long OldFormulaId, long MeasurementId, string Tank, string Characteristic, string Primary,
    string Raw, string Stored, string Adjustment, string AdjustmentAmount, bool TankOneSulfuric)
{
    public double ExpectedDouble => double.Parse(Stored, CultureInfo.InvariantCulture);
}

sealed record ItemState(int PpcId, long FormulaId, int FormulaVersion, long RevisionId, bool Applied, double Value,
    Guid? ReceiptId, int TraceCount, string BaselineConfig, Guid OldReceiptId, string OldReceiptHash, string OldTraceHash);
