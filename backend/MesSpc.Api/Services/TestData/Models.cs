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

public record ClearAllDataResponse(
    int LotSlotHistories,
    int SlotParameters,
    int LotSplitHistories,
    int LotMasters,
    int VariableMeasurements,
    int AttributeMeasurements,
    int SpcCalculationResults,
    int UploadErrors,
    int UploadDetails,
    int UploadBatches,
    int AlertEvents,
    int MeasurementValues,
    int MeasurementBatches,
    int StationOperationSessions,
    int WorkOrders,
    int MesSyncMessages,
    int PartProcessCharacteristics,
    int ProductStationItems,
    int Machines,
    int QualityCharacteristics,
    int Slots,
    int Tanks,
    int ProductionLines,
    int Factories,
    int Plants,
    int Parts,
    int Processes,
    int SpcRules,
    int SpcRuleGroups,
    int ControlChartTypes,
    int ControlChartCategories,
    int ControlChartGroups,
    int FormulaDefinitions,
    int InspectionItems,
    int Stations,
    int Products,
    int Units,
    int Shifts,
    int Operators,
    int Customers,
    int Suppliers,
    int Chemicals
);

public record ClearTransactionalDataResponse(
    int LotSlotHistories,
    int SlotParameters,
    int LotSplitHistories,
    int LotMasters,
    int VariableMeasurements,
    int AttributeMeasurements,
    int SpcCalculationResults,
    int UploadErrors,
    int UploadDetails,
    int UploadBatches,
    int AlertEvents,
    int MeasurementValues,
    int MeasurementBatches,
    int StationOperationSessions,
    int WorkOrders,
    int MesSyncMessages
);

public record ClearTaggedTestDataResponse(
    int VariableMeasurements,
    int AttributeMeasurements,
    int SpcCalculationResults,
    int UploadErrors,
    int UploadDetails,
    int UploadBatches,
    int AlertEvents,
    int Operators,
    ClearTestDataResponse LegacyTestData);

public static class TestDataTags
{
    public const string TestPrefix = "TEST_";
    public const string E2ePrefix = "E2E_";
}

