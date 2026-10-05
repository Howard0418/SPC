using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MesSpc.Calibration.Tests;

// Uses the real MVC controllers, JWT bearer validation and EF model, with a dedicated host.
// Production Program (startup migrations, hosted jobs and SMTP) is never invoked.
public sealed class CalibrationImportFactory : WebApplicationFactory<InstrumentImportController>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private const string TestKey = "isolated-calibration-import-test-signing-key-only";
    public CalibrationImportFactory() => connection.Open();
    protected override IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder().ConfigureLogging(logging=>logging.ClearProviders()).ConfigureWebHost(web =>
    {
        web.UseSetting("TEST_CONTENTROOT_MESSPC_API",AppContext.BaseDirectory);
        web.ConfigureServices(services =>
        {
            services.AddLogging(); services.AddSingleton(TimeProvider.System);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<AppDbContext>(o=>o.UseSqlite(connection));
            services.AddScoped<InstrumentCalibrationService>(); services.AddScoped<CalibrationImportService>();
            services.AddControllers().AddApplicationPart(typeof(InstrumentImportController).Assembly);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new()
            { ValidateIssuer=false,ValidateAudience=false,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)) });
            services.AddAuthorization();
        });
        web.Configure(app=>
        {
            using (var scope=app.ApplicationServices.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Database.EnsureCreated();
                db.Operators.AddRange(
                    new Operator { Id=1,Username="qa",OperatorCode="QA",OperatorName="QA",Role="Editor",IsActive=true },
                    new Operator { Id=2,Username="viewer",OperatorCode="VIEW",OperatorName="VIEW",Role="Viewer",IsActive=true },
                    new Operator { Id=3,Username="denied",OperatorCode="DENIED",OperatorName="DENIED",Role="Editor",PagePermissionsJson="[\"spc\"]",IsActive=true },
                    new Operator { Id=4,Username="inactive",OperatorCode="INACTIVE",OperatorName="INACTIVE",Role="Editor",IsActive=false });
                db.SaveChanges();
            }
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e=>e.MapControllers());
        });
    });
    public HttpClient Client(string? name="qa",string role="Editor")
    {
        var client=CreateClient();
        if (name is not null)
        {
            var jwt=new JwtSecurityToken(claims:[new(ClaimTypes.Name,name),new(ClaimTypes.Role,role)], expires:DateTime.UtcNow.AddMinutes(5),
                signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));
        }
        return client;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public class CalibrationImportHttpTests
{
    private const string Endpoint="/api/v1/instruments/import/";
    internal static MultipartFormDataContent Upload(byte[] file, object? options=null, string fileName="qa.xlsx")
    {
        var form=new MultipartFormDataContent(); var content=new ByteArrayContent(file);
        content.Headers.ContentType=new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(content,"file",fileName);
        if (options is not null) form.Add(new StringContent(JsonSerializer.Serialize(options)),"options");
        return form;
    }
    [Theory]
    [InlineData(null,"Editor",401)] [InlineData("viewer","Viewer",403)]
    [InlineData("denied","Editor",403)] [InlineData("inactive","Editor",403)] [InlineData("unknown","Editor",403)]
    public async Task BothWriteEndpointsRejectUnauthorizedUsers(string? name,string role,int expected)
    {
        using var factory=new CalibrationImportFactory(); using var client=factory.Client(name,role);
        var file=CalibrationImportTests.Workbook(CalibrationImportTests.Row(6));
        foreach (var endpoint in new[] { "preview","commit" })
        {
            using var form=Upload(file,new { });
            Assert.Equal((HttpStatusCode)expected,(await client.PostAsync(Endpoint+endpoint,form)).StatusCode);
        }
    }
    [Fact]
    public async Task RealMultipartPreviewCommitReplayAndRevokedPermission()
    {
        using var factory=new CalibrationImportFactory(); using var client=factory.Client();
        var file=CalibrationImportTests.Workbook(CalibrationImportTests.Row(6));
        using var previewForm=Upload(file);
        var response=await client.PostAsync(Endpoint+"preview",previewForm);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var json=await response.Content.ReadFromJsonAsync<JsonElement>();
        var options=new { hash=json.GetProperty("data").GetProperty("hash").GetString(), selectedRows=new[] {6}, department="品保",custodianOperatorId=1,usageStatus="Active",includeCustodian=true };
        using var commit=Upload(file,options);
        var saved=await client.PostAsync(Endpoint+"commit",commit);
        Assert.Equal(HttpStatusCode.OK,saved.StatusCode);
        Assert.Equal(1,(await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("added").GetInt32());
        using var repeat=Upload(file,options);
        var replay=await client.PostAsync(Endpoint+"commit",repeat);
        Assert.Equal(1,(await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("skipped").GetInt32());
        using (var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Operators.SingleAsync(x=>x.Id==1)).PagePermissionsJson="[]"; await db.SaveChangesAsync();
        }
        using var denied=Upload(file,options);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsync(Endpoint+"commit",denied)).StatusCode);
    }
    [Fact]
    public async Task InvalidFileAndMalformedOptionsReturnReadableErrors()
    {
        using var factory=new CalibrationImportFactory(); using var client=factory.Client();
        using var wrong=Upload([1,2,3],fileName:"x.csv");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync(Endpoint+"preview",wrong)).StatusCode);
        using var form=Upload(CalibrationImportTests.Workbook(CalibrationImportTests.Row(6)));
        form.Add(new StringContent("not json"),"options");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync(Endpoint+"commit",form)).StatusCode);
    }
    [Fact]
    public async Task ActualTemplateIsReadOnlyAndHas59RowsWithExplicitExceptions()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName,"docs","sdd-workflow.md"))) root=root.Parent;
        Assert.NotNull(root);
        var file=Path.Combine(root.FullName,"docs","QA-2-003-02A 儀器設備校驗管制一覽表-2026年-品保 - 更新.xlsx");
        var bytes=await File.ReadAllBytesAsync(file);
        var parsed=CalibrationImportParser.Parse(bytes,TimeProvider.System);
        Assert.Equal("QA-2-003-02",parsed.SheetName);         Assert.Equal(59,parsed.Rows.Count);
        Assert.Equal(59, parsed.Rows.Count(x => x.Errors.Count==0));
        Assert.Equal(0, parsed.Rows.Count(x => x.Errors.Count>0));
        Assert.Equal(new DateOnly(2027,3,16),parsed.Rows.Single(x=>x.RowNumber==6).NextCalibrationDate);
        Assert.Equal("外校", parsed.Rows.Single(x=>x.RowNumber==6).CalibrationMethod);
        Assert.Contains(parsed.Rows.Single(x=>x.RowNumber==6).Warnings, x => x.Contains("校驗方式"));
        Assert.All(parsed.Rows.Where(x=>x.RowNumber is >=49 and <=55 or >=60), x =>
        {
            Assert.Empty(x.Errors);
            Assert.Null(x.NextCalibrationDate);
            Assert.Contains(x.Warnings, w => w.Contains("自行建立日期"));
        });
        Assert.Equal(bytes,await File.ReadAllBytesAsync(file));
    }
}
