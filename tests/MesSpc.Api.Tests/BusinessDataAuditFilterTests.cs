using System.Reflection;
using System.Security.Claims;
using System.Data.Common;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
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

    [Fact]
    public async Task PpcFormulaUpdateReusesBusinessAuditTransaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var process = new Process { ProcessCode = "N2", ProcessName = "化鎳線(N2)", ControlScope = "CHEM" };
        var characteristic = new QualityCharacteristic
        {
            CharacteristicCode = "CHAR-000851", CharacteristicName = "PH", ControlScope = "CHEM",
            InputMode = "DIRECT", DecimalPlaces = 2
        };
        var chartGroup = new ControlChartGroup
        {
            GroupCode = "CHEM_TREND", GroupName = "藥液趨勢圖", GroupType = "TREND_CHART",
            BusinessScopeCode = "CHEM", IsEnabled = true
        };
        var actor = new Operator { Id = 7, OperatorCode = "AUDIT", OperatorName = "Audit QA", Username = "audit.qa", Role = "Editor" };
        db.AddRange(process, characteristic, chartGroup, actor);
        await db.SaveChangesAsync();
        var item = new PartProcessCharacteristic
        {
            ControlScope = "CHEM", ProcessId = process.Id, CharacteristicId = characteristic.Id,
            SequenceNo = 1, LSL = 9.5, USL = 11, TargetValue = 10.25, SampleSize = 1,
            DisplayMode = "TREND_CHART", ChemicalAnalysisConfigJson = """{"enabled":true,"concentrationFormula":"Primary * 1","adjustmentFormula":"IF(Concentration>U11,\"更槽\",\"\")","decimalPlaces":2}""",
            IsRequired = true, IsEnabled = true
        };
        db.PartProcessCharacteristics.Add(item);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

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
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "7"), new(ClaimTypes.Name, "audit.qa"), new("jti", "ppc-formula")], "test"));
        http.Request.Method = "PUT";
        http.Request.RouteValues["id"] = item.Id;
        var controller = new PartProcessCharacteristicsController(db, new ChemicalAnalysisFormulaVersionService(db))
        { ControllerContext = new ControllerContext { HttpContext = http } };
        var request = new PartProcessCharacteristic
        {
            ControlScope = item.ControlScope, ProcessId = item.ProcessId, CharacteristicId = item.CharacteristicId,
            SequenceNo = item.SequenceNo, LSL = item.LSL, USL = item.USL, TargetValue = item.TargetValue,
            SampleSize = item.SampleSize, DisplayMode = item.DisplayMode,
            ChemicalAnalysisConfigJson = """{"enabled":true,"concentrationFormula":"Primary * 1","adjustmentFormula":"IF(Concentration>USL,\"更槽\",\"\")","decimalPlaces":2}""",
            ChemicalAnalysisFormulaVersionReason = "修正 Excel 儲存格引用", IsRequired = true, IsEnabled = true
        };
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(PartProcessCharacteristicsController).GetTypeInfo(), ControllerName = "PartProcessCharacteristics",
            ActionName = "Update", MethodInfo = typeof(PartProcessCharacteristicsController).GetMethod("Update")!
        };
        var action = new ActionContext(http, new RouteData(), descriptor);
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?> { ["id"] = item.Id, ["req"] = request }, controller);

        await filter.OnActionExecutionAsync(executing, async () =>
            new ActionExecutedContext(action, [], controller) { Result = await controller.Update(item.Id, request) });

        Assert.Null(executing.Result);
        db.ChangeTracker.Clear();
        Assert.Contains("USL", (await db.PartProcessCharacteristics.SingleAsync(x => x.Id == item.Id)).ChemicalAnalysisConfigJson);
        var version = await db.ChemicalAnalysisFormulaVersions.SingleAsync();
        Assert.Equal("修正 Excel 儲存格引用", version.Reason);
        Assert.Equal("masterdata.record.update", (await db.AuditEvents.SingleAsync()).EventCode);
    }
}
