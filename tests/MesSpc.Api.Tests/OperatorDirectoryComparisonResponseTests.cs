using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesSpc.Api.Tests;

public sealed class OperatorDirectoryComparisonResponseTests
{
    [Fact]
    public async Task GetAcceptsPortalJsonArray()
    {
        await using var fixture = await Fixture.Create(JsonContent.Create(new[]
        {
            new OperatorDirectoryComparisonController.PortalUser(
                20, "qa.user", "QA20", "QA User", "QA", "qa@example.test", true, null, null)
        }));

        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.Get(CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(1, json.GetProperty("portalCount").GetInt32());
    }

    [Theory]
    [InlineData("text/html", "<!doctype html><html><body>SPC</body></html>")]
    [InlineData("application/json", "{\"items\":[]}")]
    [InlineData("application/json", "")]
    public async Task GetRejectsInvalidPortalPayloadWithoutThrowing(string mediaType, string payload)
    {
        var content = new StringContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        await using var fixture = await Fixture.Create(content);

        var result = Assert.IsType<ObjectResult>(await fixture.Controller.Get(CancellationToken.None));
        Assert.Equal(502, result.StatusCode);
        var json = JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("DIRECTORY_RESPONSE_INVALID", json.GetProperty("code").GetString());
    }

    private sealed class StaticHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }

    private sealed class Fixture(SqliteConnection connection, AppDbContext db, HttpClient client) : IAsyncDisposable
    {
        public OperatorDirectoryComparisonController Controller { get; } = CreateController(db, client);

        public static async Task<Fixture> Create(HttpContent content)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db, new HttpClient(new StaticHandler(content)));
        }

        private static OperatorDirectoryComparisonController CreateController(AppDbContext db, HttpClient client)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PortalDirectory:ApiBaseUrl"] = "https://portal.example.test",
                ["Auth:PortalSsoKey"] = "synthetic-test-key"
            }).Build();
            return new OperatorDirectoryComparisonController(db, factory.Object, config,
                NullLogger<OperatorDirectoryComparisonController>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
