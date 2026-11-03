using MesSpc.Api.Services.Localization;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record EtchPoint(string Side, int RepeatNo, int OpNo, decimal BeforeValue, decimal AfterValue);
public sealed record EtchReportRequest(DateTime ReportDate, string LineCode, decimal LineSpeed, string OperatorName, List<EtchPoint> Points, JsonElement? Provenance = null, int MeasurementNo = 1);
public sealed record EtchSyncResult(Guid BatchId, bool Unchanged, int PointCount);
public sealed record EtchImportInspection(string State, string BusinessKey, string LineCode, DateTime ReportDate,
    int SampleCountPerSide, int PointCount, int StoredPointCount, bool Verified, Guid BatchId,
    int AdditionalExistingRowCount, string Message, int MeasurementNo, int? StoredMeasurementNo, string? StoredReportHash = null);

/// <summary>Whole etch report synchronization. A report is one subgroup per side, never a PortalDaily scalar upsert.</summary>
public sealed class EtchReportSyncService(AppDbContext db)
{
    public static readonly string[] Codes = ["ETCH_A_AVG", "ETCH_B_AVG", "ETCH_RATE"];
    public static bool RequiresWriteTransaction(bool preview) => !preview;
    public static (int Ops, decimal Length) Rule(string line) => line switch
    {
        "PT1" => (5, .975m),
        "PT2" => (10, .975m),
        "QE1" => (5, 1.944m),
        "QE2" => (10, 2.268m),
        _ => throw ApiUiException.Attach(new ArgumentException("只支援PT1、PT2、QE1、QE2。"), "spc.api.etchLineUnsupported")
    };

    /// <summary>Validate a complete paired report; blank points and negative differences must not enter a subgroup.</summary>
    public static void Validate(EtchReportRequest r)
    {
        var (ops, _) = Rule(r.LineCode);
        if (r.ReportDate == default || r.ReportDate != r.ReportDate.Date || r.MeasurementNo is not (1 or 2) || r.LineSpeed <= 0 || r.LineSpeed > 10000 || string.IsNullOrWhiteSpace(r.OperatorName) || r.OperatorName.Length > 100)
            throw ApiUiException.Attach(new ArgumentException("日期、量測人員或線速無效。"), "spc.api.etchFieldsInvalid");
        if (r.Points is null || r.Points.Count != ops * 10 || r.Points.Any(p => p.Side is not ("A" or "B") || p.RepeatNo < 1 || p.RepeatNo > 5 || p.OpNo < 1 || p.OpNo > ops || p.BeforeValue < 0 || p.AfterValue < 0 || p.BeforeValue < p.AfterValue)
            || r.Points.Select(p => (p.Side, p.RepeatNo, p.OpNo)).Distinct().Count() != ops * 10)
            throw ApiUiException.Attach(new ArgumentException("原始點位缺漏、重複或有負咬蝕量，整份日報未同步。"), "spc.api.etchSamplesInvalid");
    }

    /// <summary>Strict imports reject changed content; explicit Portal saves may update only records owned by this report.</summary>
    public Task<EtchSyncResult> SyncAsync(EtchReportRequest request, string actor, bool allowUpdate, CancellationToken ct = default)
        => ExecuteAsync(request, actor, allowUpdate, false, ct);

    /// <summary>Validate mapping and content without creating a batch or measurement.</summary>
    public Task<EtchSyncResult> PreviewAsync(EtchReportRequest request, CancellationToken ct = default)
        => ExecuteAsync(request, "preview", false, true, ct);

    /// <summary>Read actual report ownership, subgroup structure and values, independently of legacy batch hashes.</summary>
    public async Task<EtchImportInspection> InspectAsync(EtchReportRequest request, CancellationToken ct = default)
    {
        Validate(request);
        var (machine, mappings) = await ResolveMappingsAsync(request.LineCode, ct);
        var desired = BuildDesired(request, mappings);
        var key = $"ETCH:{request.ReportDate:yyyy-MM-dd}:{request.LineCode}";
        var rows = await db.VariableMeasurements.IgnoreQueryFilters().AsNoTracking().Where(x => x.SourceReference == key).ToListAsync(ct);
        var ids = mappings.Select(x => x.Id).ToArray();
        var canonical = rows.Where(x => ids.Contains(x.PartProcessCharacteristicId)).ToList();
        int? storedMeasurementNo = null; string? storedReportHash = null;
        EtchImportInspection Result(string state, bool verified, string message) => new(state, key, request.LineCode, request.ReportDate,
            Rule(request.LineCode).Ops * 5, desired.Count, canonical.Count, verified, canonical.FirstOrDefault()?.UploadBatchId ?? Guid.Empty,
            rows.Count - canonical.Count, message, request.MeasurementNo, storedMeasurementNo, storedReportHash);
        if (rows.Count == 0)
        {
            await PreviewAsync(request, ct); // Includes same-day records owned by a different source.
            return Result("NEW", false, "尚未匯入，可進行嚴格預覽。");
        }
        if (rows.Any(x => x.IsDeleted || x.MeasuredAt.Date != request.ReportDate || x.MachineId != machine.Id || x.ProcessId != machine.ProcessId))
            return Result("CONFLICT", false, "同日線已有封存或日期／線別不符紀錄，不重新匯入。");
        var extraIds = rows.Where(x => !ids.Contains(x.PartProcessCharacteristicId)).Select(x => x.PartProcessCharacteristicId).Distinct().ToArray();
        if (extraIds.Length > 0)
        {
            var legacySpeedIds = await db.PartProcessCharacteristics.IgnoreQueryFilters().AsNoTracking()
                .Where(x => extraIds.Contains(x.Id) && x.ProcessId == machine.ProcessId && x.Characteristic!.CharacteristicCode == "ETCH_LINE_SPEED")
                .Select(x => x.Id).ToListAsync(ct);
            if (extraIds.Except(legacySpeedIds).Any())
                return Result("CONFLICT", false, "同日線已存在於其他管制項目，不重新匯入。");
        }
        var expected = desired.ToDictionary(x => (x.Mapping.Id, x.SampleNo), x => x.Value);
        if (canonical.Count != desired.Count || canonical.Select(x => (x.PartProcessCharacteristicId, x.SampleNo)).Distinct().Count() != desired.Count
            || canonical.Any(x => !double.IsFinite(x.MeasuredValue) || !expected.ContainsKey((x.PartProcessCharacteristicId, x.SampleNo))))
            return Result("CONFLICT", false, "同日線既有樣本數或點位不完整，不補寫或覆寫；請先核對。");
        storedMeasurementNo = await ReadStoredMeasurementNoAsync(request, canonical.Select(x => x.UploadBatchId).Distinct().ToArray(), ct);
        if (request.MeasurementNo == 1 && storedMeasurementNo == 2)
            return Result("CONFLICT", false, "已保存第二次量測，第一次不得反向覆蓋。");
        if (request.MeasurementNo == 2 && storedMeasurementNo == 1)
        {
            storedReportHash = await StoredReportHashAsync(canonical[0].UploadBatchId, ct);
            if (storedReportHash is null) return Result("CONFLICT", false, "第一次原始來源不完整，無法安全取代；請先核對。");
            return Result("REPLACE_FIRST", false, "完整第二次量測可取代已確認第一次；保留第一次批次與原值。");
        }
        if (request.MeasurementNo == 2 && storedMeasurementNo is null)
            return Result("CONFLICT", false, "舊資料未記錄量測次數，無法確認為第一次；請先核對原來源，不自動覆寫。");
        if (canonical.Any(x => Math.Abs(x.MeasuredValue - expected[(x.PartProcessCharacteristicId, x.SampleNo)]) > 1e-8))
            return Result("CONFLICT", false, "同次量測的既有數值不同，不覆寫；請先核對。");
        return Result("ALREADY_EXISTS", true, "已存在且完整原值核對通過，略過匯入。");
    }

    private async Task<int?> ReadStoredMeasurementNoAsync(EtchReportRequest request, Guid[] batches, CancellationToken ct)
    {
        if (batches.Length != 1) return null;
        var payloads = await db.UploadDetails.AsNoTracking().Where(x => x.UploadBatchId == batches[0] && x.RowNo == 1)
            .Select(x => x.PayloadJson).Take(2).ToListAsync(ct);
        if (payloads.Count != 1) return null;
        try
        {
            using var doc = JsonDocument.Parse(payloads[0]);
            if (!doc.RootElement.TryGetProperty("Report", out var old)) return null;
            var saved = old.Deserialize<EtchReportRequest>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (saved is null || saved.ReportDate != request.ReportDate || saved.LineCode != request.LineCode) return null;
            if (old.TryGetProperty("MeasurementNo", out var no) && no.TryGetInt32(out var value)) return value is 1 or 2 ? value : null;
            // Legacy imports did not store the line label. Same immutable workbook hash + complete original pairs
            // proves this request is already the stored source; this permits read-only recognition, never replacement.
            static string? Hash(JsonElement? provenance) => provenance is { ValueKind: JsonValueKind.Object } p
                && p.TryGetProperty("fileSha256", out var hash) && hash.ValueKind == JsonValueKind.String ? hash.GetString() : null;
            var sourceHash = Hash(saved.Provenance);
            if (sourceHash?.Length != 64 || !sourceHash.All(Uri.IsHexDigit) || !string.Equals(sourceHash, Hash(request.Provenance), StringComparison.OrdinalIgnoreCase)
                || saved.Points is null || saved.Points.Count != request.Points.Count || Math.Abs(saved.LineSpeed - request.LineSpeed) > .00000001m) return null;
            var a = saved.Points.OrderBy(x => x.Side).ThenBy(x => x.RepeatNo).ThenBy(x => x.OpNo).ToArray();
            var b = request.Points.OrderBy(x => x.Side).ThenBy(x => x.RepeatNo).ThenBy(x => x.OpNo).ToArray();
            return a.Zip(b).All(p => p.First.Side == p.Second.Side && p.First.RepeatNo == p.Second.RepeatNo && p.First.OpNo == p.Second.OpNo
                && Math.Abs(p.First.BeforeValue - p.Second.BeforeValue) < .00000001m && Math.Abs(p.First.AfterValue - p.Second.AfterValue) < .00000001m) ? request.MeasurementNo : null;
        }
        catch (JsonException) { return null; }
    }

    /// <summary>Cross-system first-report fingerprint: length-prefixed invariant fields, sorted full before/after pairs.</summary>
    public static string ContentHash(EtchReportRequest request)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var fields = new List<string> { request.ReportDate.ToString("yyyy-MM-dd", culture), request.LineCode, request.LineSpeed.ToString("G29", culture), request.OperatorName };
        foreach (var p in request.Points.OrderBy(x => x.Side, StringComparer.Ordinal).ThenBy(x => x.RepeatNo).ThenBy(x => x.OpNo))
            fields.AddRange([p.Side, p.RepeatNo.ToString(culture), p.OpNo.ToString(culture), p.BeforeValue.ToString("G29", culture), p.AfterValue.ToString("G29", culture)]);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(fields.Select(x => x.Length.ToString(culture) + ":" + x)))));
    }

    private async Task<string?> StoredReportHashAsync(Guid batch, CancellationToken ct)
    {
        var payload = await db.UploadDetails.AsNoTracking().Where(x => x.UploadBatchId == batch && x.RowNo == 1).Select(x => x.PayloadJson).FirstOrDefaultAsync(ct);
        try
        {
            if (payload is null) return null;
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("Report", out var report)) return null;
            var saved = report.Deserialize<EtchReportRequest>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (saved is null) return null; Validate(saved); return ContentHash(saved);
        }
        catch (JsonException) { return null; }
        catch (ArgumentException) { return null; }
    }

    /// <summary>Governed import: insert, immutable skip, or an explicitly identified complete first-to-second replacement.</summary>
    public async Task<EtchSyncResult> ImportGovernedAsync(EtchReportRequest request, string actor, CancellationToken ct = default)
    {
        Validate(request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (db.Database.IsSqlServer())
        {
            var key = $"ETCH:{request.ReportDate:yyyy-MM-dd}:{request.LineCode}";
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={key}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Etch report lock unavailable', 1;", ct);
        }
        var check = await InspectAsync(request, ct);
        if (check.State == "ALREADY_EXISTS") { await tx.CommitAsync(ct); return new(check.BatchId, true, check.PointCount); }
        if (check.State is not ("NEW" or "REPLACE_FIRST")) throw new InvalidOperationException(check.Message);
        var result = await SyncAsync(request, actor, check.State == "REPLACE_FIRST", ct);
        var verified = await InspectAsync(request, ct);
        if (verified.State != "ALREADY_EXISTS" || !verified.Verified) throw new InvalidOperationException("匯入後完整性核對失敗，已回復本次交易。");
        await tx.CommitAsync(ct); return result;
    }

    /// <summary>Insert a new complete report or read-only skip a verified existing report; never update existing rows.</summary>
    public async Task<EtchSyncResult> ImportOnlyAsync(EtchReportRequest request, string actor, CancellationToken ct = default)
    {
        var check = await InspectAsync(request, ct);
        if (check.State == "ALREADY_EXISTS") return new(check.BatchId, true, check.PointCount);
        if (check.State != "NEW") throw new InvalidOperationException(check.Message);
        try { return await SyncAsync(request, actor, false, ct); }
        catch (InvalidOperationException)
        {
            // A concurrent writer may have committed after inspection. Re-read; never change its rows.
            var retry = await InspectAsync(request, ct);
            if (retry.State == "ALREADY_EXISTS") return new(retry.BatchId, true, retry.PointCount);
            throw;
        }
    }

    private static List<(PartProcessCharacteristic Mapping, int SampleNo, double Value)> BuildDesired(EtchReportRequest request, List<PartProcessCharacteristic> mappings)
    {
        var (ops, length) = Rule(request.LineCode);
        var ordered = request.Points.OrderBy(p => p.Side).ThenBy(p => p.RepeatNo).ThenBy(p => p.OpNo).ToList();
        var desired = new List<(PartProcessCharacteristic Mapping, int SampleNo, double Value)>();
        foreach (var m in mappings)
            if (m.Characteristic!.CharacteristicCode is "ETCH_A_AVG" or "ETCH_B_AVG")
                desired.AddRange(ordered.Where(p => p.Side == (m.Characteristic.CharacteristicCode == "ETCH_A_AVG" ? "A" : "B"))
                    .Select(p => (m, (p.RepeatNo - 1) * ops + p.OpNo, (double)(p.BeforeValue - p.AfterValue))));
            else desired.Add((m, 1, (double)(ordered.Average(p => p.BeforeValue - p.AfterValue) * request.LineSpeed / length)));
        return desired;
    }

    /// <summary>Prefer line-specific mappings; accept optional-machine mappings only for a uniquely owned line process.</summary>
    public async Task<(Machine Machine, List<PartProcessCharacteristic> Mappings)> ResolveMappingsAsync(string line, CancellationToken ct = default)
    {
        var (ops, _) = Rule(line);
        var machine = await db.Machines.AsNoTracking().SingleOrDefaultAsync(x => x.MachineCode == line && x.IsEnabled, ct)
            ?? throw ApiUiException.Attach(new InvalidOperationException($"找不到已啟用的咬蝕線別主檔：{line}。"), "spc.api.etchLineMissing", new Dictionary<string,string> { ["line"] = line });
        var query = db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Characteristic)
            .Where(x => x.ControlScope == "PROCESS" && x.IsEnabled && Codes.Contains(x.Characteristic!.CharacteristicCode));
        var mappings = await query.Where(x => x.MachineId == machine.Id).ToListAsync(ct);
        if (mappings.Count == 0)
        {
            var processMatches = await db.Processes.AnyAsync(x => x.Id == machine.ProcessId && x.ProcessCode == line && x.IsEnabled, ct);
            var machineCount = await db.Machines.CountAsync(x => x.ProcessId == machine.ProcessId && x.IsEnabled, ct);
            if (!processMatches || machineCount != 1)
                throw ApiUiException.Attach(new InvalidOperationException("咬蝕量／速率管制項目未完整設定。"), "spc.api.etchMappingsIncomplete");
            mappings = await query.Where(x => x.MachineId == null && x.ProcessId == machine.ProcessId).ToListAsync(ct);
        }
        if (mappings.Count != 3 || mappings.Select(x => x.Characteristic!.CharacteristicCode).Distinct().Count() != 3
            || mappings.Any(x => x.ProcessId != machine.ProcessId))
            throw ApiUiException.Attach(new InvalidOperationException("咬蝕量／速率管制項目未完整設定。"), "spc.api.etchMappingsIncomplete");
        foreach (var m in mappings)
            if (m.SampleSize != (m.Characteristic!.CharacteristicCode is "ETCH_A_AVG" or "ETCH_B_AVG" ? ops * 5 : 1))
                throw ApiUiException.Attach(new InvalidOperationException("管制項目樣本數與完整子組不一致。"), "spc.api.etchSampleCountMismatch");
        return (machine, mappings);
    }

    private async Task<EtchSyncResult> ExecuteAsync(EtchReportRequest request, string actor, bool allowUpdate, bool preview, CancellationToken ct)
    {
        Validate(request);
        var ordered = request.Points.OrderBy(p => p.Side).ThenBy(p => p.RepeatNo).ThenBy(p => p.OpNo).ToList();
        var content = request.MeasurementNo == 1
            ? JsonSerializer.Serialize(new { request.ReportDate, request.LineCode, request.LineSpeed, request.OperatorName, Points = ordered })
            : JsonSerializer.Serialize(new { request.ReportDate, request.LineCode, request.LineSpeed, request.OperatorName, request.MeasurementNo, Points = ordered });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var key = $"ETCH:{request.ReportDate:yyyy-MM-dd}:{request.LineCode}";
        await using var tx = RequiresWriteTransaction(preview) && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        if (!preview && db.Database.IsSqlServer())
        {
            // Transaction-owned lock also serializes first insertion, when no measurement rows exist yet.
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={key}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Etch report lock unavailable', 1;", ct);
        }
        var (machine, mappings) = await ResolveMappingsAsync(request.LineCode, ct);
        var ids = mappings.Select(x => x.Id).ToList(); var next = request.ReportDate.AddDays(1);
        if (!allowUpdate && await db.VariableMeasurements.IgnoreQueryFilters().AnyAsync(x => x.SourceReference == key
            && (x.IsDeleted || !ids.Contains(x.PartProcessCharacteristicId) || x.MeasuredAt < request.ReportDate || x.MeasuredAt >= next), ct))
            throw ApiUiException.Attach(new InvalidOperationException("同日同線已有其他結構或封存紀錄，嚴格匯入拒絕重複。"), "spc.api.etchOverwriteDenied");
        var existing = await db.VariableMeasurements.Where(x => ids.Contains(x.PartProcessCharacteristicId) && x.MeasuredAt >= request.ReportDate && x.MeasuredAt < next).ToListAsync(ct);
        if (existing.Any(x => x.SourceReference != key)) throw ApiUiException.Attach(new InvalidOperationException("同日已有其他來源資料，請先核對，未覆寫。"), "spc.api.etchSourceConflict");
        if (existing.GroupBy(x => (x.PartProcessCharacteristicId, x.SampleNo)).Any(g => g.Count() != 1)) throw ApiUiException.Attach(new InvalidOperationException("既有子組點位重複，未覆寫。"), "spc.api.etchExistingDuplicate");
        var desired = BuildDesired(request, mappings);
        var expected = desired.ToDictionary(x => (x.Mapping.Id, x.SampleNo), x => x.Value);
        if (existing.Any(x => !expected.ContainsKey((x.PartProcessCharacteristicId, x.SampleNo)))) throw ApiUiException.Attach(new InvalidOperationException("既有子組結構衝突。"), "spc.api.etchExistingStructureConflict");
        if (existing.Count == desired.Count && existing.All(x => Math.Abs(x.MeasuredValue - expected[(x.PartProcessCharacteristicId, x.SampleNo)]) < 1e-10 && x.Operator == request.OperatorName))
        {
            var batchId = existing[0].UploadBatchId;
            var oldHash = await db.UploadBatches.Where(b => b.UploadBatchId == batchId).Select(b => b.FileHash).SingleOrDefaultAsync(ct)
                ?? throw ApiUiException.Attach(new InvalidOperationException($"同日同線既有資料缺少匯入批次：{key}，請先核對既有資料來源。"), "spc.api.etchBatchMissing", new Dictionary<string,string> { ["source"] = key });
            if (oldHash == hash) { if(tx is not null)await tx.CommitAsync(ct); return new(batchId, true, desired.Count); }
        }
        if (existing.Count > 0 && !allowUpdate) throw ApiUiException.Attach(new InvalidOperationException("同日同線內容不同，嚴格匯入拒絕覆寫。"), "spc.api.etchOverwriteDenied");
        if (preview) return new(Guid.Empty, false, desired.Count);
        var batch = new UploadBatch { SourceType = "EtchReport", UploadType = "Variable", ImportStatus = "Imported", OriginalFileName = key, FileHash = hash, TotalRows = desired.Count, ValidRows = desired.Count, ConfirmedAt = DateTime.UtcNow, CreatedBy = actor };
        db.UploadBatches.Add(batch);
        db.UploadDetails.Add(new() { UploadBatchId = batch.UploadBatchId, RowNo = 1, IsValid = true, CreatedBy = actor, PayloadJson = JsonSerializer.Serialize(new { BusinessKey = key, Actor = actor, Before = existing.Select(x => new { x.Id, x.UploadBatchId, x.SampleNo, x.PartProcessCharacteristicId, x.MeasuredValue, x.Operator }).ToList(), Report = request }) });
        foreach (var item in desired)
        {
            var row = existing.SingleOrDefault(x => x.PartProcessCharacteristicId == item.Mapping.Id && x.SampleNo == item.SampleNo);
            if (row is null) { row = new() { PartId = item.Mapping.PartId ?? 0, ProcessId = item.Mapping.ProcessId, MachineId = machine.Id, CharacteristicId = item.Mapping.CharacteristicId, PartProcessCharacteristicId = item.Mapping.Id, SampleNo = item.SampleNo, MeasuredAt = request.ReportDate, SourceReference = key, SerialNo = key, LotNo = key, SourceType = SourceType.Manual, CreatedBy = actor }; db.VariableMeasurements.Add(row); }
            row.MeasuredValue = item.Value; row.Operator = request.OperatorName; row.UploadBatchId = batch.UploadBatchId; row.UpdatedAt = DateTime.UtcNow; row.UpdatedBy = actor;
        }
        await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct);
        return new(batch.UploadBatchId, false, desired.Count);
    }
}
