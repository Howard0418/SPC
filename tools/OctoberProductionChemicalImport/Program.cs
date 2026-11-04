using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OctoberProductionFormulaF;

namespace OctoberProductionChemicalImport;

internal static class Program
{
    private const string FormalManifestSha256 = "57fe52a02fae3127f596c1a3e2dc73f28d6e02109daf7592f7486a3aa168b49b";
    private const string PrimaryManifestSha256 = "dc48a7e465c9bd800fddb4f29ae6c5e511edfbf2830b904c1ae6e03a14695f23";
    private const string SecondaryManifestSha256 = "74645d538a4197209332a6f2c28277820f086761c54fef2e5055ca3fc7ae9018";
    private const string ApiRoot = "https://pmr-spc-srv.pmr.com.tw:8443/api";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 6 || args[5] is not ("--plan-reviewed" or "--apply-reviewed" or "--verify-reviewed"))
            throw new ArgumentException("CONFIG FORMAL_MANIFEST PRIMARY_MANIFEST SECONDARY_MANIFEST EVIDENCE_DIR --plan-reviewed|--apply-reviewed|--verify-reviewed");
        var (configPath, formalPath, primaryPath, secondaryPath, evidenceDirectory, mode) =
            (args[0], args[1], args[2], args[3], args[4], args[5]);
        Directory.CreateDirectory(evidenceDirectory);
        var runId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var apply = mode == "--apply-reviewed";

        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(configPath)).Build();
        var appEnvironment = config["AppEnvironment"];
        var rawConnection = config.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Production SQL connection is missing.");
        var cs = new SqlConnectionStringBuilder(rawConnection.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var versionText = await http.GetStringAsync(ApiRoot + "/version");
        var healthText = await http.GetStringAsync(ApiRoot + "/health");
        using var versionDocument = JsonDocument.Parse(versionText);
        var apiEnvironment = versionDocument.RootElement.GetProperty("environment").GetString();
        OctoberProductionChemicalImportPlanner.ValidateTarget(appEnvironment, cs.DataSource, cs.InitialCatalog, apiEnvironment);

        var (formal, sourceRows, conflictIds, calculationChecks) = LoadReviewedInputs(formalPath, primaryPath, secondaryPath);
        var actor = CurrentActor();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        await VerifyActualDatabaseAsync(db);
        await VerifyBusinessKeysAsync(db, formal);
        await using var writer = await OpenProductionWriterAsync(actor);

        var existingBefore = await ExistingDayKeysAsync(db, sourceRows.Select(x => x.PpcId).Distinct().ToArray());
        var plan = OctoberProductionChemicalImportPlanner.Build(sourceRows, existingBefore, conflictIds);
        var allPotential = OctoberProductionChemicalImportPlanner.Build(sourceRows, new HashSet<ProductionChemicalDayKey>(), conflictIds).Included;
        if (plan.ExcludedConflict.Count != 12 || allPotential.Count != 1199)
            throw new InvalidOperationException("Reviewed formula/F conflict scope changed.");
        if (apply && (plan.Included.Count != 335 || plan.SkippedExisting.Count != 864))
            throw new InvalidOperationException($"Production pre-write plan drifted: include={plan.Included.Count}, skip={plan.SkippedExisting.Count}.");
        if (mode == "--verify-reviewed" && plan.Included.Count != 0)
            throw new InvalidOperationException($"Idempotence verification still finds {plan.Included.Count} rows eligible.");

        var before = await CaptureStateAsync(db);
        var pureChecks = await PreviewPlanAsync(db, plan.Included, actor);
        if (pureChecks != plan.Included.Count + plan.Included.Count(x => x.Source.RecheckPrimary is not null))
            throw new InvalidOperationException("Preview coverage mismatch.");
        var planHash = Hash(string.Join("\n", plan.Included.Select(x => x.Source.SourceId)));
        var beforePath = Path.Combine(evidenceDirectory, $"{runId}-before.json");
        File.WriteAllText(beforePath, JsonSerializer.Serialize(new
        {
            mode, environment = "production", database = "PMR_SPC_2026", actor,
            apiVersion = JsonSerializer.Deserialize<object>(versionText), apiHealth = JsonSerializer.Deserialize<object>(healthText),
            sources = new { formal = FormalManifestSha256, primary = PrimaryManifestSha256, secondary = SecondaryManifestSha256 },
            sourceRows = sourceRows.Count, calculationChecks, previewChecks = pureChecks,
            plan = new { included = plan.Included.Count, skippedExisting = plan.SkippedExisting.Count, conflicts = plan.ExcludedConflict.Count, planHash },
            state = before
        }, JsonOptions));

        object? backup = null;
        var created = 0;
        var repeated = 0;
        var measurementIds = new List<long>();
        var notificationAttempts = 0;
        if (apply)
        {
            backup = await BackupAsync(cs.ConnectionString, evidenceDirectory, runId);
            var schedule = new ChemicalFormulaScheduleService(db, new EphemeralDataProtectionProvider(), TimeProvider.System,
                NullLogger<ChemicalFormulaScheduleService>.Instance);
            var progressPath = Path.Combine(evidenceDirectory, $"{runId}-progress.json");
            foreach (var row in plan.Included)
            {
                var input = new ChemicalPreviewInput(row.MeasuredAt, row.Source.Primary, row.Source.Secondary, row.SourceKey, "Initial");
                var preview = await schedule.PreviewAsync(row.Source.PpcId, input, actor, default);
                AssertResult(preview.Result.Raw, row.Source.Excel, row.Source.SourceId);
                var receipt = await schedule.PrepareAsync(row.Source.PpcId, new(input, preview.Token), actor, default);
                Guid? recheckReceipt = null;
                if (row.Source.RecheckPrimary is not null)
                {
                    var reInput = input with { Primary = row.Source.RecheckPrimary, Secondary = row.Source.RecheckSecondary, Phase = "Recheck" };
                    var rePreview = await schedule.PreviewAsync(row.Source.PpcId, reInput, actor, default);
                    AssertResult(rePreview.Result.Raw, row.Source.RecheckExcel!, row.Source.SourceId + ":recheck");
                    recheckReceipt = (await schedule.PrepareAsync(row.Source.PpcId, new(reInput, rePreview.Token), actor, default)).Id;
                }
                var committed = await CommitAsync(writer, receipt.Id, recheckReceipt, actor);
                if (committed.AlreadyProcessed) repeated++; else created++;
                measurementIds.Add(committed.MeasurementId);
                File.WriteAllText(progressPath, JsonSerializer.Serialize(new
                {
                    completed = created + repeated, total = plan.Included.Count, created, repeated,
                    last = row.Source.SourceId, measurementIds
                }, JsonOptions));
            }
            if (created != 335 || repeated != 0 || measurementIds.Distinct().Count() != 335)
                throw new InvalidOperationException($"Unexpected first apply result: created={created}, repeated={repeated}.");

            var mail = new SuppressedEmail();
            var processor = new ChemicalStatisticsProcessor(db, new SpcService(db, mail, config),
                NullLogger<ChemicalStatisticsProcessor>.Instance);
            var jobs = await db.ChemicalStatisticsJobs.AsNoTracking()
                .Where(x => measurementIds.Contains(x.VariableMeasurementId)).OrderBy(x => x.CreatedAtUtc).Select(x => x.Id).ToListAsync();
            foreach (var job in jobs) await processor.ProcessOneAsync(job, default);
            var notCompleted = await db.ChemicalStatisticsJobs.AsNoTracking()
                .CountAsync(x => measurementIds.Contains(x.VariableMeasurementId) && x.Status != "Completed");
            var unsupported = await (from job in db.ChemicalStatisticsJobs.AsNoTracking()
                                     join measurement in db.VariableMeasurements.AsNoTracking() on job.VariableMeasurementId equals measurement.Id
                                     join mapping in db.PartProcessCharacteristics.AsNoTracking() on measurement.PartProcessCharacteristicId equals mapping.Id
                                     where measurementIds.Contains(job.VariableMeasurementId) && job.Status != "Completed" && mapping.ChartTypeId == null
                                     select job.Id).CountAsync();
            if (jobs.Count != 335 || notCompleted != unsupported)
                throw new InvalidOperationException($"Unexpected chemical statistics state: jobs={jobs.Count}, incomplete={notCompleted}, unsupported={unsupported}.");
            notificationAttempts = mail.Attempts;
        }

        var expectedImported = await FindTaskMeasurementsAsync(db, allPotential);
        if ((apply || mode == "--verify-reviewed") && expectedImported.Count != 335)
            throw new InvalidOperationException($"Expected 335 task-owned measurements; found {expectedImported.Count}.");
        var traceChecks = (apply || mode == "--verify-reviewed") ? await VerifyImportedAsync(db, expectedImported, allPotential) : 0;
        var chartChecks = (apply || mode == "--verify-reviewed")
            ? await VerifyChartsAsync(db, expectedImported.Select(x => x.PartProcessCharacteristicId).Distinct().ToArray(), config) : 0;
        var taskMeasurementIds = expectedImported.Select(x => x.Id).ToArray();
        var statisticsJobs = taskMeasurementIds.Length == 0
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : await db.ChemicalStatisticsJobs.AsNoTracking().Where(x => taskMeasurementIds.Contains(x.VariableMeasurementId))
                .GroupBy(x => x.Status).ToDictionaryAsync(x => x.Key, x => x.Count());
        var failedStatistics = await (from job in db.ChemicalStatisticsJobs.AsNoTracking()
                 join measurement in db.VariableMeasurements.AsNoTracking() on job.VariableMeasurementId equals measurement.Id
                 join mapping in db.PartProcessCharacteristics.AsNoTracking() on measurement.PartProcessCharacteristicId equals mapping.Id
                 join machine in db.Machines.AsNoTracking() on mapping.MachineId equals machine.Id
                 join tank in db.Tanks.AsNoTracking() on mapping.TankId equals tank.Id
                 join characteristic in db.QualityCharacteristics.AsNoTracking() on mapping.CharacteristicId equals characteristic.Id
                 where taskMeasurementIds.Contains(job.VariableMeasurementId) && job.Status == "Failed"
                 group new { job, mapping } by new { mapping.Id, machine.MachineCode, tank.TankCode, tank.TankName, characteristic.CharacteristicCode, characteristic.CharacteristicName, mapping.ChartTypeId } into grouped
                 orderby grouped.Key.Id
                 select new { ppcId = grouped.Key.Id, grouped.Key.MachineCode, grouped.Key.TankCode, grouped.Key.TankName, grouped.Key.CharacteristicCode, grouped.Key.CharacteristicName, grouped.Key.ChartTypeId, count = grouped.Count(), lastError = grouped.Max(x => x.job.LastError) })
            .ToListAsync();

        var after = await CaptureStateAsync(db, before.ExistingMeasurementMaxId);
        if (before.ExistingMeasurementFingerprint != after.ExistingMeasurementFingerprint ||
            before.CurrentPpc != after.CurrentPpc || before.ActiveF != after.ActiveF || before.FormulaSchedules != after.FormulaSchedules)
            throw new InvalidOperationException("Existing measurements or formal master/formula/F data changed.");
        var reportPath = Path.Combine(evidenceDirectory, $"{runId}-{mode.TrimStart('-')}.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            mode = apply ? "APPLIED" : mode == "--verify-reviewed" ? "VERIFIED_NO_CHANGE" : "PLAN_ONLY",
            environment = "production", database = "PMR_SPC_2026", actor,
            sourceVerification = "SHA256-pinned reviewed October evidence",
            plan = new { included = plan.Included.Count, skippedExisting = plan.SkippedExisting.Count, conflicts = plan.ExcludedConflict.Count, planHash },
            result = new { created, repeated, taskMeasurements = expectedImported.Count, traceChecks, chartChecks, statisticsJobs, failedStatistics, notificationAttempts },
            backup, before = before.Counts, after = after.Counts,
            existingMeasurementsUnchanged = true, currentPpcUnchanged = true, activeFUnchanged = true, formulaSchedulesUnchanged = true
        }, JsonOptions));
        Console.WriteLine($"{(apply ? "APPLIED" : mode == "--verify-reviewed" ? "NO_CHANGE" : "PLAN")}: include={plan.Included.Count}; skip={plan.SkippedExisting.Count}; conflicts={plan.ExcludedConflict.Count}; created={created}; taskMeasurements={expectedImported.Count}");
        return 0;
    }

    private static (FormalManifest Formal, List<ProductionChemicalSourceRow> Rows, HashSet<string> Conflicts, int Checks)
        LoadReviewedInputs(string formalPath, string primaryPath, string secondaryPath)
    {
        RequireHash(formalPath, FormalManifestSha256); RequireHash(primaryPath, PrimaryManifestSha256); RequireHash(secondaryPath, SecondaryManifestSha256);
        var formal = JsonSerializer.Deserialize<FormalManifest>(File.ReadAllText(formalPath), JsonOptions) ?? throw new InvalidOperationException("Formal manifest is invalid.");
        var primary = JsonSerializer.Deserialize<RawManifest>(File.ReadAllText(primaryPath), JsonOptions) ?? throw new InvalidOperationException("Primary manifest is invalid.");
        var secondary = JsonSerializer.Deserialize<RawManifest>(File.ReadAllText(secondaryPath), JsonOptions) ?? throw new InvalidOperationException("Secondary manifest is invalid.");
        if (formal.Schema != "SPC-OctFormulaF-Production-v1" || formal.Month != "2026-10" || formal.Rows.Count != 1211 || primary.Rows.Count != 1230 || secondary.Rows.Count != 3)
            throw new InvalidOperationException("Reviewed October manifest scope mismatch.");

        string Key(string hash, string sheet, int row) => $"{hash}|{sheet}|{row}";
        var main = primary.Rows.Where(x => x.Status == "validated").ToDictionary(x => Key(x.FileHash, x.Sheet, x.SourceRow));
        var secondaryRows = secondary.Rows.Where(x => x.Status == "validated").ToDictionary(x => Key(x.FileHash, x.Sheet, x.SourceRow));
        var scheduleRows = formal.Rows.Select(x => new SourceScheduleRow(x.PpcId, x.BusinessKey.Text,
            DateOnly.ParseExact(x.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture), x.Formula,
            x.FCells.ToDictionary(y => y.Key, y => decimal.Parse(y.Value, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase), x.SourceId)).ToList();
        var formulaPlan = OctoberProductionFormulaFPlanner.Build(scheduleRows);
        var conflicts = formulaPlan.Conflicts.SelectMany(x => x.SourceIds).ToHashSet(StringComparer.Ordinal);
        if (formulaPlan.IncludedRowCount != 1199 || conflicts.Count != 12) throw new InvalidOperationException("Formula/F conflict plan mismatch.");

        var rows = new List<ProductionChemicalSourceRow>();
        foreach (var row in formal.Rows)
        {
            var lookup = row.Origin == "secondary" ? secondaryRows : main;
            if (!lookup.TryGetValue(Key(row.SourceFileHash, row.Sheet, row.SourceRow), out var raw))
                throw new InvalidOperationException("Reviewed raw source evidence missing: " + row.SourceId);
            if (raw.FileDay != row.Day || raw.SamplingPhase != row.SamplingPhase || raw.SamplingStage != row.SamplingStage || raw.Primary != row.Primary || raw.Excel != row.Excel)
                throw new InvalidOperationException("Reviewed source evidence drift: " + row.SourceId);
            rows.Add(new(row.PpcId, DateOnly.ParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture), row.SourceId,
                row.SourceFileHash, Path.GetFileName(row.SourceFile), row.Sheet, row.SourceRow, row.BusinessKey.MachineCode,
                row.SamplingPhase ?? "", row.SamplingStage ?? "", row.Primary, row.Secondary,
                row.Origin == "main" ? raw.RecheckPrimary : null, null, row.Excel, row.Origin == "main" ? raw.RecheckExcel : null));
        }
        return (formal, rows, conflicts, rows.Count);
    }

    private static async Task<int> PreviewPlanAsync(AppDbContext db, IReadOnlyList<PlannedChemicalMeasurement> rows, string actor)
    {
        var schedule = new ChemicalFormulaScheduleService(db, new EphemeralDataProtectionProvider(), TimeProvider.System,
            NullLogger<ChemicalFormulaScheduleService>.Instance);
        var checks = 0;
        foreach (var row in rows)
        {
            var input = new ChemicalPreviewInput(row.MeasuredAt, row.Source.Primary, row.Source.Secondary, row.SourceKey, "Initial");
            var preview = await schedule.PreviewAsync(row.Source.PpcId, input, actor, default);
            AssertResult(preview.Result.Raw, row.Source.Excel, row.Source.SourceId); checks++;
            if (row.Source.RecheckPrimary is null) continue;
            var recheck = await schedule.PreviewAsync(row.Source.PpcId, input with { Primary = row.Source.RecheckPrimary, Secondary = row.Source.RecheckSecondary, Phase = "Recheck" }, actor, default);
            AssertResult(recheck.Result.Raw, row.Source.RecheckExcel!, row.Source.SourceId + ":recheck"); checks++;
        }
        return checks;
    }

    private static void AssertResult(string actual, string expected, string source)
    {
        if (Math.Abs(decimal.Parse(actual, CultureInfo.InvariantCulture) - decimal.Parse(expected, CultureInfo.InvariantCulture)) > .00000001m)
            throw new InvalidOperationException("SPC/Excel calculation mismatch: " + source);
    }

    private static async Task<HashSet<ProductionChemicalDayKey>> ExistingDayKeysAsync(AppDbContext db, int[] ppcIds)
    {
        var result = new HashSet<ProductionChemicalDayKey>();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT DISTINCT PartProcessCharacteristicId,CONVERT(date,PortalDailyDate) FROM dbo.VariableMeasurements WHERE IsDeleted=0 AND PortalDailyDate>='2026-10-01' AND PortalDailyDate<'2026-11-01' AND PartProcessCharacteristicId IN ({string.Join(',', ppcIds)})";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new(reader.GetInt32(0), DateOnly.FromDateTime(reader.GetDateTime(1))));
        return result;
    }

    private static async Task<SqlConnection> OpenProductionWriterAsync(string actor)
    {
        var template = Environment.GetEnvironmentVariable("SPC_CHEMICAL_DB_CONNECTION", EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable("SPC_CHEMICAL_DB_CONNECTION") ?? throw new InvalidOperationException("Integrated chemical writer connection is missing.");
        var builder = new SqlConnectionStringBuilder(template);
        if (!builder.IntegratedSecurity || !new[] { "172.16.110.16", "PMR-MESSQL" }.Contains(builder.DataSource, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Integrated chemical writer template is invalid.");
        builder.InitialCatalog = "PMR_SPC_2026";
        var writer = new SqlConnection(builder.ConnectionString); await writer.OpenAsync();
        await using var command = writer.CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address')),USER_NAME(),HAS_PERMS_BY_NAME('dbo.CommitChemicalMeasurementWithTrace','OBJECT','EXECUTE')";
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        var principal = reader.GetString(2).Split('\\').Last().Split('@').First();
        if (reader.GetString(0) != "PMR_SPC_2026" || reader.GetString(1) != "172.16.110.16" || !principal.Equals(actor, StringComparison.OrdinalIgnoreCase) || reader.GetInt32(3) != 1)
        { await writer.DisposeAsync(); throw new InvalidOperationException("Production integrated writer identity or permission mismatch."); }
        return writer;
    }

    private static async Task<CommitResult> CommitAsync(SqlConnection writer, Guid initial, Guid? recheck, string actor)
    {
        await using var command = writer.CreateCommand();
        command.CommandType = CommandType.StoredProcedure; command.CommandText = "dbo.CommitChemicalMeasurementWithTrace"; command.CommandTimeout = 60;
        command.Parameters.AddWithValue("@InitialReceiptId", initial); command.Parameters.AddWithValue("@RecheckReceiptId", recheck.HasValue ? recheck.Value : DBNull.Value); command.Parameters.AddWithValue("@Actor", actor);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Chemical commit returned no result.");
        return new(reader.GetInt64(reader.GetOrdinal("measurementId")), reader.GetBoolean(reader.GetOrdinal("alreadyProcessed")));
    }

    private static async Task<List<VariableMeasurement>> FindTaskMeasurementsAsync(AppDbContext db, IReadOnlyList<PlannedChemicalMeasurement> potential)
    {
        var hashes = potential.Select(x => Hash(x.SourceKey)).ToHashSet(StringComparer.Ordinal);
        return await db.VariableMeasurements.AsNoTracking().Where(x => x.ChemicalSourceKeyHash != null && hashes.Contains(x.ChemicalSourceKeyHash)).OrderBy(x => x.Id).ToListAsync();
    }

    private static async Task<int> VerifyImportedAsync(AppDbContext db, IReadOnlyList<VariableMeasurement> measurements, IReadOnlyList<PlannedChemicalMeasurement> potential)
    {
        var byHash = potential.ToDictionary(x => Hash(x.SourceKey), StringComparer.Ordinal);
        var trace = new ChemicalTraceQueryService(db); var checks = 0;
        foreach (var measurement in measurements)
        {
            var expected = byHash[measurement.ChemicalSourceKeyHash!];
            if (measurement.PartProcessCharacteristicId != expected.Source.PpcId || measurement.PortalDailyDate?.Date != expected.Source.Day.ToDateTime(TimeOnly.MinValue) ||
                measurement.SamplingPhase != expected.Source.SamplingPhase || measurement.SamplingStage != expected.Source.SamplingStage)
                throw new InvalidOperationException("Stored measurement identity mismatch: " + expected.Source.SourceId);
            var evidence = await trace.GetAsync(expected.Source.PpcId, null, measurement.Id, 0, 0, default) ?? throw new InvalidOperationException("Missing website trace: " + measurement.Id);
            var row = evidence.Rows.Single(); var initial = row.Calculations.Single(x => x.Phase == "Initial" && x.IsCurrent);
            if (initial.Primary != expected.Source.Primary || initial.Secondary != expected.Source.Secondary ||
                Math.Abs(decimal.Parse(initial.RawResult!, CultureInfo.InvariantCulture) - decimal.Parse(expected.Source.Excel, CultureInfo.InvariantCulture)) > .00000001m)
                throw new InvalidOperationException("Initial trace mismatch: " + expected.Source.SourceId);
            var receiptPayload = await db.ChemicalCalculationReceipts.AsNoTracking()
                .Where(x => x.Id == measurement.ChemicalReceiptId)
                .Select(x => x.PayloadJson).SingleAsync();
            using var payload = JsonDocument.Parse(receiptPayload);
            var originalInput = payload.RootElement.GetProperty("originalInput");
            using var source = JsonDocument.Parse(originalInput.GetProperty("SourceKey").GetString()!);
            if (source.RootElement.GetProperty("timeBasis").GetString() != OctoberProductionChemicalImportPlanner.TimeBasis)
                throw new InvalidOperationException("Historical time basis is missing from trace.");
            if (expected.Source.RecheckPrimary is not null)
            {
                var recheck = row.Calculations.Single(x => x.Phase == "Recheck" && x.IsCurrent);
                if (recheck.Primary != expected.Source.RecheckPrimary || Math.Abs(decimal.Parse(recheck.RawResult!, CultureInfo.InvariantCulture) - decimal.Parse(expected.Source.RecheckExcel!, CultureInfo.InvariantCulture)) > .00000001m)
                    throw new InvalidOperationException("Recheck trace mismatch: " + expected.Source.SourceId);
            }
            checks++;
        }
        return checks;
    }

    private static async Task<int> VerifyChartsAsync(AppDbContext db, int[] ppcIds, IConfiguration config)
    {
        var service = new SpcService(db, new SuppressedEmail(), config); var checks = 0;
        foreach (var id in ppcIds)
        {
            _ = await service.GetInteractiveChartAsync(id, null, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31, 23, 59, 59))
                ?? throw new InvalidOperationException("Formal chart service returned no result for PPC " + id); checks++;
        }
        return checks;
    }

    private static async Task VerifyBusinessKeysAsync(AppDbContext db, FormalManifest manifest)
    {
        var expected = manifest.Rows.GroupBy(x => x.PpcId).ToDictionary(x => x.Key, x => x.Select(y => y.BusinessKey).Distinct().Single());
        var ids = expected.Keys.ToArray();
        var actual = await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var (id, key) in expected)
            if (!actual.TryGetValue(id, out var item) || !item.IsEnabled || item.ControlScope != "CHEM" || item.Machine?.MachineCode != key.MachineCode || item.Tank?.TankCode != key.TankCode || item.Characteristic?.CharacteristicCode != key.CharacteristicCode || item.Unit != key.Unit)
                throw new InvalidOperationException("Production business identity changed for PPC " + id);
    }

    private static async Task VerifyActualDatabaseAsync(AppDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != "PMR_SPC_2026" || reader.GetString(1) != "172.16.110.16") throw new InvalidOperationException("Actual production SQL target mismatch.");
    }

    private static async Task<object> BackupAsync(string connection, string evidenceDirectory, string runId)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var command = sql.CreateCommand();
        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
        var directory = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("SQL backup directory unavailable.");
        var path = directory.TrimEnd('\\', '/') + $"\\PMR_SPC_2026_pre_oct_chemical_{runId}.bak"; command.CommandTimeout = 300;
        command.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT"; command.Parameters.AddWithValue("@path", path); await command.ExecuteNonQueryAsync();
        command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await command.ExecuteNonQueryAsync();
        var proof = new { server = "172.16.110.16", database = "PMR_SPC_2026", path, checksum = true, verifyOnly = true };
        File.WriteAllText(Path.Combine(evidenceDirectory, $"{runId}-backup.json"), JsonSerializer.Serialize(proof, JsonOptions)); return proof;
    }

    private static async Task<StateSnapshot> CaptureStateAsync(AppDbContext db, long? protectedMeasurementMaxId = null)
    {
        var max = protectedMeasurementMaxId ?? await db.VariableMeasurements.MaxAsync(x => (long?)x.Id) ?? 0;
        var old = await FingerprintAsync(db, "VariableMeasurements", $"WHERE Id<={max}");
        var currentPpc = await FingerprintAsync(db, "PartProcessCharacteristics", "");
        var activeF = await FingerprintAsync(db, "ChemicalFTableVersions", "WHERE IsActive=1") + ":" + await FingerprintAsync(db, "ChemicalFTableCells", "WHERE VersionId IN(SELECT Id FROM ChemicalFTableVersions WHERE IsActive=1)");
        var schedules = await FingerprintAsync(db, "ChemicalAnalysisFormulaVersions", "") + ":" + await FingerprintAsync(db, "ChemicalFormulaPolicies", "") + ":" + await FingerprintAsync(db, "ChemicalFormulaScheduleRevisions", "") + ":" + await FingerprintAsync(db, "ChemicalFormulaEffectivities", "");
        var counts = new Dictionary<string, long>();
        foreach (var table in new[] { "VariableMeasurements", "UploadBatches", "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalStatisticsJobs", "SpcCalculationResults", "AlertEvents" }) counts[table] = await CountAsync(db, table);
        return new(max, old, currentPpc, activeF, schedules, counts);
    }

    private static async Task<string> FingerprintAsync(AppDbContext db, string table, string where)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}] {where}";
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        return reader.GetInt64(0).ToString(CultureInfo.InvariantCulture) + ":" + (reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1).ToString(CultureInfo.InvariantCulture));
    }

    private static async Task<long> CountAsync(AppDbContext db, string table)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.[{table}]";
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static string CurrentActor()
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows operator identity is required.");
        var actor = WindowsIdentity.GetCurrent().Name.Split('\\').Last().Split('@').First();
        if (actor.Length is 0 or > 100) throw new InvalidOperationException("Windows operator identity is invalid."); return actor;
    }

    private static void RequireHash(string path, string expected)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (actual != expected) throw new InvalidOperationException("Reviewed evidence hash mismatch: " + Path.GetFileName(path));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class SuppressedEmail : IEmailNotificationService
    {
        public int Attempts { get; private set; }
        private Task<bool> Suppress() { Attempts++; return Task.FromResult(true); }
        public Task<bool> SendAlertEmailAsync(AlertEvent a, string b, string c, SmtpSettingsOverride? d = null, string? e = null) => Suppress();
        public Task<bool> SendTestEmailAsync(string a, SmtpSettingsOverride? b = null, string? c = null) => Suppress();
        public Task<bool> SendHtmlEmailAsync(string a, string b, string c, string d, SmtpSettingsOverride? e = null, string? f = null, string? g = null) => Suppress();
        public Task<bool> SendReportEmailAsync(string a, string b, string c, string d, byte[] e, string f, string? g = null) => Suppress();
    }

    private sealed record FormalManifest(string Schema, string Month, Dictionary<string, string> Inputs, List<SourceFile> Files, List<FormalRow> Rows);
    private sealed record SourceFile(string Path, string Sha256);
    private sealed record FormalRow(int PpcId, BusinessKey BusinessKey, string Day, string Formula, Dictionary<string, string> FCells, Dictionary<string, string> FLabels, string SourceId, string SourceFile, string SourceFileHash, string Sheet, int SourceRow, string? SamplingPhase, string? SamplingStage, string Primary, string? Secondary, string Excel, string Lower, string Upper, string Origin);
    private sealed record BusinessKey(string MachineCode, string TankCode, string CharacteristicCode, string Unit, string TankName, string CharacteristicName) { public string Text => $"{MachineCode}|{TankCode}|{CharacteristicCode}|{Unit}"; }
    private sealed record RawManifest(List<RawRow> Rows);
    private sealed record RawRow(string Sheet, int SourceRow, string Status, string FileHash, string FileDay, string SamplingPhase, string SamplingStage, string Primary, string? Secondary, string Excel, string? RecheckPrimary, string? RecheckExcel);
    private sealed record CommitResult(long MeasurementId, bool AlreadyProcessed);
    private sealed record StateSnapshot(long ExistingMeasurementMaxId, string ExistingMeasurementFingerprint, string CurrentPpc, string ActiveF, string FormulaSchedules, Dictionary<string, long> Counts);
}
