using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;

namespace MesSpc.Api.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (!db.FormulaDefinitions.Any())
        {
            db.FormulaDefinitions.AddRange(
                New("AVG", "Average", "AVG(values)"),
                New("STDEV", "Standard Deviation", "STDEV(values)"),
                New("RANGE", "Range", "MAX(values)-MIN(values)"),
                New("MAX", "Max", "MAX(values)"),
                New("MIN", "Min", "MIN(values)"),
                New("COUNT", "Count", "COUNT(values)"),
                New("SUM", "Sum", "SUM(values)"),
                New("CP", "Process Capability", "(USL-LSL)/(6*STDEV(values))"),
                New("CPK", "Process Capability Index", "MIN((USL-AVG(values))/(3*STDEV(values)),(AVG(values)-LSL)/(3*STDEV(values)))")
            );
            db.SaveChanges();
        }

        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { ProductCode = "P-1001", ProductName = "Housing A" },
                new Product { ProductCode = "P-1002", ProductName = "Bracket B" }
            );
            db.SaveChanges();
        }

        if (!db.Stations.Any())
        {
            db.Stations.AddRange(
                new Station { StationCode = "ST-01", StationName = "Incoming QC" },
                new Station { StationCode = "ST-02", StationName = "Assembly Line 1" }
            );
            db.SaveChanges();
        }

        if (!db.InspectionItems.Any())
        {
            db.InspectionItems.AddRange(
                new InspectionItem
                {
                    ItemCode = "LEN-001",
                    ItemName = "Length",
                    DataType = DataType.Numeric,
                    Unit = "mm",
                    Usl = 10.2,
                    Lsl = 9.8,
                    Ucl = 10.1,
                    Lcl = 9.9,
                    TargetValue = 10.0,
                    IsSpcEnabled = true
                },
                new InspectionItem
                {
                    ItemCode = "WID-001",
                    ItemName = "Width",
                    DataType = DataType.Numeric,
                    Unit = "mm",
                    Usl = 5.2,
                    Lsl = 4.8,
                    Ucl = 5.1,
                    Lcl = 4.9,
                    TargetValue = 5.0,
                    IsSpcEnabled = true
                }
            );
            db.SaveChanges();
        }

        if (!db.ProductStationItems.Any())
        {
            var product = db.Products.OrderBy(p => p.Id).First();
            var station = db.Stations.OrderBy(s => s.Id).First();
            var items = db.InspectionItems.Select(x => x.Id).ToList();
            db.ProductStationItems.AddRange(items.Select(itemId => new ProductStationItem
            {
                ProductId = product.Id,
                StationId = station.Id,
                InspectionItemId = itemId,
                SampleSize = 5,
                IsActive = true
            }));
            db.SaveChanges();
        }
    }

    private static FormulaDefinition New(string code, string name, string expr) =>
        new()
        {
            FormulaCode = code,
            DisplayName = name,
            Expression = expr,
            IsBuiltIn = true,
            IsActive = true
        };
}
