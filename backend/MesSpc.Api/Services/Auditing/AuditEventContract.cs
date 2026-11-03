using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MesSpc.Api.Services.Auditing;

/// <summary>A fixed error code; never includes rejected input.</summary>
public sealed class AuditEventValidationException() : Exception("AUDIT_EVENT_INVALID");

/// <summary>Server-only actor descriptor. User snapshots are always reloaded from Operators.</summary>
public sealed record AuditActor(string Type, int? OperatorId, string Assurance, string? SessionIdHash, string? ServiceCode, string? AttemptedAccountHash)
{
    public static AuditActor User(int id, string assurance, string? sessionHash) => new("User", id, assurance, sessionHash, null, null);
    public static AuditActor Anonymous(string? attemptedAccountHmac = null) => new("Anonymous", null, "None", null, null, attemptedAccountHmac);
    public static AuditActor Service(string code) => new("Service", null, "Service", null, code, null);
}

/// <summary>Server event input without client-supplied identity, timestamps, environment or raw JSON.</summary>
public sealed record AuditEventDraft(string EventCode, string Outcome)
{
    public Guid? ClientEventId { get; init; }
    public string? PageKey { get; init; }
    public string? ReasonCode { get; init; }
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
    public string? HttpMethod { get; init; }
    public int? HttpStatus { get; init; }
    public IReadOnlyDictionary<string, object?> Details { get; init; } = new Dictionary<string, object?>();
}

internal static partial class AuditEventContract
{
    internal static readonly IReadOnlyDictionary<string, string> Pages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["calibration.instruments"] = "/calibration-instruments", ["measurement.entry"] = "/measurements",
        ["analysis.spc.overall"] = "/spc", ["analysis.particle.monitoring"] = "/particle-monitoring",
        ["analysis.etch"] = "/etch-analysis", ["analysis.capability"] = "/process-analysis/capability",
        ["analysis.violations"] = "/process-analysis/violations", ["analysis.spc.control_chart"] = "/spc/control-chart/{ppcId?}",
        ["analysis.trend.overall"] = "/trend-chart", ["analysis.trend.ppc"] = "/spc/trend/{ppcId?}",
        ["report.monthly_control_chart"] = "/monthly-control-chart", ["alert.overview"] = "/alerts", ["alert.workflow"] = "/alerts-workflow",
        ["master.parts"] = "/parts", ["master.processes"] = "/processes", ["master.characteristics"] = "/characteristics",
        ["master.ppc"] = "/part-process-characteristics", ["chemical.analysis.overview"] = "/chemical-analysis-overview",
        ["chemical.ftable"] = "/chemical-f-table", ["spc.control_chart.groups"] = "/control-chart-groups", ["spc.rule.groups"] = "/spc-rule-groups",
        ["data.upload"] = "/uploads", ["data.upload.preview"] = "/uploads/{batchId}/preview", ["traceability.query"] = "/spc/query",
        ["spc.alerts"] = "/spc/alerts", ["settings.smtp"] = "/settings/smtp", ["settings.spc_reports"] = "/settings/spc-reports",
        ["admin.operators"] = "/operators", ["genealogy.overview"] = "/genealogy", ["traceability.master"] = "/traceability-master", ["help.guide"] = "/guide"
    };
    private static readonly Dictionary<string, string> Categories = BuildCategories();
    private static Dictionary<string, string> BuildCategories()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string category, string codes) { foreach (var code in codes.Split(' ')) result.Add(code, category); }
        Add("Authentication", "auth.sso.exchange auth.logout.request auth.local_login.request");
        Add("Security", "security.access.check security.operator.create security.operator.update security.operator.change_status security.operator.change_role security.operator.change_permissions security.setting.change");
        Add("Navigation", "navigation.page.enter");
        Add("Read", "read.query.execute read.filter.apply read.export.generate read.download.authorize");
        Add("Measurement", "measurement.record.create measurement.record.update measurement.record.delete measurement.record.recheck measurement.batch.exclude");
        Add("Import", "import.batch.preview import.batch.confirm import.batch.delete");
        Add("Configuration", "configuration.formula.update configuration.formula.restore configuration.ftable.apply configuration.ftable.restore configuration.specification.change configuration.rule.change");
        Add("MasterData", "masterdata.record.create masterdata.record.update masterdata.record.deactivate masterdata.record.delete");
        Add("Audit", "audit.events.query audit.events.export audit.evidence.access audit.retention.execute audit.legal_hold.change");
        return result;
    }

    internal static string Category(string code) => Categories.TryGetValue(code, out var category) ? category : throw new AuditEventValidationException();
    internal static void Require(bool valid) { if (!valid) throw new AuditEventValidationException(); }
    [GeneratedRegex("^[A-Za-z0-9_.:-]+$", RegexOptions.CultureInvariant)] private static partial Regex Identifier();
    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)] private static partial Regex Hash();
    internal static void Code(string? value, int max = 100) => Require(value is null || value.Length is > 0 && value.Length <= max && Identifier().IsMatch(value));
    internal static void HashValue(string? value) => Require(value is null || Hash().IsMatch(value));

    internal static string SerializeDetails(string category, AuditEventDraft draft)
    {
        // No arbitrary object serialization: only typed, allowlisted values enter the dictionary.
        var allowed = category switch
        {
            "Authentication" => "FlowVersion ProvisioningResult LegacyAssertion",
            "Security" => "ChangedFields PermissionCodes PreviousStatus NewStatus",
            "Navigation" => "NavigationKind PreviousPageKey",
            "Read" when draft.EventCode is "read.export.generate" or "read.download.authorize" => "ExportFormat ResultCount DateFromUtc DateToUtc",
            "Read" => "FilterFields DateFromUtc DateToUtc ResultCount",
            "Measurement" => "ChangedFields Revision Recheck RecordCount",
            "Import" => "SourceType TotalCount SucceededCount SkippedCount FailedCount ContentHash",
            "Configuration" => "VersionId EffectiveDate ChangedFields CanonicalHash",
            "MasterData" => "ChangedFields BeforeHash AfterHash RecordCount",
            "Audit" => "FilterFields DateFromUtc DateToUtc ResultCount RecordCount ArchiveChecksum LegalHoldId",
            _ => throw new AuditEventValidationException()
        };
        var fields = allowed.Split(' ').ToHashSet(StringComparer.Ordinal);
        var values = new SortedDictionary<string, object?>(StringComparer.Ordinal) { ["SchemaVersion"] = 1 };
        Require(draft.Details is not null && draft.Details.Count <= 12);
        foreach (var (key, value) in draft.Details!)
        {
            Require(fields.Contains(key) && value is not null);
            switch (key)
            {
                case "ChangedFields": case "PermissionCodes": case "FilterFields":
                    Require(value is string[] { Length: <= 64 });
                    var entries = (string[])value!;
                    foreach (var item in entries) { Require(item is not null); Code(item, 64); }
                    values[key] = entries.Order(StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToArray(); break;
                case "LegacyAssertion": case "Recheck":
                    Require(value is bool); values[key] = value; break;
                case "ResultCount": case "RecordCount": case "TotalCount": case "SucceededCount": case "SkippedCount": case "FailedCount": case "Revision": case "VersionId":
                    Require(value is int or long && Convert.ToInt64(value, CultureInfo.InvariantCulture) >= 0); values[key] = value; break;
                case "DateFromUtc": case "DateToUtc":
                    Require(value is DateTime { Kind: DateTimeKind.Utc }); values[key] = value; break;
                case "EffectiveDate":
                    Require(value is DateOnly); values[key] = value; break;
                case "ContentHash": case "BeforeHash": case "AfterHash": case "CanonicalHash": case "ArchiveChecksum":
                    Require(value is string); HashValue((string)value!); values[key] = value; break;
                case "LegalHoldId":
                    Require(value is Guid id && id != Guid.Empty); values[key] = value; break;
                default:
                    Require(value is string); var text = (string)value!;
                    var permitted = key switch
                    {
                        "NavigationKind" => new[] { "Push", "Replace", "Reload", "Pop" }.Contains(text),
                        "PreviousPageKey" => Pages.ContainsKey(text),
                        "ExportFormat" => new[] { "Csv", "Xlsx", "Pdf", "Png" }.Contains(text),
                        "PreviousStatus" or "NewStatus" => new[] { "Active", "Inactive", "Deleted", "Enabled", "Disabled" }.Contains(text),
                        "ProvisioningResult" => new[] { "Existing", "Created", "Denied", "Failed", "NotRequired" }.Contains(text),
                        "FlowVersion" => text is "1" or "2" or "v1" or "v2",
                        "SourceType" => new[] { "Excel", "Csv", "TransFiles", "Portal", "Api" }.Contains(text),
                        _ => false
                    };
                    Require(permitted); values[key] = text; break;
            }
        }
        if (values.TryGetValue("DateFromUtc", out var from) && values.TryGetValue("DateToUtc", out var to))
            Require((DateTime)from! <= (DateTime)to!);
        var json = JsonSerializer.Serialize(values);
        Require(Encoding.UTF8.GetByteCount(json) <= 4096);
        return json;
    }
}
