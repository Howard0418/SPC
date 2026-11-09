using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Enums;

namespace TestChemicalReimport;

internal static class Program
{
    private const string ApiRoot = "https://spctest.pmr.com.tw/api";
    private const string AdjustmentRepairReason = "2026-11-09 藥液日別調整公式補齊";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length >= 1 && args[0] is "--cleanup-inventory" or "--cleanup-apply")
            return await RunCleanupAsync(args);
        if (args.Length is not (4 or 5) || args[3] is not ("--plan" or "--apply" or "--verify" or "--repair-adjustment-plan" or "--repair-adjustment-apply" or "--repair-adjustment-verify"))
            throw new ArgumentException("PREVIEW_JSON TEST_APPSETTINGS EVIDENCE_DIR --plan|--apply|--verify|--repair-adjustment-plan|--repair-adjustment-apply|--repair-adjustment-verify [PRODUCTION_APPSETTINGS_FOR_TEST_BACKUP_ONLY]");
        var repairAdjustment = args[3].StartsWith("--repair-adjustment-", StringComparison.Ordinal);
        var operation = repairAdjustment ? args[3]["--repair-adjustment-".Length..] : args[3][2..];
        Directory.CreateDirectory(args[2]);
        var cfg = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[1])).Build();
        var connection = cfg.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Test SQL connection missing.");
        var cs = new SqlConnectionStringBuilder(connection.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.OrdinalIgnoreCase));
        if (cfg["AppEnvironment"] != "test" || cs.InitialCatalog != "PMR_SPC_TEST") throw new InvalidOperationException("Only PMR_SPC_TEST is permitted.");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var versionText = await http.GetStringAsync(ApiRoot + "/version");
        using (var version = JsonDocument.Parse(versionText))
            if (version.RootElement.GetProperty("environment").GetString() != "test") throw new InvalidOperationException("Test API environment guard failed.");

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        await VerifyDatabaseAsync(db);
        var source = Load(args[0]);
        if (source.Count != 3215) throw new InvalidOperationException($"Approved September onward scope changed: {source.Count}.");
        var masters = await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .Where(x => x.ControlScope == "CHEM").Select(x => new MasterRow(x.Id, x.Machine == null ? "" : x.Machine.MachineCode,
                x.Tank == null ? "" : x.Tank.TankCode, x.Tank == null ? "" : x.Tank.TankName,
                x.Characteristic == null ? "" : x.Characteristic.CharacteristicCode,
                x.Characteristic == null ? "" : x.Characteristic.CharacteristicName, x.Unit ?? "", x.IsEnabled,
                x.Process.ProcessCode, x.ChemicalAnalysisConfigJson ?? "{}")).ToListAsync();
        var identities = source.Select(x => new SourceIdentity(x.Line, x.Slot, x.AnalysisItem, x.SamplingStage, x.Day)).Distinct().ToArray();
        IReadOnlyDictionary<SourceIdentity, int> mapping;
        try { mapping = TestChemicalReimportPlanner.Resolve(identities, masters.Select(x => new MasterIdentity(x.PpcId, x.MachineCode, x.TankName, x.CharacteristicName, x.IsEnabled))); }
        catch (Exception ex)
        {
            var diagnostics = identities.Select(x => new { source = x, candidates = masters.Where(m => m.IsEnabled && TestChemicalReimportPlanner.LineMatches(x.Line, m.MachineCode) && TestChemicalReimportPlanner.Canon(m.TankName) == TestChemicalReimportPlanner.Canon(x.Slot) && TestChemicalReimportPlanner.CanonItem(m.CharacteristicName) == TestChemicalReimportPlanner.CanonItem(x.AnalysisItem)).Select(m => new { m.PpcId, m.MachineCode, m.TankName, m.CharacteristicName, m.Unit }) }).ToList();
            File.WriteAllText(Path.Combine(args[2], "mapping-diagnostics.json"), JsonSerializer.Serialize(new { error = ex.Message, diagnostics }, JsonOptions)); throw;
        }
        var masterById = masters.ToDictionary(x => x.PpcId);
        var rows = source.Select(x => new PlannedRow(x, mapping[new(x.Line, x.Slot, x.AnalysisItem, x.SamplingStage, x.Day)], masterById[mapping[new(x.Line, x.Slot, x.AnalysisItem, x.SamplingStage, x.Day)]])).ToList();
        var duplicate = rows.GroupBy(x => new { x.PpcId, x.Source.Day, Phase = TestChemicalReimportPlanner.Phase(x.Source.Shift), x.Source.SamplingStage }).FirstOrDefault(x => x.Count() != 1);
        if (duplicate is not null) throw new InvalidOperationException("Approved source has duplicate natural key: " + JsonSerializer.Serialize(duplicate.Key));
        var scheduleConflicts = rows.Where(x => x.Source.Initial.Mode == "FORMULA")
            .GroupBy(x => new { x.PpcId, x.Source.Day, x.Source.Shift })
            .Select(group => new { group.Key, Rows = group.ToList(), Variants = group.Select(ScheduleSignature).Distinct(StringComparer.Ordinal).ToList() })
            .Where(x => x.Variants.Count != 1).Select(x => new { x.Key.PpcId, x.Key.Day, sources = x.Rows.Select(r => new { r.Source.SourceFile, r.Source.Sheet, r.Source.SourceExcelRow, r.Source.Shift, r.Source.SamplingStage, signature = ScheduleSignature(r) }), variants = x.Variants }).ToList();
        if (scheduleConflicts.Count > 0)
        {
            File.WriteAllText(Path.Combine(args[2], "schedule-conflicts.json"), JsonSerializer.Serialize(scheduleConflicts, JsonOptions));
            throw new InvalidOperationException($"同一主檔／日期／班別存在 {scheduleConflicts.Count} 組公式或F衝突；禁止建立日期排程。");
        }

        var schedule = new ChemicalFormulaScheduleService(db, new EphemeralDataProtectionProvider(), TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
        await using var rehearsal = await db.Database.BeginTransactionAsync();
        var scheduleResult = await EnsureSchedulesAsync(db, rows, actor: CurrentActor(), default,
            repairAdjustment ? AdjustmentRepairReason : "2026-11-05 九月起測試區藥液日報重匯");
        var actor = CurrentActor(); var previewChecks = 0; var previewErrors = new List<object>();
        foreach (var row in rows)
        {
            if (row.Source.Initial.Mode == "DIRECT") continue;
            try
            {
                var input = Input(row, row.Source.Initial, "Initial");
                var preview = await schedule.PreviewAsync(row.PpcId, input, actor, default);
                AssertExcel(preview.Result.Raw, row.Source.Initial.Concentration, row.Source.SourceId); previewChecks++;
                if (row.Source.Recheck is not null)
                {
                    var recheck = await schedule.PreviewAsync(row.PpcId, Input(row, row.Source.Recheck, "Recheck"), actor, default);
                    AssertExcel(recheck.Result.Raw, row.Source.Recheck.Concentration, row.Source.SourceId + ":recheck"); previewChecks++;
                }
            }
            catch (Exception ex) { previewErrors.Add(new { row.Source.SourceFile, row.Source.Sheet, row.Source.SourceExcelRow, row.Source.Day, row.Source.Line, row.Source.Slot, row.Source.AnalysisItem, row.PpcId, error = ex.Message }); }
        }
        await rehearsal.RollbackAsync(); db.ChangeTracker.Clear();
        var state = await CaptureStateAsync(db);
        File.WriteAllText(Path.Combine(args[2], "preflight.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", sourceRows = rows.Count, mappings = rows.Select(x => x.PpcId).Distinct().Count(), previewChecks, previewErrorCount = previewErrors.Count, previewErrors, scheduleResult, rehearsalRolledBack = true, state, apiVersion = JsonSerializer.Deserialize<object>(versionText) }, JsonOptions));
        if (previewErrors.Count > 0) throw new InvalidOperationException($"公式／F日期預覽失敗 {previewErrors.Count} 筆；禁止寫入，詳見preflight.json。");
        if (operation == "plan") { Console.WriteLine($"PLAN: rows={rows.Count}; mappings={rows.Select(x => x.PpcId).Distinct().Count()}; previews={previewChecks}; active={state.ActiveMeasurements}"); return 0; }
        object? backup = null;
        if (operation == "apply")
        {
            var backupConnection = cs.ConnectionString;
            if (args.Length == 5)
            {
                var maintenanceCfg = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[4])).Build();
                var maintenance = new SqlConnectionStringBuilder((maintenanceCfg.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Maintenance SQL connection missing.")).Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.OrdinalIgnoreCase));
                if (maintenance.DataSource != "172.16.110.16") throw new InvalidOperationException("Maintenance server guard failed.");
                if (maintenanceCfg["AppEnvironment"] == "production" && maintenance.InitialCatalog == "PMR_SPC_2026")
                    maintenance.InitialCatalog = "PMR_SPC_TEST";
                else if (maintenanceCfg["AppEnvironment"] != "test" || maintenance.InitialCatalog != "PMR_SPC_TEST")
                    throw new InvalidOperationException("Maintenance target guard failed.");
                backupConnection = maintenance.ConnectionString;
            }
            backup = await BackupAsync(backupConnection, args[2]);
            if (repairAdjustment)
            {
                var before = await CaptureStateAsync(db);
                var existingRepairs = await db.ChemicalFormulaScheduleRevisions.CountAsync(x => x.Reason == AdjustmentRepairReason);
                object repairedSchedules = new { reusedExisting = existingRepairs };
                if (existingRepairs == 0)
                {
                    await using var repairTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    repairedSchedules = await EnsureSchedulesAsync(db, rows, actor, default, AdjustmentRepairReason);
                    await repairTx.CommitAsync();
                    db.ChangeTracker.Clear();
                }
                else if (existingRepairs != 82) throw new InvalidOperationException("Adjustment schedule repair revision count is incomplete.");
                var after = await CaptureStateAsync(db);
                if (before != after) throw new InvalidOperationException("Adjustment schedule repair changed protected measurement/F/master counts.");
                var repairRevisionCount = await db.ChemicalFormulaScheduleRevisions.CountAsync(x => x.Reason == AdjustmentRepairReason);
                if (repairRevisionCount != 82) throw new InvalidOperationException("Adjustment schedule repair idempotence failed.");
                File.WriteAllText(Path.Combine(args[2], "adjustment-repair-result.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", backup, repairedSchedules, protectedStateBefore = before, protectedStateAfter = after, idempotenceRevisionCount = repairRevisionCount, productionWritten = false }, JsonOptions));
                Console.WriteLine($"ADJUSTMENT REPAIR APPLIED: schedules=82; protectedMeasurements={after.ActiveMeasurements}");
                return 0;
            }
            var existingTaskRevisions = await db.ChemicalFormulaScheduleRevisions.CountAsync(x => x.Reason == "2026-11-05 九月起測試區藥液日報重匯");
            object appliedSchedules = new { reusedExisting = existingTaskRevisions };
            if (existingTaskRevisions == 0) { await using var scheduleTx = await db.Database.BeginTransactionAsync(); appliedSchedules = await EnsureSchedulesAsync(db, rows, actor, default); await scheduleTx.CommitAsync(); db.ChangeTracker.Clear(); }
            else if (existingTaskRevisions != 82) throw new InvalidOperationException("Existing task schedule revision count is incomplete.");
            var reactivated = await ReactivateAsync(db, rows, actor, default);
            db.ChangeTracker.Clear(); var verification = await VerifyMeasurementsAsync(db, rows, default);
            var second = await ReactivateAsync(db, rows, actor, default);
            if (second.Changed != 0 || second.AlreadyActive != rows.Count) throw new InvalidOperationException("Reactivation idempotence failed.");
            File.WriteAllText(Path.Combine(args[2], "apply-result.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", backup, appliedSchedules, sourceRows = rows.Count, reactivated, idempotence = second, verification, productionWritten = false }, JsonOptions));
            Console.WriteLine($"APPLIED: rows={verification.Measurements}; traces={verification.FormulaTraces}; reactivated={reactivated.Changed}; retryNew=0"); return 0;
        }
        if (repairAdjustment)
        {
            var repairRevisionCount = await db.ChemicalFormulaScheduleRevisions.CountAsync(x => x.Reason == AdjustmentRepairReason);
            if (repairRevisionCount != 82) throw new InvalidOperationException("Adjustment schedule repair verification failed.");
            File.WriteAllText(Path.Combine(args[2], "adjustment-repair-verify.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", sourceRows = rows.Count, previewChecks, previewErrorCount = previewErrors.Count, repairRevisionCount, protectedState = state, productionWritten = false }, JsonOptions));
            Console.WriteLine($"ADJUSTMENT REPAIR VERIFIED: schedules={repairRevisionCount}; previews={previewChecks}; errors=0");
            return 0;
        }
        var verified = await VerifyMeasurementsAsync(db, rows, default);
        File.WriteAllText(Path.Combine(args[2], "verify-result.json"), JsonSerializer.Serialize(new { database = "PMR_SPC_TEST", verified, productionWritten = false }, JsonOptions));
        Console.WriteLine($"VERIFIED: rows={verified.Measurements}; traces={verified.FormulaTraces}"); return 0;
    }

    private static async Task<int> RunCleanupAsync(string[] args)
    {
        var apply = args[0] == "--cleanup-apply";
        if (args.Length != (apply ? 4 : 3)) throw new ArgumentException("--cleanup-inventory TEST_APPSETTINGS EVIDENCE_DIR | --cleanup-apply TEST_APPSETTINGS EVIDENCE_DIR PRODUCTION_APPSETTINGS_FOR_TEST_BACKUP_ONLY");
        Directory.CreateDirectory(args[2]);
        var cfg = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[1])).Build();
        var raw = cfg.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Test SQL connection missing.");
        var cs = new SqlConnectionStringBuilder(raw.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.OrdinalIgnoreCase));
        if (cfg["AppEnvironment"] != "test" || cs.InitialCatalog != "PMR_SPC_TEST") throw new InvalidOperationException("Only PMR_SPC_TEST is permitted.");
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.OpenConnectionAsync(); await VerifyDatabaseAsync(db);
        var before = await CleanupInventoryAsync(db);
        File.WriteAllText(Path.Combine(args[2], "cleanup-inventory-before.json"), JsonSerializer.Serialize(before, JsonOptions));
        if (!apply) { Console.WriteLine($"CLEANUP INVENTORY: september={before.SeptemberActive}; isolatedPpcs={before.IsolatedPpcs.Count}; isolatedMeasurements={before.IsolatedActiveMeasurements}; testProcesses={before.TestProcesses.Count}; testCharacteristics={before.TestCharacteristics.Count}"); return 0; }
        var maintenanceCfg = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[3])).Build();
        var maintenance = new SqlConnectionStringBuilder((maintenanceCfg.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Maintenance SQL connection missing.")).Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.OrdinalIgnoreCase));
        if (maintenanceCfg["AppEnvironment"] != "production" || maintenance.InitialCatalog != "PMR_SPC_2026" || maintenance.DataSource != "172.16.110.16") throw new InvalidOperationException("Maintenance source guard failed.");
        maintenance.InitialCatalog = "PMR_SPC_TEST";
        var backup = await BackupAsync(maintenance.ConnectionString, args[2]);
        var targetPpcs = before.IsolatedPpcs.Select(x => x.Id).ToArray();
        var start = new DateTime(2026, 9, 1); var end = new DateTime(2026, 10, 1); var now = DateTime.UtcNow; var actor = CurrentActor();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var archivedMeasurements = await db.VariableMeasurements.Where(x => !x.IsDeleted &&
            (((x.PortalDailyDate ?? x.MeasuredAt) >= start && (x.PortalDailyDate ?? x.MeasuredAt) < end) || targetPpcs.Contains(x.PartProcessCharacteristicId)))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
        var archivedPpcs = targetPpcs.Length == 0 ? 0 : await db.PartProcessCharacteristics.Where(x => targetPpcs.Contains(x.Id) && !x.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.IsEnabled, false).SetProperty(x => x.UpdatedAt, now).SetProperty(x => x.UpdatedBy, actor));
        var blocked = before.TestProcesses.Concat(before.TestCharacteristics).Where(x => x.ActiveReferences != 0).ToList();
        if (blocked.Count != 0) throw new InvalidOperationException("Explicit test master is still referenced by active PPC: " + JsonSerializer.Serialize(blocked));
        var testProcessIds = before.TestProcesses.Select(x => x.Id).ToArray();
        var testCharacteristicIds = before.TestCharacteristics.Select(x => x.Id).ToArray();
        var archivedProcesses = testProcessIds.Length == 0 ? 0 : await db.Processes.Where(x => testProcessIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted,true).SetProperty(x => x.IsEnabled,false).SetProperty(x => x.UpdatedAt,now).SetProperty(x => x.UpdatedBy,actor));
        var archivedCharacteristics = testCharacteristicIds.Length == 0 ? 0 : await db.QualityCharacteristics.Where(x => testCharacteristicIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted,true).SetProperty(x => x.IsEnabled,false).SetProperty(x => x.UpdatedAt,now).SetProperty(x => x.UpdatedBy,actor));
        await tx.CommitAsync(); db.ChangeTracker.Clear();
        var after = await CleanupInventoryAsync(db);
        if (after.SeptemberActive != 0 || after.IsolatedPpcs.Count != 0 || after.IsolatedActiveMeasurements != 0 || after.TestProcesses.Count != 0 || after.TestCharacteristics.Count != 0) throw new InvalidOperationException("Cleanup verification failed.");
        if (before.OctoberNormalCount != after.OctoberNormalCount || before.OctoberNormalHash != after.OctoberNormalHash) throw new InvalidOperationException("October normal measurement baseline changed.");
        File.WriteAllText(Path.Combine(args[2], "cleanup-result.json"), JsonSerializer.Serialize(new { database="PMR_SPC_TEST", backup, archivedMeasurements, archivedPpcs, archivedProcesses, archivedCharacteristics, before, after, productionConnected=false }, JsonOptions));
        Console.WriteLine($"CLEANUP APPLIED: september={before.SeptemberActive}; isolatedPpcs={archivedPpcs}; measurements={archivedMeasurements}; processes={archivedProcesses}; characteristics={archivedCharacteristics}; octoberNormal={after.OctoberNormalCount}"); return 0;
    }

    private static async Task<CleanupInventory> CleanupInventoryAsync(AppDbContext db)
    {
        var ppcs = await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Part).Include(x => x.Process).Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
            .Where(x => x.ControlScope == "CHEM").ToListAsync();
        var isolated = ppcs.Where(x => TestChemicalReimportPlanner.IsIsolatedReconciliationTarget(x.Part?.PartNo, x.Part?.PartName, x.Process?.ProcessCode, x.Process?.ProcessName, x.Machine?.MachineCode, x.Machine?.MachineName, x.Tank?.TankCode, x.Tank?.TankName, x.Characteristic?.CharacteristicCode, x.Characteristic?.CharacteristicName) ||
            TestChemicalReimportPlanner.IsExplicitTestMasterLabel(x.Part?.PartNo, x.Part?.PartName, x.Process?.ProcessCode, x.Process?.ProcessName, x.Machine?.MachineCode, x.Machine?.MachineName, x.Tank?.TankCode, x.Tank?.TankName, x.Characteristic?.CharacteristicCode, x.Characteristic?.CharacteristicName))
            .Select(x => new CleanupPpc(x.Id, x.Part?.PartName ?? "", x.Process?.ProcessName ?? "", x.Machine?.MachineName ?? "", x.Tank?.TankName ?? "", x.Characteristic?.CharacteristicName ?? "")).OrderBy(x => x.Id).ToList();
        var ids = isolated.Select(x => x.Id).ToArray(); var sep = new DateTime(2026,9,1); var oct = new DateTime(2026,10,1); var nov = new DateTime(2026,11,1);
        var september = await db.VariableMeasurements.AsNoTracking().CountAsync(x => (x.PortalDailyDate ?? x.MeasuredAt) >= sep && (x.PortalDailyDate ?? x.MeasuredAt) < oct);
        var isolatedMeasurements = ids.Length == 0 ? 0 : await db.VariableMeasurements.AsNoTracking().CountAsync(x => ids.Contains(x.PartProcessCharacteristicId));
        var octoberRows = await db.VariableMeasurements.AsNoTracking().Where(x => !ids.Contains(x.PartProcessCharacteristicId) && (x.PortalDailyDate ?? x.MeasuredAt) >= oct && (x.PortalDailyDate ?? x.MeasuredAt) < nov)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.PartProcessCharacteristicId, x.PortalDailyDate, x.SamplingPhase, x.SamplingStage, x.MeasuredValue, x.RecheckValue }).ToListAsync();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(octoberRows))));
        var isolatedIds = isolated.Select(x => x.Id).ToHashSet();
        var activeRefs = ppcs.Where(x => !isolatedIds.Contains(x.Id)).Select(x => new { x.ProcessId, x.CharacteristicId }).ToList();
        var testProcesses = (await db.Processes.AsNoTracking().Where(x => x.ProcessCode.Contains("測試") || x.ProcessName.Contains("測試")).ToListAsync())
            .Where(x => TestChemicalReimportPlanner.IsExplicitTestMasterLabel(x.ProcessCode,x.ProcessName)).Select(x => new CleanupMaster(x.Id,x.ProcessCode,x.ProcessName,activeRefs.Count(r => r.ProcessId==x.Id))).OrderBy(x=>x.Id).ToList();
        var testCharacteristics = (await db.QualityCharacteristics.AsNoTracking().Where(x => x.CharacteristicCode.Contains("測試") || x.CharacteristicName.Contains("測試")).ToListAsync())
            .Where(x => TestChemicalReimportPlanner.IsExplicitTestMasterLabel(x.CharacteristicCode,x.CharacteristicName)).Select(x => new CleanupMaster(x.Id,x.CharacteristicCode,x.CharacteristicName,activeRefs.Count(r => r.CharacteristicId==x.Id))).OrderBy(x=>x.Id).ToList();
        return new(september, isolatedMeasurements, isolated, octoberRows.Count, hash, testProcesses, testCharacteristics);
    }

    private static List<SourceRow> Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path)); var result = new List<SourceRow>();
        foreach (var row in doc.RootElement.GetProperty("rows").EnumerateArray())
        {
            var day = DateOnly.Parse(row.GetProperty("reportDate").GetString()!, CultureInfo.InvariantCulture);
            if (day < new DateOnly(2026, 9, 1) || row.GetProperty("status").GetString() != "READY") continue;
            var fValues = row.GetProperty("sourceFValues").EnumerateObject().ToDictionary(x => x.Name.Replace("F!", "", StringComparison.OrdinalIgnoreCase), x => x.Value.GetDecimal(), StringComparer.OrdinalIgnoreCase);
            result.Add(new(day, row.GetProperty("reportDateTime").GetString()!, row.GetProperty("shift").GetString()!, row.GetProperty("line").GetString()!, row.GetProperty("samplingStage").GetString()!, row.GetProperty("slot").GetString()!, row.GetProperty("analysisItem").GetString()!, row.GetProperty("sourceFile").GetString()!, row.GetProperty("sourceHash").GetString()!, row.GetProperty("sheet").GetString()!, row.GetProperty("sourceRow").GetInt32(), ReadPhase(row.GetProperty("initial")), row.TryGetProperty("recheck", out var re) && re.ValueKind == JsonValueKind.Object ? ReadPhase(re) : null,
                row.TryGetProperty("adjustmentFormula", out var adjustment) && adjustment.ValueKind == JsonValueKind.String ? adjustment.GetString() : null,
                row.TryGetProperty("adjustmentAmountFormula", out var amount) && amount.ValueKind == JsonValueKind.String ? amount.GetString() : null, fValues));
        }
        return result;
    }

    private static PhaseRow ReadPhase(JsonElement phase) => new(phase.GetProperty("mode").GetString()!,
        phase.TryGetProperty("titration", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetRawText() : null,
        phase.TryGetProperty("secondary", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetRawText() : null,
        phase.GetProperty("concentration").GetDecimal(),
        phase.TryGetProperty("normalizedFormula", out var formula) && formula.ValueKind == JsonValueKind.String ? formula.GetString() : null);

    private static ChemicalPreviewInput Input(PlannedRow row, PhaseRow phase, string calculationPhase)
    {
        var sourceKey = JsonSerializer.Serialize(new { kind = "TransFilesChemical-v2", fileHash = row.Source.SourceHash, file = row.Source.SourceFile, sheet = row.Source.Sheet, row = row.Source.SourceExcelRow, samplingPhase = TestChemicalReimportPlanner.Phase(row.Source.Shift), samplingStage = row.Source.SamplingStage, timeBasis = "report-shift test only" });
        return new(row.Source.MeasuredAt, phase.Primary ?? throw new InvalidOperationException("Formula row primary missing."), phase.Secondary, sourceKey, calculationPhase);
    }

    private static void AssertExcel(string actual, decimal expected, string source)
    {
        if (Math.Abs(decimal.Parse(actual, CultureInfo.InvariantCulture) - expected) > .00000001m) throw new InvalidOperationException($"SPC/Excel mismatch {source}: {actual}!={expected}");
    }

    private static string ScheduleSignature(PlannedRow row)
    {
        var initial = (row.Source.Initial.Formula ?? "").Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        var recheck = (row.Source.Recheck?.Formula ?? initial).Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        var adjustment = (row.Source.AdjustmentFormula ?? "").Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        var amount = (row.Source.AdjustmentAmountFormula ?? "").Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        var f = string.Join(";", row.Source.FValues.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key.ToUpperInvariant() + "=" + x.Value.ToString("0.############################", CultureInfo.InvariantCulture)));
        return initial + "|" + recheck + "|" + adjustment + "|" + amount + "|" + f;
    }

    private static async Task<object> EnsureSchedulesAsync(AppDbContext db, IReadOnlyList<PlannedRow> rows, string actor, CancellationToken ct, string reason = "2026-11-05 九月起測試區藥液日報重匯")
    {
        var formulaRows = rows.Where(x => x.Source.Initial.Mode == "FORMULA").ToList();
        var histories = await db.ChemicalFTableVersionHistories.ToListAsync(ct);
        var historyBySignature = new Dictionary<string, ChemicalFTableVersionHistory>(StringComparer.Ordinal);
        foreach (var history in histories)
        {
            try
            {
                var cells = JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(history.CellsJson) ?? [];
                var signature = FSignature(cells.Where(x => x.NumericValue.HasValue).ToDictionary(x => x.CellAddress.Replace("F!", "", StringComparison.OrdinalIgnoreCase).Replace("$", ""), x => x.NumericValue!.Value, StringComparer.OrdinalIgnoreCase));
                historyBySignature[signature] = history;
            }
            catch { }
        }
        var nextF = (histories.Select(x => x.VersionNo).DefaultIfEmpty().Max()) + 1; var fCreated = 0; var formulaCreated = 0; var schedules = 0; var effects = 0;
        foreach (var row in formulaRows)
        {
            var signature = FSignature(row.Source.FValues);
            if (historyBySignature.ContainsKey(signature)) continue;
            var history = new ChemicalFTableVersionHistory { VersionNo = nextF++, VersionCode = "TEST-REIMPORT-" + ShortHash(signature), DisplayName = "測試區日報F歷史快照", CellsJson = FCellsJson(row.Source.FValues), ChangeType = "HistoricalExcelSnapshot", ChangedBy = actor, Reason = reason, ChangedAt = DateTime.UtcNow };
            db.Add(history); historyBySignature[signature] = history; fCreated++;
        }
        await db.SaveChangesAsync(ct);
        foreach (var group in formulaRows.GroupBy(x => x.PpcId))
        {
            var existingVersions = await db.ChemicalAnalysisFormulaVersions.Where(x => x.PartProcessCharacteristicId == group.Key).OrderBy(x => x.VersionNo).ToListAsync(ct);
            var nextVersion = existingVersions.Select(x => x.VersionNo).DefaultIfEmpty().Max() + 1;
            var versionBySignature = new Dictionary<string, ChemicalAnalysisFormulaVersion>(StringComparer.Ordinal);
            foreach (var variant in group.GroupBy(ScheduleSignature))
            {
                var sample = variant.First(); var config = FormulaConfig(sample);
                var found = existingVersions.LastOrDefault(x => JsonNode.DeepEquals(JsonNode.Parse(x.ConfigJson), JsonNode.Parse(config)));
                if (found is null)
                {
                    found = new ChemicalAnalysisFormulaVersion { PartProcessCharacteristicId = group.Key, VersionNo = nextVersion++, ConfigJson = config, ChangeType = "HistoricalSnapshot", ChangedBy = actor, Reason = reason, ChangedAt = DateTime.UtcNow };
                    db.Add(found); existingVersions.Add(found); formulaCreated++;
                }
                versionBySignature[variant.Key] = found;
            }
            await db.SaveChangesAsync(ct);
            var desired = new List<ChemicalFormulaEffectivity>();
            foreach (var dayGroup in group.GroupBy(x => x.Source.Day).OrderBy(x => x.Key))
            {
                var shifts = dayGroup.GroupBy(x => x.Source.Shift).ToDictionary(x => x.Key, x => x.First());
                foreach (var pair in shifts)
                {
                    var fromLocal = dayGroup.Key.ToDateTime(pair.Key == "MIDDLE" && shifts.ContainsKey("EARLY") ? new TimeOnly(12, 0) : TimeOnly.MinValue);
                    var untilLocal = dayGroup.Key.AddDays(1).ToDateTime(TimeOnly.MinValue);
                    if (pair.Key == "EARLY" && shifts.ContainsKey("MIDDLE")) untilLocal = dayGroup.Key.ToDateTime(new TimeOnly(12, 0));
                    desired.Add(new ChemicalFormulaEffectivity { PartProcessCharacteristicId = group.Key, FormulaVersionId = versionBySignature[ScheduleSignature(pair.Value)].Id, FTableHistoryId = historyBySignature[FSignature(pair.Value.Source.FValues)].Id, EffectiveFromUtc = ToUtc(fromLocal), EffectiveUntilUtcExclusive = ToUtc(untilLocal) });
                }
            }
            var min = desired.Min(x => x.EffectiveFromUtc); var max = desired.Max(x => x.EffectiveUntilUtcExclusive!.Value);
            var policy = await db.ChemicalFormulaPolicies.SingleOrDefaultAsync(x => x.PartProcessCharacteristicId == group.Key, ct);
            var preserved = new List<ChemicalFormulaEffectivity>();
            if (policy?.ActiveScheduleRevisionId is long active)
            {
                foreach (var old in await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => x.PartProcessCharacteristicId == group.Key && x.ScheduleRevisionId == active).ToListAsync(ct))
                {
                    if (old.EffectiveFromUtc < min) preserved.Add(new() { PartProcessCharacteristicId = group.Key, FormulaVersionId = old.FormulaVersionId, FTableHistoryId = old.FTableHistoryId, EffectiveFromUtc = old.EffectiveFromUtc, EffectiveUntilUtcExclusive = old.EffectiveUntilUtcExclusive is null || old.EffectiveUntilUtcExclusive > min ? min : old.EffectiveUntilUtcExclusive });
                    if (old.EffectiveUntilUtcExclusive is null || old.EffectiveUntilUtcExclusive > max) preserved.Add(new() { PartProcessCharacteristicId = group.Key, FormulaVersionId = old.FormulaVersionId, FTableHistoryId = old.FTableHistoryId, EffectiveFromUtc = old.EffectiveFromUtc < max ? max : old.EffectiveFromUtc, EffectiveUntilUtcExclusive = old.EffectiveUntilUtcExclusive });
                }
            }
            var all = preserved.Concat(desired).OrderBy(x => x.EffectiveFromUtc).ToList();
            for (var i = 1; i < all.Count; i++) if (all[i - 1].EffectiveUntilUtcExclusive is null || all[i - 1].EffectiveUntilUtcExclusive > all[i].EffectiveFromUtc) throw new InvalidOperationException("歷史公式排程重疊：" + group.Key);
            var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = group.Key, RevisionNo = (await db.ChemicalFormulaScheduleRevisions.Where(x => x.PartProcessCharacteristicId == group.Key).MaxAsync(x => (int?)x.RevisionNo, ct) ?? 0) + 1, Actor = actor, Reason = reason, CreatedAtUtc = DateTime.UtcNow };
            db.Add(revision); await db.SaveChangesAsync(ct); foreach (var item in all) item.ScheduleRevisionId = revision.Id; db.AddRange(all);
            await db.SaveChangesAsync(ct); revision.PublishedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct);
            if (policy is null) { policy = new() { PartProcessCharacteristicId = group.Key }; db.Add(policy); } policy.ActiveScheduleRevisionId = revision.Id;
            await db.SaveChangesAsync(ct); schedules++; effects += all.Count;
        }
        return new { fCreated, formulaCreated, schedules, effects };
    }

    private static string FormulaConfig(PlannedRow row)
    {
        return TestChemicalReimportPlanner.FormulaConfig(row.Master.ConfigJson, row.Source.Initial.Formula!,
            row.Source.Recheck?.Formula ?? row.Source.Initial.Formula!, row.Source.AdjustmentFormula, row.Source.AdjustmentAmountFormula);
    }

    private static string FSignature(IReadOnlyDictionary<string, decimal> cells) => string.Join(";", cells.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key.ToUpperInvariant() + "=" + x.Value.ToString("0.############################", CultureInfo.InvariantCulture)));
    private static string FCellsJson(IReadOnlyDictionary<string, decimal> cells) => JsonSerializer.Serialize(cells.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => new ChemicalFTableHistoryCell("F!" + x.Key, null, x.Value)));
    private static string ShortHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12];
    private static DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));

    private static async Task VerifyDatabaseAsync(AppDbContext db)
    {
        await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await cmd.ExecuteReaderAsync(); await reader.ReadAsync();
        if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16") throw new InvalidOperationException("Actual test SQL target mismatch.");
    }

    private static async Task<State> CaptureStateAsync(AppDbContext db) => new(
        await db.VariableMeasurements.LongCountAsync(x => !x.IsDeleted), await db.ChemicalCalculationReceipts.LongCountAsync(),
        await db.ChemicalCalculationTraces.LongCountAsync(), await db.PartProcessCharacteristics.LongCountAsync(x => x.ControlScope == "CHEM" && x.IsEnabled),
        await db.ChemicalFTableVersions.LongCountAsync(x => x.IsActive));

    private static async Task<Dictionary<string, string?>> PayloadAsync(ChemicalPortalMeasurementService portal, PlannedRow row, string actor)
    {
        var source = row.Source; var initial = source.Initial; var recheck = source.Recheck;
        var payload = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["PpcId"] = row.PpcId.ToString(CultureInfo.InvariantCulture), ["ControlScope"] = "CHEM", ["ProcessCode"] = row.Master.ProcessCode,
            ["MachineCode"] = row.Master.MachineCode, ["TankCode"] = row.Master.TankCode, ["CharacteristicCode"] = row.Master.CharacteristicCode,
            ["Unit"] = row.Master.Unit, ["MeasuredAt"] = source.MeasuredAt, ["SamplingPhase"] = TestChemicalReimportPlanner.Phase(source.Shift),
            ["SamplingStage"] = source.SamplingStage, ["SampleNo"] = "1", ["Operator"] = actor,
            ["TitrationValue"] = initial.Primary ?? "", ["SecondaryTitrationValue"] = initial.Secondary,
            ["MeasuredValue"] = initial.Mode == "DIRECT" ? initial.Concentration.ToString(CultureInfo.InvariantCulture) : "0",
            ["RecheckTitrationValue"] = recheck?.Primary, ["RecheckSecondaryTitrationValue"] = recheck?.Secondary,
            ["RecheckValue"] = recheck is { Mode: "DIRECT" } ? recheck.Concentration.ToString(CultureInfo.InvariantCulture) : "",
            ["SourceFile"] = source.SourceFile, ["SourceHash"] = source.SourceHash, ["SourceSheet"] = source.Sheet, ["SourceRow"] = source.SourceExcelRow.ToString(CultureInfo.InvariantCulture)
        };
        if (initial.Mode == "FORMULA")
        {
            var preview = await portal.PreviewAsync(new(row.PpcId, source.MeasuredAt, initial.Primary!, initial.Secondary, TestChemicalReimportPlanner.Phase(source.Shift), source.SamplingStage, "Initial"), actor, default);
            AssertExcel(preview.Result.Raw, initial.Concentration, source.SourceId); payload["ChemicalPreviewToken"] = preview.Token;
            if (recheck is not null)
            {
                var second = await portal.PreviewAsync(new(row.PpcId, source.MeasuredAt, recheck.Primary!, recheck.Secondary, TestChemicalReimportPlanner.Phase(source.Shift), source.SamplingStage, "Recheck"), actor, default);
                AssertExcel(second.Result.Raw, recheck.Concentration, source.SourceId + ":recheck"); payload["ChemicalRecheckPreviewToken"] = second.Token;
            }
        }
        return payload;
    }

    private static async Task<Verification> VerifyMeasurementsAsync(AppDbContext db, IReadOnlyList<PlannedRow> rows, CancellationToken ct)
    {
        var ids = rows.Select(x => x.PpcId).Distinct().ToArray(); var start = new DateTime(2026, 9, 1); var end = new DateTime(2026, 11, 6);
        var actual = await db.VariableMeasurements.AsNoTracking().Where(x => !x.IsDeleted && ids.Contains(x.PartProcessCharacteristicId) && x.PortalDailyDate >= start && x.PortalDailyDate < end).ToListAsync(ct);
        if (actual.Count != rows.Count) throw new InvalidOperationException($"Imported measurement count mismatch: {actual.Count}!={rows.Count}");
        var byKey = actual.ToDictionary(x => $"{x.PartProcessCharacteristicId}|{x.PortalDailyDate:yyyy-MM-dd}|{x.SamplingPhase}|{x.SamplingStage}"); var traceChecks = 0;
        foreach (var row in rows)
        {
            var key = $"{row.PpcId}|{row.Source.Day:yyyy-MM-dd}|{TestChemicalReimportPlanner.Phase(row.Source.Shift)}|{row.Source.SamplingStage}";
            if (!byKey.TryGetValue(key, out var value)) throw new InvalidOperationException("Missing imported natural key: " + key);
            if (Math.Abs(Convert.ToDecimal(value.MeasuredValue, CultureInfo.InvariantCulture) - row.Source.Initial.Concentration) > .00000001m) throw new InvalidOperationException("Stored value mismatch: " + row.Source.SourceId);
            if (row.Source.Recheck is not null && (!value.RecheckValue.HasValue || Math.Abs(Convert.ToDecimal(value.RecheckValue.Value, CultureInfo.InvariantCulture) - row.Source.Recheck.Concentration) > .00000001m)) throw new InvalidOperationException("Stored recheck mismatch: " + row.Source.SourceId);
            if (row.Source.Initial.Mode == "FORMULA")
            {
                var traces = await db.ChemicalCalculationTraces.AsNoTracking().CountAsync(x => x.VariableMeasurementId == value.Id, ct);
                var expected = row.Source.Recheck is null ? 1 : 2; if (traces < expected || !await db.ChemicalCalculationTraces.AsNoTracking().AnyAsync(x => x.VariableMeasurementId == value.Id && x.Phase == "Initial" && x.ReceiptId == value.ChemicalReceiptId, ct)) throw new InvalidOperationException("Trace evidence mismatch: " + row.Source.SourceId); traceChecks += expected;
            }
        }
        return new(actual.Count, traceChecks);
    }

    private static async Task<Reactivation> ReactivateAsync(AppDbContext db, IReadOnlyList<PlannedRow> rows, string actor, CancellationToken ct)
    {
        var schedule = new ChemicalFormulaScheduleService(db, new EphemeralDataProtectionProvider(), TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
        var ids = rows.Select(x => x.PpcId).Distinct().ToArray(); var start = new DateTime(2026, 9, 1); var end = new DateTime(2026, 11, 6);
        var all = await db.VariableMeasurements.IgnoreQueryFilters().Where(x => ids.Contains(x.PartProcessCharacteristicId) && x.PortalDailyDate >= start && x.PortalDailyDate < end).ToListAsync(ct);
        var grouped = all.GroupBy(x => $"{x.PartProcessCharacteristicId}|{x.PortalDailyDate:yyyy-MM-dd}|{x.SamplingPhase}|{x.SamplingStage}").ToDictionary(x => x.Key, x => x.ToList());
        var mappings = await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Tank).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var changed = 0; var active = 0; var receipts = 0; var traces = 0;
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var controlledBatchId = StableGuid("TEST-CHEMICAL-REIMPORT-2026-09-01-V4");
        if (!await db.UploadBatches.AnyAsync(x => x.UploadBatchId == controlledBatchId, ct))
        {
            db.UploadBatches.Add(new UploadBatch
            {
                UploadBatchId = controlledBatchId, UploadType = "ChemicalDaily", SourceType = "ControlledReimport",
                ImportStatus = "Confirmed", OriginalFileName = "chemical-preview-v4.json", TotalRows = rows.Count,
                ValidRows = rows.Count, ErrorRows = 0, ConfirmedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, CreatedBy = actor
            });
            await db.SaveChangesAsync(ct);
        }
        foreach (var row in rows)
        {
            var key = $"{row.PpcId}|{row.Source.Day:yyyy-MM-dd}|{TestChemicalReimportPlanner.Phase(row.Source.Shift)}|{row.Source.SamplingStage}";
            grouped.TryGetValue(key, out var candidates); candidates ??= [];
            if (candidates.Count > 1) throw new InvalidOperationException($"Natural key resolves more than once: {key}, count={candidates.Count}");
            var isNew = candidates.Count == 0;
            var map = mappings[row.PpcId];
            var vm = isNew ? new MesSpc.Api.Domain.Entities.VariableMeasurement
            {
                PartId = map.PartId ?? 0, ProcessId = map.ProcessId, MachineId = map.MachineId, CharacteristicId = map.CharacteristicId,
                PartProcessCharacteristicId = map.Id, LineId = map.Tank?.LineId, TankId = map.TankId, SlotId = map.SlotId,
                SampleNo = 1, PortalDailyDate = row.Source.Day.ToDateTime(TimeOnly.MinValue), SamplingPhase = TestChemicalReimportPlanner.Phase(row.Source.Shift),
                SamplingStage = row.Source.SamplingStage, SourceType = SourceType.Manual, CreatedAt = DateTime.UtcNow, CreatedBy = actor
            } : candidates[0];
            if (!isNew && !vm.IsDeleted) { active++; continue; }
            ChemicalCalculationReceipt? initialReceipt = null; ChemicalCalculationReceipt? recheckReceipt = null;
            if (row.Source.Initial.Mode == "FORMULA")
            {
                var input = Input(row, row.Source.Initial, "Initial"); var preview = await schedule.PreviewAsync(row.PpcId, input, actor, ct); AssertExcel(preview.Result.Raw, row.Source.Initial.Concentration, row.Source.SourceId);
                initialReceipt = await schedule.PrepareAsync(row.PpcId, new ChemicalPrepareRequest(input, preview.Token), actor, ct); receipts++;
                if (row.Source.Recheck is not null)
                {
                    var reInput = Input(row, row.Source.Recheck, "Recheck"); var rePreview = await schedule.PreviewAsync(row.PpcId, reInput, actor, ct); AssertExcel(rePreview.Result.Raw, row.Source.Recheck.Concentration, row.Source.SourceId + ":recheck");
                    recheckReceipt = await schedule.PrepareAsync(row.PpcId, new ChemicalPrepareRequest(reInput, rePreview.Token), actor, ct); receipts++;
                }
            }
            vm.MeasuredValue = Convert.ToDouble(row.Source.Initial.Concentration, CultureInfo.InvariantCulture); vm.RecheckValue = row.Source.Recheck is null ? null : Convert.ToDouble(row.Source.Recheck.Concentration, CultureInfo.InvariantCulture);
            vm.MeasuredAt = DateTimeOffset.Parse(row.Source.MeasuredAt, CultureInfo.InvariantCulture).UtcDateTime; vm.Operator = actor; vm.SourceReference = $"{row.Source.SourceFile}|{row.Source.Sheet}|{row.Source.SourceExcelRow}";
            // Test cleanup removed historical batches while retaining soft-deleted measurements.
            // Both reactivated and new rows are therefore tied to this controlled, auditable batch.
            vm.UploadBatchId = TestChemicalReimportPlanner.NormalizeUploadBatch(vm.UploadBatchId, controlledBatchId);
            vm.ChemicalReceiptId = initialReceipt?.Id; vm.ChemicalSourceKeyHash = initialReceipt?.SourceKeyHash; vm.IsDeleted = false; vm.UpdatedAt = DateTime.UtcNow; vm.UpdatedBy = actor;
            if (isNew) { db.VariableMeasurements.Add(vm); await db.SaveChangesAsync(ct); grouped[key] = [vm]; }
            if (initialReceipt is not null)
            {
                foreach (var pair in new[] { (Phase: "Initial", Receipt: initialReceipt), (Phase: "Recheck", Receipt: recheckReceipt) })
                {
                    if (pair.Receipt is null) continue; var result = ChemicalPortalMeasurementService.Result(pair.Receipt);
                    var version = (await db.ChemicalCalculationTraces.Where(x => x.VariableMeasurementId == vm.Id && x.Phase == pair.Phase).MaxAsync(x => (int?)x.VersionNo, ct) ?? 0) + 1;
                    db.ChemicalCalculationTraces.Add(new() { VariableMeasurementId = vm.Id, ReceiptId = pair.Receipt.Id, Phase = pair.Phase, VersionNo = version, RawResultCanonical = result.Raw, StoredResultCanonical = result.Stored, CreatedAtUtc = DateTime.UtcNow }); traces++;
                }
                db.ChemicalStatisticsJobs.Add(new() { Id = Guid.NewGuid(), VariableMeasurementId = vm.Id, ReceiptId = initialReceipt.Id, Status = "Pending", CreatedAtUtc = DateTime.UtcNow });
            }
            changed++; if (changed % 100 == 0) { await db.SaveChangesAsync(ct); Console.WriteLine($"REACTIVATED {changed}/{rows.Count}"); }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(changed, active, receipts, traces);
    }

    private static async Task<object> BackupAsync(string connection, string evidenceDirectory)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var command = sql.CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))"; await using (var reader = await command.ExecuteReaderAsync()) { await reader.ReadAsync(); if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16") throw new InvalidOperationException("Backup target guard failed."); }
        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"; var directory = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("SQL backup directory unavailable.");
        var path = directory.TrimEnd('\\', '/') + "\\PMR_SPC_TEST_pre_sep_reimport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".bak";
        command.CommandTimeout = 300; command.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT"; command.Parameters.AddWithValue("@path", path); await command.ExecuteNonQueryAsync();
        command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await command.ExecuteNonQueryAsync(); return new { path, checksum = true, verifyOnly = true };
    }

    private static Guid StableGuid(string value) { var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value)); return new Guid(hash[..16]); }

    private static string CurrentActor()
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows actor required.");
        var value = WindowsIdentity.GetCurrent().Name.Split('\\').Last().Split('@').First();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100) throw new InvalidOperationException("Invalid actor."); return value;
    }

    private sealed record SourceRow(DateOnly Day, string MeasuredAt, string Shift, string Line, string SamplingStage, string Slot, string AnalysisItem, string SourceFile, string SourceHash, string Sheet, int SourceExcelRow, PhaseRow Initial, PhaseRow? Recheck, string? AdjustmentFormula, string? AdjustmentAmountFormula, IReadOnlyDictionary<string, decimal> FValues)
    { public string SourceId => $"{SourceHash}|{Sheet}|{SourceExcelRow}|{Shift}|{SamplingStage}"; }
    private sealed record PhaseRow(string Mode, string? Primary, string? Secondary, decimal Concentration, string? Formula = null);
    private sealed record MasterRow(int PpcId, string MachineCode, string TankCode, string TankName, string CharacteristicCode, string CharacteristicName, string Unit, bool IsEnabled, string ProcessCode, string ConfigJson);
    private sealed record PlannedRow(SourceRow Source, int PpcId, MasterRow Master);
    private sealed record State(long ActiveMeasurements, long Receipts, long Traces, long EnabledChemicalMasters, long ActiveFTables);
    private sealed record Verification(int Measurements, int FormulaTraces);
    private sealed record Reactivation(int Changed, int AlreadyActive, int Receipts, int Traces);
    private sealed record CleanupPpc(int Id, string Part, string Process, string Machine, string Tank, string Characteristic);
    private sealed record CleanupMaster(int Id,string Code,string Name,int ActiveReferences);
    private sealed record CleanupInventory(int SeptemberActive, int IsolatedActiveMeasurements, IReadOnlyList<CleanupPpc> IsolatedPpcs, int OctoberNormalCount, string OctoberNormalHash, IReadOnlyList<CleanupMaster> TestProcesses, IReadOnlyList<CleanupMaster> TestCharacteristics);

    private sealed class SuppressedEmail : IEmailNotificationService
    {
        public int Attempts { get; private set; }
        private Task<bool> Suppress() { Attempts++; return Task.FromResult(true); }
        public Task<bool> SendAlertEmailAsync(AlertEvent a, string b, string c, SmtpSettingsOverride? d = null, string? e = null) => Suppress();
        public Task<bool> SendTestEmailAsync(string a, SmtpSettingsOverride? b = null, string? c = null) => Suppress();
        public Task<bool> SendHtmlEmailAsync(string a, string b, string c, string d, SmtpSettingsOverride? e = null, string? f = null, string? g = null) => Suppress();
        public Task<bool> SendReportEmailAsync(string a, string b, string c, string d, byte[] e, string f, string? g = null) => Suppress();
    }
}
