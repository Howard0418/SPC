using Moq;
using ClosedXML.Excel;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Localization;
using MesSpc.Api.SpcEngine.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ReportLocalizationTests
{
    private static string T(string key,string locale)=>ApiUiMessageCatalog.Shared.Format("spc.api."+key,locale);
    private const string Raw="原名 <b>@:key {name}</b>";

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public void OverviewWorkbookChangesOnlyDisplayAndPreservesTypesDataAndStyles(string locale)
    {
        var row=new ChartSummaryDto {PartProcessCharacteristicId=7,ControlCategory=Raw,GroupType="TREND_CHART",ChartKind="趨勢圖",ChartKindMessage=new UiDisplayMessage("spc.api.reportTrendChart"),
            LineOrProcessName=Raw,SlotName="S0",ChartName="=1+1",ChartType=Raw,Usl=.4,Lsl=.2,Ucl=.38,Lcl=.22,
            LimitCalculationMethod="自訂輸入 (Manual)",LimitCalculationMethodMessage=new UiDisplayMessage("spc.api.reportManualLimits"),
            TotalCount=0,OosCount=0,PreviousMonthOosCount=0,OosPercentage=0,OocCount=0,Ca=0,Pp=1.2,Ppk=1.1,ResponsibleUser=Raw,Remarks=Raw};
        var start=new DateTime(2026,10,1,0,0,0,DateTimeKind.Utc);var end=start.AddMonths(1);
        var bytes=SpcOverviewReportService.BuildWorkbook([row],start,end,locale);
        using var workbook=new XLWorkbook(new MemoryStream(bytes));var sheet=workbook.Worksheet("未分類");
        Assert.Equal(4,workbook.Worksheets.Count);Assert.Equal(T("reportOverviewTitle",locale),sheet.Cell(1,1).GetString());
        Assert.Equal(T("reportControlCategory",locale),sheet.Cell(3,2).GetString());Assert.Equal("USL",sheet.Cell(3,8).GetString());
        Assert.Equal(7,sheet.Cell(4,1).GetValue<int>());Assert.Equal(Raw,sheet.Cell(4,2).GetString());Assert.Equal("S0",sheet.Cell(4,5).GetString());
        Assert.Equal(T("reportTrendChart",locale),sheet.Cell(4,3).GetString());Assert.Equal(T("reportManualLimits",locale),sheet.Cell(4,12).GetString());
        Assert.Equal("=1+1",sheet.Cell(4,6).GetString());Assert.Equal("",sheet.Cell(4,6).FormulaA1);
        Assert.Equal(.4,sheet.Cell(4,8).GetValue<double>());Assert.Equal(.2,sheet.Cell(4,9).GetValue<double>());Assert.Equal(0,sheet.Cell(4,13).GetValue<int>());
        Assert.Equal(XLDataType.Number,sheet.Cell(4,13).DataType);Assert.Equal(XLDataType.Text,sheet.Cell(4,6).DataType);
        Assert.Equal(Raw,sheet.Cell(4,24).GetString());Assert.Equal(Raw,sheet.Cell(4,25).GetString());Assert.Equal(3,sheet.SheetView.SplitRow);
        Assert.True(sheet.Cell(3,1).Style.Font.Bold);Assert.Equal(XLColor.Navy,sheet.Cell(3,1).Style.Fill.BackgroundColor);
        Assert.Equal($"{start:yyyy-MM-dd HH:mm} ~ {end:yyyy-MM-dd HH:mm}",sheet.Cell(2,2).GetString());
        Assert.Equal("趨勢圖",row.ChartKind);Assert.Equal("自訂輸入 (Manual)",row.LimitCalculationMethod);
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public async Task CpkExportRetainsOriginalScopeNumbersThresholdColorsAndFileName(string locale)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Processes.Add(new Process {Id=1,ProcessCode="P0",ProcessName=Raw});
        db.QualityCharacteristics.Add(new QualityCharacteristic {Id=1,CharacteristicCode="C0",CharacteristicName=Raw,CharacteristicNameEn="English <b>@:key {name}</b>"});
        var sets=new[]{Array.Empty<double>(),new[]{1d},new[]{0d,1d,2d},new[]{0d,1d,2d},new[]{0d,1d,2d},new[]{1d,1d,1d}};
        for(var index=0;index<sets.Length;index++) {
            var id=index+1;db.Parts.Add(new Part {Id=id,PartNo="RAW"+id,PartName=Raw});
            db.PartProcessCharacteristics.Add(new PartProcessCharacteristic {Id=id,PartId=id,ProcessId=1,CharacteristicId=1,IsEnabled=true,SampleSize=3,
                USL=index==0?null:index==3?4:index==4?3.9:5,LSL=index==0?null:index==3?-2:index==4?-1.9:-3});
            foreach(var value in sets[index])db.VariableMeasurements.Add(new VariableMeasurement {PartProcessCharacteristicId=id,MeasuredValue=value,MeasuredAt=new DateTime(2020,1,1)});
        }
        await db.SaveChangesAsync();var controller=new ReportsController(db) {ControllerContext=Context(locale)};
        var result=Assert.IsType<FileContentResult>(await controller.ExportCpkSummary("2026-10"));
        Assert.Equal("PMR_Quality_CPK_Summary_202610.xlsx",result.FileDownloadName);
        using var workbook=new XLWorkbook(new MemoryStream(result.FileContents));Assert.Equal(2,workbook.Worksheets.Count);
        var executive=workbook.Worksheets.Worksheet(1);var cpk=workbook.Worksheets.Worksheet(2);
        Assert.Equal(T("reportExecutiveSheet",locale),executive.Name);Assert.Equal(T("reportCpkSheet",locale),cpk.Name);
        Assert.Equal("2026-10",executive.Cell("C4").GetString());Assert.Equal(13,executive.Cell("C8").GetValue<int>());
        Assert.Equal("100%",executive.Cell("C11").GetString()); // Characterizes the existing all-data scope; month filtering is a separate task.
        var keys=new[]{"reportUntested","reportSingleSample","reportGradeA","reportGradeB","reportGradeC","reportGradeC"};
        var cpks=new[]{0d,0d,1.33,1d,.97,0d};
        for(var index=0;index<sets.Length;index++) {
            var row=cpk.RowsUsed().Single(row=>row.Cell(2).GetString()=="RAW"+(index+1));
            Assert.Equal(Raw,row.Cell(3).GetString());Assert.Equal(Raw,row.Cell(4).GetString());Assert.Equal(locale=="en-US"?"English <b>@:key {name}</b>":Raw,row.Cell(5).GetString());
            Assert.Equal(3,row.Cell(6).GetValue<int>());Assert.Equal(cpks[index],row.Cell(12).GetValue<double>());Assert.Equal(T(keys[index],locale),row.Cell(13).GetString());
            if(index>0)Assert.Equal(XLColor.FromHtml(index==2?"#D4EDDA":index==3?"#FFF3CD":"#F8D7DA"),row.Cell(13).Style.Fill.BackgroundColor);
            else Assert.Equal(T("reportNone",locale),row.Cell(7).GetString());
        }
        Assert.Equal(13,await db.VariableMeasurements.CountAsync());Assert.Empty(await db.SpcCalculationResults.ToListAsync());
    }

    [Fact]
    public void DisplayMetadataUsesStableCodesAndInvalidMetadataFallsBackLiterally()
    {
        Assert.Equal("spc.api.reportMovingRange",SpcReportPresentation.CalculationMethod("MR_METHOD","XBAR_R",false).MessageKey);
        Assert.Equal("spc.api.reportSampleStdDev",SpcReportPresentation.CalculationMethod("SAMPLE_STD_DEV","I_MR",false).MessageKey);
        Assert.Equal("spc.api.reportManualLimits",SpcReportPresentation.CalculationMethod("MR_METHOD","I_MR",true).MessageKey);
        Assert.Equal(Raw,SpcReportPresentation.Display(new UiDisplayMessage("common.actions.refresh"),Raw,"en-US"));
        Assert.Equal(Raw,SpcReportPresentation.Display(new UiDisplayMessage("spc.api.reportTrendChart",new Dictionary<string,string>{{"extra","bad"}}),Raw,"en-US"));
    }

    [Theory]
    [InlineData("en-US","en-US")][InlineData("zh-CN","zh-CN")][InlineData("invalid","zh-TW")]
    public async Task PopupQueryHasOnlyCanonicalDisplayPrecedence(string query,string expected)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller=new ReportsController(db) {ControllerContext=Context("en-US")};
        var response=Assert.IsType<FileContentResult>(await controller.ExportCpkSummary("2026-10",query));
        using var workbook=new XLWorkbook(new MemoryStream(response.FileContents));
        Assert.Equal(T("reportExecutiveSheet",expected),workbook.Worksheets.Worksheet(1).Name);
        Assert.Equal("PMR_Quality_CPK_Summary_202610.xlsx",response.FileDownloadName);
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public void ReportMailEscapesOnlyHtmlAndPreservesBackgroundDefaultAndLegacyNames(string locale)
    {
        var date=new DateTime(2026,10,26,8,0,0);var range="2026-10-01 ~ 2026-10-25";
        var original=System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            var manual=SpcReportPresentation.ManualMail(Raw,true,date,range,locale);
            var scheduled=SpcReportPresentation.ScheduledMail(Raw,false,date,locale);
            Assert.Contains("&lt;b&gt;@:key {name}&lt;/b&gt;",manual.HtmlBody);Assert.DoesNotContain("<b>",manual.HtmlBody);
            Assert.Contains(range,manual.HtmlBody);Assert.Contains("2026-10-26",scheduled.Subject);
            Assert.Equal("【SPC 項目管制總覽週報測試寄送】2026-10-26 08:00",manual.LegacyFileSubject);
            Assert.Equal("【SPC 項目管制總覽月報】2026-10-26",scheduled.LegacyFileSubject);
            Assert.Equal("【SPC 項目管制總覽月報】2026-10-26",SpcReportPresentation.ScheduledMail(Raw,false,date).Subject);
            Assert.Equal("fr-FR",System.Globalization.CultureInfo.CurrentCulture.Name);
        } finally {System.Globalization.CultureInfo.CurrentCulture=original;}
    }

    [Theory]
    [InlineData("zh-TW")][InlineData("zh-CN")][InlineData("en-US")]
    public async Task ManualReportKeepsOriginalResponseAttachmentRecipientsAndAddsKnownFailureMetadata(string locale)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Operators.Add(new Operator {Id=1,OperatorCode="001",OperatorName=Raw,Email="fixture@example.invalid",IsActive=true});await db.SaveChangesAsync();
        var builder=new Moq.Mock<ISpcOverviewReportBuilder>(Moq.MockBehavior.Strict);var bytes=SpcOverviewReportService.BuildWorkbook([new ChartSummaryDto {ChartName="比重",CharacteristicName="比重",CharacteristicNameEn="Proportion",CharacteristicCode="D0"}],new(2026,10,1),new(2026,11,1),locale);
        builder.Setup(x=>x.BuildExcelAsync(Moq.It.IsAny<DateTime>(),Moq.It.IsAny<DateTime>(),Moq.It.IsAny<CancellationToken>(),locale)).ReturnsAsync(bytes);
        var sender=new Moq.Mock<IEmailNotificationService>(Moq.MockBehavior.Strict);
        sender.Setup(x=>x.SendReportEmailAsync("fixture@example.invalid",Raw,Moq.It.IsAny<string>(),Moq.It.IsAny<string>(),bytes,Moq.It.IsAny<string>(),locale)).ReturnsAsync(false);
        var controller=new SpcReportSchedulesController(db,builder.Object,sender.Object) {ControllerContext=Context(locale)};
        var response=Assert.IsType<OkObjectResult>(await controller.SendNow(new("weekly",[1]),default));
        var json=System.Text.Json.JsonSerializer.SerializeToElement(response.Value,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal("週報",json.GetProperty("displayType").GetString());Assert.StartsWith("SPC_Item_Overview_週報_",json.GetProperty("fileName").GetString());
        Assert.Equal(0,json.GetProperty("success").GetInt32());var recipient=json.GetProperty("recipients")[0];
        Assert.Equal("寄信服務回傳失敗。",recipient.GetProperty("errorMessage").GetString());Assert.Equal("spc.api.mailSmtpFailed",recipient.GetProperty("displayMessage").GetProperty("messageKey").GetString());
        using var attachment=new XLWorkbook(new MemoryStream(bytes));Assert.Equal(locale=="en-US"?"Proportion":"比重",attachment.Worksheet("未分類").Cell(4,6).GetString());
        builder.VerifyAll();sender.VerifyAll();Assert.Empty(await db.SpcReportSchedules.ToListAsync());
    }

    private static ControllerContext Context(string locale) {
        var http=new DefaultHttpContext();http.Request.Path="/api/v1/reports/cpk-summary";http.Request.Headers[ApiUiMessageCatalog.LocaleHeader]=locale;
        http.Request.Headers.AcceptLanguage="de-DE";return new ControllerContext {HttpContext=http};
    }
}
