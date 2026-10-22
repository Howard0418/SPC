using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ParticleQueryServiceTests
{
    [Fact]
    public async Task SqlUtcTimes_ShouldKeepTicksAndRoundTripToLocationComparison()
    {
        await using var db = CreateContext();
        var batch = AddBatch(db);
        var stored = new DateTime(2026, 9, 29, 0, 8, 0, DateTimeKind.Unspecified);
        for (var location = 1; location <= 9; location++)
        {
            Add(db, batch, $"R{location}", 0.5m, location * 10, 749, $"C{location}");
        }
        foreach (var entry in db.ChangeTracker.Entries<ParticleMeasurement>()) entry.Entity.MeasurementTime = stored;
        await db.SaveChangesAsync();
        var service = new ParticleQueryService(db);
        var from = stored.AddDays(-1);
        var to = stored.AddDays(1);
        var page = await service.GetMeasurementsAsync(from, to, ["R1"], [0.5m], null, null, 1, 50, false);
        var trend = await service.GetTrendAsync(from, to, "R1", 0.5m, null);
        var chart = await service.GetSpcAsync(from, to, "R1", 0.5m, null, "C");
        foreach (var time in new[] { page.Items.Single().MeasurementTime, trend.Points.Single().Time, chart.Points.Single().Time })
        {
            time.Kind.Should().Be(DateTimeKind.Utc);
            time.Ticks.Should().Be(stored.Ticks);
        }
        var json = System.Text.Json.JsonSerializer.Serialize(chart, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var timestamp = document.RootElement.GetProperty("points")[0].GetProperty("time").GetString()!;
        timestamp.Should().EndWith("Z");
        var comparison = await service.GetLocationComparisonAsync(DateTimeOffset.Parse(timestamp).UtcDateTime, 0.5m, null, batch.UploadBatchId, "Sheet1", 749);
        comparison.Items.Should().HaveCount(9).And.OnlyContain(item => !item.IsMissing && item.Count.HasValue);
        comparison.Items.Select(item => item.Count).Should().Equal(Enumerable.Range(1, 9).Select(value => (long?)(value * 10)));
    }

    [Fact]
    public async Task Trend_ShouldIsolateLocationAndParticleSize()
    {
        await using var db = CreateContext();
        var batch = AddBatch(db);
        Add(db, batch, "R8", 0.5m, 11, 1, "C");
        Add(db, batch, "R9", 0.5m, 22, 1, "D");
        Add(db, batch, "R8", 1m, 33, 1, "E");
        await db.SaveChangesAsync();

        var result = await new ParticleQueryService(db).GetTrendAsync(Start.AddDays(-1), Start.AddDays(1), "R8", 0.5m, null);

        result.Points.Select(point => point.Count).Should().Equal(11);
        result.SeriesKey.Should().Be("R8|0.5|DEFAULT");
    }

    [Fact]
    public async Task LocationComparison_ShouldReturnMissingLocationsAndAmbiguousCandidates()
    {
        await using var db = CreateContext();
        var first = AddBatch(db);
        var second = AddBatch(db);
        Add(db, first, "R1", 0.5m, 10, 26, "C");
        Add(db, first, "R2", 0.5m, 20, 26, "D");
        Add(db, second, "R1", 0.5m, 30, 30, "C");
        await db.SaveChangesAsync();
        var service = new ParticleQueryService(db);

        var ambiguous = await service.GetLocationComparisonAsync(Start, 0.5m, null, null, null, null);
        ambiguous.IsAmbiguous.Should().BeTrue();
        ambiguous.Candidates.Should().HaveCount(2);

        var selected = await service.GetLocationComparisonAsync(Start, 0.5m, null, first.UploadBatchId, "Sheet1", 26);
        selected.IsAmbiguous.Should().BeFalse();
        selected.Items.Should().HaveCount(9);
        selected.Items.Single(item => item.Location == "R1").Count.Should().Be(10);
        selected.Items.Single(item => item.Location == "R9").Count.Should().BeNull();
    }

    [Fact]
    public async Task Spc_ShouldCalculateOnlyTheRequestedSequence()
    {
        await using var db = CreateContext();
        var batch = AddBatch(db);
        for (var index = 1; index <= 20; index++)
        {
            db.ParticleMeasurements.Add(new ParticleMeasurement
            {
                UploadBatchId = batch.UploadBatchId, MeasurementTime = Start.AddHours(index), Location = "R8",
                ParticleSize = 0.5m, Count = 4, SourceSheet = "Sheet1", SourceRow = index, SourceColumn = "C", RawValue = "4"
            });
        }
        Add(db, batch, "R9", 0.5m, 999, 99, "D");
        await db.SaveChangesAsync();

        var result = await new ParticleQueryService(db).GetSpcAsync(Start, Start.AddDays(2), "R8", 0.5m, null, "C");

        result.PointCount.Should().Be(20);
        result.Points.Should().OnlyContain(point => point.Count == 4);
        result.StatControlLimits!.Cl.Should().Be(4);
    }

    [Fact]
    public async Task Spc_UChart_ShouldUseStoredSamplingVolumeAndUnit()
    {
        await using var db = CreateContext();
        var batch = AddBatch(db);
        for (var index = 1; index <= 20; index++)
        {
            db.ParticleMeasurements.Add(new ParticleMeasurement
            {
                UploadBatchId = batch.UploadBatchId, MeasurementTime = Start.AddHours(index), Location = "R8",
                ParticleSize = 0.5m, Count = 10, SamplingVolume = 2.5m, SamplingVolumeUnit = "L",
                SourceSheet = "Sheet1", SourceRow = index, SourceColumn = "C", RawValue = "10"
            });
        }
        await db.SaveChangesAsync();

        var result = await new ParticleQueryService(db).GetSpcAsync(Start, Start.AddDays(2), "R8", 0.5m, null, "U");

        result.ChartType.Should().Be("U_CHART");
        result.SamplingBasis.Should().Be("sampling-volume");
        result.SamplingVolumeUnit.Should().Be("L");
        result.Points.Should().OnlyContain(point => point.Value == 4);
    }

    private static readonly DateTime Start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UploadBatch AddBatch(AppDbContext db) { var batch = new UploadBatch { UploadType = "Particle", ImportStatus = "Imported" }; db.UploadBatches.Add(batch); return batch; }
    private static void Add(AppDbContext db, UploadBatch batch, string location, decimal size, long count, int row, string column) => db.ParticleMeasurements.Add(new ParticleMeasurement
    {
        UploadBatchId = batch.UploadBatchId, MeasurementTime = Start, Location = location, ParticleSize = size, Count = count,
        SourceSheet = "Sheet1", SourceRow = row, SourceColumn = column, RawValue = count.ToString()
    });
}
