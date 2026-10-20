using FluentAssertions;
using MesSpc.Api.Services;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
namespace MesSpc.Api.Tests;
public class VersionHistoryDateRangeTests
{
    [Fact]
    public void ExistingFTableHistoryMigrationMustBeDiscoverable()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=.;Database=metadata;Integrated Security=True;TrustServerCertificate=True").Options);
        db.Database.GetMigrations().Should().Contain("20261007090000_AddChemicalFTableVersionHistories");
    }

    [Theory]
    [InlineData("2026-10-19", "2026-10-19", "2026-10-18T16:00:00Z", "2026-10-19T16:00:00Z")]
    [InlineData("2026-12-31", "2027-01-01", "2026-12-30T16:00:00Z", "2027-01-01T16:00:00Z")]
    public void RangeUsesTaipeiInclusiveDays(string from,string to,string start,string end)
    {
        var range=VersionHistoryDateRange.Parse(from,to);
        range.FromUtc.Should().Be(DateTime.Parse(start).ToUniversalTime());
        range.UntilUtc.Should().Be(DateTime.Parse(end).ToUniversalTime());
    }
    [Theory]
    [InlineData("2026-02-30",null)] [InlineData("10/19/2026",null)]
    [InlineData("2026-10-20","2026-10-19")] [InlineData("2026-1-1",null)]
    public void InvalidRangeIsRejected(string from,string? to)
        => FluentActions.Invoking(()=>VersionHistoryDateRange.Parse(from,to)).Should().Throw<ArgumentException>();
    [Fact] public void EmptyAndOneSidedRangesAreAllowed()
    {
        VersionHistoryDateRange.Parse(null,null).FromUtc.Should().BeNull();
        VersionHistoryDateRange.Parse("2026-10-19",null).UntilUtc.Should().BeNull();
        VersionHistoryDateRange.Parse(null,"2026-10-19").FromUtc.Should().BeNull();
    }
}
public class SpecificationLimitVersionTests
{
    [Fact]
    public async Task EfWritesKeepSnapshotsAndIgnoreUnrelatedFields()
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var ppc=new PartProcessCharacteristic{ USL=20, LSL=10, TargetValue=15 };
        db.Add(ppc);await db.SaveChangesAsync();
        ppc.USL=22;await db.SaveChangesAsync();
        ppc.UCL=19;await db.SaveChangesAsync();
        var segment=new ControlLimitSegment { PartProcessCharacteristicId=ppc.Id,StartDate=new DateTime(2026,10,20),USL=23 };
        db.Add(segment);await db.SaveChangesAsync();segment.USL=24;await db.SaveChangesAsync();db.Remove(segment);await db.SaveChangesAsync();
        var history=await db.SpecificationLimitVersions.OrderBy(x=>x.VersionNo).ToListAsync();
        history.Should().HaveCount(5);history.Select(x=>x.VersionNo).Should().Equal(1,2,3,4,5);
        history[1].PreviousSnapshotJson.Should().Contain("20");history[1].SnapshotJson.Should().Contain("22");
        history.Last().ChangeType.Should().Be("Delete");history.Last().SnapshotJson.Should().BeNull();
        history.Last().PreviousSnapshotJson.Should().Contain("24");
    }
    [Fact]
    public async Task ConcurrentSpecificationChangesRejectStaleSnapshot()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var a=new AppDbContext(options);var ppc=new PartProcessCharacteristic{USL=20};a.Add(ppc);await a.SaveChangesAsync();
        await using var b=new AppDbContext(options);var stale=await b.PartProcessCharacteristics.SingleAsync();
        ppc.USL=21;await a.SaveChangesAsync();stale.USL=22;
        Func<Task> save=()=>b.SaveChangesAsync();await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var fresh=new AppDbContext(options);(await fresh.PartProcessCharacteristics.SingleAsync()).USL.Should().Be(21);
        (await fresh.SpecificationLimitVersions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public void SynchronousEfWritesAreAlsoTracked()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var ppc=new PartProcessCharacteristic{USL=20};db.Add(ppc);db.SaveChanges();ppc.USL=21;db.SaveChanges();
        db.SpecificationLimitVersions.Count().Should().Be(2);
    }
    [Fact]
    public async Task HistoryFailureRollsBackSpecificationWrite()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db=new AppDbContext(options);await db.Database.EnsureCreatedAsync();
        var process=new Process{ProcessCode="TEST",ProcessName="測試"};
        var characteristic=new QualityCharacteristic{CharacteristicCode="TEST",CharacteristicName="測試"};
        db.AddRange(process,characteristic);await db.SaveChangesAsync();
        var ppc=new PartProcessCharacteristic{ProcessId=process.Id,CharacteristicId=characteristic.Id};db.Add(ppc);await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_spec_history BEFORE INSERT ON SpecificationLimitVersions BEGIN SELECT RAISE(ABORT, 'test rejection'); END;");
        ppc.USL=99;Func<Task> save=()=>db.SaveChangesAsync();await save.Should().ThrowAsync<DbUpdateException>();
        await using var fresh=new AppDbContext(options);(await fresh.PartProcessCharacteristics.SingleAsync()).USL.Should().BeNull();
    }
    [Fact]
    public async Task ActorComesFromServerAndHistoryIsImmutable()
    {
        var accessor=new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"測試人員")},"test"))}};
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,accessor);
        db.Add(new PartProcessCharacteristic{USL=10});await db.SaveChangesAsync();
        var row=await db.SpecificationLimitVersions.SingleAsync();row.ChangedBy.Should().Be("測試人員");
        row.ChangedBy="偽造";Func<Task> save=()=>db.SaveChangesAsync();await save.Should().ThrowAsync<InvalidOperationException>();
    }
}
