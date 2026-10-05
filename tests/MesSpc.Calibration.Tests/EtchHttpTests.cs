using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public class EtchHttpTests
{
    [Fact]
    public void StrictImportRejectsProductionConfiguration()
    {
        Assert.True(EtchTestImportController.IsTest("test","Server=172.16.110.16;Database=PMR_SPC_TEST"));
        Assert.False(EtchTestImportController.IsTest("test","Server=172.16.110.16;Database=PMR_SPC_2026"));
        Assert.False(EtchTestImportController.IsTest("production","Server=172.16.110.16;Database=PMR_SPC_TEST"));
    }
    [Theory]
    [InlineData(null,HttpStatusCode.Unauthorized)]
    [InlineData("Viewer",HttpStatusCode.Forbidden)]
    [InlineData("Editor",HttpStatusCode.BadRequest)]
    public async Task EndpointRequiresWriteRole(string? role,HttpStatusCode expected)
    {
        const string key="isolated-etch-test-signing-key-only-20260917";
        using var server=new TestServer(new WebHostBuilder().ConfigureServices(s=>{
            s.AddLogging();s.AddDbContext<AppDbContext>(o=>o.UseSqlite("Data Source=:memory:"));s.AddScoped<EtchReportSyncService>();
            s.AddControllers().AddApplicationPart(typeof(EtchReportsController).Assembly);
            s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=false,ValidateAudience=false,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))});s.AddAuthorization();
        }).Configure(a=>{a.UseRouting();a.UseAuthentication();a.UseAuthorization();a.UseEndpoints(e=>e.MapControllers());}));
        using var client=server.CreateClient();
        if(role is not null){var jwt=new JwtSecurityToken(claims:[new(ClaimTypes.Role,role)],expires:DateTime.UtcNow.AddMinutes(1),signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256));client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));}
        var response=await client.PutAsJsonAsync("/api/v1/etch-reports",new EtchReportRequest(new(2026,9,1),"PT2",1,"test",[]));
        Assert.Equal(expected,response.StatusCode);
        foreach(var action in new[]{"preview","confirm"})
        {
            var strict=await client.PostAsJsonAsync("/api/v1/etch-import-test/"+action,new EtchReportRequest(new(2026,9,1),"PT2",1,"test",[]));
            Assert.Equal(role is null?HttpStatusCode.Unauthorized:HttpStatusCode.Forbidden,strict.StatusCode);
        }
    }
}
