using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace MesSpc.Api.Tests;
public class AuthUiMessageTests
{
    [Theory]
    [InlineData("notConfigured",503,"AUTH_SSO_NOT_CONFIGURED","spc.api.ssoNotConfigured","Portal SSO 尚未設定。")]
    [InlineData("expired",401,"AUTH_SSO_EXPIRED","spc.api.ssoExpired","Portal SSO 請求已失效。")]
    [InlineData("signature",401,"AUTH_SSO_SIGNATURE","spc.api.ssoSignature","Portal SSO 簽章不正確。")]
    [InlineData("login",410,"AUTH_LOCAL_DISABLED","spc.api.localLoginDisabled","SPC 已停用本機密碼登入，請使用 AD 帳號或工號登入。")]
    public async Task FailurePreservesLegacyStatusAndMessageWithSafeMetadata(string scenario,int status,string code,string key,string message)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Auth:PortalSsoKey"]=scenario=="notConfigured"?null:new string('x',40)}).Build();
        var controller=new AuthController(config,db);
        var request=new AuthController.PortalSsoRequest("fixture",null,scenario=="expired"?0:DateTimeOffset.UtcNow.ToUnixTimeSeconds(),Guid.NewGuid().ToString(),"bad-signature");
        var result=Assert.IsAssignableFrom<ObjectResult>(scenario=="login"?controller.Login():await controller.PortalSso(request));
        Assert.Equal(status,result.StatusCode);var json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(message,json.GetProperty("message").GetString());Assert.Equal(code,json.GetProperty("code").GetString());Assert.Equal(key,json.GetProperty("messageKey").GetString());Assert.Empty(json.GetProperty("args").EnumerateObject());Assert.Empty(db.Operators);
    }
    [Theory]
    [InlineData("Viewer","POST",403,false)]
    [InlineData("Viewer","GET",200,true)]
    [InlineData("Viewer","HEAD",200,true)]
    [InlineData("Viewer","OPTIONS",200,true)]
    [InlineData("Editor","POST",200,true)]
    public async Task ViewerGuardKeepsAuthorizationAndLegacyMessage(string role,string method,int status,bool expectedNext)
    {
        var called=false;var context=new DefaultHttpContext();context.Response.Body=new MemoryStream();context.Request.Path="/api/parts";context.Request.Method=method;
        context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"fixture"),new Claim(ClaimTypes.Role,role)},"fixture"));
        await new ViewerWriteGuardMiddleware(_=>{called=true;return Task.CompletedTask;}).InvokeAsync(context);
        Assert.Equal(expectedNext,called);Assert.Equal(status,context.Response.StatusCode);
        if(!called){context.Response.Body.Position=0;var json=await JsonDocument.ParseAsync(context.Response.Body);Assert.Equal("檢視者只能查看與查詢資料，無法執行新增、修改、刪除或匯入操作。",json.RootElement.GetProperty("message").GetString());Assert.Equal("spc.api.viewerWriteDenied",json.RootElement.GetProperty("messageKey").GetString());}
    }
}
