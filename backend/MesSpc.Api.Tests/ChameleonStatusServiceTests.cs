using System.Net;
using System.Text;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class ChameleonStatusServiceTests
{
    [Fact]
    public async Task GetLatestPointsAsync_UsesFinsSingleEquipmentDefinition()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["/reading/equipments/RTR_Layer_Lamination/channels/latest-data"] = """{"at":0,"reading":{"D10":123}}""",
            ["/config/fins/equipments"] = """[{"equipment":{"equipmentId":"RTR_Layer_Lamination","templateId":"RTR_Layer_Lamination"}}]""",
            ["/config/fins/templates"] = """[{"templateId":"RTR_Layer_Lamination","channels":[{"channelId":"D10","channelName":"上左温度显示值","categories":["data"]}]}]"""
        });

        var result = await service.GetLatestPointsAsync("CHA-111", "RTR_Layer_Lamination", CancellationToken.None);

        Assert.Equal("上左温度显示值", Assert.Single(result.Points).ChannelName);
    }

    [Fact]
    public async Task GetLatestPointsAsync_StillUsesMelsecEquipmentListDefinition()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["/reading/equipments/HCP/channels/latest-data"] = """{"at":0,"reading":{"D2000":45}}""",
            ["/config/fins/equipments"] = "[]",
            ["/config/melsec/equipments"] = """[{"equipmentList":[{"equipmentId":"HCP","templateId":"HCP"}]}]""",
            ["/config/melsec/templates"] = """[{"templateId":"HCP","channels":[{"channelId":"D2000","channelName":"槽溫","categories":["data"]}]}]"""
        });

        var result = await service.GetLatestPointsAsync("CHA-111", "HCP", CancellationToken.None);

        Assert.Equal("槽溫", Assert.Single(result.Points).ChannelName);
    }

    private static ChameleonStatusService CreateService(IReadOnlyDictionary<string, string> responses)
    {
        var client = new HttpClient(new StubHandler(responses)) { BaseAddress = new Uri("http://chameleon") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Chameleon:Sources:0:SourceId"] = "CHA-111",
            ["Chameleon:Sources:0:DisplayName"] = "Chameleon 111",
            ["Chameleon:Sources:0:BaseUrl"] = "http://chameleon",
            ["Chameleon:Sources:0:Enabled"] = "true"
        }).Build();
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return new ChameleonStatusService(client, configuration, db, new MemoryCache(new MemoryCacheOptions()), NullLogger<ChameleonStatusService>.Instance);
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (responses.TryGetValue(request.RequestUri!.AbsolutePath, out var json))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
