using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class AuthenticationAuditTests
{
    private const string Key = "audit-auth-test-key-32-characters-or-more";
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public AppDbContext Db { get; private set; } = null!;
        public AuthenticationAudit Audit { get; private set; } = null!;
        public AuthController Controller { get; private set; } = null!;
        public async Task InitializeAsync()
        {
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            Db = new(options); await Db.Database.EnsureCreatedAsync();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["AppEnvironment"]="test", ["Auth:Enabled"]="true", ["Auth:JwtKey"]=Key, ["Auth:PortalSsoKey"]=Key,
                ["Audit:AuthenticationEnabled"]="true", ["Audit:AttemptedAccountHmacKey"]=Key, ["Database:InitializeOnStartup"]="false"
            }).Build();
            Audit = new(new AuditEventWriter(Db,options,config,TimeProvider.System,NullLogger<AuditEventWriter>.Instance), config, NullLogger<AuthenticationAudit>.Instance);
            Controller = new(config,Db,Audit) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private static AuthController.PortalSsoRequest Request(string account="audit.qa", bool legacy=false)
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var nonce=Guid.NewGuid().ToString("N");
        var text=legacy?$"{account}|QA|{now}|{nonce}":$"{account}|QA|AUDIT-QA|||{now}|{nonce}";
        var signature=Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Key),Encoding.UTF8.GetBytes(text)));
        return new(account,"QA",now,nonce,signature,"AUDIT-QA");
    }
    [Fact]
    public async Task SuccessAndLogoutShareSessionWithoutStoringSecrets()
    {
        await using var f=new Fixture();await f.InitializeAsync();var request=Request();
        var result=Assert.IsType<OkObjectResult>(await f.Controller.PortalSso(request));
        var json=JsonSerializer.SerializeToElement(result.Value);var token=json.GetProperty("token").GetString()!;
        var jwt=new JwtSecurityTokenHandler().ReadJwtToken(token);var jti=jwt.Claims.Single(x=>x.Type=="jti").Value;
        Assert.False(string.IsNullOrWhiteSpace(jti));var actorId=json.GetProperty("user").GetProperty("id").GetInt32();
        var login=await f.Db.AuditEvents.AsNoTracking().SingleAsync();
        Assert.Equal("Succeeded",login.Outcome);Assert.Equal("PortalAssertion",login.ActorAssurance);Assert.Equal(actorId,login.ActorOperatorId);
        Assert.Equal(f.Audit.SessionHash(jti),login.SessionIdHash);
        f.Controller.HttpContext.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actorId.ToString()),new Claim("jti",jti)},"test"));
        Assert.IsType<NoContentResult>(await f.Controller.Logout());
        var logout=await f.Db.AuditEvents.AsNoTracking().SingleAsync(x=>x.EventCode=="auth.logout.request");
        Assert.Equal(login.SessionIdHash,logout.SessionIdHash);
        var evidence=JsonSerializer.Serialize(await f.Db.AuditEvents.AsNoTracking().ToListAsync());
        foreach(var secret in new[]{token,jti,request.Nonce,request.Signature})Assert.DoesNotContain(secret,evidence);
    }
    [Theory]
    [InlineData("signature","AUTH_SSO_SIGNATURE")]
    [InlineData("expired","AUTH_SSO_EXPIRED")]
    [InlineData("min","AUTH_SSO_EXPIRED")]
    [InlineData("max","AUTH_SSO_EXPIRED")]
    public async Task UnverifiedFailuresRecordOnlyHmacAndFixedReason(string scenario,string code)
    {
        await using var f=new Fixture();await f.InitializeAsync();var req=Request("unverified-secret-account");
        req=scenario switch {"expired"=>req with{Timestamp=0},"min"=>req with{Timestamp=long.MinValue},"max"=>req with{Timestamp=long.MaxValue},_=>req with{Signature="bad"}};
        Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(req));
        var row=await f.Db.AuditEvents.SingleAsync();Assert.Equal(code,row.ReasonCode);Assert.Null(row.ActorOperatorId);Assert.Null(row.ActorAccountSnapshot);Assert.Equal(64,row.AttemptedAccountHash!.Length);
        Assert.DoesNotContain(req.Username,JsonSerializer.Serialize(row));Assert.Equal(0,await f.Db.Operators.CountAsync());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplayAndDisabledAccountHaveCorrectActorAssurance(bool deleted)
    {
        await using var f=new Fixture();await f.InitializeAsync();var req=Request();
        await f.Controller.PortalSso(req);Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(req));
        Assert.Equal("AUTH_SSO_REPLAYED",(await f.Db.AuditEvents.AsNoTracking().SingleAsync(x=>x.Outcome=="Denied")).ReasonCode);
        var user=await f.Db.Operators.SingleAsync();user.IsActive=false;user.IsDeleted=deleted;await f.Db.SaveChangesAsync();
        Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(Request()));
        var denied=await f.Db.AuditEvents.AsNoTracking().SingleAsync(x=>x.ReasonCode=="AUTH_ACCOUNT_DISABLED");
        Assert.Equal(user.Id,denied.ActorOperatorId);Assert.Equal("PortalAssertion",denied.ActorAssurance);
    }
    [Fact]
    public async Task FailedSuccessAuditReturns503AndRollsBackNewIdentity()
    {
        await using var f=new Fixture();await f.InitializeAsync();
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAuthAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic audit failure'); END;");
        var result=Assert.IsType<ObjectResult>(await f.Controller.PortalSso(Request()));Assert.Equal(503,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.Empty(await f.Db.Operators.ToListAsync());Assert.Empty(await f.Db.AuditEvents.ToListAsync());
        Assert.DoesNotContain("synthetic audit failure",JsonSerializer.Serialize(result.Value));
    }
    [Fact]
    public async Task AuditFailureRestoresExistingProfileAndLegacySuccessIsTagged()
    {
        await using var f=new Fixture();await f.InitializeAsync();
        f.Db.Operators.Add(new Operator{Username="audit.qa",OperatorCode="AUDIT-QA",OperatorName="Original",Department="OriginalDepartment"});await f.Db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await f.Controller.PortalSso(Request(legacy:true)));
        var legacy=await f.Db.AuditEvents.AsNoTracking().SingleAsync();Assert.True(JsonDocument.Parse(legacy.DetailsJson).RootElement.GetProperty("LegacyAssertion").GetBoolean());
        var user=await f.Db.Operators.SingleAsync();user.OperatorName="Original";await f.Db.SaveChangesAsync();
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAuthAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic failure'); END;");
        var result=Assert.IsType<ObjectResult>(await f.Controller.PortalSso(Request()));Assert.Equal(503,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.Equal("Original",(await f.Db.Operators.SingleAsync()).OperatorName);
        Assert.Equal(1,await f.Db.AuditEvents.CountAsync());
    }
    [Fact]
    public async Task DenialAndLogoutKeepTheirResultsWhenAuditUnavailable()
    {
        await using var f=new Fixture();await f.InitializeAsync();
        f.Db.Operators.Add(new Operator{Id=1,OperatorCode="QA",OperatorName="QA"});await f.Db.SaveChangesAsync();
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAuthAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic audit failure'); END;");
        Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(Request() with{Signature="bad"}));
        f.Controller.HttpContext.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,"1")},"test"));
        Assert.IsType<NoContentResult>(await f.Controller.Logout());
        var local=Assert.IsType<ObjectResult>(await f.Controller.Login());Assert.Equal(410,local.StatusCode);
    }
    [Theory]
    [InlineData("/api/v1/auth/logout",true)]
    [InlineData("/api/v1/auth/portal-sso",true)]
    [InlineData("/api/v1/auth/logout/other",false)]
    [InlineData("/api/operators",false)]
    public async Task ViewerOnlyGetsNarrowAuthenticationExceptions(string path,bool allowed)
    {
        var called=false;var context=new DefaultHttpContext();context.Request.Method="POST";context.Request.Path=path;context.Response.Body=new MemoryStream();
        context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,"Viewer")},"test"));
        await new ViewerWriteGuardMiddleware(_=>{called=true;return Task.CompletedTask;}).InvokeAsync(context);Assert.Equal(allowed,called);
    }
    [Fact]
    public void ActivationRequiresAuthStrongKeysAndExternalMigrations()
    {
        var values=new Dictionary<string,string?>{["Audit:AuthenticationEnabled"]="true",["Auth:Enabled"]="false",["AppEnvironment"]="test",["Audit:AttemptedAccountHmacKey"]=Key,["Database:InitializeOnStartup"]="false"};
        Assert.Throws<InvalidOperationException>(()=>AuthenticationAuditStartup.ValidateConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build()));
        values["Auth:Enabled"]="true";values["Auth:JwtKey"]=Key;values["Auth:PortalSsoKey"]=Key;
        AuthenticationAuditStartup.ValidateConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        values["Database:InitializeOnStartup"]="true";
        Assert.Throws<InvalidOperationException>(()=>AuthenticationAuditStartup.ValidateConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build()));
    }
}
