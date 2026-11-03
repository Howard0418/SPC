using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class AuditEventWriterTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = new("Data Source=:memory:");
        public AppDbContext Db { get; private set; } = null!;
        public AuditEventWriter Writer { get; private set; } = null!;
        public AuditActor Actor => AuditActor.User(1, "Jwt", new string('a', 64));
        public async Task InitializeAsync(string environment = "test")
        {
            await Connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(Connection).Options;
            Db = new(options);
            await Db.Database.EnsureCreatedAsync();
            Db.Operators.Add(new Operator { Id = 1, OperatorCode = "QA", OperatorName = "Audit QA", Username = "audit-qa", Role = "Viewer" });
            await Db.SaveChangesAsync();
            Writer = new(Db, options, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppEnvironment"] = environment }).Build(), TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
    }
    private static AuditEventDraft Page(Guid? clientId = null) => new("navigation.page.enter", "Succeeded")
    {
        PageKey = "analysis.spc.overall", ClientEventId = clientId,
        Details = new Dictionary<string, object?> { ["NavigationKind"] = "Push" }
    };

    [Fact]
    public async Task AppendUsesServerIdentityUtcEnvironmentAndCanonicalRoute()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var entry = await f.Writer.AppendAsync(Page(Guid.NewGuid()), f.Actor);
        Assert.NotEqual(Guid.Empty, entry.EventId);
        Assert.Equal("Test", entry.Environment);
        Assert.Equal(DateTimeKind.Utc, entry.OccurredAtUtc.Kind);
        Assert.Equal("audit-qa", entry.ActorAccountSnapshot);
        Assert.Equal("Viewer", entry.ActorRoleSnapshot);
        Assert.Equal("/spc", entry.RouteTemplate);
        Assert.Equal("WebRouter", entry.Source);
        Assert.Equal(1, JsonDocument.Parse(entry.DetailsJson).RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Single(await f.Db.AuditEvents.ToListAsync());
    }

    [Fact]
    public async Task SameSessionRetryIsIdempotentButOtherSessionsAndActorsAreIndependent()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var draft = Page(Guid.NewGuid());
        var first = await f.Writer.AppendAsync(draft, f.Actor);
        Assert.Equal(first.EventId, (await f.Writer.AppendAsync(draft, f.Actor)).EventId);
        Assert.Equal(first.EventId, (await f.Writer.AppendAsync(draft with { TraceId = "another-request", CorrelationId = "another-correlation" }, f.Actor)).EventId);
        Assert.NotEqual(first.EventId, (await f.Writer.AppendAsync(draft, AuditActor.User(1, "Jwt", new string('b', 64)))).EventId);
        f.Db.Operators.Add(new Operator { Id = 2, OperatorCode = "QA2", OperatorName = "QA2" }); await f.Db.SaveChangesAsync();
        await f.Writer.AppendAsync(draft, AuditActor.User(2, "Jwt", new string('a', 64)));
        Assert.Equal(3, await f.Db.AuditEvents.CountAsync());
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(draft with { PageKey = "analysis.trend.overall" }, f.Actor));
        Assert.Equal(3, await f.Db.AuditEvents.CountAsync());
    }

    [Theory]
    [InlineData("unknown.event.code", "Succeeded", "analysis.spc.overall")]
    [InlineData("navigation.page.enter", "Success", "analysis.spc.overall")]
    [InlineData("navigation.page.enter", "Succeeded", "/spc?token=secret")]
    public async Task InvalidCodesAndRoutesLeaveNoRows(string code, string outcome, string page)
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(new(code, outcome) { PageKey = page }, f.Actor));
        Assert.Empty(await f.Db.AuditEvents.ToListAsync());
    }

    [Theory]
    [InlineData("Password")]
    [InlineData("Token")]
    [InlineData("QueryString")]
    [InlineData("Lot")]
    [InlineData("RequestBody")]
    public async Task SensitiveDetailsAreRejectedWithoutEchoingPayload(string field)
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var draft = Page() with { Details = new Dictionary<string, object?> { [field] = "sensitive-value" } };
        var error = await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(draft, f.Actor));
        Assert.DoesNotContain("sensitive-value", error.Message);
        Assert.DoesNotContain(field, error.Message);
        Assert.Empty(await f.Db.AuditEvents.ToListAsync());
    }

    [Fact]
    public async Task DetailsRejectWrongTypesUnknownValuesOversizeAndDuplicateSchemaVersion()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        foreach (var details in new Dictionary<string, object?>[] {
            new() { ["NavigationKind"] = "secret-token" },
            new() { ["NavigationKind"] = new { Token = "secret" } },
            new() { ["SchemaVersion"] = 900 },
            new() { ["FilterFields"] = Enumerable.Repeat("DateFromUtc", 600).ToArray() } })
        {
            var draft = details.ContainsKey("FilterFields") ? new AuditEventDraft("read.query.execute", "Succeeded") { Details = details } : Page() with { Details = details };
            await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(draft, f.Actor));
        }
        Assert.Equal(0, await f.Db.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task ClientIdRequiresVerifiedPageActorAndSession()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(Page(Guid.NewGuid()), AuditActor.Anonymous()));
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(Page(Guid.NewGuid()), AuditActor.User(1, "Jwt", null)));
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(new("read.query.execute", "Succeeded") { ClientEventId = Guid.NewGuid() }, f.Actor));
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(Page(), AuditActor.User(999, "Jwt", null)));
    }

    [Fact]
    public async Task StageSharesBusinessTransactionAndRollbackRemovesBoth()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await using (var transaction = await f.Db.Database.BeginTransactionAsync())
        {
            f.Db.Operators.Add(new Operator { Id = 2, OperatorCode = "Rollback", OperatorName = "Rollback" });
            await f.Writer.StageAsync(new("security.operator.create", "Succeeded") { TargetType = "Operator", TargetId = "2" }, f.Actor);
            await f.Db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        f.Db.ChangeTracker.Clear();
        Assert.Equal(1, await f.Db.Operators.CountAsync());
        Assert.Empty(await f.Db.AuditEvents.ToListAsync());
    }

    [Fact]
    public async Task FailedAuditInsertRollsBackBusinessSave()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT, 'forced test failure'); END;");
        f.Db.Operators.Add(new Operator { Id = 2, OperatorCode = "MustRollback", OperatorName = "MustRollback" });
        await f.Writer.StageAsync(new("security.operator.create", "Succeeded"), f.Actor);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        f.Db.ChangeTracker.Clear();
        Assert.Equal(1, await f.Db.Operators.CountAsync());
        Assert.Equal(0, await f.Db.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task IndependentAppendDoesNotFlushCallerChanges()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        f.Db.Operators.Add(new Operator { Id = 2, OperatorCode = "Pending", OperatorName = "Pending" });
        await f.Writer.AppendAsync(new("read.query.execute", "Succeeded"), f.Actor);
        Assert.Equal(1, await f.Db.Operators.CountAsync());
        Assert.Single(f.Db.ChangeTracker.Entries<Operator>(), x => x.State == EntityState.Added);
    }

    [Fact]
    public async Task ExistingEventsCannotBeChangedOrRemovedThroughAnySaveOverload()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await f.Writer.AppendAsync(Page(), f.Actor);
        var entry = await f.Db.AuditEvents.SingleAsync();
        f.Db.Entry(entry).Property(x => x.Outcome).CurrentValue = "Denied";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(false));
        Assert.Throws<InvalidOperationException>(() => f.Db.SaveChanges(false));
        f.Db.ChangeTracker.Clear();
        f.Db.AuditEvents.Remove(await f.Db.AuditEvents.SingleAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => f.Db.SaveChanges());
    }

    [Fact]
    public async Task AnonymousAuthenticationNeverGetsOperatorSnapshotsAndUnknownEnvironmentFailsClosed()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var entry = await f.Writer.AppendAsync(new("auth.sso.exchange", "Denied") { ReasonCode = "AUTH_SSO_SIGNATURE" }, AuditActor.Anonymous());
        Assert.Null(entry.ActorOperatorId); Assert.Null(entry.ActorAccountSnapshot); Assert.Equal("Anonymous", entry.ActorType);
        await using var invalid = new Fixture(); await invalid.InitializeAsync("unknown");
        await Assert.ThrowsAsync<AuditEventValidationException>(() => invalid.Writer.AppendAsync(new("auth.sso.exchange", "Denied"), AuditActor.Anonymous()));
    }

    [Fact]
    public async Task EnvironmentIsServerControlledAndProductionLabelIsCanonical()
    {
        await using var f = new Fixture(); await f.InitializeAsync("production");
        Assert.Equal("Production", (await f.Writer.AppendAsync(Page(Guid.NewGuid()), f.Actor)).Environment);
        Assert.DoesNotContain(typeof(AuditEventDraft).GetProperties(), x => x.Name is "Environment" or "ActorOperatorId" or "EventId" or "OccurredAtUtc" or "RouteTemplate");
    }

    [Fact]
    public async Task Utf8LimitAcceptsBoundedSummaryAndRejectsLargerSummary()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var valid = new AuditEventDraft("read.query.execute", "Succeeded") { Details = new Dictionary<string, object?> { ["FilterFields"] = Enumerable.Range(0, 50).Select(n => "Field" + n + new string('a', 50)).ToArray(), ["ResultCount"] = 0 } };
        await f.Writer.AppendAsync(valid, f.Actor);
        var invalid = valid with { Details = new Dictionary<string, object?> { ["FilterFields"] = Enumerable.Range(0, 64).Select(n => "Field" + n + new string('a', 57)).ToArray() } };
        await Assert.ThrowsAsync<AuditEventValidationException>(() => f.Writer.AppendAsync(invalid, f.Actor));
        Assert.Equal(1, await f.Db.AuditEvents.CountAsync());
    }
}
