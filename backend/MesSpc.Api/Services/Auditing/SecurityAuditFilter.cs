using System.Data;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Encloses security writes and success evidence in one relational transaction.</summary>
public sealed class SecurityAuditFilter(AppDbContext db, AuditPermissions permissions, SecurityAudit audit,
    IWebHostEnvironment environment, ILogger<SecurityAuditFilter> logger) : IAsyncActionFilter, IAsyncAlwaysRunResultFilter, IOrderedFilter
{
    private static readonly SemaphoreSlim SettingsFileGate = new(1, 1);
    public int Order => -1000;
    private static string? Page(Type type) => type == typeof(OperatorsController) || type == typeof(OperatorDirectoryComparisonController) ? "admin.operators"
        : type == typeof(SettingsController) ? "settings.smtp" : type == typeof(SpcReportSchedulesController) ? "settings.spc_reports" : null;
    private static bool Mutation(Type type, string method, string action) => type == typeof(OperatorsController) && method.ToUpperInvariant() is "POST" or "PUT" or "DELETE"
        || type == typeof(OperatorDirectoryComparisonController) && action == "Sync"
        || type == typeof(SettingsController) && action == "UpdateSmtpSettings"
        || type == typeof(SpcReportSchedulesController) && action == "Put";
    private static ObjectResult Error(int status, string code, string message) => new(new { success = false, data = (object?)null, code, message }) { StatusCode = status };
    private static ObjectResult Unavailable() => Error(503, "SECURITY_AUDIT_UNAVAILABLE", "無法安全保存操作紀錄，變更未完成；請稍後重試。");

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var type = context.Controller.GetType(); var page = Page(type);
        if (page is null) { await next(); return; }
        if (!audit.Enabled)
        {
            // Turning collection off is not an alternate path for granting or taking over sensitive access.
            if (await RejectSensitiveWhileDisabledAsync(context)) return;
            await next(); return;
        }
        var http = context.HttpContext; var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var mutation = Mutation(type, http.Request.Method, descriptor.ActionName);
        var writeRequest = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method);
        var required = page == "admin.operators" ? "admin.operators" : "admin.settings";
        IDbContextTransaction? transaction = null; bool fileGate = false; byte[]? beforeFile = null;
        ActionExecutedContext? executed = null;
        Dictionary<int, Operator>? beforeDirectory = null;
        var filePath = Path.Combine(environment.ContentRootPath, "appsettings.json");
        var smtp = mutation && type == typeof(SettingsController);
        var eventCode = type == typeof(OperatorsController)
            ? descriptor.ActionName == "Create" ? "security.operator.create" : descriptor.ActionName == "Delete" ? "security.operator.change_status" : "security.operator.update"
            : type == typeof(OperatorDirectoryComparisonController) ? "security.operator.update" : "security.setting.change";
        try
        {
            if (mutation)
            {
                if (smtp) { await SettingsFileGate.WaitAsync(http.RequestAborted); fileGate = true; }
                transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, http.RequestAborted);
            }
            var actor = await permissions.CurrentAsync(http.User, http.RequestAborted);
            if (actor is null || !SpcPagePermissions.Resolve(actor.Role, actor.PagePermissionsJson).Contains(required)
                || writeRequest && UserRoles.Normalize(actor.Role) != UserRoles.Editor)
            {
                if (transaction is not null) { await transaction.RollbackAsync(CancellationToken.None); await transaction.DisposeAsync(); transaction = null; }
                context.Result = Error(403, "SECURITY_ACCESS_DENIED", "權限不足或已變更，請重新載入並洽安全管理者。");
                await audit.DeniedAsync(http, "SECURITY_ACCESS_DENIED", page); return;
            }
            if (!mutation) { await next(); return; }
            http.Items[SecurityAudit.CorrelationKey] = Guid.NewGuid().ToString("N");
            Operator? target = null;
            if (type == typeof(OperatorsController) && context.ActionArguments.TryGetValue("id", out var idObject) && idObject is int id)
                target = await db.Operators.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, http.RequestAborted);
            context.ActionArguments.TryGetValue("req", out var requestObject);
            var operatorRequest = requestObject as OperatorsController.OperatorRequest;
            if (target is not null && ProtectedIdentityChange(target, operatorRequest, descriptor.ActionName == "Delete") && !permissions.IsSecurityAdministrator(actor.Id))
            {
                await RejectAsync("SECURITY_ADMINISTRATOR_PROTECTED"); return;
            }
            if (type == typeof(OperatorsController) && context.ActionArguments.TryGetValue("req", out var request) && request is OperatorsController.OperatorRequest req)
            {
                var old = SpcPagePermissions.Resolve(target?.Role, target?.PagePermissionsJson);
                var requested = req.PagePermissions?.ToArray() ?? old;
                if (req.PagePermissions is not null && (requested.Length > 64 || requested.Any(x => !SpcPagePermissions.Catalog.Any(item => item.Code == x))))
                {
                    context.Result = Error(400, "SECURITY_PERMISSION_INVALID", "權限代碼不正確，請重新載入權限清單；匯出與證據權限須同時具備查閱權。");
                    await RollbackAsync(); await audit.FailureAsync(http, eventCode, 400, page, "SECURITY_PERMISSION_INVALID"); return;
                }
                if (!await permissions.CanChangeAsync(http.User, target?.Id ?? 0, old, requested, http.RequestAborted))
                { await RejectAsync("AUDIT_GRANT_DENIED"); return; }
                if (target is not null) await StageOperatorAsync(http, page, target, req);
            }
            if (type == typeof(OperatorsController) && descriptor.ActionName == "Delete" && target is not null)
                await audit.StageAsync(http, "security.operator.change_status", page, "Operator", target.Id.ToString(), new Dictionary<string, object?> {
                    ["ChangedFields"] = new[] { "IsDeleted" }, ["PreviousStatus"] = target.IsActive ? "Active" : "Inactive", ["NewStatus"] = "Deleted" }, 204);
            if (type == typeof(OperatorDirectoryComparisonController))
                beforeDirectory = await db.Operators.AsNoTracking().ToDictionaryAsync(x => x.Id, http.RequestAborted);
            if (type == typeof(SpcReportSchedulesController))
                await audit.StageAsync(http, "security.setting.change", page, "Setting", "SpcReportSchedule", new Dictionary<string, object?> { ["ChangedFields"] = new[] { "ReportSchedule" } });
            if (smtp)
            {
                // Independent intent must be committed before the file or the business transaction is touched.
                await transaction!.RollbackAsync(http.RequestAborted); await transaction.DisposeAsync(); transaction = null;
                if (File.Exists(filePath)) beforeFile = await File.ReadAllBytesAsync(filePath, http.RequestAborted);
                await audit.PrepareSettingAsync(http);
                transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, http.RequestAborted);
                // Revalidate after opening the independent intent window.
                var current = await permissions.CurrentAsync(http.User, http.RequestAborted);
                if (current is null || UserRoles.Normalize(current.Role) != UserRoles.Editor
                    || !SpcPagePermissions.Resolve(current.Role, current.PagePermissionsJson).Contains(required))
                { await RejectAsync("SECURITY_ACCESS_REVOKED"); return; }
                await audit.StageAsync(http, "security.setting.change", page, "Setting", "Smtp", new Dictionary<string, object?> { ["ChangedFields"] = new[] { "SmtpSettings", "RecipientOperatorIds" } });
            }
            executed = await next();
            var status = executed.Result switch { ObjectResult o => o.StatusCode ?? 200, StatusCodeResult s => s.StatusCode, _ => 200 };
            if (executed.Exception is not null && !executed.ExceptionHandled || status >= 400)
            {
                await RollbackAsync(); await RestoreFileAsync();
                if (executed.Exception is not null && !executed.ExceptionHandled || status >= 500)
                { executed.ExceptionHandled = true; executed.Result = Unavailable(); status = 503; }
                await audit.FailureAsync(http, eventCode, status, page, "SECURITY_OPERATION_REJECTED"); return;
            }
            if (type == typeof(OperatorsController) && descriptor.ActionName == "Create"
                && context.ActionArguments["req"] is OperatorsController.OperatorRequest create)
            {
                var created = await db.Operators.AsNoTracking().SingleAsync(x => x.OperatorCode == create.OperatorCode.Trim(), http.RequestAborted);
                if (permissions.IsSecurityAdministrator(created.Id) && !permissions.IsSecurityAdministrator(actor.Id))
                {
                    await RejectAsync("SECURITY_ADMINISTRATOR_PROTECTED");
                    executed.Result = context.Result;
                    return;
                }
                await audit.StageAsync(http, "security.operator.create", page, "Operator", created.Id.ToString(), new Dictionary<string, object?> {
                    ["ChangedFields"] = new[] { "OperatorCode", "OperatorName", "Username", "Role", "IsActive", "PagePermissions" },
                    ["PermissionCodes"] = SpcPagePermissions.Resolve(created.Role, created.PagePermissionsJson) });
            }
            if (beforeDirectory is not null)
            {
                var after = await db.Operators.AsNoTracking().ToListAsync(http.RequestAborted);
                foreach (var op in after)
                {
                    beforeDirectory.TryGetValue(op.Id, out var prior);
                    var fields = ProfileChanges(prior, op.OperatorCode, op.OperatorName, op.Username, op.Department, op.Email);
                    if (prior is not null && fields.Length == 0) continue;
                    if (prior is not null && IsPrivileged(prior) && (prior.Username != op.Username || prior.OperatorCode != op.OperatorCode)
                        && !permissions.IsSecurityAdministrator(actor.Id))
                    {
                        await RejectAsync("SECURITY_ADMINISTRATOR_PROTECTED");
                        executed.Result = context.Result;
                        return;
                    }
                    await audit.StageAsync(http, prior is null ? "security.operator.create" : "security.operator.update", page,
                        "Operator", op.Id.ToString(), new Dictionary<string, object?> { ["ChangedFields"] = fields });
                }
            }
            await db.SaveChangesAsync(http.RequestAborted); await transaction!.CommitAsync(http.RequestAborted);
            http.Items[SecurityAudit.RecordedKey] = true;
        }
        catch
        {
            await RollbackAsync(); await RestoreFileAsync();
            if (executed is not null) { executed.ExceptionHandled = true; executed.Result = Unavailable(); }
            else context.Result = Unavailable();
            await audit.FailureAsync(http, eventCode, 503, page);
            logger.LogError("SECURITY_OPERATION_ROLLED_BACK; event {EventCode}; trace {TraceId}", eventCode, System.Diagnostics.Activity.Current?.TraceId.ToString());
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (fileGate) SettingsFileGate.Release();
        }

        async Task RollbackAsync()
        {
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                finally { await transaction.DisposeAsync(); transaction = null; }
            }
            db.ChangeTracker.Clear();
        }
        async Task RestoreFileAsync()
        {
            if (!smtp || beforeFile is null) return;
            try
            {
                if (!(await File.ReadAllBytesAsync(filePath, CancellationToken.None)).SequenceEqual(beforeFile))
                    await File.WriteAllBytesAsync(filePath, beforeFile, CancellationToken.None);
            }
            catch { logger.LogCritical("SECURITY_SETTING_FILE_RESTORE_FAILED; correlation {CorrelationId}", http.Items[SecurityAudit.CorrelationKey]); }
        }
        async Task RejectAsync(string reason)
        {
            await RollbackAsync();
            context.Result = Error(403, reason, "稽核權限須由安全管理者明示授予，且不可替自己加權。");
            await audit.DeniedAsync(http, reason, page);
        }
    }

    private bool IsPrivileged(Operator target) => permissions.IsSecurityAdministrator(target.Id)
        || AuditPermissions.TryExplicit(target.PagePermissionsJson, out var codes) && codes.Overlaps(AuditPermissions.Codes);

    private bool ProtectedIdentityChange(Operator target, OperatorsController.OperatorRequest? request, bool deleting)
    {
        if (!IsPrivileged(target)) return false;
        if (deleting) return true;
        if (request is null) return false;
        return target.Username != Null(request.Username) || target.OperatorCode != request.OperatorCode?.Trim()
            || UserRoles.Normalize(target.Role) != UserRoles.Normalize(request.Role) || target.IsActive != request.IsActive
            || !SpcPagePermissions.Resolve(target.Role, target.PagePermissionsJson).ToHashSet(StringComparer.Ordinal)
                .SetEquals(request.PagePermissions?.ToArray() ?? SpcPagePermissions.Resolve(request.Role, target.PagePermissionsJson));
    }

    private async Task<bool> RejectSensitiveWhileDisabledAsync(ActionExecutingContext context)
    {
        var type = context.Controller.GetType(); var action = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName;
        if (!Mutation(type, context.HttpContext.Request.Method, action) || type != typeof(OperatorsController) && type != typeof(OperatorDirectoryComparisonController)) return false;
        try
        {
            var reject = false;
            if (type == typeof(OperatorDirectoryComparisonController))
            {
                var users = await db.Operators.AsNoTracking().ToListAsync(context.HttpContext.RequestAborted);
                reject = users.Any(IsPrivileged);
            }
            else
            {
                Operator? target = null;
                if (context.ActionArguments.TryGetValue("id", out var id) && id is int operatorId)
                    target = await db.Operators.AsNoTracking().SingleOrDefaultAsync(x => x.Id == operatorId, context.HttpContext.RequestAborted);
                context.ActionArguments.TryGetValue("req", out var requestObject);
                var req = requestObject as OperatorsController.OperatorRequest;
                if (target is not null) reject = ProtectedIdentityChange(target, req, action == "Delete");
                if (req?.PagePermissions is not null)
                {
                    var old = SpcPagePermissions.Resolve(target?.Role, target?.PagePermissionsJson).Where(x => AuditPermissions.Codes.Contains(x)).ToHashSet(StringComparer.Ordinal);
                    var requested = req.PagePermissions.Where(x => x?.StartsWith("admin.audit.", StringComparison.Ordinal) == true).ToHashSet(StringComparer.Ordinal);
                    reject |= !old.SetEquals(requested);
                }
                if (action == "Create")
                {
                    var administrators = permissions.AdministratorIds;
                    var present = await db.Operators.IgnoreQueryFilters().Where(x => administrators.Contains(x.Id)).CountAsync(context.HttpContext.RequestAborted);
                    reject |= present != administrators.Length;
                }
            }
            if (!reject) return false;
            context.Result = Error(403, "AUDIT_SECURITY_COLLECTION_DISABLED", "敏感權限與身分異動須先啟用安全稽核，請洽安全管理者。");
            return true;
        }
        catch { context.Result = Unavailable(); return true; }
    }

    private async Task StageOperatorAsync(HttpContext http, string page, Operator prior, OperatorsController.OperatorRequest req)
    {
        var fields = ProfileChanges(prior, req.OperatorCode?.Trim(), req.OperatorName?.Trim(), Null(req.Username), req.Department?.Trim(), req.Email?.Trim());
        if (fields.Length > 0) await audit.StageAsync(http, "security.operator.update", page, "Operator", prior.Id.ToString(), new Dictionary<string, object?> { ["ChangedFields"] = fields });
        if (UserRoles.Normalize(prior.Role) != UserRoles.Normalize(req.Role))
            await audit.StageAsync(http, "security.operator.change_role", page, "Operator", prior.Id.ToString(), new Dictionary<string, object?> { ["ChangedFields"] = new[] { "Role" } });
        if (prior.IsActive != req.IsActive)
            await audit.StageAsync(http, "security.operator.change_status", page, "Operator", prior.Id.ToString(), new Dictionary<string, object?> {
                ["ChangedFields"] = new[] { "IsActive" }, ["PreviousStatus"] = prior.IsActive ? "Active" : "Inactive", ["NewStatus"] = req.IsActive ? "Active" : "Inactive" });
        var old = SpcPagePermissions.Resolve(prior.Role, prior.PagePermissionsJson).ToHashSet(StringComparer.Ordinal);
        var next = req.PagePermissions?.Distinct().ToArray() ?? SpcPagePermissions.Resolve(req.Role, prior.PagePermissionsJson);
        if (!old.SetEquals(next))
            await audit.StageAsync(http, "security.operator.change_permissions", page, "Operator", prior.Id.ToString(), new Dictionary<string, object?> { ["ChangedFields"] = new[] { "PagePermissions" }, ["PermissionCodes"] = next });
        if (fields.Length == 0 && prior.IsActive == req.IsActive && UserRoles.Normalize(prior.Role) == UserRoles.Normalize(req.Role) && old.SetEquals(next))
            await audit.StageAsync(http, "security.operator.update", page, "Operator", prior.Id.ToString(), new Dictionary<string, object?> { ["ChangedFields"] = Array.Empty<string>() });
    }

    private static string? Null(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static string[] ProfileChanges(Operator? before, string? code, string? name, string? username, string? department, string? email)
    {
        var fields = new List<string>();
        if (before is null || before.OperatorCode != code) fields.Add("OperatorCode");
        if (before is null || before.OperatorName != name) fields.Add("OperatorName");
        if (before is null || before.Username != username) fields.Add("Username");
        if (before is null || before.Department != department) fields.Add("Department");
        if (before is null || before.Email != email) fields.Add("Email");
        return fields.ToArray();
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (audit.Enabled && !context.ModelState.IsValid && context.Result is ObjectResult { StatusCode: 400 }
            && !context.HttpContext.Items.ContainsKey(SecurityAudit.RecordedKey) && context.Controller is not null && Page(context.Controller.GetType()) is { } page)
            await audit.FailureAsync(context.HttpContext, "security.access.check", 400, page, "SECURITY_REQUEST_INVALID");
        await next();
    }
}
