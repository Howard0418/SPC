namespace MES.SPC.E2ETests.Fixtures;

public class TestSettings
{
    public string ApiBaseUrl { get; init; } = "http://localhost:5243";
    public string WebBaseUrl { get; init; } = "http://localhost:5173";
    public GenerateRequest GenerateRequest { get; init; } = new();
}

public class GenerateRequest
{
    public int ProductCount { get; init; } = 2;
    public int WorkOrderCount { get; init; } = 2;
    public int LotPerWorkOrder { get; init; } = 2;
    public int MeasurementPerLot { get; init; } = 20;
    public bool GenerateSpcAnomalies { get; init; } = true;
}

