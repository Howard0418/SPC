using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record ChemicalHistoryRepairCandidate(long MeasurementId, int PpcId, Guid UploadBatchId,
    Guid InitialReceiptId, Guid? RecheckReceiptId, string SourceKeyHash, long MeasuredAtUtcTicks,
    string InitialStored, string? RecheckStored);
public sealed record ChemicalHistoryRepairManifest(int Version, string ContentHash, List<ChemicalHistoryRepairCandidate> Items);
public sealed record ChemicalHistoryRepairBlocked(long MeasurementId, int PpcId, DateTime MeasuredAt,
    string ReasonCode, string Suggestion);
public sealed record ChemicalHistoryRepairDryRun(int TotalUnknown, ChemicalHistoryRepairManifest Manifest,
    List<ChemicalHistoryRepairBlocked> Blocked);
public sealed record ChemicalHistoryRepairApplyResult(string ContentHash, int Updated, int NoChange);

/// <summary>
/// Repairs only a lost measurement-to-receipt link when immutable evidence already exists and is unique.
/// It never creates historical evidence or evaluates current formulas/specification limits.
/// </summary>
public sealed class ChemicalHistoryRepairService(AppDbContext db)
{
    public const string AuditAction = "ChemicalHistoryTraceLinkRepairV1";
    private const int ManifestVersion = 1;

    public async Task<ChemicalHistoryRepairDryRun> DryRunAsync(CancellationToken ct)
    {
        var chemicalIds = await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x => x.ControlScope == ControlScopeCodes.Chemical).Select(x => x.Id).ToListAsync(ct);
        var unlinked = await db.VariableMeasurements.AsNoTracking()
            .Where(x => chemicalIds.Contains(x.PartProcessCharacteristicId) && x.ChemicalReceiptId == null)
            .OrderBy(x => x.Id).ToListAsync(ct);
        var rows = new List<VariableMeasurement>();
        var history = new ChemicalHistoryQueryService(db);
        foreach (var chunk in unlinked.Chunk(200))
        {
            var presentations = await history.GetAsync(chunk);
            rows.AddRange(chunk.Where(x => presentations.TryGetValue(x.Id, out var value)
                && (value.Judgment == "Unknown" || value.JudgmentSource == "Unavailable")));
        }
        var evidence = await LoadEvidenceAsync(rows.Select(x => x.Id).ToList(), ct);
        var existingKeys = await db.VariableMeasurements.AsNoTracking()
            .Where(x => x.ChemicalSourceKeyHash != null && chemicalIds.Contains(x.PartProcessCharacteristicId))
            .Select(x => new { x.Id, x.PartProcessCharacteristicId, x.ChemicalSourceKeyHash }).ToListAsync(ct);
        var batches = (await db.UploadBatches.AsNoTracking().Where(x => rows.Select(r => r.UploadBatchId).Contains(x.UploadBatchId))
            .Select(x => x.UploadBatchId).ToListAsync(ct)).ToHashSet();
        var candidates = new List<ChemicalHistoryRepairCandidate>();
        var blocked = new List<ChemicalHistoryRepairBlocked>();
        foreach (var row in rows)
        {
            var result = BuildCandidate(row, evidence, batches, existingKeys
                .Where(x => x.Id != row.Id && x.PartProcessCharacteristicId == row.PartProcessCharacteristicId)
                .Select(x => x.ChemicalSourceKeyHash!).ToHashSet(StringComparer.Ordinal));
            if (result.Candidate is not null) candidates.Add(result.Candidate);
            else blocked.Add(new(row.Id, row.PartProcessCharacteristicId, row.MeasuredAt, result.ReasonCode!, result.Suggestion!));
        }
        return new(rows.Count, CreateManifest(candidates), blocked);
    }

    public async Task<ChemicalHistoryRepairApplyResult> ApplyAsync(ChemicalHistoryRepairManifest manifest, string actor, CancellationToken ct)
    {
        if (manifest.Version != ManifestVersion || manifest.ContentHash != Hash(manifest.Items))
            throw new InvalidOperationException("Repair manifest hash or version is invalid.");
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100)
            throw new ArgumentException("A valid repair actor is required.", nameof(actor));
        if (manifest.Items.Select(x => x.MeasurementId).Distinct().Count() != manifest.Items.Count)
            throw new InvalidOperationException("Repair manifest contains duplicate measurements.");
        if (manifest.Items.Count == 0) return new(manifest.ContentHash, 0, 0);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var ids = manifest.Items.Select(x => x.MeasurementId).ToList();
            var rows = await db.VariableMeasurements.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync(ct);
            if (rows.Count != ids.Count) throw new InvalidOperationException("Repair manifest is stale: measurement missing.");
            var evidence = await LoadEvidenceAsync(ids, ct);
            var batches = (await db.UploadBatches.AsNoTracking().Where(x => rows.Select(r => r.UploadBatchId).Contains(x.UploadBatchId))
                .Select(x => x.UploadBatchId).ToListAsync(ct)).ToHashSet();
            var existingKeys = await db.VariableMeasurements.AsNoTracking()
                .Where(x => x.ChemicalSourceKeyHash != null && !ids.Contains(x.Id))
                .Select(x => new { x.PartProcessCharacteristicId, x.ChemicalSourceKeyHash }).ToListAsync(ct);
            var byId = manifest.Items.ToDictionary(x => x.MeasurementId);
            var updated = new List<VariableMeasurement>();
            var noChange = 0;
            foreach (var row in rows)
            {
                var expected = byId[row.Id];
                var exactLink = row.ChemicalReceiptId == expected.InitialReceiptId
                    && row.ChemicalSourceKeyHash == expected.SourceKeyHash;
                if (!exactLink && (row.ChemicalReceiptId.HasValue || row.ChemicalSourceKeyHash is not null))
                    throw new InvalidOperationException($"Repair manifest is stale: measurement {row.Id} already has different evidence.");
                var result = BuildCandidate(row, evidence, batches, existingKeys
                    .Where(x => x.PartProcessCharacteristicId == row.PartProcessCharacteristicId)
                    .Select(x => x.ChemicalSourceKeyHash!).ToHashSet(StringComparer.Ordinal));
                if (result.Candidate is null || result.Candidate != expected)
                    throw new InvalidOperationException($"Repair manifest is stale: evidence changed for measurement {row.Id}.");
                if (exactLink) noChange++;
                else updated.Add(row);
            }

            // Complete validation for the whole manifest before changing tracked values.
            var now = DateTime.UtcNow;
            foreach (var row in updated)
            {
                var item = byId[row.Id];
                row.ChemicalReceiptId = item.InitialReceiptId;
                row.ChemicalSourceKeyHash = item.SourceKeyHash;
                row.UpdatedAt = now;
                row.UpdatedBy = actor;
            }
            foreach (var group in updated.GroupBy(x => x.UploadBatchId))
            {
                var nextRow = (await db.UploadDetails.Where(x => x.UploadBatchId == group.Key)
                    .MaxAsync(x => (int?)x.RowNo, ct) ?? 0) + 1;
                db.UploadDetails.Add(new UploadDetail
                {
                    UploadBatchId = group.Key, RowNo = nextRow, IsValid = false, CreatedBy = actor,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        action = AuditAction, actor, recordedAt = now, manifestHash = manifest.ContentHash,
                        measurementIds = group.Select(x => x.Id).Order().ToArray()
                    })
                });
            }
            if (updated.Count > 0) await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(manifest.ContentHash, updated.Count, noChange);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static ChemicalHistoryRepairManifest CreateManifest(List<ChemicalHistoryRepairCandidate> items)
    {
        var ordered = items.OrderBy(x => x.MeasurementId).ToList();
        return new(ManifestVersion, Hash(ordered), ordered);
    }

    private static string Hash(IReadOnlyList<ChemicalHistoryRepairCandidate> items)
    {
        var canonical = string.Join('\n', items.OrderBy(x => x.MeasurementId).Select(x => string.Join('|',
            x.MeasurementId, x.PpcId, x.UploadBatchId.ToString("N"), x.InitialReceiptId.ToString("N"),
            x.RecheckReceiptId?.ToString("N") ?? "", x.SourceKeyHash, x.MeasuredAtUtcTicks,
            x.InitialStored, x.RecheckStored ?? "")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static (ChemicalHistoryRepairCandidate? Candidate, string? ReasonCode, string? Suggestion) BuildCandidate(
        VariableMeasurement row, EvidenceSet evidence, HashSet<Guid> batches, HashSet<string> usedSourceKeys)
    {
        var own = evidence.Traces.Where(x => x.VariableMeasurementId == row.Id).ToList();
        if (own.Count == 0) return Block("NO_TRACE_EVIDENCE", "補上量測當時的Receipt／Trace、公式F與規格證據後再執行；不可套用目前主檔。");
        var initial = own.Where(x => x.Phase == "Initial").ToList();
        var recheck = own.Where(x => x.Phase == "Recheck").ToList();
        if (initial.Count != 1 || (row.RecheckValue.HasValue ? recheck.Count != 1 : recheck.Count != 0) || own.Count != initial.Count + recheck.Count)
            return Block("TRACE_NOT_UNIQUE", "每一階段須只保留一筆可驗證Trace；請先釐清重複或缺少的版本。");
        if (!batches.Contains(row.UploadBatchId)) return Block("UPLOAD_BATCH_MISSING", "補齊原匯入批次追溯後再執行。");
        var initialRead = ChemicalTraceQueryService.Read(initial[0], evidence.Receipts, evidence.Effects, evidence.Formulas, row.PartProcessCharacteristicId);
        var recheckRead = recheck.Count == 1
            ? ChemicalTraceQueryService.Read(recheck[0], evidence.Receipts, evidence.Effects, evidence.Formulas, row.PartProcessCharacteristicId)
            : null;
        if (initialRead.EvidenceStatus != "Recorded" || (recheckRead is not null && recheckRead.EvidenceStatus != "Recorded"))
            return Block("TRACE_EVIDENCE_INVALID", "修正Receipt Payload、Formula／F快照或Trace canonical值，使不可變證據可完整驗證。");
        if (!SameStored(initialRead.StoredResult, row.MeasuredValue)
            || (row.RecheckValue.HasValue && !SameStored(recheckRead?.StoredResult, row.RecheckValue.Value)))
            return Block("STORED_VALUE_MISMATCH", "核對量測值與Receipt保存結果；不可以其中一方覆蓋另一方。");
        if (initialRead.Judgment is not ("OK" or "NG") || (recheckRead is not null && recheckRead.Judgment is not ("OK" or "NG")))
            return Block("JUDGMENT_MISSING", "補齊量測當時的有效規格判定證據。");
        if (!evidence.Receipts.TryGetValue(initial[0].ReceiptId, out var initialReceipt)
            || (recheck.Count == 1 && !evidence.Receipts.TryGetValue(recheck[0].ReceiptId, out _)))
            return Block("RECEIPT_MISSING", "補齊Trace所引用的原始Receipt。");
        var recheckReceipt = recheck.Count == 1 ? evidence.Receipts[recheck[0].ReceiptId] : null;
        if (initialReceipt.PartProcessCharacteristicId != row.PartProcessCharacteristicId
            || recheckReceipt is not null && recheckReceipt.PartProcessCharacteristicId != row.PartProcessCharacteristicId
            || ReceiptWallClockTicks(initialReceipt.MeasuredAtUtc) != MeasurementTicks(row.MeasuredAt)
            || recheckReceipt is not null && ReceiptWallClockTicks(recheckReceipt.MeasuredAtUtc) != MeasurementTicks(row.MeasuredAt))
            return Block("IDENTITY_OR_TIME_MISMATCH", "核對PPC與實際量測時間；不得以匯入時間或目前日期替代。");
        if (!ValidSourceKey(initialReceipt) || recheckReceipt is not null
            && (!ValidSourceKey(recheckReceipt) || recheckReceipt.SourceKeyHash != initialReceipt.SourceKeyHash))
            return Block("SOURCE_KEY_MISMATCH", "核對Receipt的SourceKeyHash與Payload，初複驗須屬於同一來源鍵。");
        if (usedSourceKeys.Contains(initialReceipt.SourceKeyHash))
            return Block("SOURCE_KEY_ALREADY_LINKED", "此來源鍵已連結其他量測；請先釐清重複資料。");
        return (new(row.Id, row.PartProcessCharacteristicId, row.UploadBatchId, initialReceipt.Id,
            recheckReceipt?.Id, initialReceipt.SourceKeyHash, UtcTicks(row.MeasuredAt), initialRead.StoredResult!, recheckRead?.StoredResult), null, null);

        static (ChemicalHistoryRepairCandidate?, string, string) Block(string code, string suggestion) => (null, code, suggestion);
    }

    private static bool SameStored(string? canonical, double value) => decimal.TryParse(canonical,
        NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed == Convert.ToDecimal(value);
    private static long UtcTicks(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc).Ticks;
    // UploadService stores the explicit-offset Portal value as the Taipei wall-clock time, while receipts retain UTC.
    private static long ReceiptWallClockTicks(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(8).Ticks;
    private static long MeasurementTicks(DateTime stored) => DateTime.SpecifyKind(stored, DateTimeKind.Unspecified).Ticks;
    private static bool ValidSourceKey(ChemicalCalculationReceipt receipt)
    {
        if (receipt.SourceKeyHash.Length != 64) return false;
        try
        {
            using var json = JsonDocument.Parse(receipt.PayloadJson);
            return json.RootElement.TryGetProperty("sourceKeyHash", out var value)
                && value.ValueKind == JsonValueKind.String && value.GetString() == receipt.SourceKeyHash;
        }
        catch (JsonException) { return false; }
    }

    private async Task<EvidenceSet> LoadEvidenceAsync(List<long> measurementIds, CancellationToken ct)
    {
        var traces = await db.ChemicalCalculationTraces.AsNoTracking()
            .Where(x => measurementIds.Contains(x.VariableMeasurementId)).ToListAsync(ct);
        var receiptIds = traces.Select(x => x.ReceiptId).Distinct().ToList();
        var receipts = await db.ChemicalCalculationReceipts.AsNoTracking().Where(x => receiptIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var effectIds = receipts.Values.Select(x => x.EffectivityId).Distinct().ToList();
        var effects = await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => effectIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var formulaIds = effects.Values.Select(x => x.FormulaVersionId).Distinct().ToList();
        var formulas = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().Where(x => formulaIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return new(traces, receipts, effects, formulas);
    }

    private sealed record EvidenceSet(List<ChemicalCalculationTrace> Traces,
        Dictionary<Guid, ChemicalCalculationReceipt> Receipts,
        Dictionary<long, ChemicalFormulaEffectivity> Effects,
        Dictionary<long, ChemicalAnalysisFormulaVersion> Formulas);
}
