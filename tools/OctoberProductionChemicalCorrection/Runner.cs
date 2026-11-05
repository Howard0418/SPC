using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace OctoberProductionChemicalCorrection;

internal static class ChemicalCorrectionRunner
{
    private const string FormalHash = "57fe52a02fae3127f596c1a3e2dc73f28d6e02109daf7592f7486a3aa168b49b";
    private const string PrimaryHash = "dc48a7e465c9bd800fddb4f29ae6c5e511edfbf2830b904c1ae6e03a14695f23";
    private const string SecondaryHash = "74645d538a4197209332a6f2c28277820f086761c54fef2e5055ca3fc7ae9018";
    private const string AuditHash = "0d760e2ff0747a79f7d0fc466f01a9b024cb3f777d9922fead2b72cf3a88889b";
    private const string ApiRoot = "https://pmr-spc-srv.pmr.com.tw:8443/api";
    private const string TaskCode = "SPC-CHEM-OCT-PROD-VALUE-CORRECTION-20261105";
    private const string TimeBasis = "preserve existing formal measured time; source report day and shift/stage are authoritative";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 7 || args[6] is not ("--plan" or "--apply" or "--verify"))
            throw new ArgumentException("CONFIG FORMAL PRIMARY SECONDARY AUDIT EVIDENCE_DIR --plan|--apply|--verify");
        var (configPath, formalPath, primaryPath, secondaryPath, auditPath, evidenceDirectory, mode) =
            (args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
        Directory.CreateDirectory(evidenceDirectory);
        RequireHash(formalPath, FormalHash); RequireHash(primaryPath, PrimaryHash); RequireHash(secondaryPath, SecondaryHash); RequireHash(auditPath, AuditHash);
        var sources = LoadSources(formalPath, primaryPath, secondaryPath, auditPath);
        if (sources.Count != 493) throw new InvalidOperationException("Reviewed correction scope is not 493 rows.");

        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(configPath)).Build();
        var rawConnection = config.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Production SQL connection is missing.");
        var cs = new SqlConnectionStringBuilder(rawConnection.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var versionText = await http.GetStringAsync(ApiRoot + "/version");
        var healthText = await http.GetStringAsync(ApiRoot + "/health");
        using var versionDocument = JsonDocument.Parse(versionText);
        ValidateTarget(config["AppEnvironment"], cs.DataSource, cs.InitialCatalog, versionDocument.RootElement.GetProperty("environment").GetString());
        var actor = CurrentActor();
        await using var db = CreateDb(cs.ConnectionString);
        await db.Database.OpenConnectionAsync();
        await VerifyDatabaseAsync(db);

        var expected = await ResolveExpectedAsync(db, sources, actor);
        var runId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var common = new
        {
            generatedAt = DateTimeOffset.Now, mode, environment = "production", database = "PMR_SPC_2026", actor,
            apiVersion = JsonSerializer.Deserialize<object>(versionText), apiHealth = JsonSerializer.Deserialize<object>(healthText),
            source = new { formal = FormalHash, primary = PrimaryHash, secondary = SecondaryHash, audit = AuditHash, rows = sources.Count }
        };

        if (mode == "--verify")
        {
            var verification = await VerifyAsync(db, expected);
            File.WriteAllText(Path.Combine(evidenceDirectory, $"{runId}-verify.json"), JsonSerializer.Serialize(new { common, verification }, JsonOptions));
            Console.WriteLine($"VERIFIED: corrected={verification.Corrected}; pending={verification.Pending}; measurements={verification.MeasurementCount}; traces={verification.TraceChecks}; charts={verification.ChartChecks}; statisticsFailed={verification.StatisticsStatuses.GetValueOrDefault("Failed")}");
            return 0;
        }

        var plan = BuildPlan(expected);
        var planPayload = new
        {
            common, pending = plan.Corrections.Count, alreadyCorrect = plan.AlreadyCorrect.Count,
            corrections = plan.Corrections.Select(x => new { x.SourceId, x.MeasurementId, x.RowVersion, x.BeforeInitial, x.BeforeRecheck, x.AfterInitial, x.AfterRecheck, x.Key, x.FileName, x.Sheet, x.SourceRow })
        };
        var planPath = Path.Combine(evidenceDirectory, $"{runId}-plan.json");
        File.WriteAllText(planPath, JsonSerializer.Serialize(planPayload, JsonOptions));
        if (mode == "--plan")
        {
            Console.WriteLine($"PLAN: pending={plan.Corrections.Count}; alreadyCorrect={plan.AlreadyCorrect.Count}; file={planPath}");
            return 0;
        }
        if (plan.Corrections.Count != 493 || plan.AlreadyCorrect.Count != 0)
            throw new InvalidOperationException($"Apply scope drifted: pending={plan.Corrections.Count}, already={plan.AlreadyCorrect.Count}.");

        var before = await CaptureStateAsync(db, expected.Select(x => x.Current.Id).ToArray());
        var backup = await BackupAsync(cs.ConnectionString, evidenceDirectory, runId);
        var apply = await ApplyAsync(cs.ConnectionString, expected, actor);
        db.ChangeTracker.Clear();
        var afterExpected = await ResolveExpectedAsync(db, sources, actor);
        var verificationAfter = await VerifyAsync(db, afterExpected);
        var after = await CaptureStateAsync(db, expected.Select(x => x.Current.Id).ToArray());
        if (before.MeasurementCount != after.MeasurementCount || before.NonTargetMeasurementFingerprint != after.NonTargetMeasurementFingerprint ||
            before.PpcFingerprint != after.PpcFingerprint || before.FormulaFFingerprint != after.FormulaFFingerprint)
            throw new InvalidOperationException("Protected production state changed outside the 493 corrected measurements.");
        if (verificationAfter.Corrected != 493 || verificationAfter.Pending != 0 || verificationAfter.MeasurementIds.Distinct().Count() != 493)
            throw new InvalidOperationException("Post-correction verification did not reach 493 exact rows.");
        var report = new { common, backup, before, apply, verification = verificationAfter, after };
        File.WriteAllText(Path.Combine(evidenceDirectory, $"{runId}-apply.json"), JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"APPLIED: updated={apply.Updated}; clearedRechecks={apply.ClearedRechecks}; receipts={apply.Receipts}; traces={apply.Traces}; statisticsCompleted={apply.StatisticsCompleted}; statisticsFailed={apply.StatisticsFailed}");
        return 0;
    }

    private static ChemicalCorrectionPlan BuildPlan(IReadOnlyList<ExpectedRow> expected) => ChemicalCorrectionPlanner.Build(
        expected.Select(x => new CorrectionSource(x.Source.SourceId, x.Source.PpcId, x.Source.Day, x.Source.SamplingPhase, x.Source.SamplingStage,
            x.InitialStored, x.RecheckStored, x.Source.FileName, x.Source.Sheet, x.Source.SourceExcelRow)).ToArray(),
        expected.Select(x => new CorrectionCurrent(x.Current.Id, x.Current.PartProcessCharacteristicId, DateOnly.FromDateTime(x.Current.PortalDailyDate!.Value),
            x.Current.SamplingPhase, x.Current.SamplingStage, x.Current.MeasuredValue, x.Current.RecheckValue, x.Current.ChemicalSourceKeyHash,
            Convert.ToBase64String(x.Current.RowVersion ?? []))).ToArray());

    private static async Task<IReadOnlyList<ExpectedRow>> ResolveExpectedAsync(AppDbContext db, IReadOnlyList<SourceRow> sources, string actor)
    {
        var ids = sources.Select(x => x.PpcId).Distinct().ToArray();
        var current = await db.VariableMeasurements.AsNoTracking().Where(x => !x.IsDeleted && ids.Contains(x.PartProcessCharacteristicId) &&
            x.PortalDailyDate >= new DateTime(2026, 10, 1) && x.PortalDailyDate < new DateTime(2026, 11, 1)).ToListAsync();
        var grouped = current.GroupBy(x => new CorrectionNaturalKey(x.PartProcessCharacteristicId, DateOnly.FromDateTime(x.PortalDailyDate!.Value), x.SamplingPhase, x.SamplingStage))
            .ToDictionary(x => x.Key, x => x.ToList());
        var schedule = new ChemicalFormulaScheduleService(db, new EphemeralDataProtectionProvider(), TimeProvider.System,
            NullLogger<ChemicalFormulaScheduleService>.Instance);
        var output = new List<ExpectedRow>();
        foreach (var source in sources.OrderBy(x => x.Day).ThenBy(x => x.PpcId).ThenBy(x => x.SourceId, StringComparer.Ordinal))
        {
            var key = new CorrectionNaturalKey(source.PpcId, source.Day, source.SamplingPhase, source.SamplingStage);
            if (!grouped.TryGetValue(key, out var candidates) || candidates.Count != 1)
                throw new InvalidOperationException("Expected exactly one formal measurement for " + key);
            var row = candidates[0];
            var sourceKey = SourceKey(source);
            var measuredAt = DateTime.SpecifyKind(row.MeasuredAt, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);
            var initialInput = new ChemicalPreviewInput(measuredAt, source.Primary, source.Secondary, sourceKey, "Initial");
            var initial = await schedule.PreviewAsync(source.PpcId, initialInput, actor, default);
            AssertRaw(initial.Result.Raw, source.Excel, source.SourceId);
            ChemicalPreviewResult? recheck = null;
            if (source.RecheckPrimary is not null)
            {
                var input = initialInput with { Primary = source.RecheckPrimary, Secondary = null, Phase = "Recheck" };
                recheck = await schedule.PreviewAsync(source.PpcId, input, actor, default);
                AssertRaw(recheck.Result.Raw, source.RecheckExcel!, source.SourceId + ":recheck");
            }
            output.Add(new(source, row, sourceKey, initialInput, initial,
                double.Parse(initial.Result.Stored, CultureInfo.InvariantCulture), recheck,
                recheck is null ? null : double.Parse(recheck.Result.Stored, CultureInfo.InvariantCulture)));
        }
        return output;
    }

    private static async Task<ApplyResult> ApplyAsync(string connectionString, IReadOnlyList<ExpectedRow> expected, string actor)
    {
        await using var db = CreateDb(connectionString);
        await db.Database.OpenConnectionAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ids = expected.Select(x => x.Current.Id).ToArray();
        var rows = await db.VariableMeasurements.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        if (rows.Count != 493) throw new InvalidOperationException("Correction measurement set changed.");
        foreach (var item in expected)
        {
            var row = rows[item.Current.Id];
            if (!string.IsNullOrWhiteSpace(row.ChemicalSourceKeyHash) || !Same(row.MeasuredValue, item.Current.MeasuredValue) || !Same(row.RecheckValue, item.Current.RecheckValue) ||
                !Convert.ToBase64String(row.RowVersion ?? []).Equals(Convert.ToBase64String(item.Current.RowVersion ?? []), StringComparison.Ordinal))
                throw new InvalidOperationException("Formal row drifted before correction: " + row.Id);
        }
        var oldTraces = await db.ChemicalCalculationTraces.AsNoTracking().Where(x => ids.Contains(x.VariableMeasurementId)).ToListAsync();
        if (oldTraces.Count != 0 || rows.Values.Any(x => x.ChemicalReceiptId.HasValue))
            throw new InvalidOperationException("Legacy correction scope unexpectedly already has chemical evidence.");

        var protection = new EphemeralDataProtectionProvider();
        var schedule = new ChemicalFormulaScheduleService(db, protection, TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
        var prepared = new Dictionary<long, PreparedRow>();
        foreach (var item in expected)
        {
            var initialPreview = await schedule.PreviewAsync(item.Source.PpcId, item.InitialInput, actor, default);
            AssertRaw(initialPreview.Result.Raw, item.Source.Excel, item.Source.SourceId);
            var initialReceipt = await schedule.PrepareAsync(item.Source.PpcId, new(item.InitialInput, initialPreview.Token), actor, default);
            ChemicalPreviewResult? recheckPreview = null; ChemicalCalculationReceipt? recheckReceipt = null;
            if (item.Source.RecheckPrimary is not null)
            {
                var recheckInput = item.InitialInput with { Primary = item.Source.RecheckPrimary, Secondary = null, Phase = "Recheck" };
                recheckPreview = await schedule.PreviewAsync(item.Source.PpcId, recheckInput, actor, default);
                AssertRaw(recheckPreview.Result.Raw, item.Source.RecheckExcel!, item.Source.SourceId + ":recheck");
                recheckReceipt = await schedule.PrepareAsync(item.Source.PpcId, new(recheckInput, recheckPreview.Token), actor, default);
            }
            prepared[item.Current.Id] = new(initialReceipt, initialPreview, recheckReceipt, recheckPreview);
        }

        var oldResults = await db.SpcCalculationResults.Where(x => x.VariableMeasurementId.HasValue && ids.Contains(x.VariableMeasurementId.Value) && !x.IsDeleted).ToListAsync();
        var oldAlerts = await db.AlertEvents.Where(x => x.VariableMeasurementId.HasValue && ids.Contains(x.VariableMeasurementId.Value) && !x.IsDeleted).ToListAsync();
        var now = DateTime.UtcNow; var rowNo = 0; var cleared = 0;
        foreach (var item in expected)
        {
            var row = rows[item.Current.Id]; var evidence = prepared[row.Id]; var batchId = Guid.NewGuid(); rowNo++;
            var batch = new UploadBatch { UploadBatchId = batchId, UploadType = "Variable", SourceType = "TransFilesChemicalCorrection",
                ImportStatus = "StatisticsPending", OriginalFileName = item.Source.FileName, FileHash = item.Source.FileHash,
                TotalRows = 1, ValidRows = 1, ErrorRows = 0, ConfirmedAt = now, CreatedAt = now, CreatedBy = actor };
            db.UploadBatches.Add(batch);
            db.UploadDetails.Add(new UploadDetail { UploadBatchId = batchId, RowNo = 1, IsValid = true, CreatedAt = now, CreatedBy = actor,
                PayloadJson = JsonSerializer.Serialize(new { task = TaskCode, item.Source.SourceId, measurementId = row.Id,
                    source = new { item.Source.FileName, item.Source.FileHash, item.Source.Sheet, item.Source.SourceExcelRow, item.Source.Day, item.Source.SamplingPhase, item.Source.SamplingStage, item.Source.Primary, item.Source.Secondary, item.Source.RecheckPrimary },
                    before = new { row.UploadBatchId, row.MeasuredValue, row.RecheckValue, row.AdjustAction, row.AdjustAmount, row.ChemicalReceiptId, row.ChemicalSourceKeyHash, row.SourceReference, row.UpdatedAt, row.UpdatedBy },
                    after = new { initial = evidence.InitialPreview.Result, recheck = evidence.RecheckPreview?.Result, initialReceiptId = evidence.InitialReceipt.Id, recheckReceiptId = evidence.RecheckReceipt?.Id }
                }, JsonOptions) });
            if (item.Source.RecheckPrimary is null && row.RecheckValue.HasValue) cleared++;
            row.MeasuredValue = double.Parse(evidence.InitialPreview.Result.Stored, CultureInfo.InvariantCulture);
            row.RecheckValue = evidence.RecheckPreview is null ? null : double.Parse(evidence.RecheckPreview.Result.Stored, CultureInfo.InvariantCulture);
            var final = evidence.RecheckPreview?.Result ?? evidence.InitialPreview.Result;
            row.AdjustAction = final.Adjustment; row.AdjustAmount = final.AdjustmentAmount;
            row.ChemicalReceiptId = evidence.InitialReceipt.Id; row.ChemicalSourceKeyHash = Hash(item.SourceKey);
            row.UploadBatchId = batchId; row.SourceType = SourceType.Excel; row.SourceReference = "TransFilesChemicalCorrection-v1";
            row.UpdatedAt = now; row.UpdatedBy = actor;
            db.ChemicalCalculationTraces.Add(new ChemicalCalculationTrace { VariableMeasurementId = row.Id, ReceiptId = evidence.InitialReceipt.Id,
                Phase = "Initial", VersionNo = 1, RawResultCanonical = evidence.InitialPreview.Result.Raw, StoredResultCanonical = evidence.InitialPreview.Result.Stored, CreatedAtUtc = now });
            if (evidence.RecheckReceipt is not null && evidence.RecheckPreview is not null)
                db.ChemicalCalculationTraces.Add(new ChemicalCalculationTrace { VariableMeasurementId = row.Id, ReceiptId = evidence.RecheckReceipt.Id,
                    Phase = "Recheck", VersionNo = 1, RawResultCanonical = evidence.RecheckPreview.Result.Raw, StoredResultCanonical = evidence.RecheckPreview.Result.Stored, CreatedAtUtc = now });
            db.ChemicalStatisticsJobs.Add(new ChemicalStatisticsJob { Id = Guid.NewGuid(), VariableMeasurementId = row.Id,
                ReceiptId = evidence.InitialReceipt.Id, Status = "Pending", Attempts = 0, CreatedAtUtc = now });
        }
        foreach (var result in oldResults) { result.IsDeleted = true; result.UpdatedAt = now; result.UpdatedBy = actor; }
        foreach (var alert in oldAlerts) { alert.IsDeleted = true; alert.UpdatedAt = now; alert.UpdatedBy = actor; alert.Status = "Closed"; alert.ClosedAt = now; alert.RootCause = "2026-11-05 依固定 Excel 來源更正舊量測值"; }
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        db.ChangeTracker.Clear();
        var jobIds = await db.ChemicalStatisticsJobs.AsNoTracking().Where(x => ids.Contains(x.VariableMeasurementId) && x.CreatedAtUtc >= now.AddSeconds(-1))
            .OrderBy(x => x.CreatedAtUtc).Select(x => x.Id).ToListAsync();
        var mail = new SuppressedEmail();
        var processor = new ChemicalStatisticsProcessor(db, new SpcService(db, mail, new ConfigurationBuilder().Build()),
            NullLogger<ChemicalStatisticsProcessor>.Instance);
        foreach (var jobId in jobIds) await processor.ProcessOneAsync(jobId, default);
        var statuses = await db.ChemicalStatisticsJobs.AsNoTracking().Where(x => jobIds.Contains(x.Id)).GroupBy(x => x.Status).ToDictionaryAsync(x => x.Key, x => x.Count());
        return new(493, cleared, prepared.Count + prepared.Values.Count(x => x.RecheckReceipt is not null),
            prepared.Count + prepared.Values.Count(x => x.RecheckReceipt is not null), oldResults.Count, oldAlerts.Count,
            statuses.GetValueOrDefault("Completed"), statuses.GetValueOrDefault("Failed"), mail.Attempts);
    }

    private static async Task<VerificationResult> VerifyAsync(AppDbContext db, IReadOnlyList<ExpectedRow> expected)
    {
        var pending = 0; var corrected = 0; var traceChecks = 0; var ids = new List<long>();
        foreach (var item in expected)
        {
            var row = item.Current; ids.Add(row.Id); var expectedHash = Hash(item.SourceKey);
            var values = Same(row.MeasuredValue, item.InitialStored) && Same(row.RecheckValue, item.RecheckStored);
            if (values && string.Equals(row.ChemicalSourceKeyHash, expectedHash, StringComparison.OrdinalIgnoreCase) && row.ChemicalReceiptId.HasValue)
            {
                var traces = await db.ChemicalCalculationTraces.AsNoTracking().Where(x => x.VariableMeasurementId == row.Id).ToListAsync();
                var initial = traces.SingleOrDefault(x => x.Phase == "Initial" && x.ReceiptId == row.ChemicalReceiptId);
                var recheck = traces.SingleOrDefault(x => x.Phase == "Recheck");
                if (initial is null || initial.StoredResultCanonical != item.InitialPreview.Result.Stored ||
                    (item.RecheckStored.HasValue ? recheck?.StoredResultCanonical != item.RecheckPreview!.Result.Stored : recheck is not null))
                    throw new InvalidOperationException("Trace verification failed for measurement " + row.Id);
                corrected++; traceChecks += traces.Count;
            }
            else if (!values && string.IsNullOrWhiteSpace(row.ChemicalSourceKeyHash)) pending++;
            else throw new InvalidOperationException("Correction verification found mixed state for measurement " + row.Id);
        }
        var ppcIds = expected.Select(x => x.Source.PpcId).Distinct().ToArray();
        var charts = new SpcService(db, new SuppressedEmail(), new ConfigurationBuilder().Build()); var chartChecks = 0;
        foreach (var ppcId in ppcIds)
        {
            _ = await charts.GetInteractiveChartAsync(ppcId, null, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31, 23, 59, 59))
                ?? throw new InvalidOperationException("Formal chart service returned no result for PPC " + ppcId);
            chartChecks++;
        }
        var currentReceiptIds = expected.Select(x => x.Current.ChemicalReceiptId).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var statuses = currentReceiptIds.Length == 0 ? new Dictionary<string, int>() : await db.ChemicalStatisticsJobs.AsNoTracking()
            .Where(x => ids.Contains(x.VariableMeasurementId) && currentReceiptIds.Contains(x.ReceiptId)).GroupBy(x => x.Status).ToDictionaryAsync(x => x.Key, x => x.Count());
        var blockers = await (from measurement in db.VariableMeasurements.AsNoTracking()
            join mapping in db.PartProcessCharacteristics.AsNoTracking() on measurement.PartProcessCharacteristicId equals mapping.Id
            join machine in db.Machines.AsNoTracking() on mapping.MachineId equals machine.Id
            join tank in db.Tanks.AsNoTracking() on mapping.TankId equals tank.Id
            join characteristic in db.QualityCharacteristics.AsNoTracking() on mapping.CharacteristicId equals characteristic.Id
            join job in db.ChemicalStatisticsJobs.AsNoTracking() on measurement.Id equals job.VariableMeasurementId
            where ids.Contains(measurement.Id) && currentReceiptIds.Contains(job.ReceiptId) && job.Status == "Failed"
            group job by new { mapping.Id, machine.MachineCode, tank.TankCode, tank.TankName, characteristic.CharacteristicCode, characteristic.CharacteristicName, mapping.ChartTypeId } into grouped
            orderby grouped.Key.Id
            select new StatisticsBlocker(grouped.Key.Id, grouped.Key.MachineCode, grouped.Key.TankCode, grouped.Key.TankName,
                grouped.Key.CharacteristicCode, grouped.Key.CharacteristicName, grouped.Key.ChartTypeId, grouped.Count(), grouped.Max(x => x.LastError)))
            .ToListAsync();
        return new(corrected, pending, await db.VariableMeasurements.LongCountAsync(), traceChecks, chartChecks, statuses, blockers, ids.ToArray());
    }

    private static List<SourceRow> LoadSources(string formalPath, string primaryPath, string secondaryPath, string auditPath)
    {
        var formal = JsonSerializer.Deserialize<FormalManifest>(File.ReadAllText(formalPath), JsonOptions) ?? throw new InvalidOperationException("Invalid formal manifest.");
        var primary = JsonSerializer.Deserialize<RawManifest>(File.ReadAllText(primaryPath), JsonOptions) ?? throw new InvalidOperationException("Invalid primary manifest.");
        var secondary = JsonSerializer.Deserialize<RawManifest>(File.ReadAllText(secondaryPath), JsonOptions) ?? throw new InvalidOperationException("Invalid secondary manifest.");
        var audit = JsonSerializer.Deserialize<AuditReport>(File.ReadAllText(auditPath), JsonOptions) ?? throw new InvalidOperationException("Invalid audit.");
        var targets = audit.Details.Where(x => x.Status == "既有資料值不符").Select(x => Key(x.SourceHash, x.Sheet, x.SourceRow)).ToHashSet(StringComparer.Ordinal);
        if (targets.Count != 493) throw new InvalidOperationException("Audit target count changed.");
        var main = primary.Rows.Where(x => x.Status == "validated").ToDictionary(x => Key(x.FileHash, x.Sheet, x.SourceRow), StringComparer.Ordinal);
        var extra = secondary.Rows.Where(x => x.Status == "validated").ToDictionary(x => Key(x.FileHash, x.Sheet, x.SourceRow), StringComparer.Ordinal);
        var output = new List<SourceRow>();
        foreach (var row in formal.Rows.Where(x => targets.Contains(Key(x.SourceFileHash, x.Sheet, x.SourceRow))))
        {
            var raw = (row.Origin == "secondary" ? extra : main)[Key(row.SourceFileHash, row.Sheet, row.SourceRow)];
            if (raw.FileDay != row.Day || raw.SamplingPhase != row.SamplingPhase || raw.SamplingStage != row.SamplingStage || raw.Excel != row.Excel)
                throw new InvalidOperationException("Pinned source drift: " + row.SourceId);
            output.Add(new(row.SourceId, row.PpcId, DateOnly.ParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture), row.SamplingPhase, row.SamplingStage,
                row.Primary, row.Secondary, row.Excel, raw.RecheckPrimary, raw.RecheckExcel, Path.GetFileName(row.SourceFile), row.SourceFileHash, row.Sheet, row.SourceRow));
        }
        if (output.Select(x => new CorrectionNaturalKey(x.PpcId, x.Day, x.SamplingPhase, x.SamplingStage)).Distinct().Count() != output.Count)
            throw new InvalidOperationException("Correction source natural keys are not unique.");
        return output;
    }

    private static string SourceKey(SourceRow row) => JsonSerializer.Serialize(new { kind = "TransFilesChemical-v1", fileHash = row.FileHash,
        sheet = row.Sheet, row = row.SourceExcelRow, samplingPhase = row.SamplingPhase, samplingStage = row.SamplingStage,
        task = TaskCode, timeBasis = TimeBasis, sourceId = row.SourceId });
    private static string Key(string hash, string sheet, int row) => $"{hash}|{sheet}|{row}";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool Same(double left, double right) => Math.Abs(left - right) <= 0.00000001;
    private static bool Same(double? left, double? right) => left.HasValue == right.HasValue && (!left.HasValue || Same(left.Value, right!.Value));
    private static void AssertRaw(string actual, string expected, string source)
    {
        if (Math.Abs(decimal.Parse(actual, CultureInfo.InvariantCulture) - decimal.Parse(expected, CultureInfo.InvariantCulture)) > .00000001m)
            throw new InvalidOperationException("SPC/Excel calculation mismatch: " + source);
    }
    private static void RequireHash(string path, string expected)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (actual != expected) throw new InvalidOperationException("Pinned evidence hash mismatch: " + Path.GetFileName(path));
    }
    private static string CurrentActor()
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Windows operator identity is required.");
        var actor = WindowsIdentity.GetCurrent().Name.Split('\\').Last().Split('@').First();
        if (actor.Length is 0 or > 100) throw new InvalidOperationException("Windows operator identity is invalid."); return actor;
    }
    private static AppDbContext CreateDb(string connection) => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
    private static void ValidateTarget(string? app, string? server, string? database, string? api)
    {
        if (!string.Equals(app, "production", StringComparison.OrdinalIgnoreCase) || !string.Equals(server, "172.16.110.16", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(database, "PMR_SPC_2026", StringComparison.OrdinalIgnoreCase) || !string.Equals(api, "production", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production target identity mismatch.");
    }
    private static async Task VerifyDatabaseAsync(AppDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        if (reader.GetString(0) != "PMR_SPC_2026" || reader.GetString(1) != "172.16.110.16") throw new InvalidOperationException("Actual production database mismatch.");
    }
    private static async Task<object> BackupAsync(string connection, string directory, string runId)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var command = sql.CreateCommand();
        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
        var root = (string?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("SQL backup directory unavailable.");
        var path = root.TrimEnd('\\', '/') + $"\\PMR_SPC_2026_pre_oct_493_correction_{runId}.bak"; command.CommandTimeout = 300;
        command.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT"; command.Parameters.AddWithValue("@path", path); await command.ExecuteNonQueryAsync();
        command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await command.ExecuteNonQueryAsync();
        var proof = new { server = "172.16.110.16", database = "PMR_SPC_2026", path, checksum = true, verifyOnly = true };
        File.WriteAllText(Path.Combine(directory, $"{runId}-backup.json"), JsonSerializer.Serialize(proof, JsonOptions)); return proof;
    }
    private static async Task<StateSnapshot> CaptureStateAsync(AppDbContext db, long[] targetIds)
    {
        var idList = string.Join(',', targetIds);
        var nonTarget = await FingerprintAsync(db, "VariableMeasurements", $"WHERE Id NOT IN ({idList})");
        var ppc = await FingerprintAsync(db, "PartProcessCharacteristics", "");
        var formulaF = await FingerprintAsync(db, "ChemicalAnalysisFormulaVersions", "") + ":" + await FingerprintAsync(db, "ChemicalFormulaEffectivities", "") + ":" + await FingerprintAsync(db, "ChemicalFTableVersionHistories", "");
        return new(await db.VariableMeasurements.LongCountAsync(), nonTarget, ppc, formulaF);
    }
    private static async Task<string> FingerprintAsync(AppDbContext db, string table, string where)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}] {where}";
        await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
        return reader.GetInt64(0) + ":" + (reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1));
    }
    private sealed class SuppressedEmail : IEmailNotificationService
    {
        public int Attempts { get; private set; }
        private Task<bool> Done() { Attempts++; return Task.FromResult(true); }
        public Task<bool> SendAlertEmailAsync(AlertEvent a, string b, string c, SmtpSettingsOverride? d = null, string? e = null) => Done();
        public Task<bool> SendTestEmailAsync(string a, SmtpSettingsOverride? b = null, string? c = null) => Done();
        public Task<bool> SendHtmlEmailAsync(string a, string b, string c, string d, SmtpSettingsOverride? e = null, string? f = null, string? g = null) => Done();
        public Task<bool> SendReportEmailAsync(string a, string b, string c, string d, byte[] e, string f, string? g = null) => Done();
    }

    private sealed record SourceRow(string SourceId, int PpcId, DateOnly Day, string SamplingPhase, string SamplingStage, string Primary, string? Secondary,
        string Excel, string? RecheckPrimary, string? RecheckExcel, string FileName, string FileHash, string Sheet, int SourceExcelRow);
    private sealed record ExpectedRow(SourceRow Source, VariableMeasurement Current, string SourceKey, ChemicalPreviewInput InitialInput,
        ChemicalPreviewResult InitialPreview, double InitialStored, ChemicalPreviewResult? RecheckPreview, double? RecheckStored);
    private sealed record PreparedRow(ChemicalCalculationReceipt InitialReceipt, ChemicalPreviewResult InitialPreview,
        ChemicalCalculationReceipt? RecheckReceipt, ChemicalPreviewResult? RecheckPreview);
    private sealed record ApplyResult(int Updated, int ClearedRechecks, int Receipts, int Traces, int SoftDeletedResults, int SoftDeletedAlerts,
        int StatisticsCompleted, int StatisticsFailed, int NotificationAttempts);
    private sealed record StatisticsBlocker(int PpcId, string MachineCode, string TankCode, string TankName, string CharacteristicCode,
        string CharacteristicName, int? ChartTypeId, int Count, string? LastError);
    private sealed record VerificationResult(int Corrected, int Pending, long MeasurementCount, int TraceChecks, int ChartChecks,
        Dictionary<string, int> StatisticsStatuses, List<StatisticsBlocker> StatisticsBlockers, long[] MeasurementIds);
    private sealed record StateSnapshot(long MeasurementCount, string NonTargetMeasurementFingerprint, string PpcFingerprint, string FormulaFFingerprint);
    private sealed record FormalManifest(List<FormalRow> Rows);
    private sealed record FormalRow(int PpcId, string Day, string SourceId, string SourceFile, string SourceFileHash, string Sheet, int SourceRow,
        string SamplingPhase, string SamplingStage, string Primary, string? Secondary, string Excel, string Origin);
    private sealed record RawManifest(List<RawRow> Rows);
    private sealed record RawRow(string FileHash, string FileDay, string Sheet, int SourceRow, string Status, string SamplingPhase, string SamplingStage,
        string Primary, string? Secondary, string Excel, string? RecheckPrimary, string? RecheckExcel);
    private sealed record AuditReport(List<AuditDetail> Details);
    private sealed record AuditDetail(string SourceHash, string Sheet, int SourceRow, string Status);
}
