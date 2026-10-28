using ClosedXML.Excel;
using MesSpc.Api.Controllers;
using MesSpc.Api.Services;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests;

public class MonthlyCategoryWorkbookTests
{
    [Fact]
    public void EmptyReportKeepsThreeNamedSheetsAndHeaders()
    {
        using var book = Open([]);
        Assert.Equal(new[] { "製程管制", "藥液管制", "藥液趨勢" }, book.Worksheets.Select(x => x.Name));
        foreach (var sheet in book.Worksheets)
        {
            Assert.Equal("ID", sheet.Cell(3, 1).GetString());
            Assert.Equal("無資料", sheet.Cell(4, 1).GetString());
        }
    }

    [Fact]
    public void UnknownDisplayLabelsAreRetainedWithoutGuessingClassification()
    {
        using var book = Open([new() { PartProcessCharacteristicId = 17, ControlCategory = "藥液管制", GroupType = "CONTROL_CHART", Usl = 14.125 }]);
        Assert.True(book.Worksheets.Contains("未分類"));
        Assert.Equal(17, book.Worksheet("未分類").Cell(4, 1).GetValue<int>());
        Assert.Equal(14.125, book.Worksheet("未分類").Cell(4, 8).GetValue<double>());
        Assert.Equal("無資料", book.Worksheet("藥液管制").Cell(4, 1).GetString());
    }

    private static XLWorkbook Open(IEnumerable<ChartSummaryDto> rows) => new(new MemoryStream(
        SpcOverviewReportService.BuildWorkbook(rows, new(2024, 2, 1), new(2024, 3, 1))));

    [Fact]
    public void StableCodesSeparateTrendAndPreserveProductAndDeduplicate()
    {
        var rows = new[] { Row(1,"PROCESS","CONTROL_CHART"), Row(2,"CHEM","CONTROL_CHART"), Row(3,"CHEM","TREND_CHART"), Row(4,"PRODUCT","CONTROL_CHART") };
        using var book = Open(rows.Concat([rows[0]]));
        var names = new[] { "製程管制", "藥液管制", "藥液趨勢", "產品管制" };
        for (var i = 0; i < names.Length; i++)
        {
            Assert.Equal(i+1, book.Worksheet(names[i]).Cell(4,1).GetValue<int>());
            Assert.Equal(4, book.Worksheet(names[i]).LastRowUsed()!.RowNumber());
        }
    }

    [Fact]
    public void ConflictingClassificationCannotSilentlyDuplicateOrHideData() =>
        Assert.Throws<InvalidOperationException>(() => Open([Row(1,"CHEM","CONTROL_CHART"), Row(1,"CHEM","TREND_CHART")]));

    private static ChartSummaryDto Row(int id,string code,string type) => new() {PartProcessCharacteristicId=id,GroupCode=code,GroupType=type};

    [Fact]
    public async Task ReportUsesExactHalfOpenIntervalIncludingLeapDayAndExcludingNextMonth()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.AddRange(new Process {Id=1,ProcessCode="P",ProcessName="P"},new ControlChartGroup {Id=1,GroupCode="CHEM",GroupType="CONTROL_CHART",GroupName="任意名稱",IsEnabled=true},
            new ControlChartType {Id=1,ChartGroupId=1,ChartTypeCode="I_MR",ChartTypeName="I",DataCategory="Variable",IsEnabled=true},
            new QualityCharacteristic {Id=1,CharacteristicCode="C",CharacteristicName="C",DataCategory="Variable"},
            new PartProcessCharacteristic {Id=1,ProcessId=1,ControlScope="CHEM",DisplayMode="CONTROL_CHART",CharacteristicId=1,ChartTypeId=1,IsEnabled=true,SampleSize=1});
        var start = new DateTime(2024,1,31,16,0,0,DateTimeKind.Utc);
        var end = new DateTime(2024,2,29,16,0,0,DateTimeKind.Utc);
        var dates = new[] { start.AddTicks(-1), start, end.AddSeconds(-1), end };
        for (var i=0;i<dates.Length;i++) db.VariableMeasurements.Add(new VariableMeasurement {Id=i+1,PartProcessCharacteristicId=1,MeasuredAt=dates[i],MeasuredValue=i,CharacteristicId=1});
        await db.SaveChangesAsync();
        var spc = new SpcService(db,Mock.Of<IEmailNotificationService>(),new ConfigurationBuilder().Build());
        var service = new SpcOverviewReportService(db,spc);
        using var book = new XLWorkbook(new MemoryStream(await service.BuildExcelAsync(start,end)));
        Assert.Equal(2,book.Worksheet("藥液管制").Cell(4,13).GetValue<int>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.BuildExcelAsync(end,start));
        Assert.Equal(4,await db.VariableMeasurements.CountAsync());
        Assert.Empty(await db.AlertEvents.ToListAsync());
    }
}
