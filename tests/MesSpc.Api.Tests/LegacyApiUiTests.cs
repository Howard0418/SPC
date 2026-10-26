using MesSpc.Api.Services.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class LegacyApiUiTests
{
    [Fact]
    public async Task ProcessDeleteKeepsOldRelatedItemsAndAddsSafeDescriptorsInSameOrder()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var process=new Process{ProcessCode="P",ProcessName="原製程"};db.Add(process);await db.SaveChangesAsync();
        const string raw="<b>@:key {name}</b>";db.Add(new Machine{MachineCode="M",MachineName=raw,ProcessId=process.Id});await db.SaveChangesAsync();
        var result=Assert.IsType<ConflictObjectResult>(await new ProcessesController(db).Delete(process.Id));
        var json=System.Text.Json.JsonSerializer.SerializeToElement(result.Value,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal("生產機台：M ("+raw+")",json.GetProperty("relatedItems")[0].GetString());
        var descriptor=json.GetProperty("relatedItemMessages")[0];Assert.Equal("spc.api.masterRelatedMachine",descriptor.GetProperty("messageKey").GetString());Assert.Equal(raw,descriptor.GetProperty("args").GetProperty("name").GetString());Assert.Single(db.Processes);Assert.Single(db.Machines);
    }
    [Fact]
    public async Task ProcessMissingNameRetainsBadRequestStringAndDoesNotWrite()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller=new ProcessesController(db){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        var result=Assert.IsType<BadRequestObjectResult>(await controller.Create(new Process{ProcessCode="P",ProcessName=" "}));
        Assert.Equal("製程中文名稱為必填欄位。",result.Value);Assert.Equal("spc.api.masterProcessNameRequired",controller.Response.Headers["X-SPC-Message-Key"]);Assert.Empty(db.Processes);
    }
    [Fact]
    public async Task DuplicateGroupPreservesRawIdentifiersAndDoesNotCreateAnother()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Add(new ControlChartGroup{GroupCode="A",GroupName="Raw",BusinessScopeCode="CHEM",GroupType="CONTROL_CHART",IsEnabled=true});await db.SaveChangesAsync();
        var result=Assert.IsType<ConflictObjectResult>(await new ControlChartGroupsController(db).Create(new ControlChartGroup{GroupCode="B",GroupName="Other",BusinessScopeCode="CHEM",GroupType="CONTROL_CHART",IsEnabled=true}));
        var error=Assert.IsType<ApiUiError>(result.Value);Assert.Equal("spc.api.masterGroupExists",error.MessageKey);Assert.Equal("CHEM",error.Args["scope"]);Assert.Equal("CONTROL_CHART",error.Args["groupType"]);Assert.Equal("業務範圍「CHEM」已有啟用中的「CONTROL_CHART」大類別；同一範圍與圖表模式只能啟用一個。",error.Message);Assert.Single(db.ControlChartGroups);
    }
    [Fact]
    public void HeaderMetadataPreservesOriginalResultAndBody()
    {
        var context=new DefaultHttpContext();var original=new BadRequestObjectResult("原字串 <b>@:key</b>");
        var result=ApiUiError.WithLegacyMessage(original,context,"spc.api.unexpected");
        Assert.Same(original,result);Assert.Equal("原字串 <b>@:key</b>",result.Value);Assert.Equal(400,result.StatusCode);
        Assert.Equal("spc.api.unexpected",context.Response.Headers["X-SPC-Message-Key"]);
        Assert.Same(original,ApiUiError.WithLegacyMessage(original,null,"spc.api.unexpected"));
    }
    [Fact]
    public void HeaderMetadataRejectsUnknownOrParameterizedMessages()
    {
        var context=new DefaultHttpContext();var original=new BadRequestObjectResult("original");
        Assert.Throws<KeyNotFoundException>(()=>ApiUiError.WithLegacyMessage(original,context,"spc.api.unknown"));
        Assert.Throws<ArgumentException>(()=>ApiUiError.WithLegacyMessage(original,context,"spc.api.invalidField"));
        Assert.Empty(context.Response.Headers);
    }
}
