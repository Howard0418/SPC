using System.Text.Json;
using ClosedXML.Excel;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Data.Sqlite;

namespace MesSpc.Api.Tests;

public class MonthlyDispositionTests
{
    [Fact]
    public void LongUnicodeCommentRetainsContentWithinExcelColumnLimit()
    {
        var row=new MesSpc.Api.Controllers.ChartSummaryDto {PartProcessCharacteristicId=1,GroupCode="CHEM",GroupType="CONTROL_CHART",Remarks=new string('測',600)};
        using var book=new XLWorkbook(new MemoryStream(SpcOverviewReportService.BuildWorkbook([row],new(2026,10,1),new(2026,11,1))));
        var sheet=book.Worksheet("藥液管制");
        Assert.Equal(row.Remarks,sheet.Cell(4,25).GetString());
        Assert.InRange(sheet.Column(25).Width,1,255);
        Assert.True(sheet.Cell(4,25).Style.Alignment.WrapText);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletePeriodUsesCurrentDispositionDeduplicatesSignalsAndNeverWrites(bool relational)
    {
        await using var connection=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        var options=new DbContextOptionsBuilder<AppDbContext>();
        if(relational) options.UseSqlite(connection);else options.UseInMemoryDatabase(Guid.NewGuid().ToString());
        await using var db=new AppDbContext(options.Options);
        if(relational) await db.Database.EnsureCreatedAsync();
        db.AddRange(new Process {Id=1,ProcessCode="P",ProcessName="P"},
            new QualityCharacteristic {Id=1,CharacteristicCode="C",CharacteristicName="C",DataCategory="Variable"},
            new ControlChartGroup {Id=1,GroupCode="CHEM",GroupType="CONTROL_CHART",GroupName="藥液",IsEnabled=true},
            new ControlChartType {Id=1,ChartGroupId=1,ChartTypeCode="I_MR",ChartTypeName="I",IsEnabled=true,DataCategory="Variable"},
            new PartProcessCharacteristic {Id=1,ProcessId=1,CharacteristicId=1,ChartTypeId=1,ControlScope="CHEM",DisplayMode="CONTROL_CHART",USL=1,UCL=1,LCL=0,SampleSize=1,IsEnabled=true});
        var day=new DateTime(2026,10,1);
        db.VariableMeasurements.AddRange(Enumerable.Range(1,1001).Select(i=>new VariableMeasurement {Id=i,PartProcessCharacteristicId=1,ProcessId=1,CharacteristicId=1,MeasuredAt=day.AddSeconds(i),MeasuredValue=2}));
        // More than one alert page; no event at the final point must still be pending.
        db.AlertEvents.AddRange(Enumerable.Range(1,1000).Select(i=>new AlertEvent {Id=i,VariableMeasurementId=i,Status=i%2==0?"Closed":"InProgress",OccurredAt=day}));
        await db.SaveChangesAsync();
        var spc=new SpcService(db,Mock.Of<IEmailNotificationService>(),new ConfigurationBuilder().Build());
        var row=Assert.Single(await spc.GetChartSummaryListAsync("CHEM",null,day,day,groupType:"CONTROL_CHART"));
        var json=JsonSerializer.SerializeToElement(row);
        Assert.Equal(1001,row.TotalCount);
        Assert.Equal(1001,json.GetProperty("AbnormalCount").GetInt32());
        Assert.Equal(500,json.GetProperty("HandledCount").GetInt32());
        Assert.Equal(501,json.GetProperty("UnhandledCount").GetInt32());
        db.AlertEvents.Single(a=>a.Id==1).Status="Closed";await db.SaveChangesAsync();
        row=Assert.Single(await spc.GetChartSummaryListAsync("CHEM",null,day,day,groupType:"CONTROL_CHART"));
        json=JsonSerializer.SerializeToElement(row);
        Assert.Equal(501,json.GetProperty("HandledCount").GetInt32());
        using var book=new XLWorkbook(new MemoryStream(SpcOverviewReportService.BuildWorkbook([row],day,day.AddDays(1))));
        var sheet=book.Worksheet("藥液管制");
        Assert.Equal("已處置件數",sheet.Cell(3,27).GetString());Assert.Equal("未處置件數",sheet.Cell(3,28).GetString());
        Assert.Equal(1001,sheet.Cell(4,26).GetValue<int>());Assert.Equal(501,sheet.Cell(4,27).GetValue<int>());Assert.Equal(500,sheet.Cell(4,28).GetValue<int>());
        Assert.Equal(1001,await db.VariableMeasurements.CountAsync());Assert.Equal(1000,await db.AlertEvents.CountAsync());Assert.Empty(await db.SpcCalculationResults.ToListAsync());
    }
}
