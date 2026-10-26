using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class ChartUiMessageTests
{
    [Theory]
    [InlineData("missing","SPC 管制項目設定不存在。","spc.api.chartMappingMissing")]
    [InlineData("start","起日為必填。","spc.api.segmentStartRequired")]
    [InlineData("range","起日不可大於迄日。","spc.api.segmentDateOrder")]
    [InlineData("overlap","分段時間區間不可重疊，請調整起迄日期。","spc.api.segmentOverlap")]
    public async Task InvalidSegmentKeepsLegacyStringAndDoesNotWrite(string scenario,string message,string key)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var item=new PartProcessCharacteristic{IsEnabled=true};if(scenario!="missing"){db.Add(item);await db.SaveChangesAsync();}
        var start=new DateTime(2026,10,1);if(scenario=="overlap"){db.Add(new ControlLimitSegment{PartProcessCharacteristicId=item.Id,StartDate=start});await db.SaveChangesAsync();}
        var request=new ControlLimitSegmentRequest{PartProcessCharacteristicId=item.Id,StartDate=scenario=="start"?default:start,EndDate=scenario=="range"?start.AddDays(-1):start.AddDays(1)};
        var controller=new ControlLimitSegmentsController(db){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        var result=Assert.IsType<BadRequestObjectResult>(await controller.Create(request));Assert.Equal(message,result.Value);Assert.Equal(key,controller.Response.Headers["X-SPC-Message-Key"]);Assert.Equal(scenario=="overlap"?1:0,db.ControlLimitSegments.Count());
    }
}
