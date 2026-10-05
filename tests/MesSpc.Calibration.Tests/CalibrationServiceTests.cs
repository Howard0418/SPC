using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Calibration.Tests;

public class CalibrationServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AppDbContext db;
    private readonly InstrumentCalibrationService service;
    public CalibrationServiceTests()
    {
        connection.Open();
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Operators.Add(new Operator { Id = 1, OperatorCode = "QA", OperatorName = "QA", Email = "qa@example.invalid", IsActive = true });
        db.SaveChanges();
        service = new(db, TimeProvider.System);
    }
    private static InstrumentInput Input() => new("I-01", "量規", "品保", 1, 12, null, new(2026,10,1), "Active", [1], true, null, "建立");
    [Fact]
    public async Task DetailsPersistClearAndAuditWithoutChangingDates()
    {
        var input = Input() with { MeasurementSpecification=" 0~150 mm ", Precision="0.01 mm", Remarks="第一行\n第二行", CalibrationStandard="SOP-01", AcceptanceCriteria="±0.02 mm" };
        var i = await service.SaveInstrumentAsync(null, input, "qa", default);
        Assert.Equal("0~150 mm",i.MeasurementSpecification); Assert.Equal("0.01 mm",i.Precision);
        Assert.Equal("第一行\n第二行",i.Remarks); Assert.Equal("SOP-01",i.CalibrationStandard); Assert.Equal("±0.02 mm",i.AcceptanceCriteria);
        await Assert.ThrowsAsync<CalibrationException>(()=>service.SaveInstrumentAsync(i.Id,input with { Version=i.Version, Precision=new string('x',2001) },"qa",default));
        var due=i.NextCalibrationDate;
        i=await service.SaveInstrumentAsync(i.Id,input with { Version=i.Version,Remarks=" " },"qa",default);
        Assert.Null(i.Remarks); Assert.Equal(due,i.NextCalibrationDate);
        Assert.Empty(await db.Set<InstrumentCalibrationRecord>().ToListAsync());
        Assert.Contains(await db.Set<CalibrationAuditLog>().ToListAsync(),x=>x.Action=="InstrumentUpdated" && x.AfterJson.Contains("MeasurementSpecification"));
    }
    [Fact]
    public async Task NormalizesCodeAndRejectsDuplicatesAndStaleUpdates()
    {
        var i = await service.SaveInstrumentAsync(null, Input() with { Code = " i-01 " }, "qa", default);
        Assert.Equal("I-01", i.Code);
        await Assert.ThrowsAsync<CalibrationException>(() => service.SaveInstrumentAsync(null, Input(), "qa", default));
        await Assert.ThrowsAsync<CalibrationException>(() => service.SaveInstrumentAsync(i.Id, Input() with { Version = Guid.NewGuid() }, "qa", default));
    }
    [Fact]
    public async Task PassedUpdatesDueAndFailedPreservesItAndHistory()
    {
        var i = await service.SaveInstrumentAsync(null, Input(), "qa", default);
        var request = new CalibrationInput(new(2026,9,1), "Passed", null, "", null, Guid.NewGuid(), i.Version);
        var first = await service.RecordAsync(i.Id, request, "qa", default);
        Assert.Equal(new DateOnly(2027,9,1), i.NextCalibrationDate);
        Assert.Equal(first.Id, (await service.RecordAsync(i.Id, request, "qa", default)).Id);
        await service.RecordAsync(i.Id, new(new(2026,9,2), "Failed", null, "校正不合格", null, Guid.NewGuid(), i.Version), "qa", default);
        Assert.Equal(new DateOnly(2027,9,1), i.NextCalibrationDate);
        Assert.Equal("Failed", i.LatestResult);
        Assert.Equal(2, await db.Set<InstrumentCalibrationRecord>().CountAsync());
        Assert.Equal(3, await db.Set<CalibrationAuditLog>().CountAsync());
    }
    [Fact]
    public async Task ManualDueChangeRequiresReasonAndAllowsBlankLastDate()
    {
        var i = await service.SaveInstrumentAsync(null, Input() with { LastCalibrationDate = null }, "qa", default);
        Assert.Null(i.LastCalibrationDate);
        await Assert.ThrowsAsync<CalibrationException>(() =>
            service.SaveInstrumentAsync(i.Id, Input() with { NextCalibrationDate = new(2026, 11, 1), Version = i.Version, Reason = "" }, "qa", default));
        var updated = await service.SaveInstrumentAsync(i.Id, Input() with { NextCalibrationDate = new(2026, 11, 1), Version = i.Version, Reason = "延後經確認" }, "qa", default);
        Assert.Equal(new DateOnly(2026, 11, 1), updated.NextCalibrationDate);
        var blank = await service.SaveInstrumentAsync(null, Input() with { Code = "I-02", NextCalibrationDate = null }, "qa", default);
        Assert.Null(blank.NextCalibrationDate);
        var firstDue = await service.SaveInstrumentAsync(blank.Id, Input() with { Code = "I-02", NextCalibrationDate = new(2026, 11, 1), Version = blank.Version, Reason = "" }, "qa", default);
        Assert.Equal(new DateOnly(2026, 11, 1), firstDue.NextCalibrationDate);
    }
    [Fact]
    public async Task SavesAndClearsPlacementLocation()
    {
        var i = await service.SaveInstrumentAsync(null, Input() with { Location = " 812實驗室 " }, "qa", default);
        Assert.Equal("812實驗室", i.Location);
        await service.SaveInstrumentAsync(i.Id, Input() with { Version = i.Version, Location = "  ", Reason = "清空地點" }, "qa", default);
        Assert.Null((await db.Set<CalibrationInstrument>().SingleAsync()).Location);
    }
    [Fact]
    public async Task SavesAndClearsCalibrationMethod()
    {
        var i = await service.SaveInstrumentAsync(null, Input() with { CalibrationMethod = " 外校 " }, "qa", default);
        Assert.Equal("外校", i.CalibrationMethod);
        await service.SaveInstrumentAsync(i.Id, Input() with { Version = i.Version, CalibrationMethod = "  ", Reason = "清空校驗方式" }, "qa", default);
        Assert.Null((await db.Set<CalibrationInstrument>().SingleAsync()).CalibrationMethod);
    }
    [Fact]
    public async Task InstrumentListIsOrderedByCodeNotDueDate()
    {
        await service.SaveInstrumentAsync(null, Input() with { Code = "Z-99", NextCalibrationDate = new(2026, 10, 1) }, "qa", default);
        await service.SaveInstrumentAsync(null, Input() with { Code = "A-01", NextCalibrationDate = new(2026, 12, 1) }, "qa", default);
        var controller = new InstrumentCalibrationsController(db, service, null!, TimeProvider.System, null!, null!);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetInstruments(null, null, null, 1, 20, default));
        var json = JsonSerializer.Serialize(ok.Value);
        var codes = JsonDocument.Parse(json).RootElement.GetProperty("data").EnumerateArray().Select(x => x.GetProperty("Code").GetString()).ToArray();
        Assert.Equal(new[] { "A-01", "Z-99" }, codes);
    }
    [Fact]
    public async Task ChangingDueCancelsPendingButPreservesSent()
    {
        var i = await service.SaveInstrumentAsync(null, Input(), "qa", default);
        db.Add(new CalibrationNotification { InstrumentId=i.Id, CycleId=i.CurrentCycleId, DueDate=i.NextCalibrationDate!.Value, Stage="BEFORE-30", RecipientKey="qa", State="Pending" });
        await db.SaveChangesAsync();
        await service.SaveInstrumentAsync(i.Id, Input() with { NextCalibrationDate=new(2026,11,1), Version=i.Version, Reason="延後經確認" }, "qa", default);
        Assert.Equal("Cancelled", (await db.Set<CalibrationNotification>().SingleAsync()).State);
    }
    [Theory]
    [InlineData(".pdf", "%PDF-1.7", true)] [InlineData(".pdf", "not-pdf", false)]
    [InlineData(".exe", "%PDF-1.7", false)]
    public void CertificateValidatesContent(string extension, string bytes, bool valid)
        => Assert.Equal(valid, CalibrationCertificates.ValidContent(extension, System.Text.Encoding.ASCII.GetBytes(bytes)));
    [Fact]
    public void MeasurementWritePathDoesNotUseCalibrationTypes()
    {
        foreach (var type in new[] { typeof(MesSpc.Api.Controllers.ManualMeasurementsV1Controller), typeof(MesSpc.Api.Services.SpcService) })
        {
            var used = type.GetConstructors().SelectMany(c => c.GetParameters().Select(p => p.ParameterType))
                .Concat(type.GetMethods().SelectMany(m => m.GetParameters().Select(p => p.ParameterType)));
            Assert.DoesNotContain(used, t => t.Name.Contains("Calibration", StringComparison.Ordinal));
        }
    }
    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
