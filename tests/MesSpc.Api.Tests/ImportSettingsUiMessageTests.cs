using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace MesSpc.Api.Tests;
public class ImportSettingsUiMessageTests
{
    [Theory]
    [InlineData("required","spc.api.equipmentPointIdentityRequired","來源、設備與點位 ID 為必填。")]
    [InlineData("role","spc.api.equipmentStatusRoleInvalid","狀態用途只允許 RUNNING 或 ALARM。")]
    [InlineData("active","spc.api.equipmentActiveWhenInvalid","訊號成立條件只允許 NONZERO 或 ZERO。")]
    public async Task InvalidEquipmentMappingRetainsMessageAndDoesNotSave(string scenario,string key,string message)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller=new EquipmentPointsController(db,null!);var request=new EquipmentPointMapping{SourceId=scenario=="required"?"":"source",EquipmentId="equipment",ChannelId="channel",StatusRole=scenario=="role"?"INVALID":"RUNNING",ActiveWhen=scenario=="active"?"INVALID":"NONZERO"};
        var result=Assert.IsType<BadRequestObjectResult>(await controller.UpsertMapping(request,default));var json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(message,json.GetProperty("message").GetString());Assert.Equal(key,json.GetProperty("messageKey").GetString());Assert.Empty(db.EquipmentPointMappings);
    }
    [Fact]
    public async Task EmptyChemicalFileRetainsOriginalErrorField()
    {
        var result=Assert.IsType<BadRequestObjectResult>(await new ChemicalDailyReportController(null!,null!).Preview(null!));var json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("請上傳 .xlsx 檔案",json.GetProperty("error").GetString());Assert.Equal("spc.api.uploadXlsxRequired",json.GetProperty("messageKey").GetString());
    }
    [Fact]
    public async Task AnonymousCalibrationImportIsStillUnauthorizedWithoutCallingServices()
    {
        var controller=new InstrumentImportController(null!,null!){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        var result=Assert.IsType<UnauthorizedObjectResult>(await controller.Access(default));var json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.False(json.GetProperty("success").GetBoolean());Assert.Equal("請先登入。",json.GetProperty("message").GetString());Assert.Equal("spc.api.signInRequired",json.GetProperty("messageKey").GetString());
    }
}
