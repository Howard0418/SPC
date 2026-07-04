using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services.TestData;

public class TestDataSeeder(
    FakeDataFactory fakeDataFactory,
    MeasurementGenerator measurementGenerator,
    DatabaseSeeder databaseSeeder)
{
    public async Task<GenerateTestDataResponse> GenerateAsync(GenerateTestDataRequest req)
    {
        var start = DateTimeOffset.UtcNow;
        var runId = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..20];

        await databaseSeeder.EnsureFormulaDefinitionsAsync();

        var products = fakeDataFactory.CreateProducts(req.ProductCount, runId);
        var stations = fakeDataFactory.CreateStations(runId);
        var items = fakeDataFactory.CreateInspectionItems(runId);
        await databaseSeeder.SaveProductsStationsItemsAsync(products, stations, items);
        await databaseSeeder.SaveProductStationItemsAsync(products, stations, items);

        var workOrders = fakeDataFactory.CreateWorkOrders(req.WorkOrderCount, products, runId);
        await databaseSeeder.SaveWorkOrdersAsync(workOrders);

        var (batches, lots, serials) = measurementGenerator.CreateBatches(workOrders, stations, items, req, runId);
        await databaseSeeder.SaveBatchesAsync(batches);

        var alertCount = req.GenerateSpcAnomalies
            ? await databaseSeeder.CreateAlertsForAnomaliesAsync(batches)
            : 0;

        var measurementValueCount = batches.Sum(x => x.Values.Count);
        var durationMs = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
        return new GenerateTestDataResponse(
            runId,
            products.Count,
            stations.Count,
            items.Count,
            workOrders.Count,
            lots,
            serials,
            batches.Count,
            measurementValueCount,
            alertCount,
            durationMs);
    }

    public Task<ClearTestDataResponse> ClearAsync(string? runId = null) => databaseSeeder.ClearAllTestDataAsync(runId);

    public Task<ClearAllDataResponse> ClearAllDatabaseDataAsync() => databaseSeeder.ClearAllDatabaseDataAsync();

    public Task<ClearTransactionalDataResponse> ClearTransactionalDataAsync() => databaseSeeder.ClearTransactionalDataAsync();

    public Task<ClearTaggedTestDataResponse> ClearTaggedTestDataAsync() => databaseSeeder.ClearTaggedTestDataAsync();
}

