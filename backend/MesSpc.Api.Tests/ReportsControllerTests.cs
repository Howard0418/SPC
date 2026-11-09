using ClosedXML.Excel;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public sealed class ReportsControllerTests
{
    [Fact]
    public async Task ExportCpkSummary_UsesOnlyRequestedTaipeiMonth()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var part = new Part { PartNo = "P-1", PartName = "Part 1" };
        var process = new Process { ProcessCode = "PROC", ProcessName = "Process" };
        var characteristic = new QualityCharacteristic { CharacteristicCode = "Q1", CharacteristicName = "Quality" };
        db.AddRange(part, process, characteristic);
        await db.SaveChangesAsync();
        var ppc = new PartProcessCharacteristic
        {
            PartId = part.Id, ProcessId = process.Id, CharacteristicId = characteristic.Id,
            LSL = 0, USL = 30, SampleSize = 1, IsEnabled = true
        };
        db.Add(ppc);
        await db.SaveChangesAsync();
        var uploadBatchId = Guid.NewGuid();
        db.UploadBatches.Add(new UploadBatch { UploadBatchId = uploadBatchId });
        db.VariableMeasurements.AddRange(
            Measurement(new DateTime(2026, 9, 30, 15, 59, 59, DateTimeKind.Utc), 100),
            Measurement(new DateTime(2026, 9, 30, 16, 0, 0, DateTimeKind.Utc), 10),
            Measurement(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), 20),
            Measurement(new DateTime(2026, 10, 31, 16, 0, 0, DateTimeKind.Utc), 200));
        await db.SaveChangesAsync();

        var controller = new ReportsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Path = "/api/v1/reports/cpk-summary";

        var result = Assert.IsType<FileContentResult>(await controller.ExportCpkSummary("2026-10"));
        Assert.Equal("PMR_Quality_CPK_Summary_202610.xlsx", result.FileDownloadName);
        using var workbook = new XLWorkbook(new MemoryStream(result.FileContents));
        Assert.Equal("2026-10", workbook.Worksheet(1).Cell("C4").GetString());
        Assert.Equal(2, workbook.Worksheet(1).Cell("C8").GetValue<int>());
        Assert.Equal(15, workbook.Worksheet(2).Cell(2, 9).GetValue<double>());

        VariableMeasurement Measurement(DateTime measuredAt, double value) => new()
        {
            UploadBatchId = uploadBatchId,
            PartId = part.Id,
            ProcessId = process.Id,
            CharacteristicId = characteristic.Id,
            PartProcessCharacteristicId = ppc.Id,
            SampleNo = 1,
            MeasuredAt = measuredAt,
            MeasuredValue = value
        };
    }
}
