using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Parsers;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

/// <summary>
/// 負責將解析後的 ChemicalAnalysisRow 寫入資料庫
/// 設計原則：
///  - 找不到 Product(Line) / Station(Tank) / InspectionItem(AnalysisItem) 時自動建立
///  - 主測 (Titration + Concentration) 與複驗 (Recheck) 分別作為獨立 MeasurementValue 存入
///  - ECR 資訊存入 MeasurementBatch 的 LotNo (暫用，後續可擴充 EcrRecord 資料表)
/// </summary>
public class ChemicalImportService
{
    private readonly AppDbContext _db;

    public ChemicalImportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChemicalImportSummary> ImportAsync(
        ChemicalDailyReportParseResult parseResult,
        string? importedBy = null,
        CancellationToken ct = default)
    {
        var summary = new ChemicalImportSummary();

        foreach (var row in parseResult.Rows)
        {
            try
            {
                // 1. 解析/建立 Product (以 Line 名稱)
                var product = await GetOrCreateProductAsync(row.Line, ct);

                // 2. 解析/建立 Station (以 Tank 名稱)
                var station = await GetOrCreateStationAsync(row.Tank, ct);

                // 3. 解析/建立 InspectionItem (以 AnalysisItem 名稱，更新 LSL/USL)
                var item = await GetOrCreateInspectionItemAsync(row, ct);

                // 4. 確認 ProductStationItem 存在
                await EnsureProductStationItemAsync(product.Id, station.Id, item.Id, ct);

                // 5. 建立 MeasurementBatch
                var lotNote = row.EcrNo != null ? $"ECR:{row.EcrNo}" : null;
                var batch = new MeasurementBatch
                {
                    BatchNo = $"CHEM-{row.SheetName}-{row.MeasuredDate:yyyyMMdd}-{row.Tank}-{row.AnalysisItem}".Replace(" ", "_"),
                    ProductId = product.Id,
                    StationId = station.Id,
                    LotNo = lotNote,
                    MeasuredAt = row.MeasuredDate.ToUniversalTime(),
                    OperatorName = row.Analyst,
                    SourceType = Domain.Enums.SourceType.Excel,
                    CreatedBy = importedBy
                };
                _db.MeasurementBatches.Add(batch);
                await _db.SaveChangesAsync(ct);

                // 6. 寫入量測值 — 主測
                int sampleNo = 1;
                if (row.TitrationMl.HasValue)
                {
                    _db.MeasurementValues.Add(new MeasurementValue
                    {
                        BatchId = batch.Id,
                        InspectionItemId = item.Id,
                        SampleNo = sampleNo++,
                        ValueNumeric = row.TitrationMl,
                        ValueText = $"TITRATION_ML",
                        CreatedBy = importedBy
                    });
                }
                if (row.Concentration.HasValue)
                {
                    _db.MeasurementValues.Add(new MeasurementValue
                    {
                        BatchId = batch.Id,
                        InspectionItemId = item.Id,
                        SampleNo = sampleNo++,
                        ValueNumeric = row.Concentration,
                        ValueText = "CONCENTRATION",
                        CreatedBy = importedBy
                    });
                }

                // 7. 寫入量測值 — 複驗
                if (row.RecheckTitrationMl.HasValue)
                {
                    _db.MeasurementValues.Add(new MeasurementValue
                    {
                        BatchId = batch.Id,
                        InspectionItemId = item.Id,
                        SampleNo = sampleNo++,
                        ValueNumeric = row.RecheckTitrationMl,
                        ValueText = "RECHECK_TITRATION_ML",
                        CreatedBy = importedBy
                    });
                }
                if (row.RecheckConcentration.HasValue)
                {
                    _db.MeasurementValues.Add(new MeasurementValue
                    {
                        BatchId = batch.Id,
                        InspectionItemId = item.Id,
                        SampleNo = sampleNo++,
                        ValueNumeric = row.RecheckConcentration,
                        ValueText = "RECHECK_CONCENTRATION",
                        CreatedBy = importedBy
                    });
                }

                await _db.SaveChangesAsync(ct);
                summary.ImportedRows++;
            }
            catch (Exception ex)
            {
                summary.FailedRows++;
                summary.Errors.Add($"[{row.SheetName}] {row.Tank} / {row.AnalysisItem}: {ex.Message}");
            }
        }

        return summary;
    }

    private async Task<Product> GetOrCreateProductAsync(string line, CancellationToken ct)
    {
        var code = Sanitize(line);
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductCode == code && !p.IsDeleted, ct);
        if (product == null)
        {
            product = new Product { ProductCode = code, ProductName = line };
            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);
        }
        return product;
    }

    private async Task<Station> GetOrCreateStationAsync(string tank, CancellationToken ct)
    {
        var code = Sanitize(tank);
        var station = await _db.Stations
            .FirstOrDefaultAsync(s => s.StationCode == code && !s.IsDeleted, ct);
        if (station == null)
        {
            station = new Station { StationCode = code, StationName = tank };
            _db.Stations.Add(station);
            await _db.SaveChangesAsync(ct);
        }
        return station;
    }

    private async Task<InspectionItem> GetOrCreateInspectionItemAsync(ChemicalAnalysisRow row, CancellationToken ct)
    {
        var code = Sanitize(row.AnalysisItem);
        var item = await _db.InspectionItems
            .FirstOrDefaultAsync(i => i.ItemCode == code && !i.IsDeleted, ct);

        if (item == null)
        {
            item = new InspectionItem
            {
                ItemCode = code,
                ItemName = row.AnalysisItem,
                Usl = row.USL,
                Lsl = row.LSL,
                IsSpcEnabled = true
            };
            _db.InspectionItems.Add(item);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // 若 LSL/USL 有差異，更新
            bool changed = false;
            if (row.USL.HasValue && item.Usl != row.USL) { item.Usl = row.USL; changed = true; }
            if (row.LSL.HasValue && item.Lsl != row.LSL) { item.Lsl = row.LSL; changed = true; }
            if (changed) await _db.SaveChangesAsync(ct);
        }

        return item;
    }

    private async Task EnsureProductStationItemAsync(int productId, int stationId, int itemId, CancellationToken ct)
    {
        var exists = await _db.ProductStationItems.AnyAsync(
            p => p.ProductId == productId && p.StationId == stationId && p.InspectionItemId == itemId && !p.IsDeleted, ct);
        if (!exists)
        {
            _db.ProductStationItems.Add(new ProductStationItem
            {
                ProductId = productId,
                StationId = stationId,
                InspectionItemId = itemId,
                SampleSize = 1
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private static string Sanitize(string s) =>
        new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray())
            .Trim('-', '_')
            .ToUpperInvariant()[..Math.Min(50, s.Length)];
}

public class ChemicalImportSummary
{
    public int ImportedRows { get; set; }
    public int FailedRows { get; set; }
    public List<string> Errors { get; set; } = new();
}
