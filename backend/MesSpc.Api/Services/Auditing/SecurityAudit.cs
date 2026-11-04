using System.Security.Claims;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Security event orchestration. Only fixed metadata reaches the append-only writer.</summary>
public sealed class SecurityAudit(AppDbContext db, AuditEventWriter writer, AuthenticationAudit authentication,
    AuditPermissions permissions, IConfiguration configuration, ILogger<SecurityAudit> logger)
{
    internal const string RecordedKey = "SPC.SecurityAudit.Recorded";
    internal const string CorrelationKey = "SPC.SecurityAudit.Correlation";
    public bool Enabled => configuration.GetValue<bool>("Audit:SecurityEnabled");

    public static void ValidateConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Audit:SecurityEnabled")) return;
        if (!configuration.GetValue<bool>("Audit:AuthenticationEnabled")) throw new InvalidOperationException("SECURITY_AUDIT_AUTH_REQUIRED");
        AuthenticationAuditStartup.ValidateConfiguration(configuration);
        AuditPermissions.ReadAdministrators(configuration);
    }

    public async Task StageAsync(HttpContext http, string code, string page, string targetType, string targetId,
        IReadOnlyDictionary<string, object?> details, int status = 200)
    {
        if (!Enabled) return;
        var actor = await permissions.CurrentAsync(http.User, http.RequestAborted) ?? throw new AuditEventValidationException();
        await writer.StageAsync(Draft(http, code, "Succeeded", page, targetType, targetId, details, status),
            AuditActor.User(actor.Id, "Jwt", authentication.SessionHash(http.User.FindFirstValue("jti"))), http.RequestAborted);
    }

    public async Task PrepareSettingAsync(HttpContext http)
    {
        var actor = await permissions.CurrentAsync(http.User, http.RequestAborted) ?? throw new AuditEventValidationException();
        await writer.AppendAsync(Draft(http, "security.setting.prepare", "Succeeded", "settings.smtp", "Setting", "Smtp",
            new Dictionary<string, object?> { ["ChangedFields"] = new[] { "SmtpSettings", "RecipientOperatorIds" } }, 200),
            AuditActor.User(actor.Id, "Jwt", authentication.SessionHash(http.User.FindFirstValue("jti"))), http.RequestAborted);
    }

    public async Task DeniedAsync(HttpContext http, string reason, string? page = null)
    {
        if (!Enabled || http.Items.ContainsKey(RecordedKey)) return;
        http.Items[RecordedKey] = true;
        await TryResultAsync(http, "security.access.check", reason, http.Response.StatusCode is 401 ? 401 : 403, page);
    }

    public async Task FailureAsync(HttpContext http, string code, int status, string? page, string reason = "SECURITY_OPERATION_FAILED")
    {
        if (!Enabled) return;
        http.Items[RecordedKey] = true;
        await TryResultAsync(http, code, reason, status, page);
    }

    private async Task TryResultAsync(HttpContext http, string code, string reason, int status, string? page)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            AuditActor actor = AuditActor.Anonymous();
            if (http.User.Identity?.IsAuthenticated == true && int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
                && await db.Operators.IgnoreQueryFilters().AsNoTracking().AnyAsync(x => x.Id == id, timeout.Token))
                actor = AuditActor.User(id, "Jwt", authentication.SessionHash(http.User.FindFirstValue("jti")));
            await writer.AppendAsync(Draft(http, code, status >= 500 ? "Failed" : "Denied", page, null, null,
                new Dictionary<string, object?>(), status) with { ReasonCode = reason }, actor, timeout.Token);
        }
        catch
        {
            // SQL/providers and attempted values must never be included in security diagnostics.
            logger.LogError("SECURITY_AUDIT_RESULT_NOT_PERSISTED; event {EventCode}; reason {ReasonCode}; trace {TraceId}",
                code, reason, System.Diagnostics.Activity.Current?.TraceId.ToString());
        }
    }

    private static AuditEventDraft Draft(HttpContext http, string code, string outcome, string? page, string? targetType,
        string? targetId, IReadOnlyDictionary<string, object?> details, int status) => new(code, outcome)
    {
        PageKey = page, TargetType = targetType, TargetId = targetId, Details = details, HttpStatus = status,
        HttpMethod = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" }.Contains(http.Request.Method.ToUpperInvariant()) ? http.Request.Method.ToUpperInvariant() : null,
        CorrelationId = http.Items[CorrelationKey] as string,
        TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString()
    };
}

/// <summary>Observes authorization short circuits without reading URLs, bodies, headers or query values.</summary>
public sealed class SecurityAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SecurityAudit audit)
    {
        await next(context);
        if (audit.Enabled && context.Response.StatusCode is 401 or 403)
            await audit.DeniedAsync(context, context.Response.StatusCode == 401 ? "AUTHENTICATION_REQUIRED" : "ACCESS_FORBIDDEN");
    }
}
