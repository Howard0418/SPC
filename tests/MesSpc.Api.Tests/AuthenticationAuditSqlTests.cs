using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class AuthenticationAuditSqlTests
{
    [AuditSqlFact]
    public async Task RestrictedRuntimeAuthenticatesAndFailsClosedWithoutDdlOrAuditMutation()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        using var settings=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,"release/test/backend/appsettings.json")));
        var admin=new SqlConnectionStringBuilder(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString());
        Assert.Equal("PMR_SPC_TEST",admin.InitialCatalog);admin.Pooling=false;admin.InitialCatalog="master";
        var name="SPC_AUDIT_AUTH_VERIFY_"+Guid.NewGuid().ToString("N");
        await using var master=new SqlConnection(admin.ConnectionString);await master.OpenAsync();
        await using(var create=master.CreateCommand()){create.CommandText="CREATE DATABASE ["+name+"]";await create.ExecuteNonQueryAsync();}
        admin.InitialCatalog=name;
        try
        {
            await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(admin.ConnectionString).Options);
            var assembly=db.GetService<IMigrationsAssembly>();const string prior="20261020052530_AddChemicalDirectWriterProtocol";
            var migration=assembly.CreateMigration(assembly.Migrations[prior],db.Database.ProviderName!);
            var model=db.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel,designTime:true);
            var operations=db.GetService<IMigrationsModelDiffer>().GetDifferences(null,model.GetRelationalModel());
            foreach(var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(operations,model))await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            var history=db.GetService<IHistoryRepository>();await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            foreach(var id in assembly.Migrations.Keys.Where(x=>string.CompareOrdinal(x,prior)<=0))await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id,"10.0.9")));
            await db.Database.MigrateAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(()=>AuthenticationAuditStartup.ValidateDatabaseAsync(db));
            await db.Database.ExecuteSqlRawAsync("CREATE USER AuditAuthRuntimeQa WITHOUT LOGIN; ALTER ROLE db_datareader ADD MEMBER AuditAuthRuntimeQa; ALTER ROLE db_datawriter ADD MEMBER AuditAuthRuntimeQa; ALTER ROLE SpcAuditWriter ADD MEMBER AuditAuthRuntimeQa; GRANT EXECUTE ON SCHEMA::dbo TO AuditAuthRuntimeQa;");
            await using var runtime=new SqlConnection(admin.ConnectionString);await runtime.OpenAsync();
            await using(var impersonate=runtime.CreateCommand()){impersonate.CommandText="EXECUTE AS USER=N'AuditAuthRuntimeQa'";await impersonate.ExecuteNonQueryAsync();}
            var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(runtime).Options;
            await using var context=new AppDbContext(options);
            await AuthenticationAuditStartup.ValidateDatabaseAsync(context);
            const string key="auth-sql-test-key-with-at-least-32-characters";
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
                ["Auth:Enabled"]="true",["Auth:JwtKey"]=key,["Auth:PortalSsoKey"]=key,["AppEnvironment"]="test",
                ["Audit:AuthenticationEnabled"]="true",["Audit:AttemptedAccountHmacKey"]=key,["Database:InitializeOnStartup"]="false"
            }).Build();
            var audit=new AuthenticationAudit(new AuditEventWriter(context,options,config,TimeProvider.System,NullLogger<AuditEventWriter>.Instance),config,NullLogger<AuthenticationAudit>.Instance);
            var controller=new AuthController(config,context,audit){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
            AuthController.PortalSsoRequest Request(string account){var time=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var nonce=Guid.NewGuid().ToString("N");var payload=$"{account}|QA|{account}|||{time}|{nonce}";return new(account,"QA",time,nonce,Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key),Encoding.UTF8.GetBytes(payload))),account);}
            Assert.IsType<OkObjectResult>(await controller.PortalSso(Request("qa.first")));
            Assert.Equal(1,await db.Operators.CountAsync());Assert.Equal(1,await db.AuditEvents.CountAsync());
            foreach(var sql in new[]{"UPDATE dbo.AuditEvents SET Outcome=N'Denied'","DELETE dbo.AuditEvents","ALTER TABLE dbo.AuditEvents ADD Evil int NULL","CREATE TABLE dbo.MustNotCreate(Id int)"})
                await Assert.ThrowsAsync<SqlException>(()=>context.Database.ExecuteSqlRawAsync(sql));
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.TR_AuthAudit_Fail ON dbo.AuditEvents AFTER INSERT AS BEGIN THROW 51305,'isolated auth audit failure',1; END;");
            var failure=Assert.IsType<ObjectResult>(await controller.PortalSso(Request("qa.rollback")));Assert.Equal(503,failure.StatusCode);
            Assert.Equal(1,await db.Operators.CountAsync());Assert.Equal(1,await db.AuditEvents.CountAsync());
        }
        finally
        {
            if(!System.Text.RegularExpressions.Regex.IsMatch(name,"^SPC_AUDIT_AUTH_VERIFY_[a-f0-9]{32}$"))throw new InvalidOperationException("Isolation guard failed");
            await using var cleanup=master.CreateCommand();cleanup.CommandText="ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]";await cleanup.ExecuteNonQueryAsync();
        }
    }
}
