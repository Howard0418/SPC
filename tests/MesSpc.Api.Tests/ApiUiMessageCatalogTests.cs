using System.Globalization;
using System.Text.Json;
using MesSpc.Api.Services.Localization;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public sealed class ApiUiMessageCatalogTests
{
    [Theory]
    [InlineData(null, "zh-TW")]
    [InlineData("", "zh-TW")]
    [InlineData("en", "zh-TW")]
    [InlineData("zh-tw", "zh-TW")]
    [InlineData("en-US;q=1", "zh-TW")]
    [InlineData("en-US", "en-US")]
    [InlineData("zh-CN", "zh-CN")]
    public void LocalePolicyAcceptsOnlyExplicitCanonicalValues(string? input, string expected)
    {
        Assert.Equal(expected, ApiUiMessageCatalog.NormalizeLocale(input));
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = "en-US";
        if (input is not null) context.Request.Headers[ApiUiMessageCatalog.LocaleHeader] = input;
        Assert.Equal(expected, ApiUiMessageCatalog.RequestLocale(context.Request));
    }

    [Fact]
    public void EmbeddedResourcesHaveThreeLanguagesAndSafeSinglePassNamedInterpolation()
    {
        var catalog = new ApiUiMessageCatalog();
        var raw = "<b>@:key {field}|raw</b>";
        foreach (var locale in new[] { "zh-TW", "zh-CN", "en-US" })
            Assert.Contains(raw, catalog.Format("spc.api.invalidField", locale, new Dictionary<string, string> { ["field"] = raw }));
        Assert.Contains("updated", catalog.Format("spc.api.concurrentUpdate", "en-US"), StringComparison.OrdinalIgnoreCase);
        Assert.Throws<ArgumentException>(() => catalog.Format("spc.api.invalidField", "en-US"));
        Assert.Throws<ArgumentException>(() => catalog.Format("spc.api.invalidField", "en-US", new Dictionary<string,string> { ["field"]="A",["extra"]="bad" }));
        Assert.Throws<KeyNotFoundException>(() => catalog.Format("spc.api.missing", "en-US"));
    }

    [Fact]
    public void ResourceValidationRejectsDuplicatesKeyAndParameterDrift()
    {
        var valid = """{"spc":{"api":{"named":"Hello {name} {name}"}}}""";
        Dictionary<string,string> Resources(string last) => new() { ["zh-TW"]=valid,["zh-CN"]=valid,["en-US"]=last };
        var repeated=ApiUiMessageCatalog.FromJson(Resources(valid));
        Assert.Equal("Hello {name} {name}",repeated.Format("spc.api.named","en-US",new Dictionary<string,string>{["name"]="{name}"}));
        Assert.Throws<InvalidOperationException>(() => ApiUiMessageCatalog.FromJson(Resources("""{"spc":{"api":{"named":"One {name}"}}}""")));
        Assert.Throws<InvalidOperationException>(() => ApiUiMessageCatalog.FromJson(Resources("""{"spc":{"api":{"other":"Hello {name} {name}"}}}""")));
        Assert.Throws<InvalidOperationException>(() => ApiUiMessageCatalog.FromJson(Resources("""{"spc":{"api":{"named":"A","named":"B"}}}""")));
    }

    [Fact]
    public async Task ParallelExplicitLocalesDoNotChangeAmbientCulture()
    {
        var before = CultureInfo.CurrentCulture;
        var catalog = new ApiUiMessageCatalog();
        var english = catalog.Format("spc.api.validationFailed", "en-US");
        var chinese = catalog.Format("spc.api.validationFailed", "zh-TW");
        await Task.WhenAll(Enumerable.Range(0,60).Select(index=>Task.Run(()=>
            Assert.Equal(index%2==0?english:chinese,catalog.Format("spc.api.validationFailed",index%2==0?"en-US":"zh-TW")))));
        Assert.Equal(before, CultureInfo.CurrentCulture);
    }

    [Fact]
    public void ExistingExceptionFieldsRemainSafeWithAdditiveMessageContract()
    {
        var response = ApiErrorResponseFactory.FromException(new DbUpdateConcurrencyException("SQL secret"),false);
        Assert.Equal("CONCURRENT_UPDATE",response.Code);
        Assert.Equal("資料已被其他使用者更新，請重新載入後再儲存。",response.Message);
        Assert.Null(response.Detail);
        Assert.Equal("spc.api.concurrentUpdate",response.MessageKey);
        Assert.Empty(response.Args);
        var json = JsonSerializer.Serialize(response,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.DoesNotContain("SQL secret",json);
        Assert.Contains("messageKey",json);
    }

    [Fact]
    public void ValidationDecorationRetainsOriginalProblemAndFieldErrorsWithoutLeakingInputs()
    {
        var original = new ValidationProblemDetails(new Dictionary<string,string[]> { ["Amount"]=["legacy raw error"] }) {Status=400,Title="Original title",Type="urn:original"};
        var result = new BadRequestObjectResult(original);
        Assert.Same(result,ApiUiValidation.DecorateResult(result));
        Assert.Equal(400,result.StatusCode);
        Assert.Equal("Original title",original.Title);
        Assert.Equal("legacy raw error",Assert.Single(original.Errors["Amount"]));
        Assert.Equal("spc.api.validationFailed",original.Extensions["messageKey"]);
        var json = JsonSerializer.Serialize(original,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var doc = JsonDocument.Parse(json);
        var field = doc.RootElement.GetProperty("fieldMessages").GetProperty("Amount")[0];
        Assert.Equal("spc.api.invalidField",field.GetProperty("messageKey").GetString());
        Assert.Equal("Amount",field.GetProperty("args").GetProperty("field").GetString());
        Assert.DoesNotContain("legacy raw error",field.GetRawText());
    }

    [Fact]
    public void ValidationDecorationDoesNotOverwriteAnExistingTypedError()
    {
        var problem = new ValidationProblemDetails();problem.Extensions["code"]="DOMAIN_CODE";
        var result = new BadRequestObjectResult(problem);
        ApiUiValidation.DecorateResult(result);
        Assert.Equal("DOMAIN_CODE",problem.Extensions["code"]);
        Assert.False(problem.Extensions.ContainsKey("messageKey"));
    }

    [Fact]
    public void ValidationDecorationPreservesExistingLegacyMessage()
    {
        var problem = new ValidationProblemDetails();problem.Extensions["message"]="Original safe message";
        ApiUiValidation.DecorateResult(new BadRequestObjectResult(problem));
        Assert.Equal("Original safe message",problem.Extensions["message"]);
    }
}
