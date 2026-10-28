using ClosedXML.Excel;
using MesSpc.Api.Controllers;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Localization;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class CharacteristicReportNameTests
{
    [Theory][InlineData("zh-TW","比重")][InlineData("zh-CN","比重")][InlineData("en-US","=1+1 <b>@:key {name}</b>")]
    public void OverviewSelectsNameOnlyAtCellWritingAndPreservesLiteralTextAndSource(string locale,string expected)
    {
        var item=new ChartSummaryDto {PartProcessCharacteristicId=1,ChartName="獨立原圖名",CharacteristicName="比重",CharacteristicNameEn="=1+1 <b>@:key {name}</b>",CharacteristicCode="D0",Usl=.4,Lsl=.2,TotalCount=0};
        var bytes=SpcOverviewReportService.BuildWorkbook([item],new(2026,10,1),new(2026,11,1),locale);using var book=new XLWorkbook(new MemoryStream(bytes));var sheet=book.Worksheet("未分類");
        Assert.Equal(expected,sheet.Cell(4,6).GetString());Assert.Equal(XLDataType.Text,sheet.Cell(4,6).DataType);Assert.Equal("",sheet.Cell(4,6).FormulaA1);Assert.Equal(.2,sheet.Cell(4,9).GetValue<double>());Assert.Equal(0,sheet.Cell(4,13).GetValue<int>());Assert.Equal("獨立原圖名",item.ChartName);
    }
    [Fact]
    public void NameFallbackNeverChangesCultureOrRawValues()
    {
        Assert.Equal("比重",CharacteristicNamePresentation.Name("比重"," ","D0","en-US"));Assert.Equal("Proportion",CharacteristicNamePresentation.Name(" ","Proportion","D0","zh-CN"));Assert.Equal("D0",CharacteristicNamePresentation.Name(null,null,"D0","en-US"));Assert.Equal("Unnamed characteristic",CharacteristicNamePresentation.Name(null,null,null,"en-US"));Assert.Equal("  比重  ",CharacteristicNamePresentation.Name("  比重  ",null,null,"en-US"));Assert.Equal("比重",CharacteristicNamePresentation.Name("比重","Proportion",null,"invalid"));
    }
    [Fact]
    public async Task FTableImpactAddsEnglishWithoutChangingFormulaReferenceOrWritingData()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ChemicalFTableVersions.Add(new ChemicalFTableVersion {Id=1,VersionCode="F0",IsActive=true});db.QualityCharacteristics.Add(new QualityCharacteristic {Id=1,CharacteristicCode="D0",CharacteristicName="比重",CharacteristicNameEn="Proportion"});db.PartProcessCharacteristics.Add(new PartProcessCharacteristic {Id=1,CharacteristicId=1,ProcessId=1});db.ChemicalFTableReferences.Add(new ChemicalFTableReference {Id=1,VersionId=1,PartProcessCharacteristicId=1,NormalizedCellAddress="F!B3",SourceFormula="Primary * F!B3",ReferenceContext="Concentration"});await db.SaveChangesAsync();
        var row=Assert.Single(await new ChemicalFTableService(db).GetImpactAsync("F!B3",default));Assert.Equal("比重",row.CharacteristicName);Assert.Equal("Proportion",row.CharacteristicNameEn);Assert.Equal("D0",row.CharacteristicCode);Assert.Equal("Primary * F!B3",row.SourceFormula);Assert.Equal("Concentration",row.ReferenceContext);Assert.Equal(1,await db.ChemicalFTableReferences.CountAsync());
    }
}
