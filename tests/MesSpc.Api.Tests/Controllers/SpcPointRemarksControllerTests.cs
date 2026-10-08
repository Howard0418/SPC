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

public class SpcPointRemarksControllerTests
{
    [Fact]
    public async Task UpsertPointRemark_WhenVariableMeasurementBelongsToPpc_CreatesActiveRemarkWithoutExclusion()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        var controller = CreateController(db);
        var request = new UpsertSpcPointRemarkReq(
            measurement.PartProcessCharacteristicId,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "測試備註：首件觀察");

        var result = await controller.UpsertPointRemark(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var entity = await db.SpcPointRemarks.SingleAsync();
        entity.PartProcessCharacteristicId.Should().Be(101);
        entity.PointScope.Should().Be("VariableMeasurement");
        entity.VariableMeasurementId.Should().Be(measurement.Id);
        entity.Remark.Should().Be("測試備註：首件觀察");
        entity.IsActive.Should().BeTrue();
        (await db.SpcPointExclusions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpsertPointRemark_WhenSamePointExists_UpdatesExistingRow()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        db.SpcPointRemarks.Add(new SpcPointRemark
        {
            PartProcessCharacteristicId = measurement.PartProcessCharacteristicId,
            PointScope = "VariableMeasurement",
            VariableMeasurementId = measurement.Id,
            Remark = "測試備註：原始",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);
        var request = new UpsertSpcPointRemarkReq(
            measurement.PartProcessCharacteristicId,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "測試備註：更新後");

        var result = await controller.UpsertPointRemark(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var rows = await db.SpcPointRemarks.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].Remark.Should().Be("測試備註：更新後");
    }

    [Fact]
    public async Task UpsertPointRemark_WhenRemarkIsBlank_DeactivatesExistingRow()
    {
        await using var db = CreateDb();
        var measurement = await SeedVariableMeasurementAsync(db, ppcId: 101);
        db.SpcPointRemarks.Add(new SpcPointRemark
        {
            PartProcessCharacteristicId = measurement.PartProcessCharacteristicId,
            PointScope = "VariableMeasurement",
            VariableMeasurementId = measurement.Id,
            Remark = "測試備註：待清空",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);
        var request = new UpsertSpcPointRemarkReq(
            measurement.PartProcessCharacteristicId,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            " ");

        var clearResult = await controller.UpsertPointRemark(request, CancellationToken.None);
        var listResult = await controller.GetPointRemarks(101, null, null, null, null, CancellationToken.None);

        clearResult.Should().BeOfType<OkObjectResult>();
        (await db.SpcPointRemarks.SingleAsync()).IsActive.Should().BeFalse();
        var ok = listResult.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("\"data\":[]");
    }

    [Fact]
    public async Task UpsertPointRemark_WhenVariableMeasurementBelongsToAnotherPpc_ReturnsNotFound()
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
        var request = new UpsertSpcPointRemarkReq(
            202,
            "VariableMeasurement",
            measurement.Id,
            null,
            null,
            null,
            "測試備註：跨項目");

        var result = await controller.UpsertPointRemark(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        (await db.SpcPointRemarks.CountAsync()).Should().Be(0);
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
                    [new Claim(ClaimTypes.Name, "測試人員"), new Claim(ClaimTypes.Role, "Editor")],
                    "TestAuth"))
            }
        };
        return controller;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"spc-point-remarks-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
