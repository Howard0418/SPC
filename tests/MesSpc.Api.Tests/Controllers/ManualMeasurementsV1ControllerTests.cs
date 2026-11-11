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
    public async Task GetDaily_RestoresOriginalInputsFromLatestReceiptTraceWhenUploadDetailIsAbsent()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"daily-receipt-input-{Guid.NewGuid():N}").Options);
        SeedDailyMaster(db);
        var date = new DateTime(2028, 8, 3);
        var measurement = CreateMeasurement("OPEN", 222.11, date.AddHours(8));
        measurement.RecheckValue = 221.5;
        var initialId = Guid.NewGuid();
        var recheckId = Guid.NewGuid();
        measurement.ChemicalReceiptId = initialId;
        db.VariableMeasurements.Add(measurement);
        db.ChemicalCalculationReceipts.AddRange(
            Receipt(initialId, "8.3", null),
            Receipt(recheckId, "8.1", "0.2"));
        await db.SaveChangesAsync();
        db.ChemicalCalculationTraces.AddRange(
            new() { VariableMeasurementId=measurement.Id,ReceiptId=initialId,Phase="Initial",VersionNo=1 },
            new() { VariableMeasurementId=measurement.Id,ReceiptId=recheckId,Phase="Recheck",VersionNo=1 });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetDaily(12, new DateOnly(2028, 8, 3), "OPEN");

        var json = System.Text.Json.JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("\"titrationValue\":\"8.3\"", json);
        Assert.Contains("\"recheckTitrationValue\":\"8.1\"", json);
        Assert.Contains("\"recheckSecondaryTitrationValue\":\"0.2\"", json);
        Assert.Empty(db.UploadDetails);
    }

    [Fact]
    public async Task GetDaily_KeepsExplicitlyEmptyReceiptInputBlankInsteadOfUsingStaleUploadDetail()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"daily-empty-receipt-input-{Guid.NewGuid():N}").Options);
        SeedDailyMaster(db);
        var date = new DateTime(2028, 8, 3);
        var measurement = CreateMeasurement("OPEN", 222.11, date.AddHours(8));
        var receiptId = Guid.NewGuid();
        measurement.ChemicalReceiptId = receiptId;
        db.VariableMeasurements.Add(measurement);
        db.ChemicalCalculationReceipts.Add(Receipt(receiptId, null, null));
        db.UploadDetails.Add(new UploadDetail
        {
            UploadBatchId=measurement.UploadBatchId, RowNo=1, IsValid=true,
            PayloadJson=System.Text.Json.JsonSerializer.Serialize(new { CharacteristicCode="PARRADELETE", TankCode=(string?)null, TitrationValue="9.9", SecondaryTitrationValue="8.8" })
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetDaily(12, new DateOnly(2028, 8, 3), "OPEN");

        var json = System.Text.Json.JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("\"titrationValue\":null", json);
        Assert.Contains("\"secondaryTitrationValue\":null", json);
        Assert.DoesNotContain("9.9", json);
        Assert.DoesNotContain("8.8", json);
    }

    [Fact]
    public async Task GetDaily_ReturnsOnlyRequestedSamplingPhase()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"daily-phase-{Guid.NewGuid():N}").Options);
        SeedDailyMaster(db);
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
        SeedDailyMaster(db);
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
            .UseInMemoryDatabase($"delete-daily-measurement-{Guid.NewGuid():N}")
            // This unit test verifies deletion/audit content, not transaction rollback.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);
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
        // Generic audit metadata is stamped by AppDbContext; the deletion actor lives in its immutable payload.
        Assert.Equal("System", audit.CreatedBy);
        using var evidence = System.Text.Json.JsonDocument.Parse(audit.PayloadJson);
        Assert.Equal("qa-user", evidence.RootElement.GetProperty("actor").GetString());
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

    private static void SeedDailyMaster(AppDbContext db)
    {
        db.Machines.Add(new Machine { Id=12, ProcessId=1, MachineCode="DP", MachineName="除鈀線" });
        db.QualityCharacteristics.Add(new QualityCharacteristic { Id=1, CharacteristicCode="PARRADELETE", CharacteristicName="PARRADELETE", ControlScope="CHEM" });
        db.PartProcessCharacteristics.Add(new PartProcessCharacteristic { Id=101, ProcessId=1, MachineId=12, CharacteristicId=1, ControlScope="CHEM", IsEnabled=true });
    }

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
        SamplingStage = "GENERAL",
        SourceType = SourceType.Manual
    };

    private static ChemicalCalculationReceipt Receipt(Guid id, string? primary, string? secondary) => new()
    {
        Id=id, PartProcessCharacteristicId=101, EffectivityId=1,
        MeasuredAtUtc=new DateTime(2028,8,3,0,0,0,DateTimeKind.Utc),
        CreatedAtUtc=new DateTime(2028,8,3,0,0,0,DateTimeKind.Utc),
        ExpiresAtUtc=new DateTime(2028,8,4,0,0,0,DateTimeKind.Utc),
        InputHash="hash", SourceKeyHash=Guid.NewGuid().ToString("N"), EngineVersion="test", PrecisionPolicy="test",
        PayloadJson=System.Text.Json.JsonSerializer.Serialize(new { originalInput=new { Primary=primary, Secondary=secondary } })
    };

    private sealed class StubEmailService : IEmailNotificationService
    {
        public Task<bool> SendAlertEmailAsync(AlertEvent alertEvent, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null) => Task.FromResult(true);
        public Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null) => Task.FromResult(true);
        public Task<bool> SendHtmlEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null, string? fileSubject = null) => Task.FromResult(true);
        public Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName, string? uiLocale = null) => Task.FromResult(true);
    }
}
