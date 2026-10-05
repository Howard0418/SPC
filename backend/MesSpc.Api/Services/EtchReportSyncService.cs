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
public sealed record EtchReportRequest(DateTime ReportDate, string LineCode, decimal LineSpeed, string OperatorName, List<EtchPoint> Points, JsonElement? Provenance = null);
public sealed record EtchSyncResult(Guid BatchId, bool Unchanged, int PointCount);

/// <summary>Whole etch report synchronization. A report is one subgroup per side, never a PortalDaily scalar upsert.</summary>
public sealed class EtchReportSyncService(AppDbContext db)
{
    public static readonly string[] Codes = ["ETCH_A_AVG", "ETCH_B_AVG", "ETCH_RATE"];
    public static (int Ops, decimal Length) Rule(string line) => line switch
    {
        "PT1" => (5, .975m),
        "PT2" => (10, .975m),
        "QE1" => (5, 1.944m),
        "QE2" => (10, 2.268m),
        _ => throw new ArgumentException("只支援PT1、PT2、QE1、QE2。")
    };

    /// <summary>Validate a complete paired report; blank points and negative differences must not enter a subgroup.</summary>
    public static void Validate(EtchReportRequest r)
    {
        var (ops, _) = Rule(r.LineCode);
        if (r.ReportDate == default || r.ReportDate != r.ReportDate.Date || r.LineSpeed <= 0 || r.LineSpeed > 10000 || string.IsNullOrWhiteSpace(r.OperatorName) || r.OperatorName.Length > 100)
            throw new ArgumentException("日期、量測人員或線速無效。");
        if (r.Points is null || r.Points.Count != ops * 10 || r.Points.Any(p => p.Side is not ("A" or "B") || p.RepeatNo < 1 || p.RepeatNo > 5 || p.OpNo < 1 || p.OpNo > ops || p.BeforeValue < 0 || p.AfterValue < 0 || p.BeforeValue < p.AfterValue)
            || r.Points.Select(p => (p.Side, p.RepeatNo, p.OpNo)).Distinct().Count() != ops * 10)
            throw new ArgumentException("原始點位缺漏、重複或有負咬蝕量，整份日報未同步。");
    }

    /// <summary>Strict imports reject changed content; explicit Portal saves may update only records owned by this report.</summary>
    public Task<EtchSyncResult> SyncAsync(EtchReportRequest request, string actor, bool allowUpdate, CancellationToken ct = default)
        => ExecuteAsync(request, actor, allowUpdate, false, ct);

    /// <summary>Validate mapping and content without creating a batch or measurement.</summary>
    public Task<EtchSyncResult> PreviewAsync(EtchReportRequest request, CancellationToken ct = default)
        => ExecuteAsync(request, "preview", false, true, ct);

    private async Task<EtchSyncResult> ExecuteAsync(EtchReportRequest request, string actor, bool allowUpdate, bool preview, CancellationToken ct)
    {
        Validate(request);
        var (ops, length) = Rule(request.LineCode);
        var ordered = request.Points.OrderBy(p => p.Side).ThenBy(p => p.RepeatNo).ThenBy(p => p.OpNo).ToList();
        var content = JsonSerializer.Serialize(new { request.ReportDate, request.LineCode, request.LineSpeed, request.OperatorName, Points = ordered });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var key = $"ETCH:{request.ReportDate:yyyy-MM-dd}:{request.LineCode}";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (db.Database.IsSqlServer())
        {
            // Transaction-owned lock also serializes first insertion, when no measurement rows exist yet.
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={key}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Etch report lock unavailable', 1;", ct);
        }
        var machine = await db.Machines.SingleAsync(x => x.MachineCode == request.LineCode && x.IsEnabled, ct);
        var mappings = await db.PartProcessCharacteristics.Include(x => x.Characteristic)
            .Where(x => x.MachineId == machine.Id && x.ControlScope == "PROCESS" && x.IsEnabled && Codes.Contains(x.Characteristic!.CharacteristicCode)).ToListAsync(ct);
        if (mappings.Count != 3 || mappings.Select(x => x.Characteristic!.CharacteristicCode).Distinct().Count() != 3)
            throw new InvalidOperationException("咬蝕量／速率管制項目未完整設定。");
        foreach (var m in mappings)
            if (m.SampleSize != (m.Characteristic!.CharacteristicCode is "ETCH_A_AVG" or "ETCH_B_AVG" ? ops * 5 : 1))
                throw new InvalidOperationException("管制項目樣本數與完整子組不一致。");
        var ids = mappings.Select(x => x.Id).ToList(); var next = request.ReportDate.AddDays(1);
        var existing = await db.VariableMeasurements.Where(x => ids.Contains(x.PartProcessCharacteristicId) && x.MeasuredAt >= request.ReportDate && x.MeasuredAt < next).ToListAsync(ct);
        if (existing.Any(x => x.SourceReference != key)) throw new InvalidOperationException("同日已有其他來源資料，請先核對，未覆寫。");
        if (existing.GroupBy(x => (x.PartProcessCharacteristicId, x.SampleNo)).Any(g => g.Count() != 1)) throw new InvalidOperationException("既有子組點位重複，未覆寫。");
        var desired = new List<(PartProcessCharacteristic Mapping, int SampleNo, double Value)>();
        foreach (var m in mappings)
        {
            var code = m.Characteristic!.CharacteristicCode;
            if (code is "ETCH_A_AVG" or "ETCH_B_AVG")
                desired.AddRange(ordered.Where(p => p.Side == (code == "ETCH_A_AVG" ? "A" : "B")).Select(p => (m, (p.RepeatNo - 1) * ops + p.OpNo, (double)(p.BeforeValue - p.AfterValue))));
            else desired.Add((m, 1, (double)(code == "ETCH_RATE" ? ordered.Average(p => p.BeforeValue - p.AfterValue) * request.LineSpeed / length : request.LineSpeed)));
        }
        var expected = desired.ToDictionary(x => (x.Mapping.Id, x.SampleNo), x => x.Value);
        if (existing.Any(x => !expected.ContainsKey((x.PartProcessCharacteristicId, x.SampleNo)))) throw new InvalidOperationException("既有子組結構衝突。");
        if (existing.Count == desired.Count && existing.All(x => Math.Abs(x.MeasuredValue - expected[(x.PartProcessCharacteristicId, x.SampleNo)]) < 1e-10 && x.Operator == request.OperatorName))
        {
            var batchId = existing[0].UploadBatchId;
            var oldHash = await db.UploadBatches.Where(b => b.UploadBatchId == batchId).Select(b => b.FileHash).SingleAsync(ct);
            if (oldHash == hash) { await tx.CommitAsync(ct); return new(batchId, true, desired.Count); }
        }
        if (existing.Count > 0 && !allowUpdate) throw new InvalidOperationException("同日同線內容不同，嚴格匯入拒絕覆寫。");
        if (preview) { await tx.CommitAsync(ct); return new(Guid.Empty, false, desired.Count); }
        var batch = new UploadBatch { SourceType = "EtchReport", UploadType = "Variable", ImportStatus = "Imported", OriginalFileName = key, FileHash = hash, TotalRows = desired.Count, ValidRows = desired.Count, ConfirmedAt = DateTime.UtcNow, CreatedBy = actor };
        db.UploadBatches.Add(batch);
        db.UploadDetails.Add(new() { UploadBatchId = batch.UploadBatchId, RowNo = 1, IsValid = true, CreatedBy = actor, PayloadJson = JsonSerializer.Serialize(new { BusinessKey = key, Actor = actor, Before = existing.Select(x => new { x.Id, x.UploadBatchId, x.SampleNo, x.PartProcessCharacteristicId, x.MeasuredValue, x.Operator }).ToList(), Report = request }) });
        foreach (var item in desired)
        {
            var row = existing.SingleOrDefault(x => x.PartProcessCharacteristicId == item.Mapping.Id && x.SampleNo == item.SampleNo);
            if (row is null) { row = new() { PartId = item.Mapping.PartId ?? 0, ProcessId = item.Mapping.ProcessId, MachineId = machine.Id, CharacteristicId = item.Mapping.CharacteristicId, PartProcessCharacteristicId = item.Mapping.Id, SampleNo = item.SampleNo, MeasuredAt = request.ReportDate, SourceReference = key, SerialNo = key, LotNo = key, SourceType = SourceType.Manual, CreatedBy = actor }; db.VariableMeasurements.Add(row); }
            row.MeasuredValue = item.Value; row.Operator = request.OperatorName; row.UploadBatchId = batch.UploadBatchId; row.UpdatedAt = DateTime.UtcNow; row.UpdatedBy = actor;
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(batch.UploadBatchId, false, desired.Count);
    }
}
