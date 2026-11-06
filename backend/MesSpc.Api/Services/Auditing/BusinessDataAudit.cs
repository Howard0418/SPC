using System.Data;
using System.Security.Claims;
using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Maps only approved high-risk writes to fixed audit event codes; request bodies are never inspected.</summary>
public static class BusinessDataAuditPolicy
{
    public sealed record Operation(string EventCode, string PageKey, string TargetType, string ChangedField);

    public static Operation? Resolve(Type controller, string action, string method) => (controller, action, method.ToUpperInvariant()) switch
    {
        (var c, "Create", "POST") when c == typeof(MeasurementBatchesController) => new("measurement.record.create", "measurement.entry", "MeasurementBatch", "Create"),
        (var c, "ImportCsv", "POST") when c == typeof(MeasurementImportController) => new("import.batch.confirm", "data.upload", "MeasurementBatch", "Confirm"),
        (var c, "Create", "POST") when c == typeof(ManualMeasurementsV1Controller) => new("measurement.record.create", "measurement.entry", "ChemicalDailyBatch", "Create"),
        (var c, "Update", "PUT") when c == typeof(ManualMeasurementsV1Controller) => new("measurement.record.update", "measurement.entry", "ChemicalDailyBatch", "Update"),
        (var c, "UpdateItem", "PUT") when c == typeof(ManualMeasurementsV1Controller) => new("measurement.record.update", "measurement.entry", "ChemicalMeasurement", "Update"),
        (var c, "DeleteItem", "DELETE") when c == typeof(ManualMeasurementsV1Controller) => new("measurement.record.delete", "measurement.entry", "ChemicalMeasurement", "Delete"),
        (var c, "Confirm", "POST") when c == typeof(ChemicalPortalMeasurementsController) => new("measurement.record.create", "measurement.entry", "ChemicalDailyBatch", "Confirm"),
        (var c, "Publish", "POST") when c == typeof(ChemicalFormulaScheduleController) => new("configuration.formula.update", "chemical.analysis.overview", "ChemicalFormulaSchedule", "Publish"),
        (var c, "Apply", "POST") when c == typeof(ChemicalFTableController) => new("configuration.ftable.apply", "chemical.ftable", "ChemicalFTable", "Apply"),
        (var c, "RestoreVersion", "POST") when c == typeof(ChemicalFTableController) => new("configuration.ftable.restore", "chemical.ftable", "ChemicalFTableVersion", "Restore"),
        (var c, "Confirm", "POST") when c == typeof(UploadsController) => new("import.batch.confirm", "data.upload", "UploadBatch", "Confirm"),
        (var c, "Delete", "DELETE") when c == typeof(UploadsController) => new("import.batch.delete", "data.upload", "UploadBatch", "Delete"),
        (var c, "Confirm", "POST") when c == typeof(EtchTestImportController) => new("import.batch.confirm", "analysis.etch", "EtchReport", "Confirm"),
        (var c, "UpdateRules", "PUT") when IsMaster(c) => new("configuration.rule.change", MasterPage(c), c.Name.Replace("Controller", "", StringComparison.Ordinal), "UpdateRules"),
        (var c, _, "POST") when IsMaster(c) => new("masterdata.record.create", MasterPage(c), c.Name.Replace("Controller", "", StringComparison.Ordinal), "Create"),
        (var c, _, "PUT") when IsMaster(c) => new("masterdata.record.update", MasterPage(c), c.Name.Replace("Controller", "", StringComparison.Ordinal), "Update"),
        (var c, "Delete", "DELETE") when IsMaster(c) => new("masterdata.record.delete", MasterPage(c), c.Name.Replace("Controller", "", StringComparison.Ordinal), "Delete"),
        _ => null
    };

    private static bool IsMaster(Type type) => type == typeof(PartsController) || type == typeof(ProcessesController) ||
        type == typeof(MachinesController) || type == typeof(QualityCharacteristicsController) || type == typeof(PartProcessCharacteristicsController) ||
        type == typeof(ControlChartGroupsController) || type == typeof(ControlChartTypesController) || type == typeof(SpcRuleGroupsController) || type == typeof(SpcRulesController);

    private static string MasterPage(Type type) => type == typeof(PartsController) ? "master.parts" : type == typeof(ProcessesController) ? "master.processes" :
        type == typeof(QualityCharacteristicsController) ? "master.characteristics" : type == typeof(PartProcessCharacteristicsController) ? "master.ppc" : "spc.rule.groups";

    public static bool HasWriteIntent(ControllerActionDescriptor descriptor, IDictionary<string, object?> arguments)
        => descriptor.ControllerTypeInfo.AsType() != typeof(ChemicalFTableController) || descriptor.ActionName != "Apply" ||
           arguments.TryGetValue("req", out var request) && request is ChemicalFTableController.ChemicalFTableApplyRequest { Apply: true };
}

/// <summary>Fails closed for approved business writes when audit collection is enabled, committing data and evidence together.</summary>
public sealed class BusinessDataAuditFilter(AppDbContext db, AuditEventWriter writer, AuthenticationAudit authentication,
    AuditPermissions permissions, IConfiguration configuration, ILogger<BusinessDataAuditFilter> logger) : IAsyncActionFilter, IOrderedFilter
{
    public int Order => -900;
    private bool Enabled => configuration.GetValue<bool>("Audit:BusinessDataEnabled");

    public static void ValidateConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Audit:BusinessDataEnabled")) return;
        if (!configuration.GetValue<bool>("Audit:AuthenticationEnabled")) throw new InvalidOperationException("BUSINESS_AUDIT_AUTH_REQUIRED");
        AuthenticationAuditStartup.ValidateConfiguration(configuration);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Enabled || !db.Database.IsRelational()) { await next(); return; }
        var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var operation = BusinessDataAuditPolicy.Resolve(context.Controller.GetType(), descriptor.ActionName, context.HttpContext.Request.Method);
        if (operation is null || !BusinessDataAuditPolicy.HasWriteIntent(descriptor, context.ActionArguments)) { await next(); return; }

        var http = context.HttpContext;
        var actor = await permissions.CurrentAsync(http.User, http.RequestAborted);
        if (actor is null) { context.Result = Unavailable(); return; }
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, http.RequestAborted);
        try
        {
            var executed = await next();
            var status = Status(executed.Result);
            if (status is < 200 or >= 300)
            {
                await transaction.RollbackAsync(http.RequestAborted);
                return;
            }

            await writer.StageAsync(new AuditEventDraft(operation.EventCode, "Succeeded")
            {
                PageKey = operation.PageKey,
                TargetType = operation.TargetType,
                TargetId = TargetId(http, descriptor.ActionName),
                HttpMethod = http.Request.Method.ToUpperInvariant(),
                HttpStatus = status,
                CorrelationId = http.TraceIdentifier,
                TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
                Details = new Dictionary<string, object?> { ["ChangedFields"] = new[] { operation.ChangedField } }
            }, AuditActor.User(actor.Id, "Jwt", authentication.SessionHash(http.User.FindFirstValue("jti"))), http.RequestAborted);
            await db.SaveChangesAsync(http.RequestAborted);
            await transaction.CommitAsync(http.RequestAborted);
        }
        catch (Exception ex) when (ex is AuditEventValidationException or DbUpdateException or InvalidOperationException)
        {
            await transaction.RollbackAsync(http.RequestAborted);
            logger.LogError(ex, "BUSINESS_AUDIT_TRANSACTION_FAILED; action {Action}; trace {TraceId}", descriptor.ActionName, http.TraceIdentifier);
            context.Result = Unavailable();
        }
    }

    private static int Status(IActionResult? result) => result is IStatusCodeActionResult { StatusCode: { } status } ? status : 200;
    private static string TargetId(HttpContext http, string action) => http.Request.RouteValues.TryGetValue("id", out var id) ? $"{action}:{id}" :
        http.Request.RouteValues.TryGetValue("ppcId", out var ppc) ? $"{action}:{ppc}" :
        http.Request.RouteValues.TryGetValue("uploadBatchId", out var batch) ? $"{action}:{batch}" : action;
    private static ObjectResult Unavailable() => new(new { success = false, data = (object?)null, code = "BUSINESS_AUDIT_UNAVAILABLE", message = "無法安全保存操作紀錄，變更未完成；請稍後重試。" }) { StatusCode = 503 };
}
