using System.Net.Http.Json;
using MES.SPC.E2ETests.Fixtures;

namespace MES.SPC.E2ETests.Utilities;

public sealed class TestDataApiClient : IDisposable
{
    private readonly HttpClient _http;

    public TestDataApiClient(string apiBaseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    }

    public async Task<GenerateResponse> GenerateAsync(GenerateRequest req)
    {
        var res = await _http.PostAsJsonAsync("/api/testdata/generate", new
        {
            productCount = req.ProductCount,
            workOrderCount = req.WorkOrderCount,
            lotPerWorkOrder = req.LotPerWorkOrder,
            measurementPerLot = req.MeasurementPerLot,
            generateSpcAnomalies = req.GenerateSpcAnomalies
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<GenerateResponse>())!;
    }

    public async Task<ClearResponse> ClearAsync(string? runId = null)
    {
        var path = string.IsNullOrWhiteSpace(runId) ? "/api/testdata/clear" : $"/api/testdata/clear?runId={Uri.EscapeDataString(runId)}";
        var res = await _http.DeleteAsync(path);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ClearResponse>())!;
    }

    public void Dispose() => _http.Dispose();
}

public sealed class GenerateResponse
{
    public string RunId { get; set; } = string.Empty;
    public int Products { get; set; }
    public int Stations { get; set; }
    public int InspectionItems { get; set; }
    public int WorkOrders { get; set; }
    public int Lots { get; set; }
    public int Serials { get; set; }
    public int MeasurementBatches { get; set; }
    public int MeasurementValues { get; set; }
    public int Alerts { get; set; }
}

public sealed class ClearResponse
{
    public int Products { get; set; }
    public int Stations { get; set; }
    public int InspectionItems { get; set; }
    public int WorkOrders { get; set; }
    public int MeasurementBatches { get; set; }
    public int MeasurementValues { get; set; }
    public int Alerts { get; set; }
}

