using System.Collections;
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests;

public class SpcPointExclusionCalculationTests
{
    [Fact]
    public async Task GetInteractiveChartAsync_WhenVariablePointExcluded_DoesNotCountPointAsOos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var process = new Process { Id = 1, ProcessCode = "QE", ProcessName = "品質工程" };
        var machine = new Machine { Id = 1, MachineCode = "QE1", MachineName = "快蝕線(QE1)" };
        var characteristic = new QualityCharacteristic
        {
            Id = 1,
            CharacteristicCode = "BITE",
            CharacteristicName = "A side 平均咬蝕量",
            DataCategory = "Variable"
        };
        var chartType = new ControlChartType
        {
            Id = 1,
            ChartTypeCode = "I_MR",
            ChartTypeName = "I-MR",
            DataCategory = "Variable"
        };
        var mapping = new PartProcessCharacteristic
        {
            Id = 101,
            ControlScope = "PROCESS",
            ProcessId = process.Id,
            MachineId = machine.Id,
            CharacteristicId = characteristic.Id,
            ChartTypeId = chartType.Id,
            USL = 10,
            LSL = 0,
            SampleSize = 1
        };
        var batch = new UploadBatch { UploadBatchId = Guid.NewGuid(), IsExcluded = false };

        db.AddRange(process, machine, characteristic, chartType, mapping, batch);
        await db.SaveChangesAsync();

        var measuredAt = new DateTime(2026, 10, 6, 8, 0, 0);
        db.VariableMeasurements.AddRange(
            NewMeasurement(1, mapping.Id, batch.UploadBatchId, 5, measuredAt),
            NewMeasurement(2, mapping.Id, batch.UploadBatchId, 20, measuredAt.AddMinutes(1)),
            NewMeasurement(3, mapping.Id, batch.UploadBatchId, 6, measuredAt.AddMinutes(2)));
        db.SpcPointExclusions.Add(new SpcPointExclusion
        {
            PartProcessCharacteristicId = mapping.Id,
            PointScope = "VariableMeasurement",
            VariableMeasurementId = 2,
            State = "ExcludedVisible",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new SpcService(db, new Mock<IEmailNotificationService>().Object, new ConfigurationBuilder().Build());

        var result = await service.GetInteractiveChartAsync(mapping.Id, null, null, null, null, null);

        result.Should().NotBeNull();
        var rawPoints = result!.RawDataPoints.Should().BeAssignableTo<List<SpcDataPoint>>().Subject;
        rawPoints.Single(x => x.VariableMeasurementId == 2).IsExcluded.Should().BeTrue();

        var chartPoints = GetPoints(result.ChartData).ToList();
        chartPoints.Should().HaveCount(3);
        GetBool(chartPoints.Single(x => GetLong(x, "variableMeasurementId") == 2), "isExcluded").Should().BeTrue();
        chartPoints.Count(x => GetBool(x, "outOfSpec")).Should().Be(0);
    }

    private static VariableMeasurement NewMeasurement(long id, int ppcId, Guid batchId, double value, DateTime measuredAt) => new()
    {
        Id = id,
        UploadBatchId = batchId,
        PartId = 0,
        ProcessId = 1,
        MachineId = 1,
        CharacteristicId = 1,
        PartProcessCharacteristicId = ppcId,
        LotNo = "LOT-A",
        SampleNo = (int)id,
        MeasuredValue = value,
        MeasuredAt = measuredAt
    };

    private static IEnumerable<object> GetPoints(object chartData)
    {
        var points = chartData.GetType().GetProperty("points")?.GetValue(chartData);
        return points.Should().BeAssignableTo<IEnumerable>().Subject.Cast<object>();
    }

    private static bool GetBool(object item, string propertyName) =>
        item.GetType().GetProperty(propertyName)!.GetValue(item).Should().BeOfType<bool>().Subject;

    private static long? GetLong(object item, string propertyName) =>
        item.GetType().GetProperty(propertyName)!.GetValue(item) as long?;
}
