using System.Security.Claims;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class AuditEventsControllerTests
{
    [Fact]
    public async Task QueryPersistsOwnAuditEventBeforeReturningData()
    {
        await using var fixture = await Fixture.Create("test");
        var result = await fixture.Controller.Query(new(PageSize: 10), default);
        Assert.IsType<OkObjectResult>(result);
        var row = await fixture.Db.AuditEvents.SingleAsync(x => x.EventCode == "audit.events.query");
        Assert.Equal("admin.audit.events", row.PageKey);
        Assert.Equal(7, row.ActorOperatorId);
        Assert.DoesNotContain("qa.actor", row.DetailsJson);
    }

    [Fact]
    public async Task AuditPersistenceFailureFailsClosedWithoutReturningData()
    {
        await using var fixture = await Fixture.Create("invalid-environment");
        var result = Assert.IsType<ObjectResult>(await fixture.Controller.Query(new(PageSize: 10), default));
        Assert.Equal(503, result.StatusCode);
        Assert.DoesNotContain(await fixture.Db.AuditEvents.ToListAsync(), x => x.EventCode == "audit.events.query");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public AppDbContext Db { get; private set; } = null!;
        public AuditEventsController Controller { get; private set; } = null!;
        public static async Task<Fixture> Create(string environment)
        {
            var fixture = new Fixture(); await fixture.connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(fixture.connection).Options;
            fixture.Db = new AppDbContext(options); await fixture.Db.Database.EnsureCreatedAsync();
            fixture.Db.Operators.Add(new Operator { Id = 7, OperatorCode = "QA", OperatorName = "QA", Username = "qa.actor", Role = "Editor", IsActive = true, PagePermissionsJson = "[\"admin.audit.read\",\"admin.audit.export\"]" });
            await fixture.Db.SaveChangesAsync();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppEnvironment"] = environment, ["Auth:Enabled"] = "true", ["Audit:AuthenticationEnabled"] = "true",
                ["Audit:AttemptedAccountHmacKey"] = new string('k', 32)
            }).Build();
            var writer = new AuditEventWriter(fixture.Db, options, config, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
            var authentication = new AuthenticationAudit(writer, config, NullLogger<AuthenticationAudit>.Instance);
            var permissions = new AuditPermissions(fixture.Db, config);
            fixture.Controller = new AuditEventsController(new AuditEventQueryService(fixture.Db, TimeProvider.System), permissions, writer, authentication, NullLogger<AuditEventsController>.Instance)
            { ControllerContext = new ControllerContext { HttpContext = Context() } };
            return fixture;
        }
        private static HttpContext Context()
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "audit-query-test";
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "7"), new("jti", "session")], "test"));
            return context;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
