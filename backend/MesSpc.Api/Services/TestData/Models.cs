namespace MesSpc.Api.Services.TestData;

public record GenerateTestDataRequest(
    int ProductCount = 10,
    int WorkOrderCount = 20,
    int LotPerWorkOrder = 5,
    int MeasurementPerLot = 100,
    bool GenerateSpcAnomalies = true);

public record GenerateTestDataResponse(
    string RunId,
    int Products,
    int Stations,
    int InspectionItems,
    int WorkOrders,
    int Lots,
    int Serials,
    int MeasurementBatches,
    int MeasurementValues,
    int Alerts,
    long DurationMs);

public record ClearTestDataResponse(
    int Products,
    int Stations,
    int InspectionItems,
    int WorkOrders,
    int StationSessions,
    int MeasurementBatches,
    int MeasurementValues,
    int Alerts);

public static class TestDataTags
{
    public const string TestPrefix = "TEST_";
    public const string E2ePrefix = "E2E_";
}

