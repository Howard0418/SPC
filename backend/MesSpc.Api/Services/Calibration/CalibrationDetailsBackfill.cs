using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Calibration;

/// <summary>一次性補回主檔文字：僅完全相符的編號/名稱、有效來源列及空白目的欄。</summary>
public sealed class CalibrationDetailsBackfill(AppDbContext db, InstrumentCalibrationService service)
{
    private static readonly string[] Fields = ["MeasurementSpecification", "Precision", "Remarks", "CalibrationStandard", "AcceptanceCriteria"];
    public async Task<int> ApplyAsync(CalibrationImportPreview source, bool apply, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var instruments = await db.Set<CalibrationInstrument>().ToDictionaryAsync(x => x.Code, ct);
        var count = 0;
        foreach (var row in source.Rows.Where(x => x.Errors.Count == 0))
        {
            if (!instruments.TryGetValue(row.Code, out var item) || item.Name != row.Name) continue;
            var updates = new Dictionary<string, string>();
            foreach (var field in Fields)
            {
                var existing = (string?)typeof(CalibrationInstrument).GetProperty(field)!.GetValue(item);
                var incoming = (string?)typeof(CalibrationImportRow).GetProperty(field)!.GetValue(row);
                if (string.IsNullOrWhiteSpace(existing) && !string.IsNullOrWhiteSpace(incoming) && incoming.Trim().Length <= 2000)
                    updates[field] = incoming.Trim();
            }
            if (updates.Count == 0) continue;
            count++;
            if (!apply) continue;
            var before = JsonSerializer.SerializeToElement(item);
            foreach (var (field,value) in updates) typeof(CalibrationInstrument).GetProperty(field)!.SetValue(item,value);
            item.Version = Guid.NewGuid();
            service.Audit(item.Id,"calibration-details-backfill","InstrumentDetailsBackfilled",before,
                new { source.Hash,row.RowNumber,Fields=updates,Instrument=item },"原始 Excel 僅補空白規格欄位");
        }
        if (apply) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        return count;
    }
}
