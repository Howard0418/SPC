using Bogus;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;

namespace MesSpc.Api.Services.TestData;

public class FakeDataFactory
{
    public List<Product> CreateProducts(int count, string runId)
    {
        var faker = new Faker("en");
        return Enumerable.Range(1, count).Select(i => new Product
        {
            ProductCode = $"{TestDataTags.TestPrefix}P_{runId}_{i:0000}",
            ProductName = $"{TestDataTags.E2ePrefix}{faker.Commerce.ProductName()}",
            IsActive = true
        }).ToList();
    }

    public List<Station> CreateStations(string runId)
    {
        var names = new[] { "Cutting", "Coating", "Inspection", "Packaging", "FinalQC" };
        return names.Select((name, i) => new Station
        {
            StationCode = $"{TestDataTags.TestPrefix}ST_{runId}_{i + 1:00}",
            StationName = $"{TestDataTags.E2ePrefix}{name}",
            IsActive = true
        }).ToList();
    }

    public List<InspectionItem> CreateInspectionItems(string runId)
    {
        return
        [
            new InspectionItem { ItemCode = $"{TestDataTags.TestPrefix}ITEM_{runId}_THK", ItemName = $"{TestDataTags.E2ePrefix}Thickness", Unit = "mm", Lsl = 9.8, Usl = 10.2, Lcl = 9.85, Ucl = 10.15, TargetValue = 10.0, DataType = DataType.Numeric, IsSpcEnabled = true },
            new InspectionItem { ItemCode = $"{TestDataTags.TestPrefix}ITEM_{runId}_WID", ItemName = $"{TestDataTags.E2ePrefix}Width", Unit = "mm", Lsl = 19.7, Usl = 20.3, Lcl = 19.8, Ucl = 20.2, TargetValue = 20.0, DataType = DataType.Numeric, IsSpcEnabled = true },
            new InspectionItem { ItemCode = $"{TestDataTags.TestPrefix}ITEM_{runId}_HEI", ItemName = $"{TestDataTags.E2ePrefix}Height", Unit = "mm", Lsl = 4.9, Usl = 5.1, Lcl = 4.93, Ucl = 5.07, TargetValue = 5.0, DataType = DataType.Numeric, IsSpcEnabled = true },
            new InspectionItem { ItemCode = $"{TestDataTags.TestPrefix}ITEM_{runId}_WGT", ItemName = $"{TestDataTags.E2ePrefix}Weight", Unit = "g", Lsl = 49.0, Usl = 51.0, Lcl = 49.3, Ucl = 50.7, TargetValue = 50.0, DataType = DataType.Numeric, IsSpcEnabled = true },
            new InspectionItem { ItemCode = $"{TestDataTags.TestPrefix}ITEM_{runId}_HRD", ItemName = $"{TestDataTags.E2ePrefix}Hardness", Unit = "HV", Lsl = 95, Usl = 105, Lcl = 96, Ucl = 104, TargetValue = 100.0, DataType = DataType.Numeric, IsSpcEnabled = true }
        ];
    }

    public List<WorkOrder> CreateWorkOrders(int count, List<Product> products, string runId)
    {
        var faker = new Faker("en");
        return Enumerable.Range(1, count).Select(i =>
        {
            var product = faker.PickRandom(products);
            return new WorkOrder
            {
                WorkOrderNo = $"{TestDataTags.TestPrefix}WO_{runId}_{i:0000}",
                ProductId = product.Id,
                PlannedQty = faker.Random.Int(100, 500),
                ActualQty = 0,
                Status = "Planned",
                PlannedStartTime = DateTime.UtcNow.AddHours(i),
                UpdatedAt = DateTime.UtcNow
            };
        }).ToList();
    }
}

