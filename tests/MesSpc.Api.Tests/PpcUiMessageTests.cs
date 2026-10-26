using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class PpcUiMessageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditingLowerLimitExcludesSelfAndRejectsRealDuplicate(bool duplicate)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var process=new Process{ProcessCode="P",ProcessName="Process",ControlScope="CHEM"};
        var characteristic=new QualityCharacteristic{CharacteristicCode="C",CharacteristicName="Quality",ControlScope="CHEM",DataCategory="Variable"};
        var group=new ControlChartGroup{GroupCode="G",GroupName="Group",GroupType="CONTROL_CHART",BusinessScopeCode="CHEM"};
        db.AddRange(process,characteristic,group);await db.SaveChangesAsync();
        var chart=new ControlChartType{ChartTypeCode="I_MR",ChartTypeName="I-MR",ChartGroupId=group.Id,DataCategory="Variable",RequiredSampleSize=1};db.Add(chart);await db.SaveChangesAsync();
        PartProcessCharacteristic Item()=>new(){ControlScope="CHEM",ProcessId=process.Id,CharacteristicId=characteristic.Id,ChartTypeId=chart.Id,Unit="μm",SampleSize=1,LSL=0.19999999999999998,TargetValue=0.3,USL=0.4};
        var item=Item();db.Add(item);if(duplicate)db.Add(Item());await db.SaveChangesAsync();
        var request=Item();request.LSL=0.2;var result=await new PartProcessCharacteristicsController(db).Update(item.Id,request);
        if(duplicate){var error=Assert.IsType<ConflictObjectResult>(result);var descriptor=Assert.IsType<ApiUiError>(error.Value);Assert.Equal("spc.api.ppcDuplicate",descriptor.MessageKey);Assert.Equal(0.19999999999999998,item.LSL);}
        else{Assert.IsType<OkObjectResult>(result);Assert.Equal(0.2,item.LSL);Assert.Single(db.PartProcessCharacteristics);}
    }
    [Theory]
    [InlineData(0,0,"spc.api.ppcProcessRequired","工站製程為必填。")]
    [InlineData(1,0,"spc.api.ppcCharacteristicRequired","品質特性為必填。")]
    public async Task InvalidCreatePreservesMessageStatusAndDoesNotWrite(int process,int characteristic,string key,string message)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var response=Assert.IsType<BadRequestObjectResult>(await new PartProcessCharacteristicsController(db).Create(new PartProcessCharacteristic{ProcessId=process,CharacteristicId=characteristic}));
        var json=JsonSerializer.SerializeToElement(response.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));Assert.Equal(message,json.GetProperty("message").GetString());Assert.Equal(key,json.GetProperty("messageKey").GetString());Assert.Empty(db.PartProcessCharacteristics);
    }
    [Fact]
    public void GroupNameIsSinglePassDataAndDescriptorOwnsArguments()
    {
        const string raw="<b>@:key {groupName}|raw</b>";var args=new Dictionary<string,string>{{"groupName",raw}};
        var error=ApiUiError.Create("PPC_PART_REQUIRED","spc.api.ppcPartRequired",args);args["groupName"]="modified";
        Assert.Equal(raw+"管制項目必須選擇產品料號。",error.Message);Assert.Equal(raw,error.Args["groupName"]);
        Assert.Throws<ArgumentException>(()=>ApiUiError.Create("CODE","spc.api.ppcPartRequired",new Dictionary<string,string>()));
    }
}
