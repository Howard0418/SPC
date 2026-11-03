using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Authentication-specific safe evidence. Only success persistence is fail-closed.</summary>
public sealed class AuthenticationAudit(AuditEventWriter writer, IConfiguration configuration, ILogger<AuthenticationAudit> logger)
{
    public bool Enabled => configuration.GetValue<bool>("Audit:AuthenticationEnabled");

    /// <summary>Correlates an unpredictable JWT jti without hashing or retaining the token itself.</summary>
    public string? SessionHash(string? jti)
    {
        if (string.IsNullOrWhiteSpace(jti)) return null;
        AuditEventContract.Require(jti.Length <= 128);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("spc-session:v1:" + configuration["AppEnvironment"]?.ToLowerInvariant() + ":" + jti)));
    }

    /// <summary>Stages successful exchange evidence inside the identity provisioning transaction.</summary>
    public Task StageSuccessAsync(int operatorId, string jti, bool legacy, bool created, CancellationToken ct)
    {
        if (!Enabled) return Task.CompletedTask;
        AuthenticationAuditStartup.ValidateConfiguration(configuration);
        return writer.StageAsync(new("auth.sso.exchange", "Succeeded")
        {
            HttpMethod = "POST",
            HttpStatus = 200,
            TraceId = CurrentTraceId(),
            Details = new Dictionary<string, object?> { ["FlowVersion"] = legacy ? "v1" : "v2", ["LegacyAssertion"] = legacy, ["ProvisioningResult"] = created ? "Created" : "Existing" }
        }, AuditActor.User(operatorId, "PortalAssertion", SessionHash(jti)), ct);
    }

    /// <summary>Records a fixed denial/failure result without altering the original security decision.</summary>
    public async Task FailureAsync(string reasonCode, int status, string? attemptedAccount, int? verifiedOperatorId = null,
        string eventCode = "auth.sso.exchange")
    {
        if (!Enabled) return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var actor = verifiedOperatorId is > 0 ? AuditActor.User(verifiedOperatorId.Value, "PortalAssertion", null) : AuditActor.Anonymous(AccountHash(attemptedAccount));
            await writer.AppendAsync(new(eventCode, status >= 500 ? "Failed" : "Denied") { ReasonCode = reasonCode, HttpMethod = "POST", HttpStatus = status, TraceId = CurrentTraceId() }, actor, timeout.Token);
        }
        catch
        {
            // Never include usernames, assertions, exceptions, SQL or configuration values.
            logger.LogError("AUTH_AUDIT_FAILURE_NOT_PERSISTED; reason {ReasonCode}; trace {TraceId}", reasonCode, CurrentTraceId());
        }
    }

    /// <summary>Records a logout request, not token revocation. Failure must never block local logout.</summary>
    public async Task LogoutAsync(ClaimsPrincipal principal)
    {
        if (!Enabled) return;
        try
        {
            AuditEventContract.Require(principal.Identity?.IsAuthenticated == true && int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0);
            var operatorId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await writer.AppendAsync(new("auth.logout.request", "Succeeded") { HttpMethod = "POST", HttpStatus = 204, TraceId = CurrentTraceId() },
                AuditActor.User(operatorId, "Jwt", SessionHash(principal.FindFirstValue("jti"))), timeout.Token);
        }
        catch { logger.LogError("AUTH_AUDIT_LOGOUT_NOT_PERSISTED; trace {TraceId}", CurrentTraceId()); }
    }

    private static string? CurrentTraceId() => System.Diagnostics.Activity.Current?.TraceId.ToString();

    private string? AccountHash(string? account)
    {
        if (string.IsNullOrWhiteSpace(account)) return null;
        var value = account.Trim();
        if (value.Length > 256) value = value[..256];
        var slash = value.LastIndexOf('\\'); if (slash >= 0) value = value[(slash + 1)..];
        var at = value.IndexOf('@'); if (at > 0) value = value[..at];
        var key = configuration["Audit:AttemptedAccountHmacKey"];
        AuditEventContract.Require(key is { Length: >= 32 });
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key!), Encoding.UTF8.GetBytes(configuration["AppEnvironment"] + ":" + value.ToLowerInvariant())));
    }
}
