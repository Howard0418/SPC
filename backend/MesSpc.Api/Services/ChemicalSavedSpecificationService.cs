using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record PortalSavedSpecificationSnapshot(int Version, long MeasurementId, int PpcId, Guid BatchId,
    DateTime MeasuredAt, double InitialValue, double? RecheckValue, double? Lsl, double? Usl,
    bool RecordOnly, long? SegmentId);

/// <summary>Retains server-authored specification evidence independently of statistical chart configuration.</summary>
public sealed class ChemicalSavedSpecificationService(AppDbContext db)
{
    private const string Action = "PortalChemicalSpecificationSavedV1";

    /// <summary>Called inside the Portal confirmation transaction, after persisted values are available.</summary>
    public async Task CaptureAsync(Guid batchId, string actor, CancellationToken ct)
    {
        var rows = await db.VariableMeasurements.AsNoTracking()
            .Where(x => x.UploadBatchId == batchId && x.ChemicalReceiptId == null).ToListAsync(ct);
        if (rows.Count == 0) return;
        var ids = rows.Select(x => x.PartProcessCharacteristicId).Distinct().ToList();
        var mappings = await db.PartProcessCharacteristics.AsNoTracking().Include(x => x.Characteristic)
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var segments = await db.ControlLimitSegments.AsNoTracking()
            .Where(x => ids.Contains(x.PartProcessCharacteristicId)).ToListAsync(ct);
        var snapshots = rows.Select(row =>
        {
            var mapping = mappings[row.PartProcessCharacteristicId];
            var segment = segments.Where(x => x.PartProcessCharacteristicId == mapping.Id
                    && x.StartDate <= row.MeasuredAt && (x.EndDate == null || x.EndDate >= row.MeasuredAt))
                .OrderByDescending(x => x.StartDate).FirstOrDefault();
            return new PortalSavedSpecificationSnapshot(1, row.Id, mapping.Id, batchId, row.MeasuredAt,
                row.MeasuredValue, row.RecheckValue, segment?.LSL ?? mapping.LSL, segment?.USL ?? mapping.USL,
                string.Equals(mapping.Characteristic?.InputMode, "RECORD_ONLY", StringComparison.OrdinalIgnoreCase), segment?.Id);
        }).ToList();
        var nextRow = (await db.UploadDetails.Where(x => x.UploadBatchId == batchId)
            .MaxAsync(x => (int?)x.RowNo, ct) ?? 0) + 1;
        // Same server audit format as ManualMeasurementCreated/Updated; not an input/valid measurement row.
        db.UploadDetails.Add(new UploadDetail
        {
            UploadBatchId = batchId, RowNo = nextRow, IsValid = false, CreatedBy = actor,
            PayloadJson = JsonSerializer.Serialize(new { action = Action, actor, recordedAt = DateTime.UtcNow, details = snapshots })
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Ordinary uploaded string dictionaries cannot impersonate the structured server audit record.</summary>
    public async Task<Dictionary<long, List<PortalSavedSpecificationSnapshot>>> ReadAsync(IReadOnlyList<VariableMeasurement> rows)
    {
        var batches = rows.Select(x => x.UploadBatchId).Distinct().ToList();
        var audits = await db.UploadDetails.AsNoTracking().Where(x => batches.Contains(x.UploadBatchId) && !x.IsValid
                && x.PayloadJson.Contains(Action))
            .Select(x => new { x.UploadBatchId, x.PayloadJson }).ToListAsync();
        var snapshots = new List<PortalSavedSpecificationSnapshot>();
        foreach (var audit in audits)
        {
            try
            {
                using var json = JsonDocument.Parse(audit.PayloadJson);
                if (json.RootElement.ValueKind != JsonValueKind.Object
                    || !json.RootElement.TryGetProperty("action", out var action) || action.ValueKind != JsonValueKind.String
                    || action.GetString() != Action || !json.RootElement.TryGetProperty("details", out var details)
                    || details.ValueKind != JsonValueKind.Array) continue;
                var saved = details.Deserialize<List<PortalSavedSpecificationSnapshot>>();
                if (saved is not null) snapshots.AddRange(saved.Where(x => x is not null && x.BatchId == audit.UploadBatchId));
            }
            catch (JsonException) { /* Malformed historical audit JSON is not usable judgment evidence. */ }
        }
        return snapshots.GroupBy(x => x.MeasurementId).ToDictionary(x => x.Key, x => x.ToList());
    }

    public static ChemicalHistoryPresentation Present(VariableMeasurement row, IReadOnlyList<PortalSavedSpecificationSnapshot> snapshots)
    {
        var value = row.RecheckValue ?? row.MeasuredValue;
        if (snapshots.Count != 1) return Unavailable("保存規格快照重複或不完整，無法判定");
        var saved = snapshots[0];
        if (saved.Version != 1 || saved.MeasurementId != row.Id || saved.PpcId != row.PartProcessCharacteristicId
            || saved.BatchId != row.UploadBatchId || saved.MeasuredAt != row.MeasuredAt
            || saved.InitialValue != row.MeasuredValue || saved.RecheckValue != row.RecheckValue)
            return Unavailable("保存規格快照與量測資料不一致，無法判定");
        if (saved.RecordOnly) return Unavailable("此項目儲存時為僅紀錄模式，不作規格判定");
        if (!saved.Lsl.HasValue && !saved.Usl.HasValue) return Unavailable("此筆資料儲存時未設定規格上下限，無法判定");
        if ((saved.Lsl.HasValue && !double.IsFinite(saved.Lsl.Value))
            || (saved.Usl.HasValue && !double.IsFinite(saved.Usl.Value)) || saved.Lsl > saved.Usl)
            return Unavailable("保存的規格上下限無效，無法判定");
        string Judge(double input) => (saved.Lsl.HasValue && input < saved.Lsl.Value)
            || (saved.Usl.HasValue && input > saved.Usl.Value) ? "NG" : "OK";
        return new(value, row.RecheckValue.HasValue, Judge(row.MeasuredValue), Judge(value), "SavedSpecification",
            "依本次儲存時的規格快照判定，不受管制圖型或後續主檔變更影響");

        ChemicalHistoryPresentation Unavailable(string reason) => new(value, row.RecheckValue.HasValue,
            "Unknown", "Unknown", "Unavailable", reason);
    }
}
