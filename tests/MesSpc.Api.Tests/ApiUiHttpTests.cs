using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MesSpc.Api.Services.Localization;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MesSpc.Api.Tests;

public sealed class ApiUiHttpTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName="Development",ApplicationName=typeof(ApiUiHttpTests).Assembly.FullName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(ApiUiProbeController).Assembly);
        builder.Services.Configure<ApiBehaviorOptions>(ApiUiValidation.ConfigureOptions);
        app=builder.Build();app.MapControllers();await app.StartAsync();
        client=new HttpClient { BaseAddress=new Uri(app.Urls.Single()) };
    }

    [Fact]
    public async Task InvalidModelKeepsStatusAndFieldKeysWithSafeAdditiveDescriptors()
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/i18n-probe") { Content=JsonContent.Create(new {amount=1.25m}) };
        request.Headers.Add(ApiUiMessageCatalog.LocaleHeader,"en-US");
        using var response=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root=json.RootElement;
        Assert.Equal("spc.api.validationFailed",root.GetProperty("messageKey").GetString());
        var field=Assert.Single(root.GetProperty("errors").EnumerateObject());
        Assert.Equal("Name",field.Name);
        Assert.Equal("Name",root.GetProperty("fieldMessages").GetProperty(field.Name)[0].GetProperty("args").GetProperty("field").GetString());
        Assert.Equal(400,root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ConcurrentRequestLocalesDoNotChangeJsonNumberParsingOrOtherRequests()
    {
        await Task.WhenAll(Enumerable.Range(0,12).Select(async index=>
        {
            var locale=new[] {"zh-TW","zh-CN","en-US"}[index%3];
            using var request=new HttpRequestMessage(HttpMethod.Post,"/api/i18n-probe") { Content=JsonContent.Create(new {name="Probe",amount=1.25m}) };
            request.Headers.Add(ApiUiMessageCatalog.LocaleHeader,locale);
            request.Headers.AcceptLanguage.ParseAdd("fr-FR");
            using var response=await client.SendAsync(request);response.EnsureSuccessStatusCode();
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(1.25m,json.RootElement.GetProperty("amount").GetDecimal());
            Assert.Equal(locale,json.RootElement.GetProperty("locale").GetString());
        }));
    }

    [Fact]
    public async Task ExistingSafeExceptionEnvelopeStillHasSameStatusCodeAndNoInternalSecret()
    {
        using var response=await client.GetAsync("/api/i18n-probe/failure");
        Assert.Equal(HttpStatusCode.InternalServerError,response.StatusCode);
        var text=await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SQL-secret",text);
        using var json=JsonDocument.Parse(text);
        Assert.Equal("UNEXPECTED_ERROR",json.RootElement.GetProperty("code").GetString());
        Assert.Equal("spc.api.unexpected",json.RootElement.GetProperty("messageKey").GetString());
        Assert.Equal(JsonValueKind.Object,json.RootElement.GetProperty("args").ValueKind);
    }

    public async Task DisposeAsync(){client.Dispose();await app.DisposeAsync();}
}

[ApiController]
[Route("api/i18n-probe")]
public sealed class ApiUiProbeController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody]ApiUiProbeRequest request)=>Ok(new {amount=request.Amount,locale=ApiUiMessageCatalog.RequestLocale(Request)});
    [HttpGet("failure")]
    public IActionResult Failure()=>StatusCode(500,ApiErrorResponseFactory.FromException(new InvalidOperationException("SQL-secret"),false));
}

public sealed class ApiUiProbeRequest
{
    [Required] public string? Name {get;set;}
    [Range(0,10)] public decimal Amount {get;set;}
}
