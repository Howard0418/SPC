using MesSpc.Api.Services.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace MesSpc.Api.Tests;
public class ApiUiExceptionTests
{
    [Theory]
    [InlineData("date","spc.api.particleDateOrder","from 必須早於 to。")]
    [InlineData("location","spc.api.particleLocationInvalid","Location 僅支援 R1 至 R9。")]
    [InlineData("size","spc.api.particleSizeInvalid","ParticleSize 僅支援 0.5、1、5、10。")]
    public async Task ParticleValidationKeepsOriginalRejectionWithoutInvokingQuery(string scenario,string key,string message)
    {
        var from=new DateTimeOffset(2026,10,1,0,0,0,TimeSpan.Zero);var controller=new MesSpc.Api.Controllers.ParticlesController(null!);
        var result=Assert.IsType<BadRequestObjectResult>(await controller.Measurements(from,scenario=="date"?from:from.AddDays(1),[scenario=="location"?"R99":"R1"],[scenario=="size"?2m:0.5m],null,null));
        var error=Assert.IsType<ApiUiError>(result.Value);Assert.Equal(message,error.Message);Assert.Equal(key,error.MessageKey);
    }
    [Fact]
    public void RealDateAndDecimalValidationRetainOriginalMessagesAndExplicitKeys()
    {
        var date=Assert.Throws<ArgumentException>(()=>MesSpc.Api.Services.VersionHistoryDateRange.Parse("bad-date",null));
        Assert.Equal("請使用YYYY-MM-DD有效日期，例如2026-10-19。",date.Message);Assert.Equal("spc.api.historyDateInvalid",ApiUiException.Read(date)!.MessageKey);
        var number=Assert.Throws<ArgumentException>(()=>MesSpc.Api.Services.ChemicalFormulaCalculation.ReadDecimal("not-a-number","Primary"));
        Assert.Equal("Primary須提供未先取位的有效十進位原始字串。",number.Message);Assert.Equal("Primary",ApiUiException.Read(number)!.Args["field"]);
        var safe=MesSpc.Api.Services.Security.ApiErrorResponseFactory.FromException(number,false);Assert.Equal("UNEXPECTED_ERROR",safe.Code);Assert.DoesNotContain("Primary",safe.Message);
    }
    [Fact]
    public void MetadataRetainsExceptionIdentityMessageParamNameAndInnerWithoutExposingSecret()
    {
        var inner=new InvalidOperationException("secret-token-never-in-descriptor");var original=new ArgumentException("原訊息","field",inner);
        var tagged=ApiUiException.Attach(original,"spc.api.invalidField",new Dictionary<string,string>{{"field","Raw <b>@:key {field}</b>"}});
        Assert.Same(original,tagged);Assert.Same(inner,tagged.InnerException);Assert.Equal("field",tagged.ParamName);
        var ui=Assert.IsType<ApiUiError>(ApiUiException.Describe(tagged));Assert.Equal(original.Message,ui.Message);Assert.Equal("spc.api.invalidField",ui.MessageKey);Assert.DoesNotContain("secret-token",JsonSerializer.Serialize(ui));
    }
    [Fact]
    public void UnknownExceptionKeepsOriginalLegacyMessageOnlyAndNoGuessedKey()
    {
        var ui=JsonSerializer.SerializeToElement(ApiUiException.Describe(new ArgumentException("same visible text")));
        Assert.Equal("same visible text",ui.GetProperty("message").GetString());Assert.False(ui.TryGetProperty("messageKey",out _));
    }
    [Fact]
    public void DynamicLegacyHeaderPreservesBodyAndHasBoundedUtf8Arguments()
    {
        var context=new DefaultHttpContext();var original=new BadRequestObjectResult("原body");const string raw="原名 <b>@:key {field}</b>";
        var result=ApiUiError.WithLegacyMessage(original,context,"spc.api.invalidField",new Dictionary<string,string>{{"field",raw}});
        Assert.Same(original,result);var encoded=context.Response.Headers["X-SPC-Message-Args"].ToString();Assert.True(encoded.Length<4096);
        var bytes=Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(encoded);var json=JsonSerializer.Deserialize<Dictionary<string,string>>(bytes)!;Assert.Equal(raw,json["field"]);
        var huge=new DefaultHttpContext();ApiUiError.WithLegacyMessage(original,huge,"spc.api.invalidField",new Dictionary<string,string>{{"field",new string('文',5000)}});Assert.Empty(huge.Response.Headers);
    }
}
