using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace OctoberProductionFormulaF;

internal static class Program
{
    private const string ManifestSha256 = "57fe52a02fae3127f596c1a3e2dc73f28d6e02109daf7592f7486a3aa168b49b";
    private const string ApiRoot = "https://pmr-spc-srv.pmr.com.tw:8443/api";
    private static readonly IReadOnlyDictionary<string, string> ReviewedInputHashes = new Dictionary<string, string>
    {
        ["primaryManifestSha256"] = "dc48a7e465c9bd800fddb4f29ae6c5e511edfbf2830b904c1ae6e03a14695f23",
        ["secondaryManifestSha256"] = "74645d538a4197209332a6f2c28277820f086761c54fef2e5055ca3fc7ae9018",
        ["sourceExtractSha256"] = "4b425f3fb3d1f3701a2e2b0359bcfe05880b506ec48d1c7dffc2d9b0320d5ecd",
        ["productionSnapshotSha256"] = "7fb60bd1fc5958b6700045a352e62366e35126072371d2d31080e0bd3aee243a",
        ["mappingSha256"] = "60b33ab7eba5eaede29003e31f42cc7d384043c11141ff81ffd91cc448ff6456"
    };
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--plan")
        {
            var (planManifest, localPlan, checks) = await LoadAndValidateManifestAsync(args[1], verifyFiles: false, default);
            File.WriteAllText(args[2], JsonSerializer.Serialize(new
            {
                mode = "PLAN_ONLY", planManifest.Schema, planManifest.Month,
                rows = planManifest.Rows.Count, localPlan.IncludedRowCount, localPlan.ExcludedRowCount,
                ppcs = localPlan.Periods.Select(x => x.PpcId).Distinct().Count(),
                formulas = localPlan.Formulas.Count, fTables = localPlan.FTables.Count,
                periods = localPlan.Periods.Count, conflicts = localPlan.Conflicts, calculationChecks = checks
            }, JsonOptions));
            Console.WriteLine($"PLAN: rows={planManifest.Rows.Count}; included={localPlan.IncludedRowCount}; excluded={localPlan.ExcludedRowCount}; periods={localPlan.Periods.Count}");
            return 0;
        }

        if (args.Length != 4 || args[3] is not ("--apply" or "--verify" or "--apply-reviewed" or "--verify-reviewed"))
            throw new ArgumentException("--plan MANIFEST REPORT | CONFIG MANIFEST EVIDENCE_DIR --apply|--verify|--apply-reviewed|--verify-reviewed");

        var configPath = args[0];
        var manifestPath = args[1];
        var evidenceDirectory = args[2];
        var mode = args[3];
        var reviewedEvidence = mode.EndsWith("-reviewed", StringComparison.Ordinal);
        var apply = mode.StartsWith("--apply", StringComparison.Ordinal);
        Directory.CreateDirectory(evidenceDirectory);
        var runId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var appEnvironment = config.RootElement.GetProperty("AppEnvironment").GetString();
        var rawConnection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()
            ?? throw new InvalidOperationException("Production SQL connection is missing.");
        var cs = new SqlConnectionStringBuilder(rawConnection.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var versionText = await http.GetStringAsync(ApiRoot + "/version");
        using var versionDocument = JsonDocument.Parse(versionText);
        var apiEnvironment = versionDocument.RootElement.GetProperty("environment").GetString();
        OctoberProductionFormulaFPlanner.ValidateTarget(appEnvironment, cs.DataSource, cs.InitialCatalog, apiEnvironment);
        var healthText = await http.GetStringAsync(ApiRoot + "/health");

        var (manifest, plan, calculationChecks) = await LoadAndValidateManifestAsync(manifestPath, verifyFiles: !reviewedEvidence, default);
        var actor = CurrentActor();
        const string reason = "SPC-CHEM-F-CONSISTENCY-1103-TASK-001-PROD-OCT / 已確認十月Excel公式與完整F快照；只追加歷史與未啟用日期排程";

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        await VerifyActualDatabaseAsync(db);
        await VerifyBusinessKeysAsync(db, manifest, default);

        var before = await CaptureStateAsync(db, default);
        var beforePath = Path.Combine(evidenceDirectory, $"{runId}-before.json");
        File.WriteAllText(beforePath, JsonSerializer.Serialize(new
        {
            environment = "production", database = "PMR_SPC_2026", manifestSha256 = ManifestSha256,
            apiVersion = JsonSerializer.Deserialize<object>(versionText),
            apiHealth = JsonSerializer.Deserialize<object>(healthText),
            plan = new { plan.IncludedRowCount, plan.ExcludedRowCount, periods = plan.Periods.Count, conflicts = plan.Conflicts },
            state = before
        }, JsonOptions));

        object? backup = null;
        if (apply) backup = await BackupAsync(cs.ConnectionString, evidenceDirectory, runId);

        ProductionFormulaFImportResult result;
        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var importer = new OctoberProductionFormulaFImporter(db, new EphemeralDataProtectionProvider());
            result = await importer.ApplyAsync(plan, actor, reason, default);
            var during = await CaptureProtectedAsync(db, default);
            if (!JsonSerializer.Serialize(before.Protected).Equals(JsonSerializer.Serialize(during), StringComparison.Ordinal))
                throw new InvalidOperationException("Protected production tables changed inside the formula/F transaction.");

            if (!apply)
            {
                if (result.TotalCreated != 0)
                    throw new InvalidOperationException($"Idempotence verification would create {result.TotalCreated} rows; transaction rolled back.");
                await transaction.RollbackAsync();
            }
            else
            {
                await transaction.CommitAsync();
            }
        }

        var after = await CaptureStateAsync(db, default);
        if (!JsonSerializer.Serialize(before.Protected).Equals(JsonSerializer.Serialize(after.Protected), StringComparison.Ordinal))
            throw new InvalidOperationException("Protected production fingerprints changed after formula/F processing.");
        if (after.ActiveF != before.ActiveF || after.CurrentPpc != before.CurrentPpc)
            throw new InvalidOperationException("Current PPC configuration or active F master changed.");

        var reportPath = Path.Combine(evidenceDirectory, $"{runId}-{mode.TrimStart('-')}.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            mode = apply ? "APPLIED" : "VERIFIED_NO_CHANGE",
            sourceVerification = reviewedEvidence ? "SHA256-pinned reviewed evidence; original UNC unavailable" : "live source files SHA256 verified",
            environment = "production", database = "PMR_SPC_2026", actor,
            manifestSha256 = ManifestSha256, calculationChecks,
            plan = new
            {
                sourceRows = manifest.Rows.Count, plan.IncludedRowCount, plan.ExcludedRowCount,
                ppcs = plan.Periods.Select(x => x.PpcId).Distinct().Count(),
                formulas = plan.Formulas.Count, fTables = plan.FTables.Count,
                periods = plan.Periods.Count, conflicts = plan.Conflicts
            },
            result, backup, before = before.Counts, after = after.Counts,
            protectedTablesUnchanged = true, currentPpcUnchanged = true, activeFUnchanged = true,
            measurementDataWritten = false
        }, JsonOptions));
        Console.WriteLine($"{(apply ? "APPLIED" : "NO_CHANGE")}: F={result.FHistoriesCreated}; formulas={result.FormulaVersionsCreated}; schedules={result.ScheduleRevisionsCreated}; periods={result.EffectivitiesCreated}; excluded={plan.ExcludedRowCount}");
        return 0;
    }

    private static async Task<(Manifest Manifest, ProductionFormulaFPlan Plan, int CalculationChecks)> LoadAndValidateManifestAsync(
        string path, bool verifyFiles, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (hash != ManifestSha256) throw new InvalidOperationException("Reviewed production formula/F manifest hash mismatch.");
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Manifest is invalid.");
        if (manifest.Schema != "SPC-OctFormulaF-Production-v1" || manifest.Month != "2026-10" || manifest.Rows.Count != 1211 || manifest.Files.Count != 35)
            throw new InvalidOperationException("Manifest scope does not match the reviewed October evidence.");
        if (ReviewedInputHashes.Any(x => !manifest.Inputs.TryGetValue(x.Key, out var value) || !value.Equals(x.Value, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Reviewed input evidence hashes do not match the approved October snapshot.");
        if (verifyFiles)
        {
            foreach (var file in manifest.Files)
            {
                await using var stream = File.OpenRead(file.Path);
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).ToLowerInvariant();
                if (!actual.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Source workbook changed after review: " + Path.GetFileName(file.Path));
            }
        }

        var rows = manifest.Rows.Select(row => new SourceScheduleRow(
            row.PpcId,
            row.BusinessKey.Text,
            DateOnly.ParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            row.Formula,
            row.FCells.ToDictionary(x => x.Key, x => decimal.Parse(x.Value, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase),
            row.SourceId)).ToList();
        var plan = OctoberProductionFormulaFPlanner.Build(rows);
        if (plan.IncludedRowCount != 1199 || plan.ExcludedRowCount != 12 || plan.Conflicts.Count != 6 ||
            plan.Periods.Count != 1039 || plan.Formulas.Count != 81 || plan.FTables.Count != 3 ||
            plan.Periods.Select(x => x.PpcId).Distinct().Count() != 80)
            throw new InvalidOperationException($"Recomputed plan does not match the reviewed October scope: included={plan.IncludedRowCount}, excluded={plan.ExcludedRowCount}, conflicts={plan.Conflicts.Count}, periods={plan.Periods.Count}, formulas={plan.Formulas.Count}, fTables={plan.FTables.Count}, ppcs={plan.Periods.Select(x => x.PpcId).Distinct().Count()}.");

        var checks = 0;
        foreach (var row in manifest.Rows)
        {
            var cells = row.FCells.Select(x => new ChemicalFTableHistoryCell("F!" + x.Key, null,
                decimal.Parse(x.Value, CultureInfo.InvariantCulture))).ToList();
            var lower = decimal.Parse(row.Lower, CultureInfo.InvariantCulture);
            var upper = decimal.Parse(row.Upper, CultureInfo.InvariantCulture);
            var calculated = ChemicalFormulaCalculation.Calculate(
                OctoberProductionFormulaFImporter.ConfigJson(row.Formula),
                row.Primary,
                string.IsNullOrWhiteSpace(row.Secondary) ? null : row.Secondary,
                JsonSerializer.Serialize(cells),
                lower, upper, (lower + upper) / 2m);
            var expected = decimal.Parse(row.Excel, CultureInfo.InvariantCulture);
            var actual = decimal.Parse(calculated.Raw, CultureInfo.InvariantCulture);
            if (Math.Abs(actual - expected) > 0.00000001m)
                throw new InvalidOperationException($"SPC/Excel mismatch at {row.SourceId}.");
            checks++;
        }
        return (manifest, plan, checks);
    }

    private static async Task VerifyBusinessKeysAsync(AppDbContext db, Manifest manifest, CancellationToken ct)
    {
        var expected = manifest.Rows.GroupBy(x => x.PpcId).ToDictionary(x => x.Key, x => x.Select(y => y.BusinessKey).Distinct().ToList());
        if (expected.Any(x => x.Value.Count != 1)) throw new InvalidOperationException("Manifest contains inconsistent business keys.");
        var ids = expected.Keys.ToArray();
        var actual = await db.PartProcessCharacteristics.AsNoTracking()
            .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var (id, values) in expected)
        {
            if (!actual.TryGetValue(id, out var item) || !item.IsEnabled ||
                !string.Equals(item.ControlScope, "CHEM", StringComparison.OrdinalIgnoreCase) ||
                item.Machine?.MachineCode != values[0].MachineCode || item.Tank?.TankCode != values[0].TankCode ||
                item.Characteristic?.CharacteristicCode != values[0].CharacteristicCode || item.Unit != values[0].Unit ||
                item.Tank?.TankName != values[0].TankName || item.Characteristic?.CharacteristicName != values[0].CharacteristicName)
                throw new InvalidOperationException($"Production business identity changed for PPC {id}.");
        }
    }

    private static async Task VerifyActualDatabaseAsync(AppDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != "PMR_SPC_2026" || reader.GetString(1) != "172.16.110.16")
            throw new InvalidOperationException("Actual production SQL target mismatch.");
    }

    private static async Task<object> BackupAsync(string connection, string evidenceDirectory, string runId)
    {
        await using var sql = new SqlConnection(connection);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
        var directory = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("SQL backup directory unavailable.");
        var path = directory.TrimEnd('\\', '/') + $"\\PMR_SPC_2026_pre_oct_formula_f_{runId}.bak";
        command.CommandTimeout = 300;
        command.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT";
        command.Parameters.AddWithValue("@path", path);
        await command.ExecuteNonQueryAsync();
        command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
        await command.ExecuteNonQueryAsync();
        var proof = new { server = "172.16.110.16", database = "PMR_SPC_2026", path, checksum = true, verifyOnly = true };
        File.WriteAllText(Path.Combine(evidenceDirectory, $"{runId}-backup.json"), JsonSerializer.Serialize(proof, JsonOptions));
        return proof;
    }

    private static async Task<StateSnapshot> CaptureStateAsync(AppDbContext db, CancellationToken ct)
    {
        var protectedTables = await CaptureProtectedAsync(db, ct);
        var counts = new Dictionary<string, long>();
        foreach (var table in new[] { "ChemicalFTableVersionHistories", "ChemicalAnalysisFormulaVersions", "ChemicalFormulaPolicies", "ChemicalFormulaScheduleRevisions", "ChemicalFormulaEffectivities" })
            counts[table] = await CountAsync(db, table, ct);
        var activeF = await FingerprintAsync(db, "ChemicalFTableVersions", "WHERE IsActive=1", ct) + ":" +
                      await FingerprintAsync(db, "ChemicalFTableCells", "WHERE VersionId IN(SELECT Id FROM ChemicalFTableVersions WHERE IsActive=1)", ct);
        var currentPpc = await FingerprintAsync(db, "PartProcessCharacteristics", "", ct);
        return new(protectedTables, counts, activeF, currentPpc);
    }

    private static async Task<Dictionary<string, string>> CaptureProtectedAsync(AppDbContext db, CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in new[]
                 {
                     "PartProcessCharacteristics", "ChemicalFTableVersions", "ChemicalFTableCells", "VariableMeasurements",
                     "UploadBatches", "UploadDetails", "ChemicalCalculationReceipts", "ChemicalCalculationTraces",
                     "ChemicalStatisticsJobs", "AlertEvents"
                 })
            result[table] = await FingerprintAsync(db, table, "", ct);
        return result;
    }

    private static async Task<string> FingerprintAsync(AppDbContext db, string table, string where, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}] {where}";
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return reader.GetInt64(0).ToString(CultureInfo.InvariantCulture) + ":" +
               (reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1).ToString(CultureInfo.InvariantCulture));
    }

    private static async Task<long> CountAsync(AppDbContext db, string table, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.[{table}]";
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static string CurrentActor()
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows operator identity is required.");
        var identity = WindowsIdentity.GetCurrent().Name;
        if (string.IsNullOrWhiteSpace(identity)) throw new InvalidOperationException("Windows operator identity is unavailable.");
        var actor = identity.Split('\\').Last().Split('@').First();
        if (actor.Length is 0 or > 100) throw new InvalidOperationException("Windows operator identity is invalid.");
        return actor;
    }

    private sealed record Manifest(string Schema, string Month, Dictionary<string, string> Inputs, List<SourceFile> Files, List<ManifestRow> Rows);
    private sealed record SourceFile(string Path, string Sha256);
    private sealed record ManifestRow(
        int PpcId, BusinessKey BusinessKey, string Day, string Formula, Dictionary<string, string> FCells,
        Dictionary<string, string> FLabels, string SourceId, string SourceFile, string SourceFileHash,
        string Sheet, int SourceRow, string? SamplingPhase, string? SamplingStage,
        string Primary, string? Secondary, string Excel, string Lower, string Upper, string Origin);
    private sealed record BusinessKey(string MachineCode, string TankCode, string CharacteristicCode, string Unit, string TankName, string CharacteristicName)
    {
        public string Text => $"{MachineCode}|{TankCode}|{CharacteristicCode}|{Unit}";
    }
    private sealed record StateSnapshot(Dictionary<string, string> Protected, Dictionary<string, long> Counts, string ActiveF, string CurrentPpc);
}
