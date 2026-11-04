using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ProductionChemicalMasterFormulaSync;

internal static class Program
{
    private const int ExpectedChanges = 87;
    private const int ExpectedCommonMappings = 113;
    private const int ExpectedProductionMappings = 114;
    private const string ApiRoot = "https://pmr-spc-srv.pmr.com.tw:8443/api";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 4 || args[3] is not ("--plan-reviewed" or "--apply-reviewed" or "--verify-reviewed"))
            throw new ArgumentException("TEST_CONFIG PROD_CONFIG EVIDENCE_DIR --plan-reviewed|--apply-reviewed|--verify-reviewed");
        var testConfig = LoadConfig(args[0]);
        var productionConfig = LoadConfig(args[1]);
        Directory.CreateDirectory(args[2]);
        var runId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var apply = args[3] == "--apply-reviewed";

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var versionText = await http.GetStringAsync(ApiRoot + "/version");
        var healthText = await http.GetStringAsync(ApiRoot + "/health");
        using var version = JsonDocument.Parse(versionText);
        var apiEnvironment = version.RootElement.GetProperty("environment").GetString() ?? "";
        ProductionChemicalMasterFormulaSyncPlanner.ValidateTestSource(testConfig.Environment, testConfig.Connection.DataSource, testConfig.Connection.InitialCatalog);
        ProductionChemicalMasterFormulaSyncPlanner.ValidateProductionTarget(productionConfig.Environment, productionConfig.Connection.DataSource, productionConfig.Connection.InitialCatalog, apiEnvironment);

        await using var testDb = CreateDb(testConfig.Connection.ConnectionString);
        await using var productionDb = CreateDb(productionConfig.Connection.ConnectionString);
        await testDb.Database.OpenConnectionAsync();
        await productionDb.Database.OpenConnectionAsync();
        await VerifyActualDatabaseAsync(testDb, "PMR_SPC_TEST", "172.16.110.16");
        await VerifyActualDatabaseAsync(productionDb, "PMR_SPC_2026", "172.16.110.16");
        var allSourceRows = await LoadRowsAsync(testDb, default);
        var allTargetRows = await LoadRowsAsync(productionDb, default);
        var productionIds = allTargetRows.Select(x => x.PpcId).ToHashSet();
        var sourceRows = allSourceRows.Where(x => productionIds.Contains(x.PpcId)).ToList();
        var commonIds = sourceRows.Select(x => x.PpcId).ToHashSet();
        var targetRows = allTargetRows.Where(x => commonIds.Contains(x.PpcId)).ToList();
        if (sourceRows.Any(source => targetRows.All(target => target.PpcId != source.PpcId || target.NaturalKey != source.NaturalKey)))
            throw new InvalidOperationException("A common PPC id has different business identity between test and production.");
        var plan = ProductionChemicalMasterFormulaSyncPlanner.Build(sourceRows, targetRows);
        if (sourceRows.Count != ExpectedCommonMappings || targetRows.Count != ExpectedCommonMappings || allTargetRows.Count != ExpectedProductionMappings)
            throw new InvalidOperationException($"Reviewed mapping count changed: common-test={sourceRows.Count}, common-production={targetRows.Count}, all-production={allTargetRows.Count}.");
        if (args[3] != "--verify-reviewed" && plan.Changes.Count != ExpectedChanges)
            throw new InvalidOperationException($"Reviewed difference count changed: expected {ExpectedChanges}, actual {plan.Changes.Count}.");
        var fCellsJson = await LoadActiveFCellsJsonAsync(productionDb, default);
        var calculationChecks = ValidateCalculations(plan.Changes, fCellsJson);
        var before = await CaptureStateAsync(productionDb, allTargetRows, default);
        File.WriteAllText(Path.Combine(args[2], $"{runId}-before.json"), JsonSerializer.Serialize(new
        {
            mode = args[3], sourceRows = sourceRows.Count, targetRows = targetRows.Count,
            changes = plan.Changes.Count, unchanged = plan.Unchanged.Count,
            sourceHash = HashRows(sourceRows), targetHash = HashRows(targetRows), calculationChecks,
            apiVersion = JsonSerializer.Deserialize<object>(versionText), apiHealth = JsonSerializer.Deserialize<object>(healthText), state = before
        }, JsonOptions));

        object? backup = null;
        if (apply)
        {
            backup = await BackupAsync(productionConfig.Connection.ConnectionString, args[2], runId);
            await ApplyAsync(productionDb, plan, CurrentActor(), default);
        }
        productionDb.ChangeTracker.Clear();
        var allAfterRows = await LoadRowsAsync(productionDb, default);
        var afterRows = allAfterRows.Where(x => commonIds.Contains(x.PpcId)).ToList();
        var postPlan = ProductionChemicalMasterFormulaSyncPlanner.Build(sourceRows, afterRows);
        var after = await CaptureStateAsync(productionDb, allAfterRows, default);
        if (apply)
        {
            if (postPlan.Changes.Count != 0 || postPlan.Unchanged.Count != ExpectedCommonMappings)
                throw new InvalidOperationException("Post-write production formulas do not exactly match test formulas.");
            if (after.FormulaVersionCount - before.FormulaVersionCount != ExpectedChanges)
                throw new InvalidOperationException("Formula version history count did not increase by exactly 87.");
            if (after.MeasurementFingerprint != before.MeasurementFingerprint || after.ReceiptFingerprint != before.ReceiptFingerprint
                || after.TraceFingerprint != before.TraceFingerprint || after.ActiveFFingerprint != before.ActiveFFingerprint
                || after.ScheduleFingerprint != before.ScheduleFingerprint || after.LimitFingerprint != before.LimitFingerprint)
                throw new InvalidOperationException("A protected production dataset changed.");
            ValidateOnlyAuthorizedChanges(allTargetRows, allAfterRows, plan);
        }
        else if (args[3] == "--verify-reviewed" && postPlan.Changes.Count != 0)
            throw new InvalidOperationException($"Verification found {postPlan.Changes.Count} remaining formula differences.");

        File.WriteAllText(Path.Combine(args[2], $"{runId}-{args[3].TrimStart('-')}.json"), JsonSerializer.Serialize(new
        {
            mode = apply ? "APPLIED" : args[3] == "--verify-reviewed" ? "VERIFIED" : "PLAN_ONLY",
            environment = "production", database = "PMR_SPC_2026", sourceDatabase = "PMR_SPC_TEST",
            requestedChanges = plan.Changes.Count, remainingChanges = postPlan.Changes.Count, unchangedAfter = postPlan.Unchanged.Count,
            calculationChecks, sourceHash = HashRows(sourceRows), beforeHash = HashRows(allTargetRows), afterHash = HashRows(allAfterRows),
            backup, before, after, protectedProductionDataUnchanged = apply,
            changedProperties = new[] { "concentrationFormula", "recheckConcentrationFormula" }
        }, JsonOptions));
        Console.WriteLine($"{(apply ? "APPLIED" : "VERIFIED")}: formulas={plan.Changes.Count}; remaining={postPlan.Changes.Count}; versions+={after.FormulaVersionCount - before.FormulaVersionCount}; calculations={calculationChecks}");
        return 0;
    }

    private static Config LoadConfig(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var environment = document.RootElement.GetProperty("AppEnvironment").GetString() ?? "";
        var raw = document.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString() ?? throw new InvalidOperationException("SqlServer connection missing.");
        return new(environment, new SqlConnectionStringBuilder(raw.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal)));
    }

    private static AppDbContext CreateDb(string connection) => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);

    private static async Task<List<ChemicalMasterFormulaRow>> LoadRowsAsync(AppDbContext db, CancellationToken ct) =>
        await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .Where(x => x.IsEnabled && x.ControlScope == "CHEM")
            .Select(x => new ChemicalMasterFormulaRow(x.Id,
                (x.Machine == null ? "" : x.Machine.MachineCode) + "|" + (x.Tank == null ? "" : x.Tank.TankCode) + "|" +
                (x.Characteristic == null ? "" : x.Characteristic.CharacteristicCode) + "|" + (x.Unit ?? ""),
                x.IsEnabled, x.ControlScope, x.ChemicalAnalysisConfigJson ?? "{}"))
            .ToListAsync(ct);

    private static async Task<string> LoadActiveFCellsJsonAsync(AppDbContext db, CancellationToken ct)
    {
        var active = await db.ChemicalFTableVersions.AsNoTracking().SingleAsync(x => x.IsActive, ct);
        var cells = await db.ChemicalFTableCells.AsNoTracking().Where(x => x.VersionId == active.Id).OrderBy(x => x.NormalizedCellAddress)
            .Select(x => new ChemicalFTableHistoryCell(x.NormalizedCellAddress, x.StandardSolution, x.NumericValue)).ToListAsync(ct);
        return JsonSerializer.Serialize(cells);
    }

    private static int ValidateCalculations(IEnumerable<ChemicalMasterFormulaChange> changes, string fCellsJson)
    {
        var count = 0;
        foreach (var change in changes)
        {
            var calculationConfig = JsonNode.Parse(change.MergedConfigJson)!.AsObject();
            calculationConfig.Remove("adjustmentFormula");
            calculationConfig.Remove("adjustmentAmountFormula");
            var formulaOnlyConfig = calculationConfig.ToJsonString();
            ChemicalFormulaCalculation.Calculate(formulaOnlyConfig, "2", "1", fCellsJson, 0m, 1000000m, 1m, "Initial"); count++;
            if (!string.IsNullOrWhiteSpace(change.RecheckConcentrationFormula))
            { ChemicalFormulaCalculation.Calculate(formulaOnlyConfig, "2", "1", fCellsJson, 0m, 1000000m, 1m, "Recheck"); count++; }
        }
        return count;
    }

    private static async Task ApplyAsync(AppDbContext db, ChemicalMasterFormulaSyncPlan plan, string actor, CancellationToken ct)
    {
        const string reason = "2026-11-04 使用者授權同步測試區87項藥液初驗／複驗濃度公式至正式主檔";
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var service = new ChemicalAnalysisFormulaVersionService(db);
        foreach (var change in plan.Changes.OrderBy(x => x.TargetPpcId))
        {
            var mapping = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == change.TargetPpcId, ct);
            if (!string.Equals(mapping.ChemicalAnalysisConfigJson, change.OriginalConfigJson, StringComparison.Ordinal))
                throw new InvalidOperationException($"Production config changed concurrently for {change.NaturalKey}.");
            mapping.ChemicalAnalysisConfigJson = change.MergedConfigJson; mapping.UpdatedAt = DateTime.UtcNow; mapping.UpdatedBy = actor;
            await service.RecordChangeAsync(mapping.Id, change.OriginalConfigJson, change.MergedConfigJson, actor, reason, ct);
        }
        await transaction.CommitAsync(ct);
    }

    private static void ValidateOnlyAuthorizedChanges(IEnumerable<ChemicalMasterFormulaRow> before, IEnumerable<ChemicalMasterFormulaRow> after, ChemicalMasterFormulaSyncPlan plan)
    {
        var b = before.ToDictionary(x => x.NaturalKey, StringComparer.Ordinal);
        var a = after.ToDictionary(x => x.NaturalKey, StringComparer.Ordinal);
        var c = plan.Changes.ToDictionary(x => x.NaturalKey, StringComparer.Ordinal);
        foreach (var key in b.Keys)
        {
            var expected = c.TryGetValue(key, out var change) ? change.MergedConfigJson : b[key].ConfigJson;
            using var e = JsonDocument.Parse(expected); using var actual = JsonDocument.Parse(a[key].ConfigJson);
            if (JsonSerializer.Serialize(e.RootElement) != JsonSerializer.Serialize(actual.RootElement))
                throw new InvalidOperationException($"Unexpected current-master change at {key}.");
        }
    }

    private static async Task VerifyActualDatabaseAsync(AppDbContext db, string database, string address)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != database || reader.GetString(1) != address)
            throw new InvalidOperationException($"Actual SQL target mismatch for {database}.");
    }

    private static async Task<StateSnapshot> CaptureStateAsync(AppDbContext db, IReadOnlyCollection<ChemicalMasterFormulaRow> rows, CancellationToken ct) => new(
        await CountAsync(db, "ChemicalAnalysisFormulaVersions", ct), await FingerprintAsync(db, "VariableMeasurements", ct),
        await FingerprintAsync(db, "ChemicalCalculationReceipts", ct), await FingerprintAsync(db, "ChemicalCalculationTraces", ct),
        await FingerprintAsync(db, "ChemicalFTableVersions", ct) + ":" + await FingerprintAsync(db, "ChemicalFTableCells", ct),
        await FingerprintAsync(db, "ChemicalFormulaScheduleRevisions", ct) + ":" + await FingerprintAsync(db, "ChemicalFormulaEffectivities", ct),
        await FingerprintAsync(db, "ControlLimitSegments", ct), HashRows(rows));

    private static async Task<string> FingerprintAsync(AppDbContext db, string table, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}]";
        await using var reader = await command.ExecuteReaderAsync(ct); await reader.ReadAsync(ct);
        return reader.GetInt64(0).ToString(CultureInfo.InvariantCulture) + ":" + (reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1).ToString(CultureInfo.InvariantCulture));
    }

    private static async Task<long> CountAsync(AppDbContext db, string table, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.[{table}]";
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static string HashRows(IEnumerable<ChemicalMasterFormulaRow> rows)
    {
        var text = string.Join("\n", rows.OrderBy(x => x.NaturalKey, StringComparer.Ordinal).Select(x => $"{x.NaturalKey}|{x.ConfigJson}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static async Task<object> BackupAsync(string connection, string evidenceDirectory, string runId)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var command = sql.CreateCommand();
        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
        var directory = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("SQL backup directory unavailable.");
        var path = directory.TrimEnd('\\', '/') + $"\\PMR_SPC_2026_pre_master_formula_sync_{runId}.bak";
        command.CommandTimeout = 300; command.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT"; command.Parameters.AddWithValue("@path", path);
        await command.ExecuteNonQueryAsync(); command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await command.ExecuteNonQueryAsync();
        var proof = new { server = "172.16.110.16", database = "PMR_SPC_2026", path, checksum = true, verifyOnly = true };
        File.WriteAllText(Path.Combine(evidenceDirectory, $"{runId}-backup.json"), JsonSerializer.Serialize(proof, JsonOptions)); return proof;
    }

    private static string CurrentActor()
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows operator identity required.");
        var actor = WindowsIdentity.GetCurrent().Name.Split('\\').Last().Split('@').First();
        if (actor.Length is 0 or > 100) throw new InvalidOperationException("Windows operator identity invalid."); return actor;
    }

    private sealed record Config(string Environment, SqlConnectionStringBuilder Connection);
    private sealed record StateSnapshot(long FormulaVersionCount, string MeasurementFingerprint, string ReceiptFingerprint,
        string TraceFingerprint, string ActiveFFingerprint, string ScheduleFingerprint, string LimitFingerprint, string CurrentMasterHash);
}
