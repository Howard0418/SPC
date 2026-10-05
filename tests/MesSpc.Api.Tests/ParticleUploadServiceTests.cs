using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class ParticleUploadServiceTests
{
    [Fact]
    public async Task Preview_ShouldValidateRowsAndSourceCoordinates()
    {
        await using var db = CreateContext();
        var service = new ParticleUploadService(db, NullLogger<ParticleUploadService>.Instance);
        var request = Request(
            Row("2026-09-01T08:00:00+08:00", "R1", "0.5", "12", 26, "C"),
            Row("bad-time", "R10", "2", "-1", 26, "D"),
            Row("2026-09-01T08:00:00+08:00", "R2", "1", "8", 26, "C"));

        var result = await service.CreatePreviewAsync(request, "tester");

        result.TotalRows.Should().Be(3);
        result.ValidRows.Should().Be(1);
        result.ErrorRows.Should().Be(2);
        var codes = await db.UploadErrors.Select(error => error.ErrorCode).ToListAsync();
        codes.Should().Contain(["MEASUREMENT_TIME_INVALID", "LOCATION_UNSUPPORTED", "PARTICLE_SIZE_UNSUPPORTED", "COUNT_NEGATIVE", "DUPLICATE_SOURCE_CELL"]);
    }

    [Fact]
    public async Task Confirm_ShouldRejectOrSkipExistingMeasurementAndRemainIdempotent()
    {
        await using var db = CreateContext();
        var existingBatch = new UploadBatch { UploadType = "Particle", ImportStatus = "Imported" };
        db.UploadBatches.Add(existingBatch);
        db.ParticleMeasurements.Add(new ParticleMeasurement
        {
            UploadBatchId = existingBatch.UploadBatchId,
            MeasurementTime = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Location = "R1", ParticleSize = 0.5m, Count = 9,
            SourceSheet = "Old", SourceRow = 1, SourceColumn = "A", RawValue = "9"
        });
        await db.SaveChangesAsync();

        var service = new ParticleUploadService(db, NullLogger<ParticleUploadService>.Instance);
        var preview = await service.CreatePreviewAsync(Request(
            Row("2026-09-01T08:00:00+08:00", "R1", "0.5", "12", 26, "C"),
            Row("2026-09-01T08:00:00+08:00", "R2", "0.5", "7", 26, "D")), "tester");

        preview.DuplicateRows.Should().Be(1);
        var rejected = await service.ConfirmAsync(preview.UploadBatchId, "reject");
        rejected.Should().NotBeNull();
        rejected!.ConflictCode.Should().Be("DUPLICATE_MEASUREMENT");
        (await db.ParticleMeasurements.CountAsync()).Should().Be(1);

        var imported = await service.ConfirmAsync(preview.UploadBatchId, "skip");
        imported.Should().NotBeNull();
        imported!.InsertedRows.Should().Be(1);
        imported.SkippedRows.Should().Be(1);
        (await db.ParticleMeasurements.CountAsync()).Should().Be(2);

        var replay = await service.ConfirmAsync(preview.UploadBatchId, "skip");
        replay.Should().NotBeNull();
        replay!.AlreadyImported.Should().BeTrue();
        (await db.ParticleMeasurements.CountAsync()).Should().Be(2);
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ParticlePreviewRequest Request(params ParticlePreviewRow[] rows) => new(
        Guid.NewGuid(), "particle.xlsx", Guid.NewGuid().ToString("N"), "TransFiles", rows);

    private static ParticlePreviewRow Row(string time, string location, string size, string count, int row, string column) =>
        new(time, location, size, count, null, null, null, null, "Sheet1", row, column, count);
}
