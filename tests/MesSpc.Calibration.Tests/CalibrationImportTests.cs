using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Calibration.Tests;

public class CalibrationImportTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AppDbContext db;
    private readonly CalibrationImportService service;
    private static readonly TimeProvider Clock = new FixedClock();
    private sealed class FixedClock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero); }

    public CalibrationImportTests()
    {
        connection.Open();
        db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Operators.Add(new Operator { Id=1, Username="qa", OperatorCode="QA", OperatorName="QA", Role="Editor", IsActive=true });
        db.SaveChanges();
        service = new(db, new InstrumentCalibrationService(db, Clock), Clock, NullLogger<CalibrationImportService>.Instance);
    }

    // Minimal OOXML fixtures preserve raw cached formulas; no Excel calculation engine is involved.
    internal static byte[] Workbook(string rows, bool date1904 = false, string? headers = null)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Entry(string name, string text) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8); writer.Write(text); }
            Entry("xl/workbook.xml", $"<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><workbookPr date1904=\"{(date1904 ? 1 : 0)}\"/><sheets><sheet name=\"First\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"Ignored\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId2\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Entry("xl/worksheets/sheet1.xml", "INVALID IGNORED SHEET");
            Entry("xl/worksheets/sheet2.xml", "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                (headers ?? $"<row r=\"4\">{Text("Z4", "校驗日期")}{Text("AA4", "下次\n校驗日期")}</row><row r=\"5\">{Text("A5", "儀器設備\n編號")}{Text("B5", "儀器設備名稱")}{Text("H5", "放置地點")}{Text("I5", "校驗週期")}{Text("J5", "校驗方式")}{Text("F5","量測規格")}{Text("G5","精度")}{Text("AB5","備註")}{Text("AC5","校驗規範")}{Text("AD5","允收標準")}</row>") + rows + "</sheetData></worksheet>");
        }
        return output.ToArray();
    }
    internal static string Text(string cell, string value) => $"<c r=\"{cell}\" t=\"inlineStr\"><is><t>{System.Security.SecurityElement.Escape(value)}</t></is></c>";
    internal static string Row(int row, string code="A-01", string cycle="1 次/年", string last="2026/3/17", string due="2027/3/16", string location="發泡實驗室", string method="外校")
        => $"<row r=\"{row}\">{Text($"A{row}", code)}{Text($"B{row}", "量規")}{Text($"H{row}", location)}{Text($"I{row}", cycle)}{Text($"J{row}", method)}{Text($"Z{row}", last)}{Text($"AA{row}", due)}</row>";
    private static CalibrationImportOptions Options(string hash, params int[] rows) => new(hash, rows, "品保", 1, "Active", true);

    [Fact]
    public void DetailsValidateHeadersAndCachedFormulaValues()
    {
        var cached="<c r=\"F6\" t=\"str\"><f>CONCAT(0,150)</f><v>0~150 mm</v></c>";
        var row=CalibrationImportParser.Parse(Workbook(Row(6).Replace("</row>",cached+"</row>")),Clock).Rows[0];
        Assert.Equal("0~150 mm",row.MeasurementSpecification);Assert.Contains(row.Warnings,x=>x.Contains("量測規格"));
        var missing="<c r=\"G6\"><f>1/0</f></c>";
        Assert.Contains(CalibrationImportParser.Parse(Workbook(Row(6).Replace("</row>",missing+"</row>")),Clock).Rows[0].Errors,x=>x.Contains("精度"));
        var headers="<row r=\"4\">"+Text("Z4","校驗日期")+Text("AA4","下次校驗日期")+"</row><row r=\"5\">"+Text("A5","儀器設備編號")+Text("B5","儀器設備名稱")+Text("H5","放置地點")+Text("I5","校驗週期")+Text("J5","校驗方式")+Text("F5","錯誤表頭")+"</row>";
        Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse(Workbook(Row(6),headers:headers),Clock));
    }
    [Fact]
    public async Task BackfillOnlyMatchesAndFillsBlanksWithAuditAndReplaySafety()
    {
        var item=new CalibrationInstrument { Code="A-01",Name="量規",Department="QA",CustodianOperatorId=1,Precision="原精度",NextCalibrationDate=new(2027,3,16) };
        db.Add(item);await db.SaveChangesAsync();
        var bytes=Workbook(Row(6).Replace("</row>",Text("F6","0~150 mm")+Text("G6","新精度")+Text("AB6","備註內容")+"</row>"));
        var parsed=CalibrationImportParser.Parse(bytes,Clock);
        var fill=new CalibrationDetailsBackfill(db,new InstrumentCalibrationService(db,Clock));
        Assert.Equal(1,await fill.ApplyAsync(parsed,false,default));
        Assert.Null(item.MeasurementSpecification); Assert.Empty(await db.Set<CalibrationAuditLog>().ToListAsync());
        Assert.Equal(1,await fill.ApplyAsync(parsed,true,default));
        Assert.Equal("0~150 mm",item.MeasurementSpecification); Assert.Equal("原精度",item.Precision);
        Assert.Equal(new DateOnly(2027,3,16),item.NextCalibrationDate);
        Assert.Equal(0,await fill.ApplyAsync(parsed,true,default));
        Assert.Single(await db.Set<CalibrationAuditLog>().ToListAsync());
        Assert.Empty(await db.Set<CalibrationNotification>().ToListAsync());
        item.MeasurementSpecification=null;item.Name="不同儀器";await db.SaveChangesAsync();
        Assert.Equal(0,await fill.ApplyAsync(parsed,true,default));
    }
    [Fact]
    public async Task DetailsParseImportAndRejectLongValues()
    {
        var extra=Text("F6","0~150 mm")+Text("G6","0.01 mm")+Text("AB6","第一行\n第二行")+Text("AC6","SOP-01")+Text("AD6","±0.02 mm");
        var data=Workbook(Row(6).Replace("</row>",extra+"</row>"));
        var parsed=CalibrationImportParser.Parse(data,Clock);
        Assert.Equal("0~150 mm",parsed.Rows[0].MeasurementSpecification);
        Assert.Equal("0.01 mm",parsed.Rows[0].Precision); Assert.Equal("第一行\n第二行",parsed.Rows[0].Remarks);
        Assert.Equal("SOP-01",parsed.Rows[0].CalibrationStandard); Assert.Equal("±0.02 mm",parsed.Rows[0].AcceptanceCriteria);
        await service.CommitAsync(data,"details.xlsx",Options(parsed.Hash,6),"qa",default);
        var item=await db.Set<CalibrationInstrument>().SingleAsync();
        Assert.Equal("0~150 mm",item.MeasurementSpecification);Assert.Equal("±0.02 mm",item.AcceptanceCriteria);
        var tooLong=Workbook(Row(6).Replace("</row>",Text("F6",new string('x',2001))+"</row>"));
        Assert.Contains(CalibrationImportParser.Parse(tooLong,Clock).Rows[0].Errors,x=>x.Contains("2000"));
    }
    [Fact]
    public void ReadsFirstByRelationshipAndPreservesDatesAndSkipsFooter()
    {
        var data = Workbook(Row(6, " a-01 ") + "<row r=\"7\"/>" + Row(8,"A-02","1 次/2年") + $"<row r=\"9\">{Text("A9","制定日期: ")}{Text("B9","2026/6/1")}</row>");
        var result = CalibrationImportParser.Parse(data, Clock);
        Assert.Equal("First", result.SheetName);
        Assert.Equal(new[] { 6, 8 }, result.Rows.Select(x => x.RowNumber));
        Assert.Equal("A-01", result.Rows[0].Code);
        Assert.Equal("發泡實驗室", result.Rows[0].Location);
        Assert.Equal("外校", result.Rows[0].CalibrationMethod);
        Assert.Equal(24, result.Rows[1].CycleMonths);
        Assert.Equal(new DateOnly(2027,3,16), result.Rows[0].NextCalibrationDate);
        Assert.Empty(result.Rows[0].Errors);
        Assert.NotEmpty(result.Rows[0].Warnings);
    }

    [Theory]
    [InlineData("1次/年",12)] [InlineData("1 次/2年",24)] [InlineData("1 次/5年",60)] [InlineData("1次/6月",6)]
    public void ParsesExplicitCycles(string text, int months)
        => Assert.Equal(months, CalibrationImportParser.Parse(Workbook(Row(6,cycle:text)),Clock).Rows[0].CycleMonths);

    [Theory]
    [InlineData("1次/年","2026/9/12","2027/3/16")]
    [InlineData("1次/年","2026/3/17","2026/3/17")]
    [InlineData("1次/年","2026/2/30","2027/3/16")]
    [InlineData("1次/年","03/04/2026","2027/3/16")]
    public void InvalidRowsHaveActionableErrors(string cycle,string last,string due)
        => Assert.NotEmpty(CalibrationImportParser.Parse(Workbook(Row(6,cycle:cycle,last:last,due:due)),Clock).Rows[0].Errors);

    [Fact]
    public void DeferredDatesAndUnknownCycleImportAsBlankDue()
    {
        var pending = CalibrationImportParser.Parse(Workbook(Row(6, last:"未校", due:"預計:2026/10/15")), Clock).Rows[0];
        Assert.Empty(pending.Errors);
        Assert.Null(pending.LastCalibrationDate);
        Assert.Null(pending.NextCalibrationDate);
        Assert.Contains(pending.Warnings, x => x.Contains("自行建立日期"));
        var exemption = CalibrationImportParser.Parse(Workbook(Row(6, cycle:"--", last:"-", due:"-")), Clock).Rows[0];
        Assert.Empty(exemption.Errors);
        Assert.Equal(12, exemption.CycleMonths);
        Assert.Null(exemption.NextCalibrationDate);
        Assert.Contains(exemption.Warnings, x => x.Contains("12 月"));
    }

    [Fact]
    public void MissingNamesCodesAndDuplicateCodesCannotBeSelected()
    {
        var rows=CalibrationImportParser.Parse(Workbook(Row(6)+Row(7," a-01 ")+Row(8,"")+Row(9,"A-09").Replace(Text("B9","量規"),"")),Clock).Rows;
        Assert.All(rows, row=>Assert.NotEmpty(row.Errors));
    }

    [Fact]
    public void ReadsCachedFormulaWithoutRecalculationAndRejectsMissingCache()
    {
        var row=Row(6).Replace(Text("AA6","2027/3/16"),"<c r=\"AA6\"><f>Z6+364</f><v>46462</v></c>");
        var result=CalibrationImportParser.Parse(Workbook(row),Clock).Rows[0];
        Assert.Equal(new DateOnly(2027,3,16),result.NextCalibrationDate);
        Assert.Contains(result.Warnings,x=>x.Contains("公式"));
        Assert.NotEmpty(CalibrationImportParser.Parse(Workbook(row.Replace("<v>46462</v>","")),Clock).Rows[0].Errors);
    }

    [Theory]
    [InlineData(false, 46098, "2026-03-17")]
    [InlineData(true, 44636, "2026-03-17")]
    public void SupportsExcelDateSystems(bool system1904,int serial,string expected)
    {
        var row=Row(6).Replace(Text("Z6","2026/3/17"),$"<c r=\"Z6\"><v>{serial}</v></c>");
        Assert.Equal(DateOnly.Parse(expected),CalibrationImportParser.Parse(Workbook(row,system1904),Clock).Rows[0].LastCalibrationDate);
    }

    [Fact]
    public void RejectsInvalidArchiveAndTemplate()
    {
        Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse([1,2,3],Clock));
        Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse(Workbook(Row(6),headers:""),Clock));
        var missingLocationHeader = "<row r=\"4\">" + Text("Z4","校驗日期") + Text("AA4","下次校驗日期") + "</row><row r=\"5\">" + Text("A5","儀器設備編號") + Text("B5","儀器設備名稱") + Text("I5","校驗週期") + "</row>";
        Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse(Workbook(Row(6),headers:missingLocationHeader),Clock));
        var missingMethodHeader = "<row r=\"4\">" + Text("Z4","校驗日期") + Text("AA4","下次校驗日期") + "</row><row r=\"5\">" + Text("A5","儀器設備編號") + Text("B5","儀器設備名稱") + Text("H5","放置地點") + Text("I5","校驗週期") + "</row>";
        Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse(Workbook(Row(6),headers:missingMethodHeader),Clock));
    }

    [Fact]
    public void ReadsPlacementLocationAndRejectsOverlongValue()
    {
        var blank = CalibrationImportParser.Parse(Workbook(Row(6, location:"")), Clock).Rows[0];
        Assert.Equal("", blank.Location);
        Assert.DoesNotContain(blank.Errors, x => x.Contains("放置地點"));
        var longText = new string('實', 101);
        var over = CalibrationImportParser.Parse(Workbook(Row(6, location:longText)), Clock).Rows[0];
        Assert.Contains(over.Errors, x => x.Contains("放置地點"));
    }

    [Fact]
    public void ReadsCalibrationMethodAndRejectsOverlongValue()
    {
        var blank = CalibrationImportParser.Parse(Workbook(Row(6, method:"")), Clock).Rows[0];
        Assert.Equal("", blank.CalibrationMethod);
        Assert.DoesNotContain(blank.Errors, x => x.Contains("校驗方式"));
        var longText = new string('外', 101);
        var over = CalibrationImportParser.Parse(Workbook(Row(6, method:longText)), Clock).Rows[0];
        Assert.Contains(over.Errors, x => x.Contains("校驗方式"));
        var exemption = CalibrationImportParser.Parse(Workbook(Row(6, cycle:"1 次/年", method:"免校")), Clock).Rows[0];
        Assert.Equal("免校", exemption.CalibrationMethod);
        Assert.Equal(12, exemption.CycleMonths);
        Assert.Empty(exemption.Errors);
    }

    [Fact]
    public async Task SelectedOnlyAndReplaySkipsWithoutHistoryOrNotifications()
    {
        var file=Workbook(Row(6)+Row(7,"A-02"));
        var preview=await service.PreviewAsync(file,default);
        var first=await service.CommitAsync(file,"instruments.xlsx",Options(preview.Hash,6),"qa",default);
        Assert.Equal(1,first.Added); Assert.Equal(0,first.Failed);
        var original=await db.Set<CalibrationInstrument>().SingleAsync();
        Assert.Equal("A-01",original.Code); Assert.Equal("發泡實驗室", original.Location); Assert.Equal("外校", original.CalibrationMethod); Assert.Equal("品保", original.Department); Assert.Null(original.LatestResult);
        var replay=await service.CommitAsync(file,"instruments.xlsx",Options(preview.Hash,6),"qa",default);
        Assert.Equal(1,replay.Skipped); Assert.Equal(0,replay.Added);
        Assert.Single(await db.Set<CalibrationInstrument>().ToListAsync());
        Assert.Empty(await db.Set<InstrumentCalibrationRecord>().ToListAsync());
        Assert.Empty(await db.Set<CalibrationNotification>().ToListAsync());
        Assert.Contains(await db.Set<CalibrationAuditLog>().ToListAsync(),x=>x.AfterJson.Contains("instruments.xlsx") && x.Actor=="qa");
        Assert.Equal("Existing",(await service.PreviewAsync(file,default)).Rows[0].Status);
    }

    [Fact]
    public async Task CommitDeferredDatesLeavesDueBlankForLaterEdit()
    {
        var file=Workbook(Row(6, last:"未校", due:"預計:2026/10/15"));
        var preview=await service.PreviewAsync(file,default);
        Assert.Equal(1,(await service.CommitAsync(file,"x.xlsx",Options(preview.Hash,6),"qa",default)).Added);
        var row=await db.Set<CalibrationInstrument>().SingleAsync();
        Assert.Null(row.LastCalibrationDate);
        Assert.Null(row.NextCalibrationDate);
        Assert.Equal(12, row.CycleMonths);
    }

    [Fact]
    public async Task CommitRevalidatesHashSelectionAndInactiveCustodian()
    {
        var file=Workbook(Row(6)); var preview=await service.PreviewAsync(file,default);
        await Assert.ThrowsAsync<CalibrationException>(()=>service.CommitAsync(file,"x.xlsx",Options("wrong",6),"qa",default));
        await Assert.ThrowsAsync<CalibrationException>(()=>service.CommitAsync(file,"x.xlsx",Options(preview.Hash,9),"qa",default));
        db.Operators.Single().IsActive=false; await db.SaveChangesAsync();
        var result=await service.CommitAsync(file,"x.xlsx",Options(preview.Hash,6),"qa",default);
        Assert.Equal(1,result.Failed); Assert.Empty(await db.Set<CalibrationInstrument>().ToListAsync());
    }

    [Fact]
    public async Task FailureOnSecondInsertRollsBackFirstAndWritesFailureAudit()
    {
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailImport BEFORE INSERT ON CalibrationInstruments WHEN NEW.Code = 'A-02' BEGIN SELECT RAISE(ABORT, 'injected failure'); END;");
        var file=Workbook(Row(6)+Row(7,"A-02")); var preview=await service.PreviewAsync(file,default);
        var result=await service.CommitAsync(file,"x.xlsx",Options(preview.Hash,6,7),"qa",default);
        Assert.Equal(0,result.Added); Assert.Equal(2,result.Failed);
        Assert.Empty(await db.Set<CalibrationInstrument>().AsNoTracking().ToListAsync());
        Assert.Contains(await db.Set<CalibrationAuditLog>().ToListAsync(),x=>x.Action=="InstrumentImportFailed");
    }

    [Fact]
    public async Task InvalidSelectedRowPreventsEntireWriteButCanBeExcluded()
    {
        var file=Workbook(Row(6)+Row(7,"A-02",last:"2026/2/30")); var preview=await service.PreviewAsync(file,default);
        Assert.Equal(2,(await service.CommitAsync(file,"x.xlsx",Options(preview.Hash,6,7),"qa",default)).Failed);
        Assert.Empty(await db.Set<CalibrationInstrument>().ToListAsync());
        Assert.Equal(1,(await service.CommitAsync(file,"x.xlsx",Options(preview.Hash,6),"qa",default)).Added);
    }
    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
