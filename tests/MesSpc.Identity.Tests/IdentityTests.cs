using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class IdentityTests
{
    const string Key="identity-regression-key-at-least-32-characters-long";
    static IConfiguration Config=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Auth:PortalSsoKey"]=Key,["Auth:JwtKey"]=Key}).Build();
    static AuthController.PortalSsoRequest Request(string username,string? code="P123",bool legacy=false,string? department=null)
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var nonce=Guid.NewGuid().ToString("N");
        var normalized=username.Trim().Split('\\').Last().Split('@')[0].ToLowerInvariant();
        var text=legacy?$"{normalized}|Test|{now}|{nonce}":$"{normalized}|Test|{code}|{department}||{now}|{nonce}";
        using var hmac=new HMACSHA256(Encoding.UTF8.GetBytes(Key));
        return new(username,"Test",now,nonce,Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text))),code,department);
    }
    static int Id(IActionResult result){var ok=Assert.IsType<OkObjectResult>(result);using var json=JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));return json.RootElement.GetProperty("user").GetProperty("id").GetInt32();}
    [Theory][InlineData(null)][InlineData("P123")]
    public async Task PrecreatedEmployeeAndAd_ReuseIdAndPermissions(string? storedName)
    {
        await using var f=await Fixture.Create();var row=new Operator{OperatorCode="P123",Username=storedName,OperatorName="Test",Role="Editor",PagePermissionsJson="[\"equipment.status\"]"};f.Db.Operators.Add(row);await f.Db.SaveChangesAsync();var original=row.Id;
        Assert.Equal(original,Id(await f.Controller.PortalSso(Request("PMR\\QA.User"))));
        Assert.Equal(original,Id(await f.Controller.PortalSso(Request("P123"))));
        Assert.Equal(original,Id(await f.Controller.PortalSso(Request("qa.user@pmr.example"))));
        Assert.Single(f.Db.Operators);Assert.Equal("qa.user",row.Username);Assert.Equal("Editor",row.Role);Assert.Equal("[\"equipment.status\"]",row.PagePermissionsJson);
    }
    [Fact]public async Task NewIdentity_AdThenEmployee_IsOneUser()
    {await using var f=await Fixture.Create();var id=Id(await f.Controller.PortalSso(Request("qa.user")));Assert.Equal(id,Id(await f.Controller.PortalSso(Request("P123"))));Assert.Single(f.Db.Operators);}
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DisabledOrDeletedEmployee_CannotBeRecreated(bool deleted)
    {await using var f=await Fixture.Create();f.Db.Operators.Add(new(){OperatorCode="P123",OperatorName="Test",IsActive=deleted,IsDeleted=deleted});await f.Db.SaveChangesAsync();Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(Request("qa.user")));Assert.Single(f.Db.Operators.IgnoreQueryFilters());}
    [Fact]public async Task ConflictingAdAndLegacyRetry_DoNotCreateAnotherUser()
    {await using var f=await Fixture.Create();f.Db.Operators.Add(new(){OperatorCode="P123",Username="another.person",OperatorName="Test"});await f.Db.SaveChangesAsync();Assert.IsType<ConflictObjectResult>(await f.Controller.PortalSso(Request("qa.user")));Assert.IsType<ConflictObjectResult>(await f.Controller.PortalSso(Request("qa.user",null,true)));Assert.Single(f.Db.Operators);}
    [Fact]public async Task LegacyUnsignedEmployeeCode_CannotClaimPrecreatedPermissions()
    {await using var f=await Fixture.Create();f.Db.Operators.Add(new(){OperatorCode="P123",OperatorName="Test",Role="Editor"});await f.Db.SaveChangesAsync();Assert.IsType<ConflictObjectResult>(await f.Controller.PortalSso(Request("qa.user","P123",true)));Assert.Null((await f.Db.Operators.SingleAsync()).Username);}
    [Fact]public async Task ExistingLegacyIdentity_WorksButUnsignedProfileIgnored()
    {await using var f=await Fixture.Create();var row=new Operator{OperatorCode="P123",Username="qa.user",OperatorName="Test",Department="Original"};f.Db.Operators.Add(row);await f.Db.SaveChangesAsync();Assert.Equal(row.Id,Id(await f.Controller.PortalSso(Request("qa.user","P999",true,"Tampered"))));Assert.Equal("P123",row.OperatorCode);Assert.Equal("Original",row.Department);}
    [Fact]public async Task MissingEmployeeCode_DoesNotReplaceExistingCode()
    {await using var f=await Fixture.Create();var id=Id(await f.Controller.PortalSso(Request("qa.user")));Assert.Equal(id,Id(await f.Controller.PortalSso(Request("QA.USER",null))));Assert.Equal("P123",(await f.Db.Operators.SingleAsync()).OperatorCode);}
    [Fact]public async Task BadSignatureAndReplay_AreRejected()
    {await using var f=await Fixture.Create();var req=Request("qa.user");Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(req with {Signature="bad"}));Id(await f.Controller.PortalSso(req));Assert.IsType<UnauthorizedObjectResult>(await f.Controller.PortalSso(req));Assert.Single(f.Db.Operators);}
    [Fact]public async Task ConflictingRows_AreNotSilentlyMerged()
    {await using var f=await Fixture.Create();f.Db.Operators.AddRange(new Operator{OperatorCode="P123",OperatorName="Original",Role="Editor"},new Operator{OperatorCode="QA.USER",Username="qa.user",OperatorName="Test",Role="Viewer"});await f.Db.SaveChangesAsync();Assert.IsType<ConflictObjectResult>(await f.Controller.PortalSso(Request("qa.user")));Assert.Equal(2,await f.Db.Operators.CountAsync());}
    [Fact]public async Task ConcurrentFirstLogin_OnlyOneIdentity()
    {await using var f=await Fixture.Create();await using var other=new AppDbContext(f.Options);var results=await Task.WhenAll(f.Controller.PortalSso(Request("qa.user")),new AuthController(Config,other).PortalSso(Request("qa.user")));Assert.Equal(Id(results[0]),Id(results[1]));Assert.Single(await f.Db.Operators.ToListAsync());}
    sealed class Fixture:IAsyncDisposable
    {
        readonly SqliteConnection connection;public AppDbContext Db {get;}public DbContextOptions<AppDbContext> Options {get;}public AuthController Controller{get;}
        Fixture(SqliteConnection c,DbContextOptions<AppDbContext> options){connection=c;Options=options;Db=new(options);Controller=new(Config,Db);}
        public static async Task<Fixture>Create(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();var f=new Fixture(c,new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await f.Db.Database.EnsureCreatedAsync();return f;}
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
}
