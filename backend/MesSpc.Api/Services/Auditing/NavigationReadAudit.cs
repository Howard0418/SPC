using System.Security.Claims;
using MesSpc.Api.Services.Security;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Safe, best-effort evidence for successful SPA navigation and read activity.</summary>
public sealed class NavigationReadAudit(AuditEventWriter writer, AuthenticationAudit authentication,
    AuditPermissions permissions, IConfiguration configuration)
{
    public bool Enabled => configuration.GetValue<bool>("Audit:NavigationReadEnabled");

    public static void ValidateConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Audit:NavigationReadEnabled")) return;
        if (!configuration.GetValue<bool>("Audit:AuthenticationEnabled")) throw new InvalidOperationException("NAVIGATION_AUDIT_AUTH_REQUIRED");
        AuthenticationAuditStartup.ValidateConfiguration(configuration);
    }

    public async Task RecordAsync(HttpContext http, ActivityAuditRequest request, CancellationToken ct = default)
    {
        if (!Enabled) return;
        var actor = await permissions.CurrentAsync(http.User, ct) ?? throw new AuditEventValidationException();
        var draft = Draft(http, request);
        await writer.AppendAsync(draft, AuditActor.User(actor.Id, "Jwt", authentication.SessionHash(http.User.FindFirstValue("jti"))), ct);
    }

    private static AuditEventDraft Draft(HttpContext http, ActivityAuditRequest request)
    {
        var action = request.Activity?.Trim();
        var page = request.PageKey?.Trim();
        AuditEventContract.Require(page is not null && AuditEventContract.Pages.ContainsKey(page));
        var canonicalPage = page!;
        var details = new Dictionary<string, object?>();
        string code;
        switch (action)
        {
            case "PageEnter":
                AuditEventContract.Require(request.ClientEventId is { } eventId && eventId != Guid.Empty);
                AuditEventContract.Require(request.NavigationKind is "Push" or "Replace" or "Reload" or "Pop");
                AuditEventContract.Require(request.FilterFields is null && request.ResultCount is null && request.ExportFormat is null && request.DateFromUtc is null && request.DateToUtc is null);
                code = "navigation.page.enter";
                details["NavigationKind"] = request.NavigationKind!;
                return Common(code, request, canonicalPage, details, request.ClientEventId);
            case "Query":
            case "Filter":
                AuditEventContract.Require(request.ClientEventId is null && request.NavigationKind is null && request.ExportFormat is null);
                code = action == "Query" ? "read.query.execute" : "read.filter.apply";
                details["FilterFields"] = NormalizeFields(request.FilterFields);
                details["ResultCount"] = request.ResultCount ?? 0;
                AddDates(details, request);
                return Common(code, request, canonicalPage, details, null);
            case "Export":
                AuditEventContract.Require(request.ClientEventId is null && request.NavigationKind is null && request.FilterFields is null);
                AuditEventContract.Require(request.ExportFormat is "Csv" or "Xlsx" or "Pdf" or "Png");
                code = "read.export.generate";
                details["ExportFormat"] = request.ExportFormat!;
                details["ResultCount"] = request.ResultCount ?? 0;
                AddDates(details, request);
                return Common(code, request, canonicalPage, details, null);
            default: throw new AuditEventValidationException();
        }
    }

    private static AuditEventDraft Common(string code, ActivityAuditRequest request, string page, IReadOnlyDictionary<string, object?> details, Guid? clientEventId) => new(code, "Succeeded")
    {
        PageKey = page, ClientEventId = clientEventId, HttpMethod = "POST", HttpStatus = 204,
        CorrelationId = request.CorrelationId, Details = details
    };

    private static string[] NormalizeFields(string[]? fields)
    {
        var result = (fields ?? []).Order(StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToArray();
        AuditEventContract.Require(result.Length <= 64);
        foreach (var field in result) AuditEventContract.Code(field, 64);
        return result;
    }

    private static void AddDates(IDictionary<string, object?> details, ActivityAuditRequest request)
    {
        var from = request.DateFromUtc;
        var to = request.DateToUtc;
        if (from is null && to is null) return;
        AuditEventContract.Require(from is { Kind: DateTimeKind.Utc } && to is { Kind: DateTimeKind.Utc } && from.Value <= to.Value);
        details["DateFromUtc"] = from!.Value;
        details["DateToUtc"] = to!.Value;
    }
}

/// <summary>Constrained browser activity input. It intentionally has no actor, environment, URL or arbitrary details fields.</summary>
public sealed record ActivityAuditRequest(string? Activity, string? PageKey, Guid? ClientEventId = null,
    string? NavigationKind = null, string[]? FilterFields = null, DateTime? DateFromUtc = null,
    DateTime? DateToUtc = null, int? ResultCount = null, string? ExportFormat = null, string? CorrelationId = null);
