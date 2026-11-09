using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class NavigationReadAuditTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public AppDbContext Db { get; private set; } = null!;
        public NavigationReadAudit Audit { get; private set; } = null!;
        public async Task InitializeAsync()
        {
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            Db = new AppDbContext(options); await Db.Database.EnsureCreatedAsync();
            Db.Operators.Add(new Operator { Id = 7, OperatorCode = "AUDIT", OperatorName = "Audit QA", Username = "audit.qa", Role = "Viewer" });
            await Db.SaveChangesAsync();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppEnvironment"] = "test", ["Auth:Enabled"] = "true", ["Audit:AuthenticationEnabled"] = "true",
                ["Audit:NavigationReadEnabled"] = "true", ["Audit:AttemptedAccountHmacKey"] = new string('k', 32)
            }).Build();
            var writer = new AuditEventWriter(Db, options, config, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
            var authentication = new AuthenticationAudit(writer, config, NullLogger<AuthenticationAudit>.Instance);
            Audit = new NavigationReadAudit(writer, authentication, new AuditPermissions(Db, config), config);
        }
        public HttpContext Context()
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "7"), new("jti", "navigation-test")], "test"));
            return context;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    [Fact]
    public async Task PageEnterUsesServerActorAndCanonicalRouteAndRetriesIdempotently()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var id = Guid.NewGuid();
        var request = new ActivityAuditRequest("PageEnter", "analysis.spc.overall", id, "Push");
        await f.Audit.RecordAsync(f.Context(), request);
        await f.Audit.RecordAsync(f.Context(), request);
        var row = await f.Db.AuditEvents.SingleAsync();
        Assert.Equal("navigation.page.enter", row.EventCode); Assert.Equal(7, row.ActorOperatorId); Assert.Equal("/spc", row.RouteTemplate);
        Assert.Equal("WebRouter", row.Source); Assert.Equal(id, row.ClientEventId);
    }

    [Fact]
    public async Task FilterAndExportKeepOnlySafeSummary()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var from = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(1);
        await f.Audit.RecordAsync(f.Context(), new ActivityAuditRequest("Filter", "traceability.query", FilterFields: ["Lot", "WorkOrder"], DateFromUtc: from, DateToUtc: to, ResultCount: 3));
        await f.Audit.RecordAsync(f.Context(), new ActivityAuditRequest("Export", "analysis.spc.overall", DateFromUtc: from, DateToUtc: to, ResultCount: 3, ExportFormat: "Xlsx"));
        var rows = await f.Db.AuditEvents.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(["read.filter.apply", "read.export.generate"], rows.Select(x => x.EventCode));
        var detail = JsonDocument.Parse(rows[0].DetailsJson).RootElement;
        Assert.Equal("Lot", detail.GetProperty("FilterFields")[0].GetString());
        Assert.DoesNotContain("secret-lot", JsonSerializer.Serialize(rows));
        Assert.Equal("Xlsx", JsonDocument.Parse(rows[1].DetailsJson).RootElement.GetProperty("ExportFormat").GetString());
    }

    [Theory]
    [InlineData("PageEnter", "unknown.page")]
    [InlineData("Unknown", "analysis.spc.overall")]
    public async Task InvalidActivityOrPageLeavesNoEvidence(string activity, string page)
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Audit.RecordAsync(f.Context(), new ActivityAuditRequest(activity, page)));
        Assert.Empty(await f.Db.AuditEvents.ToListAsync());
    }
}
