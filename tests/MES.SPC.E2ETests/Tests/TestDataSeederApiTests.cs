using NUnit.Framework;
using MES.SPC.E2ETests.Utilities;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class TestDataSeederApiTests
{
    [Test]
    public async Task Generate_Then_Clear_By_RunId_Should_Work()
    {
        var settings = ConfigReader.Load();
        using var client = new TestDataApiClient(settings.ApiBaseUrl);

        var generated = await client.GenerateAsync(settings.GenerateRequest);
        Assert.That(generated.RunId, Is.Not.Empty);
        Assert.That(generated.Products, Is.GreaterThan(0));
        Assert.That(generated.WorkOrders, Is.GreaterThan(0));
        Assert.That(generated.MeasurementValues, Is.GreaterThan(0));

        var clear = await client.ClearAsync(generated.RunId);
        Assert.That(clear.WorkOrders, Is.GreaterThanOrEqualTo(1));
        Assert.That(clear.MeasurementValues, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Generate_Without_Anomalies_Should_Allow_Zero_Alerts()
    {
        var settings = ConfigReader.Load();
        using var client = new TestDataApiClient(settings.ApiBaseUrl);

        var req = new Fixtures.GenerateRequest
        {
            ProductCount = 1,
            WorkOrderCount = 1,
            LotPerWorkOrder = 1,
            MeasurementPerLot = 10,
            GenerateSpcAnomalies = false
        };
        var generated = await client.GenerateAsync(req);
        Assert.That(generated.RunId, Is.Not.Empty);
        Assert.That(generated.Alerts, Is.EqualTo(0));

        _ = await client.ClearAsync(generated.RunId);
    }
}

