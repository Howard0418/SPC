using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests.Controllers;

public class ManualMeasurementsV1ControllerTests
{
    [Fact]
    public async Task GetDaily_ReturnsOnlyRequestedSamplingPhase()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"daily-phase-{Guid.NewGuid():N}").Options);
        var date = new DateTime(2028, 8, 3);
        db.VariableMeasurements.AddRange(
            CreateMeasurement("OPEN", 98.5, date.AddHours(8)),
            CreateMeasurement("MIDDLE", 96.4, date.AddHours(14)),
            CreateMeasurement("CLOSE", 94.2, date.AddHours(19)));
        await db.SaveChangesAsync();
        var controller = new ManualMeasurementsV1Controller(db,
            new SpcService(db, new StubEmailService(), new ConfigurationBuilder().Build()));

        var result = await controller.GetDaily(12, new DateOnly(2028, 8, 3), "OPEN");

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("98.5", json);
        Assert.DoesNotContain("96.4", json);
        Assert.DoesNotContain("94.2", json);
    }

    [Fact]
    public async Task GetDaily_ReturnsMiddleSamplingPhase()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"daily-middle-phase-{Guid.NewGuid():N}").Options);
        var date = new DateTime(2028, 8, 3);
        db.VariableMeasurements.AddRange(
            CreateMeasurement("OPEN", 98.5, date.AddHours(8)),
            CreateMeasurement("MIDDLE", 96.4, date.AddHours(14)),
            CreateMeasurement("CLOSE", 94.2, date.AddHours(19)));
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.GetDaily(12, new DateOnly(2028, 8, 3), "MIDDLE");

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("96.4", json);
        Assert.DoesNotContain("98.5", json);
        Assert.DoesNotContain("94.2", json);
    }

    [Fact]
    public async Task DeleteItem_RemovesPortalDailyMeasurementAndDerivedRecords()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"delete-daily-measurement-{Guid.NewGuid():N}").Options);
        var measurement = CreateMeasurement("MIDDLE", 96.4, new DateTime(2028, 8, 3, 14, 0, 0));
        db.VariableMeasurements.Add(measurement);
        await db.SaveChangesAsync();
        db.SpcCalculationResults.Add(new SpcCalculationResult
        {
            UploadBatchId = measurement.UploadBatchId,
            VariableMeasurementId = measurement.Id,
            PartProcessCharacteristicId = measurement.PartProcessCharacteristicId
        });
        db.AlertEvents.Add(new AlertEvent
        {
            VariableMeasurementId = measurement.Id,
            UploadBatchId = measurement.UploadBatchId,
            Message = "測試警示"
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.DeleteItem(measurement.Id, "qa-user");

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await db.VariableMeasurements.ToListAsync());
        Assert.Empty(await db.SpcCalculationResults.ToListAsync());
        Assert.Empty(await db.AlertEvents.ToListAsync());
        var audit = Assert.Single(await db.UploadDetails.ToListAsync());
        Assert.False(audit.IsValid);
        Assert.Equal("qa-user", audit.CreatedBy);
        Assert.Contains("ManualMeasurementDeleted", audit.PayloadJson);
        Assert.Contains("96.4", audit.PayloadJson);
    }

    [Fact]
    public async Task DeleteItem_RejectsMeasurementOutsidePortalDailyFlow()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"delete-non-daily-measurement-{Guid.NewGuid():N}").Options);
        var measurement = CreateMeasurement("GENERAL", 96.4, new DateTime(2028, 8, 3, 14, 0, 0));
        measurement.PortalDailyDate = null;
        db.VariableMeasurements.Add(measurement);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.DeleteItem(measurement.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(await db.VariableMeasurements.ToListAsync());
    }

    private static ManualMeasurementsV1Controller CreateController(AppDbContext db) => new(db,
        new SpcService(db, new StubEmailService(), new ConfigurationBuilder().Build()));

    private static VariableMeasurement CreateMeasurement(string phase, double value, DateTime measuredAt) => new()
    {
        UploadBatchId = Guid.NewGuid(),
        PartProcessCharacteristicId = 101,
        PartId = 1,
        ProcessId = 1,
        MachineId = 12,
        CharacteristicId = 1,
        SampleNo = 1,
        MeasuredValue = value,
        MeasuredAt = measuredAt,
        PortalDailyDate = measuredAt.Date,
        SamplingPhase = phase,
        SourceType = SourceType.Manual
    };

    private sealed class StubEmailService : IEmailNotificationService
    {
        public Task<bool> SendAlertEmailAsync(AlertEvent alertEvent, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null) => Task.FromResult(true);
        public Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null) => Task.FromResult(true);
        public Task<bool> SendHtmlEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null) => Task.FromResult(true);
        public Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName) => Task.FromResult(true);
    }
}
