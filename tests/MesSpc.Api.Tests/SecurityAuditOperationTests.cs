using System.Reflection;
using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MesSpc.Api.Tests;

public sealed class SecurityAuditOperationTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "spc-security-audit-" + Guid.NewGuid().ToString("N"));
        public AppDbContext Db { get; private set; } = null!;
        public DbContextOptions<AppDbContext> Options { get; private set; } = null!;
        public IConfiguration Config { get; private set; } = null!;
        public SecurityAudit Audit { get; private set; } = null!;
        public SecurityAuditFilter Filter { get; private set; } = null!;
        public HttpContext Http { get; } = new DefaultHttpContext();
        public IWebHostEnvironment Env { get; private set; } = null!;
        public async Task Init(bool enabled = true, int? administratorId = null, SqlConnection? sqlRuntime = null)
        {
            if(sqlRuntime is null)await connection.OpenAsync();
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            if(sqlRuntime is null)builder.UseSqlite(connection);else builder.UseSqlServer(sqlRuntime);
            var options=builder.Options; Options=options;
            Db = new(options); if(sqlRuntime is null)await Db.Database.EnsureCreatedAsync();
            Directory.CreateDirectory(Root);
            await File.WriteAllTextAsync(Path.Combine(Root, "appsettings.json"), "{\"SmtpSettings\":{\"Host\":\"before.test\",\"Password\":\"synthetic-mail-secret\"}}");
            Config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["AppEnvironment"]="test",["Auth:Enabled"]="true",["Audit:AuthenticationEnabled"]="true",["Audit:SecurityEnabled"]=enabled.ToString(),
                ["Auth:JwtKey"]="security-test-key-only-with-32-characters",["Auth:PortalSsoKey"]="security-test-key-only-with-32-characters",
                ["Audit:AttemptedAccountHmacKey"]="security-test-key-only-with-32-characters",["Database:InitializeOnStartup"]="false",
                ["PortalDirectory:ApiBaseUrl"]="http://directory.test",
                ["Audit:SecurityAdministratorOperatorIds:0"]=administratorId?.ToString() }).Build();
            if(sqlRuntime is null)Db.Operators.AddRange(new Operator { Id=1,OperatorCode="QA1",OperatorName="QA Actor",Username="qa.actor",Role="Editor",IsActive=true },
                new Operator { Id=2,OperatorCode="QA2",OperatorName="QA Target",Username="qa.target",Role="Viewer",IsActive=true });
            if(sqlRuntime is null)await Db.SaveChangesAsync(); Db.ChangeTracker.Clear();
            var writer = new AuditEventWriter(Db, options, Config, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
            var authentication = new AuthenticationAudit(writer, Config, NullLogger<AuthenticationAudit>.Instance);
            var permissions = new AuditPermissions(Db, Config);
            Audit = new(Db, writer, authentication, permissions, Config, NullLogger<SecurityAudit>.Instance);
            var env = new Mock<IWebHostEnvironment>(); env.SetupGet(x => x.ContentRootPath).Returns(Root); Env = env.Object;
            Filter = new(Db, permissions, Audit, Env, NullLogger<SecurityAuditFilter>.Instance);
            Http.User = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier,"1"),new(ClaimTypes.Name,"qa.actor"),new(ClaimTypes.Role,"Editor"),new("jti","synthetic-session")],"test"));
            Http.Response.Body = new MemoryStream(); Http.Request.Headers.Authorization="Bearer synthetic-token-must-not-be-retained";
            Http.Request.QueryString = new("?lot=synthetic-search-must-not-be-retained");
        }
        public async Task<IActionResult?> Execute(ControllerBase controller, string method, string verb, Dictionary<string,object?> args, Func<Task<IActionResult>> action)
        {
            Http.Items.Clear(); controller.ControllerContext=new(){HttpContext=Http}; Http.Request.Method=verb;
            var descriptor=new ControllerActionDescriptor { ControllerTypeInfo=controller.GetType().GetTypeInfo(),ControllerName=controller.GetType().Name.Replace("Controller",""),ActionName=method,MethodInfo=controller.GetType().GetMethod(method)! };
            var route=new RouteData(); if(args.TryGetValue("id",out var id))route.Values["id"]=id;
            var ctx=new ActionContext(Http,route,descriptor);
            var executing=new ActionExecutingContext(ctx,[],args,controller); ActionExecutedContext? executed=null;
            await Filter.OnActionExecutionAsync(executing,async()=>{
                try { executed=new(ctx,[],controller){Result=await action()}; }
                catch(Exception ex){executed=new(ctx,[],controller){Exception=ex};}
                return executed;
            });
            return executing.Result??executed?.Result;
        }
        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();await connection.DisposeAsync();
            var root=Path.GetFullPath(Root);
            if(!root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)
                || !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(root),"^spc-security-audit-[a-f0-9]{32}$"))
                throw new InvalidOperationException("Test cleanup guard failed");
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
    private static OperatorsController.OperatorRequest Request(string role="Editor",bool active=false,List<string>? permissions=null) =>
        new("QA2","QA Target Changed","synthetic-department","synthetic-private@example.test","qa.target",null,role,active,permissions??["analysis.spc"]);

    [Fact]
    public async Task RoleStatusPermissionsAndProfileAreAtomicSafeEvents()
    {
        await using var f=new Fixture();await f.Init();var c=new OperatorsController(f.Db);var req=Request();
        Assert.IsType<OkObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));
        var events=await f.Db.AuditEvents.AsNoTracking().ToListAsync();Assert.Equal(4,events.Count);
        Assert.All(events,x=>{Assert.Equal(1,x.ActorOperatorId);Assert.Equal("2",x.TargetId);Assert.Equal("Succeeded",x.Outcome);Assert.Equal("admin.operators",x.PageKey);});
        Assert.Contains(events,x=>x.EventCode=="security.operator.change_status");Assert.Contains(events,x=>x.EventCode=="security.operator.change_role");
        Assert.Contains(events,x=>x.EventCode=="security.operator.change_permissions");
        var json=JsonSerializer.Serialize(events);foreach(var secret in new[]{"synthetic-department","synthetic-private","synthetic-token","synthetic-search"})Assert.DoesNotContain(secret,json);
    }
    [Fact]
    public async Task FailedAuditRestoresOperatorAndNoSucceededEventSurvives()
    {
        await using var f=new Fixture();await f.Init();
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailSecurity BEFORE INSERT ON AuditEvents WHEN NEW.Outcome='Succeeded' BEGIN SELECT RAISE(ABORT,'synthetic-secret-failure'); END;");
        var c=new OperatorsController(f.Db);var req=Request();
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Equal(503,result.StatusCode);
        f.Db.ChangeTracker.Clear();var target=await f.Db.Operators.SingleAsync(x=>x.Id==2);Assert.Equal("Viewer",target.Role);Assert.True(target.IsActive);Assert.Equal("QA Target",target.OperatorName);
        Assert.DoesNotContain(await f.Db.AuditEvents.ToListAsync(),x=>x.Outcome=="Succeeded");Assert.DoesNotContain("synthetic-secret",JsonSerializer.Serialize(result.Value));
    }
    [Fact]
    public async Task CreateUsesRealIdAndSelfDeleteKeepsPriorActorSnapshot()
    {
        await using var f=new Fixture();await f.Init();var c=new OperatorsController(f.Db);
        var req=new OperatorsController.OperatorRequest("QA3","Created",null,null,"qa.created",null,"Viewer",true,["analysis.spc"]);
        Assert.IsType<OkObjectResult>(await f.Execute(c,"Create","POST",new(){["req"]=req},()=>c.Create(req)));
        var created=await f.Db.Operators.SingleAsync(x=>x.OperatorCode=="QA3");
        Assert.Equal(created.Id.ToString(),(await f.Db.AuditEvents.SingleAsync()).TargetId);
        Assert.IsType<NoContentResult>(await f.Execute(c,"Delete","DELETE",new(){["id"]=1},()=>c.Delete(1)));
        var deleted=await f.Db.AuditEvents.AsNoTracking().SingleAsync(x=>x.EventCode=="security.operator.change_status");
        Assert.Equal("QA Actor",deleted.ActorDisplayNameSnapshot);Assert.Contains("Deleted",deleted.DetailsJson);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SensitiveSelfGrantAndProtectedManagerTakeoverAreDenied(bool protectedTarget)
    {
        await using var f=new Fixture();await f.Init(administratorId:protectedTarget?2:null);var c=new OperatorsController(f.Db);
        var req=protectedTarget?Request():Request(permissions:["admin.audit.read"]);
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Equal(403,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.Equal("QA Target",(await f.Db.Operators.SingleAsync(x=>x.Id==2)).OperatorName);
        Assert.Equal("Denied",(await f.Db.AuditEvents.SingleAsync()).Outcome);
    }
    [Fact]
    public async Task SmtpPostFileAuditFailureRestoresFileAndDatabaseAndKeepsIntent()
    {
        await using var f=new Fixture();await f.Init();var original=await File.ReadAllTextAsync(Path.Combine(f.Root,"appsettings.json"));
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailSetting BEFORE INSERT ON AuditEvents WHEN NEW.EventCode='security.setting.change' AND NEW.Outcome='Succeeded' BEGIN SELECT RAISE(ABORT,'synthetic-secret-failure'); END;");
        var c=new SettingsController(f.Config,f.Env,Mock.Of<IEmailNotificationService>(),NullLogger<SettingsController>.Instance,f.Db);
        var req=new SmtpSettingsDto{Host="changed.test"};
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"UpdateSmtpSettings","POST",new(){["dto"]=req},()=>c.UpdateSmtpSettings(req)));Assert.Equal(503,result.StatusCode);
        Assert.Equal(original,await File.ReadAllTextAsync(Path.Combine(f.Root,"appsettings.json")));f.Db.ChangeTracker.Clear();Assert.Empty(await f.Db.SpcAlertNotificationSettings.ToListAsync());
        var events=await f.Db.AuditEvents.ToListAsync();Assert.Contains(events,x=>x.EventCode=="security.setting.prepare");Assert.DoesNotContain(events,x=>x.EventCode=="security.setting.change"&&x.Outcome=="Succeeded");
        Assert.DoesNotContain("synthetic-mail-secret",JsonSerializer.Serialize(events));
    }
    [Fact]
    public async Task DenialMiddlewarePreserves403AndDoesNotRetainRequestSecrets()
    {
        await using var f=new Fixture();await f.Init();f.Http.Request.Method="POST";
        var middleware=new SecurityAuditMiddleware(ctx=>{ctx.Response.StatusCode=403;return Task.CompletedTask;});
        await middleware.InvokeAsync(f.Http,f.Audit);Assert.Equal(403,f.Http.Response.StatusCode);
        var row=await f.Db.AuditEvents.SingleAsync();Assert.Equal("security.access.check",row.EventCode);
        Assert.DoesNotContain("synthetic-token",JsonSerializer.Serialize(row));Assert.DoesNotContain("synthetic-search",JsonSerializer.Serialize(row));
    }
    [Fact]
    public async Task DisabledFeatureKeepsExistingNonSensitiveBehavior()
    {
        await using var f=new Fixture();await f.Init(enabled:false);var c=new OperatorsController(f.Db);var req=Request();
        Assert.IsType<OkObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Empty(await f.Db.AuditEvents.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivilegedIdentityCannotBeReboundToBypassGrantPolicy(bool enabled)
    {
        await using var f=new Fixture();await f.Init(enabled:enabled);
        (await f.Db.Operators.SingleAsync(x=>x.Id==2)).PagePermissionsJson="[\"admin.audit.read\"]";await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
        var c=new OperatorsController(f.Db);var req=new OperatorsController.OperatorRequest("QA2","QA Target",null,null,"rebound.account",null,"Viewer",true,["admin.audit.read"]);
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Equal(403,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.Equal("qa.target",(await f.Db.Operators.SingleAsync(x=>x.Id==2)).Username);
    }
    [Fact]
    public async Task DisabledCollectionCannotBeUsedToPersistSensitiveGrants()
    {
        await using var f=new Fixture();await f.Init(enabled:false,administratorId:1);var c=new OperatorsController(f.Db);
        var req=Request(permissions:["admin.audit.read"]);
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Equal(403,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.Null((await f.Db.Operators.SingleAsync(x=>x.Id==2)).PagePermissionsJson);
    }
    [Fact]
    public async Task NonexistentAllowlistedIdCannotBeClaimedByOrdinaryCreator()
    {
        await using var f=new Fixture();await f.Init(administratorId:3);var c=new OperatorsController(f.Db);
        var req=new OperatorsController.OperatorRequest("QA3","Created",null,null,"qa.created",null,"Editor",true,["analysis.spc"]);
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Create","POST",new(){["req"]=req},()=>c.Create(req)));Assert.Equal(403,result.StatusCode);
        f.Db.ChangeTracker.Clear();Assert.False(await f.Db.Operators.AnyAsync(x=>x.OperatorCode=="QA3"));
    }
    [Fact]
    public async Task SmtpSuccessHasIntentAndFinalEventWithoutSavingPasswordInEvidence()
    {
        await using var f=new Fixture();await f.Init();
        var c=new SettingsController(f.Config,f.Env,Mock.Of<IEmailNotificationService>(),NullLogger<SettingsController>.Instance,f.Db);
        var req=new SmtpSettingsDto{Host="changed.test",Password=""};
        Assert.IsType<OkObjectResult>(await f.Execute(c,"UpdateSmtpSettings","POST",new(){["dto"]=req},()=>c.UpdateSmtpSettings(req)));
        var events=await f.Db.AuditEvents.ToListAsync();Assert.Equal(2,events.Count);Assert.Single(events.Select(x=>x.CorrelationId).Distinct());
        Assert.Contains(events,x=>x.EventCode=="security.setting.prepare"&&x.Outcome=="Succeeded");
        Assert.Contains(events,x=>x.EventCode=="security.setting.change"&&x.Outcome=="Succeeded");
        Assert.DoesNotContain("synthetic-mail-secret",JsonSerializer.Serialize(events));Assert.DoesNotContain("changed.test",JsonSerializer.Serialize(events));
        Assert.Contains("synthetic-mail-secret",await File.ReadAllTextAsync(Path.Combine(f.Root,"appsettings.json")));
    }
    [Fact]
    public async Task StaleEditorClaimCannotManageAfterCurrentDatabaseRoleRevocation()
    {
        await using var f=new Fixture();await f.Init();(await f.Db.Operators.SingleAsync(x=>x.Id==1)).Role="Viewer";await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
        var c=new OperatorsController(f.Db);var req=Request();
        var result=Assert.IsType<ObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));Assert.Equal(403,result.StatusCode);
        Assert.Equal("Viewer",(await f.Db.AuditEvents.SingleAsync()).ActorRoleSnapshot);
    }

    private sealed class DirectoryHandler(string username="qa.target") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new[]{new OperatorDirectoryComparisonController.PortalUser(
                20,username,"QA2","Portal Updated","synthetic-directory-department","synthetic-directory@example.test",true,null,null)})});
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectorySyncUsesOuterTransactionAndFailureRollsBackAllChanges(bool fail)
    {
        await using var f=new Fixture();await f.Init();
        if(fail)await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailDirectory BEFORE INSERT ON AuditEvents WHEN NEW.Outcome='Succeeded' BEGIN SELECT RAISE(ABORT,'synthetic-directory-failure'); END;");
        using var client=new HttpClient(new DirectoryHandler());var factory=new Mock<IHttpClientFactory>();factory.Setup(x=>x.CreateClient(It.IsAny<string>())).Returns(client);
        var c=new OperatorDirectoryComparisonController(f.Db,factory.Object,f.Config,NullLogger<OperatorDirectoryComparisonController>.Instance);
        var req=new OperatorDirectoryComparisonController.SyncRequest([new(20,2)]);
        var result=await f.Execute(c,"Sync","POST",new(){["request"]=req,["cancellationToken"]=CancellationToken.None},()=>c.Sync(req,CancellationToken.None));
        if(fail)Assert.Equal(503,Assert.IsType<ObjectResult>(result).StatusCode);else Assert.IsType<OkObjectResult>(result);
        f.Db.ChangeTracker.Clear();Assert.Equal(fail?"QA Target":"Portal Updated",(await f.Db.Operators.SingleAsync(x=>x.Id==2)).OperatorName);
        var rows=await f.Db.AuditEvents.ToListAsync();Assert.Equal(fail?0:1,rows.Count(x=>x.Outcome=="Succeeded"));
        Assert.DoesNotContain("synthetic-directory",JsonSerializer.Serialize(rows));
    }
    [Fact]
    public async Task DirectoryCannotRebindProtectedAdministratorEvenThroughExplicitTargetId()
    {
        await using var f=new Fixture();await f.Init(administratorId:2);
        using var client=new HttpClient(new DirectoryHandler("takeover.account"));var factory=new Mock<IHttpClientFactory>();factory.Setup(x=>x.CreateClient(It.IsAny<string>())).Returns(client);
        var c=new OperatorDirectoryComparisonController(f.Db,factory.Object,f.Config,NullLogger<OperatorDirectoryComparisonController>.Instance);
        var req=new OperatorDirectoryComparisonController.SyncRequest([new(20,2)]);
        var result=await f.Execute(c,"Sync","POST",new(){["request"]=req,["cancellationToken"]=CancellationToken.None},()=>c.Sync(req,CancellationToken.None));
        Assert.Equal(403,Assert.IsType<ObjectResult>(result).StatusCode);f.Db.ChangeTracker.Clear();
        Assert.Equal("qa.target",(await f.Db.Operators.SingleAsync(x=>x.Id==2)).Username);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReportScheduleAndAuditCommitOrRollBackTogether(bool fail)
    {
        await using var f=new Fixture();await f.Init();
        if(fail)await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailSchedule BEFORE INSERT ON AuditEvents WHEN NEW.Outcome='Succeeded' BEGIN SELECT RAISE(ABORT,'synthetic-schedule-failure'); END;");
        var c=new SpcReportSchedulesController(f.Db,Mock.Of<ISpcOverviewReportBuilder>(),Mock.Of<IEmailNotificationService>());
        var req=new SpcReportSchedulesController.UpdateScheduleRequest("synthetic-schedule-department",[],true,1,true,1,"08:00",true);
        var result=await f.Execute(c,"Put","PUT",new(){["req"]=req},()=>c.Put(req));
        if(fail)Assert.Equal(503,Assert.IsType<ObjectResult>(result).StatusCode);else Assert.IsType<OkObjectResult>(result);
        f.Db.ChangeTracker.Clear();Assert.Equal(fail?0:1,await f.Db.SpcReportSchedules.CountAsync());
        var events=await f.Db.AuditEvents.ToListAsync();Assert.Equal(fail?0:1,events.Count(x=>x.Outcome=="Succeeded"));
        Assert.DoesNotContain("synthetic-schedule-department",JsonSerializer.Serialize(events));
    }
    [Fact]
    public async Task ModelRejectionIsRecordedWithoutRetainingValidationText()
    {
        await using var f=new Fixture();await f.Init();f.Http.Request.Method="PUT";var c=new OperatorsController(f.Db);
        var descriptor=new ControllerActionDescriptor {ControllerTypeInfo=typeof(OperatorsController).GetTypeInfo(),ControllerName="Operators",ActionName="Update",MethodInfo=typeof(OperatorsController).GetMethod("Update")!};
        var action=new ActionContext(f.Http,new RouteData(),descriptor);action.ModelState.AddModelError("Password","synthetic-invalid-secret");
        var result=new BadRequestObjectResult(new{message="invalid request"});
        await f.Filter.OnResultExecutionAsync(new ResultExecutingContext(action,[],result,c),()=>Task.FromResult(new ResultExecutedContext(action,[],result,c)));
        var row=await f.Db.AuditEvents.SingleAsync();Assert.Equal(400,row.HttpStatus);Assert.Equal("SECURITY_REQUEST_INVALID",row.ReasonCode);
        Assert.DoesNotContain("synthetic-invalid-secret",JsonSerializer.Serialize(row));
    }

    [Fact]
    public async Task MethodCaseCannotSkipSecurityWriteAudit()
    {
        await using var f=new Fixture();await f.Init();var c=new OperatorsController(f.Db);var req=Request();
        Assert.IsType<OkObjectResult>(await f.Execute(c,"Update","put",new(){["id"]=2,["req"]=req},()=>c.Update(2,req)));
        var events=await f.Db.AuditEvents.ToListAsync();Assert.NotEmpty(events);Assert.All(events,x=>Assert.Equal("PUT",x.HttpMethod));
    }
    [Fact]
    public async Task RevokedDatabaseEditorCannotInvokeSettingsSideEffectsWithStaleEditorClaim()
    {
        await using var f=new Fixture();await f.Init();var actor=await f.Db.Operators.SingleAsync(x=>x.Id==1);actor.Role="Viewer";actor.PagePermissionsJson="[\"admin.settings\"]";await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
        var mail=new Mock<IEmailNotificationService>();
        var c=new SettingsController(f.Config,f.Env,mail.Object,NullLogger<SettingsController>.Instance,f.Db);var req=new TestSmtpReq{RecipientEmail="synthetic-private@example.test"};
        var result=await f.Execute(c,"TestSmtp","POST",new(){["req"]=req},()=>c.TestSmtp(req));
        Assert.Equal(403,Assert.IsType<ObjectResult>(result).StatusCode);mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoopbackHttpPipelineAuthenticatesRejectsInvalidModelsAndCommitsAuditAtomically()
    {
        await using var f=new Fixture();await f.Init();
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ContentRootPath=f.Root,ApplicationName=typeof(OperatorsController).Assembly.FullName});
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(f.Config.AsEnumerable());
        builder.Services.AddSingleton(f.Options);builder.Services.AddScoped<AppDbContext>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<AuditPermissions>();builder.Services.AddScoped<AuditEventWriter>();builder.Services.AddScoped<AuthenticationAudit>();
        builder.Services.AddScoped<SecurityAudit>();builder.Services.AddScoped<SecurityAuditFilter>();
        builder.Services.AddControllers(options=>options.Filters.AddService<SecurityAuditFilter>()).AddApplicationPart(typeof(OperatorsController).Assembly);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>options.TokenValidationParameters=new(){
            ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(f.Config["Auth:JwtKey"]!)),ValidateIssuer=false,ValidateAudience=false,ClockSkew=TimeSpan.Zero});
        builder.Services.AddAuthorization();
        await using var app=builder.Build();app.UseRouting();app.UseMiddleware<SecurityAuditMiddleware>();app.UseAuthentication();app.UseAuthorization();app.UseMiddleware<ViewerWriteGuardMiddleware>();app.MapControllers().RequireAuthorization();
        await app.StartAsync();
        try
        {
            var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var uri=new Uri(address);Assert.True(IPAddress.IsLoopback(IPAddress.Parse(uri.Host)));
            using var client=new HttpClient{BaseAddress=uri};
            string Token(int id,string role)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims:[new(ClaimTypes.NameIdentifier,id.ToString()),new(ClaimTypes.Role,role),new("jti","loopback-synthetic-session")],expires:DateTime.UtcNow.AddMinutes(1),signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(f.Config["Auth:JwtKey"]!)),SecurityAlgorithms.HmacSha256)));
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/operators")).StatusCode);
            client.DefaultRequestHeaders.Authorization=new("Bearer",Token(2,"Viewer"));
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/operators",Request())).StatusCode);
            client.DefaultRequestHeaders.Authorization=new("Bearer",Token(1,"Editor"));
            using var invalid=new StringContent("{\"operatorCode\":",Encoding.UTF8,"application/json");
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/v1/operators",invalid)).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync("/api/v1/operators/2",Request())).StatusCode);
            var events=await f.Db.AuditEvents.AsNoTracking().ToListAsync();
            Assert.Contains(events,x=>x.HttpStatus==401&&x.ActorType=="Anonymous");Assert.Contains(events,x=>x.HttpStatus==403&&x.ActorOperatorId==2);
            Assert.Contains(events,x=>x.HttpStatus==400&&x.ReasonCode=="SECURITY_REQUEST_INVALID");Assert.Equal(4,events.Count(x=>x.Outcome=="Succeeded"));
            Assert.All(events,x=>Assert.Equal("Test",x.Environment));
        }
        finally{await app.StopAsync();}
    }

    [AuditSqlFact]
    public async Task IsolatedSqlRuntimeCommitsSecurityEvidenceAndRollsBackAuditFailure()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        var managementSettings=Environment.GetEnvironmentVariable("SPC_AUDIT_SQL_MANAGEMENT_SETTINGS");
        if(!string.IsNullOrWhiteSpace(managementSettings))
        {
            var privatePath=Path.GetFullPath(managementSettings);
            if(!privatePath.StartsWith(Path.GetFullPath("C:/ProgramData/PMR-Releases/Test/"),StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Management settings must be an existing protected test release record.");
        }
        var config=new ConfigurationBuilder().AddJsonFile(string.IsNullOrWhiteSpace(managementSettings)
            ? Path.Combine(root,"release/test/backend/appsettings.json") : managementSettings).Build();
        var management=new SqlConnectionStringBuilder(config.GetConnectionString("SqlServer"));
        Assert.Equal("test",config["AppEnvironment"]);Assert.Equal("PMR_SPC_TEST",management.InitialCatalog);Assert.Equal("172.16.110.16",management.DataSource);
        // Separate management identity. Never give the deployed runtime DDL or elevate its permissions.
        if(string.IsNullOrWhiteSpace(managementSettings)){management.IntegratedSecurity=true;management.UserID="";management.Password="";}
        management.Pooling=false;management.InitialCatalog="master";
        var name="SPC_AUDIT_SECURITY_VERIFY_"+Guid.NewGuid().ToString("N");
        Assert.Matches("^SPC_AUDIT_SECURITY_VERIFY_[a-f0-9]{32}$",name);
        await using var master=new SqlConnection(management.ConnectionString);await master.OpenAsync();
        await using(var command=master.CreateCommand()){command.CommandText="CREATE DATABASE ["+name+"]";await command.ExecuteNonQueryAsync();}
        management.InitialCatalog=name;
        try
        {
            await using var admin=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(management.ConnectionString).Options);
            var assembly=admin.GetService<IMigrationsAssembly>();const string prior="20261020052530_AddChemicalDirectWriterProtocol";
            var migration=assembly.CreateMigration(assembly.Migrations[prior],admin.Database.ProviderName!);
            var model=admin.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel,designTime:true);
            var operations=admin.GetService<IMigrationsModelDiffer>().GetDifferences(null,model.GetRelationalModel());
            foreach(var cmd in admin.GetService<IMigrationsSqlGenerator>().Generate(operations,model))await admin.Database.ExecuteSqlRawAsync(cmd.CommandText);
            var history=admin.GetService<IHistoryRepository>();await admin.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            foreach(var id in assembly.Migrations.Keys.Where(x=>string.CompareOrdinal(x,prior)<=0))await admin.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id,"10.0.9")));
            await admin.Database.MigrateAsync();
            var actor=new Operator{OperatorCode="QA1",OperatorName="QA Actor",Username="qa.actor",Role="Editor",IsActive=true};
            var target=new Operator{OperatorCode="QA2",OperatorName="QA Target",Username="qa.target",Role="Viewer",IsActive=true};
            admin.Operators.AddRange(actor,target);await admin.SaveChangesAsync();Assert.Equal(1,actor.Id);Assert.Equal(2,target.Id);
            await admin.Database.ExecuteSqlRawAsync("CREATE USER AuditSecurityRuntimeQa WITHOUT LOGIN; ALTER ROLE db_datareader ADD MEMBER AuditSecurityRuntimeQa; ALTER ROLE db_datawriter ADD MEMBER AuditSecurityRuntimeQa; ALTER ROLE SpcAuditWriter ADD MEMBER AuditSecurityRuntimeQa;");
            await using var runtime=new SqlConnection(management.ConnectionString);await runtime.OpenAsync();
            await using(var command=runtime.CreateCommand()){command.CommandText="EXECUTE AS USER=N'AuditSecurityRuntimeQa'";await command.ExecuteNonQueryAsync();}
            await using var f=new Fixture();await f.Init(sqlRuntime:runtime);
            await AuthenticationAuditStartup.ValidateDatabaseAsync(f.Db);
            var c=new OperatorsController(f.Db);var request=Request();
            Assert.IsType<OkObjectResult>(await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=request},()=>c.Update(2,request)));
            Assert.Equal(4,await admin.AuditEvents.CountAsync(x=>x.Outcome=="Succeeded"));
            await admin.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.FailSecurityAudit ON dbo.AuditEvents AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Outcome=N'Succeeded') THROW 51308,'isolated security audit failure',1; END;");
            var rejected=Request(role:"Viewer",active:true,permissions:[]);
            var result=await f.Execute(c,"Update","PUT",new(){["id"]=2,["req"]=rejected},()=>c.Update(2,rejected));
            Assert.Equal(503,Assert.IsType<ObjectResult>(result).StatusCode);
            admin.ChangeTracker.Clear();var saved=await admin.Operators.SingleAsync(x=>x.Id==2);Assert.Equal("Editor",saved.Role);Assert.False(saved.IsActive);
            Assert.Equal(4,await admin.AuditEvents.CountAsync(x=>x.Outcome=="Succeeded"));
            await Assert.ThrowsAsync<SqlException>(()=>f.Db.Database.ExecuteSqlRawAsync("UPDATE dbo.AuditEvents SET Outcome=N'Denied'"));
            await Assert.ThrowsAsync<SqlException>(()=>f.Db.Database.ExecuteSqlRawAsync("DELETE dbo.AuditEvents"));
        }
        finally
        {
            Assert.Matches("^SPC_AUDIT_SECURITY_VERIFY_[a-f0-9]{32}$",name);
            await using var cleanup=master.CreateCommand();cleanup.CommandText="ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]";await cleanup.ExecuteNonQueryAsync();
        }
    }
}
