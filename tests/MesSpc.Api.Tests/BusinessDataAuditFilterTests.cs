using System.Reflection;
using System.Security.Claims;
using System.Data.Common;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class BusinessDataAuditFilterTests
{
    private sealed class OperatorReadCounter : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("Operators", StringComparison.OrdinalIgnoreCase)) Count++;
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task SuccessfulWritePersistsBusinessDataAndAuditInOneSaveChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var operatorReads = new OperatorReadCounter();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).AddInterceptors(operatorReads).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Operators.Add(new Operator { Id = 7, OperatorCode = "AUDIT", OperatorName = "Audit QA", Username = "audit.qa", Role = "Editor" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        operatorReads.Count = 0;

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppEnvironment"] = "test", ["Auth:Enabled"] = "true", ["Audit:AuthenticationEnabled"] = "true",
            ["Audit:BusinessDataEnabled"] = "true", ["Audit:AttemptedAccountHmacKey"] = new string('k', 32)
        }).Build();
        var writer = new AuditEventWriter(db, options, config, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
        var authentication = new AuthenticationAudit(writer, config, NullLogger<AuthenticationAudit>.Instance);
        var filter = new BusinessDataAuditFilter(db, writer, authentication, new AuditPermissions(db, config), config,
            NullLogger<BusinessDataAuditFilter>.Instance);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "7"), new("jti", "business-test")], "test"));
        http.Request.Method = "POST";
        var controller = new PartsController(db) { ControllerContext = new ControllerContext { HttpContext = http } };
        var request = new Part { PartNo = "AUDIT-ONE-SAVE", PartName = "Audit one save", IsEnabled = true };
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(PartsController).GetTypeInfo(), ControllerName = "Parts", ActionName = "Create",
            MethodInfo = typeof(PartsController).GetMethod("Create")!
        };
        var action = new ActionContext(http, new RouteData(), descriptor);
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?> { ["req"] = request }, controller);
        var saveCount = 0;
        db.SavingChanges += (_, _) => saveCount++;

        await filter.OnActionExecutionAsync(executing, async () =>
            new ActionExecutedContext(action, [], controller) { Result = await controller.Create(request) });

        Assert.Equal(1, saveCount);
        Assert.Equal(1, operatorReads.Count);
        Assert.Equal(1, await db.Parts.CountAsync(x => x.PartNo == "AUDIT-ONE-SAVE"));
        var evidence = await db.AuditEvents.SingleAsync();
        Assert.Equal("masterdata.record.create", evidence.EventCode);
        Assert.Equal(7, evidence.ActorOperatorId);
        Assert.Equal(200, evidence.HttpStatus);

        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailBusinessAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic audit failure'); END;");
        var failedRequest = new Part { PartNo = "AUDIT-MUST-ROLLBACK", PartName = "Must rollback", IsEnabled = true };
        var failedController = new PartsController(db) { ControllerContext = new ControllerContext { HttpContext = http } };
        var failedExecuting = new ActionExecutingContext(action, [], new Dictionary<string, object?> { ["req"] = failedRequest }, failedController);
        await filter.OnActionExecutionAsync(failedExecuting, async () =>
            new ActionExecutedContext(action, [], failedController) { Result = await failedController.Create(failedRequest) });

        Assert.IsType<ObjectResult>(failedExecuting.Result);
        Assert.Equal(503, ((ObjectResult)failedExecuting.Result!).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Parts.CountAsync(x => x.PartNo == "AUDIT-MUST-ROLLBACK"));
        Assert.Single(await db.AuditEvents.ToListAsync());
    }
}
