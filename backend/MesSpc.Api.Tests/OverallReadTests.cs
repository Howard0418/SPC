using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MesSpc.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class OverallReadTests
{
    [Theory]
    [InlineData("2026-10-29","2026-10-28")]
    [InlineData("2026-01-01","2026-10-28")]
    [InlineData("9999-12-30","9999-12-31")]
    public async Task InvalidDatesAreRejectedBeforeReadingDatabase(string start,string end)
    {
        var controller=new OverallController(null!,NullLogger<OverallController>.Instance);
        Assert.IsType<BadRequestObjectResult>(await controller.Get(DateTime.Parse(start),DateTime.Parse(end)));
    }

    [Fact]
    public async Task FullReadIncludes1001Measurements_DefaultStillLimits1000_AndDateEndpointsAreInclusive()
    {
        // Only exercises read queries; unrelated upload FK fixture is intentionally disabled.
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new() {Id=1,ProcessCode="P",ProcessName="P"});
        db.QualityCharacteristics.Add(new() {Id=1,CharacteristicCode="C",CharacteristicName="C"});
        db.PartProcessCharacteristics.AddRange(new PartProcessCharacteristic {Id=1,ProcessId=1,CharacteristicId=1,USL=10},
            new PartProcessCharacteristic {Id=2,ProcessId=1,CharacteristicId=1},
            new PartProcessCharacteristic {Id=3,ProcessId=1,CharacteristicId=1,IsEnabled=false});
        var day = new DateTime(2026,10,28);
        db.VariableMeasurements.AddRange(Enumerable.Range(1,1001).Select(i=>new VariableMeasurement {
            Id=i,PartProcessCharacteristicId=1,ProcessId=1,CharacteristicId=1,
            MeasuredAt=i==1001?day.AddDays(1).AddTicks(-1):day,MeasuredValue=i==1001?11:5 }));
        db.VariableMeasurements.Add(new() {Id=1002,PartProcessCharacteristicId=1,ProcessId=1,CharacteristicId=1,
            MeasuredAt=day.AddDays(1),MeasuredValue=11});
        await db.SaveChangesAsync();
        var spc=new SpcService(db,null!,new ConfigurationBuilder().Build());
        var legacy=await spc.GetInteractiveChartAsync(1,null,day,day);
        Assert.Equal(1000,Assert.IsType<List<SpcDataPoint>>(legacy!.RawDataPoints).Count);
        var full=await spc.GetInteractiveChartAsync(1,null,day,day,includeAllPoints:true);
        Assert.Equal(1001,Assert.IsType<List<SpcDataPoint>>(full!.RawDataPoints).Count);
        db.ControlChartTypes.Add(new() {Id=1,ChartGroupId=1,ChartTypeCode="C",ChartTypeName="C",DataCategory="Attribute"});
        db.PartProcessCharacteristics.Add(new() {Id=4,ProcessId=1,CharacteristicId=1,ChartTypeId=1,UCL=10,LCL=0});
        db.AttributeMeasurements.AddRange(Enumerable.Range(1,1001).Select(i=>new AttributeMeasurement {
            Id=i,PartProcessCharacteristicId=4,ProcessId=1,CharacteristicId=1,MeasuredAt=day.AddSeconds(i),DefectCount=i==1001?30:5 }));
        await db.SaveChangesAsync();
        var legacyAttribute=await spc.GetInteractiveChartAsync(4,null,day,day);
        Assert.Equal(1000,Assert.IsType<List<AttributeDataPoint>>(legacyAttribute!.RawDataPoints).Count);
        var fullAttribute=await spc.GetInteractiveChartAsync(4,null,day,day,includeAllPoints:true);
        Assert.Equal(1001,Assert.IsType<List<AttributeDataPoint>>(fullAttribute!.RawDataPoints).Count);
        var summary=await new OverallStatisticsService(db,spc).GetAsync(day,day);
        Assert.Equal(3,summary.ControlItemCount); Assert.Equal(1,summary.DisabledItemCount);
        Assert.Equal(1,summary.OosCount); Assert.Equal(1,summary.OocCount); Assert.Equal(2,summary.UnhandledCount);
        Assert.Equal(1002,await db.VariableMeasurements.CountAsync());
        Assert.Empty(await db.AlertEvents.ToListAsync());
    }
}
