using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class AuditRetentionServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Navigation", 180, 365)]
    [InlineData("Read", 180, 365)]
    [InlineData("Security", 730, 1825)]
    [InlineData("Measurement", 730, 1825)]
    public void PolicyUsesApprovedOnlineAndTotalRetention(string category, int onlineDays, int totalDays)
    {
        var policy = AuditRetentionPolicy.For(category);
        Assert.Equal(onlineDays, policy.OnlineDays);
        Assert.Equal(totalDays, policy.TotalDays);
    }

    [Fact]
    public async Task PreviewCountsOnlyExpiredEventsNotCoveredByActiveHold()
    {
        await using var f = await Fixture.CreateAsync();
        f.Db.AuditEvents.AddRange(Row("Read", Now.AddDays(-181), "free"), Row("Read", Now.AddDays(-181), "held"),
            Row("Security", Now.AddDays(-500), "critical"));
        f.Db.AuditLegalHolds.Add(new AuditLegalHold
        {
            HoldId = Guid.NewGuid(), Environment = "Test", DateFromUtc = Now.AddDays(-190), DateToUtc = Now.AddDays(-170),
            TargetType = "Record", TargetId = "held", ReasonCode = "CASE-001", CreatedByOperatorId = 7, CreatedAtUtc = Now
        });
        await f.Db.SaveChangesAsync();

        var result = await f.Service.PreviewAsync();

        Assert.Equal(1, result.DueForArchive);
        Assert.Equal(1, result.ActiveLegalHolds);
        Assert.Equal(3, result.OnlineCount);
    }

    [Fact]
    public async Task ExecuteArchivesVerifiesAndDeletesThenRerunIsIdempotent()
    {
        await using var f = await Fixture.CreateAsync(enabled: true);
        f.Db.AuditEvents.Add(Row("Read", Now.AddDays(-181), "free"));
        await f.Db.SaveChangesAsync();

        var first = await f.Service.ExecuteAsync();
        var second = await f.Service.ExecuteAsync();

        Assert.Equal(1, first.Archived);
        Assert.Equal(0, second.Archived);
        Assert.Empty(await f.Db.AuditEvents.Where(x => x.TargetId == "free").ToListAsync());
        Assert.Single(await f.Db.AuditEventArchives.ToListAsync());
        Assert.Equal("Verified", (await f.Db.AuditArchiveBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task ExecuteLeavesOnlineEventWhenArchiveVerificationFails()
    {
        await using var f = await Fixture.CreateAsync(enabled: true, corruptAfterEncrypt: true);
        f.Db.AuditEvents.Add(Row("Read", Now.AddDays(-181), "free"));
        await f.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<AuditRetentionException>(() => f.Service.ExecuteAsync());

        Assert.Single(await f.Db.AuditEvents.Where(x => x.TargetId == "free").ToListAsync());
        Assert.Empty(await f.Db.AuditEventArchives.ToListAsync());
    }

    [Fact]
    public async Task ExecuteRejectsDisabledOrNonTestEnvironment()
    {
        await using var disabled = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<AuditRetentionException>(() => disabled.Service.ExecuteAsync());
        await using var production = await Fixture.CreateAsync(enabled: true, environment: "Production");
        await Assert.ThrowsAsync<AuditRetentionException>(() => production.Service.ExecuteAsync());
    }

    [Fact]
    public async Task LegalHoldRequiresValidRangeAndCanBeReleased()
    {
        await using var f = await Fixture.CreateAsync(enabled: true);
        await Assert.ThrowsAsync<AuditRetentionException>(() => f.Service.CreateLegalHoldAsync(
            new(Now, Now, "CASE-001"), 7));
        var hold = await f.Service.CreateLegalHoldAsync(new(Now.AddDays(-2), Now.AddDays(2), "CASE-001", TargetId: "lot-1"), 7);
        Assert.Null(hold.ReleasedAtUtc);
        Assert.True(await f.Service.ReleaseLegalHoldAsync(hold.HoldId, 8));
        Assert.False(await f.Service.ReleaseLegalHoldAsync(hold.HoldId, 8));
        Assert.Equal(8, (await f.Db.AuditLegalHolds.SingleAsync()).ReleasedByOperatorId);
    }

    private static AuditEvent Row(string category, DateTime occurred, string target) => new()
    {
        EventId = Guid.NewGuid(), OccurredAtUtc = occurred, ReceivedAtUtc = occurred, Environment = "Test",
        Category = category, EventCode = category == "Read" ? "read.query.execute" : "security.permission.denied",
        Outcome = "Succeeded", ActorType = "Service", ActorAssurance = "Service", ActorServiceCode = "test",
        TargetType = "Record", TargetId = target, Source = "Api", DetailsJson = "{}",
        DeduplicationHash = new string('A', 64)
    };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public AppDbContext Db { get; }
        public AuditRetentionService Service { get; }

        private Fixture(SqliteConnection connection, AppDbContext db, AuditRetentionService service)
            => (this.connection, Db, Service) = (connection, db, service);

        public static async Task<Fixture> CreateAsync(bool enabled = false, string environment = "Test", bool corruptAfterEncrypt = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options); await db.Database.EnsureCreatedAsync();
            var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppEnvironment"] = environment,
                ["Audit:Retention:Enabled"] = enabled.ToString(),
                ["Audit:Retention:AllowCurrentConnectionForMaintenance"] = "true",
                ["Audit:Retention:ArchiveEncryptionKeyId"] = "test-key",
                ["Audit:Retention:ArchiveEncryptionKeys:test-key"] = key
            }).Build();
            var crypto = new AuditArchiveCryptography(configuration) { CorruptAfterEncryptForTest = corruptAfterEncrypt };
            var service = new AuditRetentionService(db, options, configuration, new FixedTime(Now), crypto,
                NullLogger<AuditRetentionService>.Instance);
            return new(connection, db, service);
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class FixedTime(DateTime value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(value);
    }
}
