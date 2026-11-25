using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MesSpc.Api.Tests;

public sealed class DatabaseRefreshLiveApiTests
{
    [Theory]
    [InlineData("Production", "production", true, true, 27, true)]
    [InlineData("Production", "production", true, false, 27, false)]
    [InlineData("Production", "production", false, true, 27, false)]
    [InlineData("Production", "test", true, true, 27, false)]
    [InlineData("Development", "production", true, true, 27, false)]
    [InlineData("Production", "production", true, true, 0, false)]
    public void RegistrationIsExplicitAndProductionOnly(string host, string environment, bool auth,
        bool enabled, int operatorId, bool expected)
    {
        Assert.Equal(expected, DatabaseRefreshLiveApi.CanRegister(host, environment, auth, enabled, operatorId));
    }

    [Fact]
    public void UnreadyPreflightIsRejected()
    {
        var now = new DateTimeOffset(2026, 11, 25, 4, 0, 0, TimeSpan.Zero);
        var proof = new DatabaseRefreshLiveApi.Preflight(Guid.NewGuid(), "PMR_SPC_2026", "PMR_SPC_TEST",
            now.AddMinutes(-2), 69, 18460, 0);
        Assert.True(DatabaseRefreshLiveApi.IsReady(proof, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { Blockers = 1 }, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { Target = "PMR_SPC_2026" }, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { SnapshotUtc = now.AddMinutes(-15) }, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { SnapshotUtc = now.AddSeconds(1) }, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { ReplacementTables = 68 }, now));
        Assert.False(DatabaseRefreshLiveApi.IsReady(proof with { MeasurementRows = 0 }, now));
    }

    [Fact]
    public async Task OnlyDesignatedOperatorCanSubmitConfirmedCurrentProof()
    {
        var root = Directory.CreateTempSubdirectory("spc-refresh-test-");
        var proofDirectory = root.CreateSubdirectory("proof");
        var queue = root.CreateSubdirectory("queue");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["AppEnvironment"] = "production";
        builder.Configuration["DatabaseRefreshLive:Enabled"] = "true";
        builder.Configuration["DatabaseRefreshLive:OperatorId"] = "27";
        builder.Configuration["DatabaseRefreshLive:PreflightDirectory"] = proofDirectory.FullName;
        builder.Configuration["DatabaseRefreshLive:QueueDirectory"] = queue.FullName;
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.MapDatabaseRefreshLive(authEnabled: true);

        var proof = new DatabaseRefreshLiveApi.Preflight(Guid.NewGuid(), "PMR_SPC_2026", "PMR_SPC_TEST",
            DateTimeOffset.UtcNow, 69, 100, 0);
        async Task WriteProof(DatabaseRefreshLiveApi.Preflight value) =>
            await File.WriteAllTextAsync(Path.Combine(proofDirectory.FullName, "preflight.json"),
                JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            const string route = "/api/database-refresh";
            void SignIn(int id)
            {
                var token = new JwtSecurityToken(claims: [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
                    expires: DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                    new JwtSecurityTokenHandler().WriteToken(token));
            }
            async Task<string> Issue()
            {
                using var result = await client.PostAsync(route + "/preflight", null);
                result.EnsureSuccessStatusCode();
                var info = await result.Content.ReadFromJsonAsync<JsonElement>();
                return info.GetProperty("confirmationId").GetGuid().ToString();
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route + "/availability")).StatusCode);
            SignIn(28);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(route + "/preflight", null)).StatusCode);
            SignIn(27);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(route + "/preflight", null)).StatusCode);
            await WriteProof(proof);
            string expiredTicket = await Issue();
            await WriteProof(proof with { OperationId = Guid.NewGuid() });
            Assert.Equal(HttpStatusCode.Conflict,
                (await client.PostAsync(route + $"/jobs/{expiredTicket}?target=PMR_SPC_TEST", null)).StatusCode);

            string ticket = await Issue();
            string job = route + $"/jobs/{ticket}";
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(job + "?target=PMR_SPC_2026", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsync(job + "?target=PMR_SPC_TEST", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsync(job + "?target=PMR_SPC_TEST", null)).StatusCode);
            Assert.Single(queue.GetFiles("*.json"));
            var result = await client.GetFromJsonAsync<JsonElement>(job);
            Assert.Equal("queued", result.GetProperty("status").GetString());
            var worker = new DatabaseRefreshHandoff(queue.FullName);
            var claimed = worker.Claim();
            Assert.NotNull(claimed);
            Assert.Equal(Guid.Parse(ticket), claimed.Id);
            Assert.True(worker.Complete(claimed.Id, succeeded: true));
            result = await client.GetFromJsonAsync<JsonElement>(job);
            Assert.Equal("succeeded", result.GetProperty("status").GetString());
            string anotherTicket = await Issue();
            Assert.Equal(HttpStatusCode.Conflict,
                (await client.PostAsync(route + $"/jobs/{anotherTicket}?target=PMR_SPC_TEST", null)).StatusCode);
            SignIn(28);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(job)).StatusCode);
        }
        finally
        {
            await app.StopAsync();
            root.Delete(recursive: true);
        }
    }
}
