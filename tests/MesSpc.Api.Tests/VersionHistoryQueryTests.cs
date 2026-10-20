using System.Text.Json;
using FluentAssertions;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class VersionHistoryQueryTests
{
    [Fact]
    public async Task AllThreeSourcesFilterBeforePaginationAndUseTaipeiBoundary()
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var ppc=new PartProcessCharacteristic{ControlScope="CHEM"};var other=new PartProcessCharacteristic{ControlScope="CHEM"};db.AddRange(ppc,other);await db.SaveChangesAsync();
        for(var i=1;i<=35;i++)
        {
            var at=i<=25?new DateTime(2026,10,18,16,0,0,DateTimeKind.Utc):new DateTime(2026,10,19,16,0,0,DateTimeKind.Utc);
            db.ChemicalAnalysisFormulaVersions.Add(new(){PartProcessCharacteristicId=ppc.Id,VersionNo=i,ChangedAt=at,ConfigJson="{}"});
            db.ChemicalFTableVersionHistories.Add(new(){VersionNo=i,VersionCode="F-TEST",ChangedAt=at});
            db.SpecificationLimitVersions.Add(new(){PartProcessCharacteristicId=ppc.Id,VersionNo=i,ChangedAt=at});
        }
        var lastTick=new DateTime(2026,10,19,16,0,0,DateTimeKind.Unspecified).AddTicks(-1);
        db.ChemicalAnalysisFormulaVersions.Add(new(){PartProcessCharacteristicId=ppc.Id,VersionNo=36,ChangedAt=lastTick,ConfigJson="{}"});
        db.ChemicalFTableVersionHistories.Add(new(){VersionNo=36,VersionCode="F-LAST",ChangedAt=lastTick});
        db.SpecificationLimitVersions.Add(new(){PartProcessCharacteristicId=ppc.Id,VersionNo=36,ChangedAt=lastTick});
        db.ChemicalAnalysisFormulaVersions.Add(new(){PartProcessCharacteristicId=other.Id,VersionNo=100,ChangedAt=new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc)});
        await db.SaveChangesAsync();
        var formula=new ChemicalAnalysisFormulaVersionsController(db,new ChemicalAnalysisFormulaVersionService(db));
        var formulaPage=Rows(await formula.GetVersions(ppc.Id,CancellationToken.None,"2026-10-19","2026-10-19",20,21));
        formulaPage.Should().HaveCount(6);formulaPage[0].GetProperty("VersionNo").GetInt32().Should().Be(6);
        var fPage=await new ChemicalFTableService(db).GetVersionHistoryAsync(21,CancellationToken.None,"2026-10-19","2026-10-19",20);
        fPage.Should().HaveCount(6);fPage[0].VersionNo.Should().Be(6);
        var specPage=Rows(await new SpecificationLimitVersionsController(db).Get(ppc.Id,CancellationToken.None,"2026-10-19","2026-10-19",20,21));
        specPage.Should().HaveCount(6);specPage[0].GetProperty("VersionNo").GetInt32().Should().Be(6);
        Rows(await formula.GetVersions(ppc.Id,CancellationToken.None)).Should().HaveCount(36);
        Rows(await formula.GetVersions(ppc.Id,CancellationToken.None))[0].GetProperty("ChangedAt").GetString().Should().EndWith("Z");
        (await db.SpecificationLimitVersions.CountAsync()).Should().Be(36);
    }
    [Theory]
    [InlineData("2026-10-20","2026-10-19")][InlineData("2026-02-30",null)]
    public async Task EveryApiRejectsInvalidRange(string from,string? to)
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service=new ChemicalFTableService(db);
        (await new ChemicalAnalysisFormulaVersionsController(db,new ChemicalAnalysisFormulaVersionService(db)).GetVersions(1,CancellationToken.None,from,to)).Should().BeOfType<BadRequestObjectResult>();
        (await new ChemicalFTableController(service).Versions(21,CancellationToken.None,from,to)).Should().BeOfType<BadRequestObjectResult>();
        (await new SpecificationLimitVersionsController(db).Get(1,CancellationToken.None,from,to)).Should().BeOfType<BadRequestObjectResult>();
    }
    private static JsonElement[] Rows(IActionResult result)=>JsonSerializer.SerializeToElement(result.Should().BeOfType<OkObjectResult>().Subject.Value).GetProperty("data").EnumerateArray().ToArray();
}
