using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests;

public class SpcSummaryTests
{
    [Fact]
    public async Task Summary_Should_Normalize_Chem_Dimension_And_Exclude_Excluded_Batches()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var process = new Process { Id = 1, ProcessCode = "ETCH", ProcessName = "蝕刻製程" };
        var machine = new Machine { Id = 1, MachineCode = "LINE-A", MachineName = "A線" };
        var characteristic = new QualityCharacteristic
        {
            Id = 1,
            CharacteristicCode = "CU",
            CharacteristicName = "銅離子濃度",
            DataCategory = "Variable"
        };
        var chartType = new ControlChartType
        {
            Id = 1,
            ChartCategoryId = 1,
            ChartTypeCode = "I_MR",
            ChartTypeName = "單值-移動全距圖",
            DataCategory = "Variable"
        };
        var customGroup = new ControlChartGroup
        {
            Id = 10,
            GroupCode = "ENV",
            GroupName = "環境管制"
        };
        var customCategory = new ControlChartCategory
        {
            Id = 10,
            ChartGroupId = customGroup.Id,
            CategoryCode = "ENV_VAR",
            CategoryName = "環境計量管制"
        };
        var customChartType = new ControlChartType
        {
            Id = 2,
            ChartCategoryId = customCategory.Id,
            ChartTypeCode = "ENV_I_MR",
            ChartTypeName = "環境單值圖",
            DataCategory = "Variable"
        };
        var customCharacteristic = new QualityCharacteristic
        {
            Id = 2,
            CharacteristicCode = "TEMP",
            CharacteristicName = "環境溫度",
            DataCategory = "Variable"
        };
        var mapping = new PartProcessCharacteristic
        {
            Id = 1,
            ControlScope = "CHEMICAL",
            ProcessId = process.Id,
            MachineId = machine.Id,
            CharacteristicId = characteristic.Id,
            ChartTypeId = chartType.Id,
            USL = 10,
            LSL = 0,
            SampleSize = 1
        };
        var includedBatch = new UploadBatch { UploadBatchId = Guid.NewGuid(), IsExcluded = false };
        var excludedBatch = new UploadBatch { UploadBatchId = Guid.NewGuid(), IsExcluded = true };
        var customMapping = new PartProcessCharacteristic
        {
            Id = 2,
            ControlScope = "PROCESS",
            ProcessId = process.Id,
            MachineId = machine.Id,
            CharacteristicId = customCharacteristic.Id,
            ChartTypeId = customChartType.Id,
            USL = 30,
            LSL = 10,
            SampleSize = 1
        };

        db.AddRange(
            process,
            machine,
            characteristic,
            chartType,
            mapping,
            customGroup,
            customCategory,
            customChartType,
            customCharacteristic,
            customMapping,
            includedBatch,
            excludedBatch);
        await db.SaveChangesAsync();

        var measuredAt = DateTime.Today.AddDays(-1);
        db.VariableMeasurements.AddRange(
            NewMeasurement(1, includedBatch.UploadBatchId, 5, measuredAt, "LOT-A"),
            NewMeasurement(2, includedBatch.UploadBatchId, 6, measuredAt.AddMinutes(1), "LOT-A"),
            NewMeasurement(3, includedBatch.UploadBatchId, 12, measuredAt.AddMinutes(2), "LOT-A"),
            NewMeasurement(4, excludedBatch.UploadBatchId, 20, measuredAt.AddMinutes(3), "LOT-A"),
            NewMeasurement(5, includedBatch.UploadBatchId, 9, measuredAt.AddMinutes(4), "LOT-B"),
            NewMeasurement(6, includedBatch.UploadBatchId, 20, measuredAt.AddMinutes(5), "LOT-ENV", 2, 2));
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailNotificationService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var service = new SpcService(db, emailService.Object, config);

        var rows = await service.GetChartSummaryListAsync(
            "CHEM",
            null,
            DateTime.Today.AddDays(-7),
            DateTime.Today,
            null,
            null);

        rows.Should().ContainSingle();
        var row = rows.Single();
        row.ControlCategory.Should().Be("藥液管制");
        row.LineOrProcessName.Should().Be("蝕刻製程 / A線");
        row.ChartName.Should().Be("銅離子濃度");
        row.ChartType.Should().Be("單值-移動全距圖");
        row.OosCount.Should().Be(1);
        row.OosPercentage.Should().Be(25);
        row.Ucl.Should().NotBeNull();
        row.Lcl.Should().NotBeNull();
        row.LimitCalculationMethod.Should().Be("移動全距法");
        row.Pp.Should().NotBeNull();
        row.Ppk.Should().NotBeNull();

        var batchRows = await service.GetChartSummaryListAsync(
            "CHEMICAL",
            null,
            null,
            null,
            "LOT-B",
            null);

        batchRows.Should().ContainSingle();
        batchRows.Single().OosCount.Should().Be(0);
        batchRows.Single().OosPercentage.Should().Be(0);

        var customRows = await service.GetChartSummaryListAsync(
            "ENV",
            null,
            DateTime.Today.AddDays(-7),
            DateTime.Today);

        customRows.Should().ContainSingle();
        customRows.Single().ControlCategory.Should().Be("環境管制");
        customRows.Single().ChartName.Should().Be("環境溫度");
    }

    private static VariableMeasurement NewMeasurement(
        long id,
        Guid batchId,
        double value,
        DateTime measuredAt,
        string lotNo,
        int ppcId = 1,
        int characteristicId = 1)
    {
        return new VariableMeasurement
        {
            Id = id,
            UploadBatchId = batchId,
            PartId = 0,
            ProcessId = 1,
            MachineId = 1,
            CharacteristicId = characteristicId,
            PartProcessCharacteristicId = ppcId,
            LotNo = lotNo,
            SampleNo = (int)id,
            MeasuredValue = value,
            MeasuredAt = measuredAt
        };
    }
}
