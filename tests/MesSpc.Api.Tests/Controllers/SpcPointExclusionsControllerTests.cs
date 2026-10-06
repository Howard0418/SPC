using System.Security.Claims;
using FluentAssertions;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests.Controllers;

public class SpcPointExclusionsControllerTests
{
    [Fact]
    public async Task UpsertPointExclusion_WhenVariableMeasurementBelongsToPpc_CreatesActiveHiddenState()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        var controller = CreateController(db);
        var request = new UpsertSpcPointExclusionReq(
            measurement.PartProcessCharacteristicId,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "ExcludedHidden",
            "輸入值確認異常");

        var result = await controller.UpsertPointExclusion(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var entity = await db.SpcPointExclusions.SingleAsync();
        entity.PartProcessCharacteristicId.Should().Be(101);
        entity.PointScope.Should().Be("VariableMeasurement");
        entity.VariableMeasurementId.Should().Be(measurement.Id);
        entity.State.Should().Be("ExcludedHidden");
        entity.Reason.Should().Be("輸入值確認異常");
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertPointExclusion_WhenSamePointExists_UpdatesExistingRow()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        db.SpcPointExclusions.Add(new SpcPointExclusion
        {
            PartProcessCharacteristicId = measurement.PartProcessCharacteristicId,
            PointScope = "VariableMeasurement",
            VariableMeasurementId = measurement.Id,
            State = "ExcludedVisible",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);
        var request = new UpsertSpcPointExclusionReq(
            measurement.PartProcessCharacteristicId,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "ExcludedHidden",
            "改為隱藏");

        var result = await controller.UpsertPointExclusion(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var rows = await db.SpcPointExclusions.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].State.Should().Be("ExcludedHidden");
        rows[0].Reason.Should().Be("改為隱藏");
    }

    [Fact]
    public async Task RestorePointExclusion_WhenActiveRowExists_DeactivatesRowAndListNoLongerReturnsIt()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        var exclusion = new SpcPointExclusion
        {
            PartProcessCharacteristicId = measurement.PartProcessCharacteristicId,
            PointScope = "VariableMeasurement",
            VariableMeasurementId = measurement.Id,
            State = "ExcludedHidden",
            IsActive = true
        };
        db.SpcPointExclusions.Add(exclusion);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var restoreResult = await controller.RestorePointExclusion(exclusion.Id, CancellationToken.None);
        var listResult = await controller.GetPointExclusions(101, null, null, null, null, CancellationToken.None);

        restoreResult.Should().BeOfType<OkObjectResult>();
        (await db.SpcPointExclusions.SingleAsync()).IsActive.Should().BeFalse();
        var ok = listResult.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("\"data\":[]");
    }

    [Fact]
    public async Task UpsertPointExclusion_WhenVariableMeasurementBelongsToAnotherPpc_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        db.PartProcessCharacteristics.Add(new PartProcessCharacteristic
        {
            Id = 202,
            ProcessId = 1,
            CharacteristicId = 1,
            IsEnabled = true
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);
        var request = new UpsertSpcPointExclusionReq(
            202,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "ExcludedVisible",
            null);

        var result = await controller.UpsertPointExclusion(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        (await db.SpcPointExclusions.CountAsync()).Should().Be(0);
    }

    private static async Task<VariableMeasurement> SeedVariableMeasurementAsync(AppDbContext db, int ppcId)
    {
        db.PartProcessCharacteristics.Add(new PartProcessCharacteristic
        {
            Id = ppcId,
            ProcessId = 1,
            CharacteristicId = 1,
            IsEnabled = true
        });
        var uploadBatch = new UploadBatch { UploadBatchId = Guid.NewGuid() };
        var measurement = new VariableMeasurement
        {
            UploadBatchId = uploadBatch.UploadBatchId,
            PartProcessCharacteristicId = ppcId,
            PartId = 1,
            ProcessId = 1,
            CharacteristicId = 1,
            SampleNo = 1,
            MeasuredValue = 12.3,
            MeasuredAt = DateTime.UtcNow
        };
        db.UploadBatches.Add(uploadBatch);
        db.VariableMeasurements.Add(measurement);
        await db.SaveChangesAsync();
        return measurement;
    }

    private static SpcController CreateController(AppDbContext db)
    {
        var emailService = new Mock<IEmailNotificationService>();
        var config = new ConfigurationBuilder().Build();
        var controller = new SpcController(new SpcService(db, emailService.Object, config), db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "spc-editor"), new Claim(ClaimTypes.Role, "Editor")],
                    "TestAuth"))
            }
        };
        return controller;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"spc-point-exclusions-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
