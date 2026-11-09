using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Auditing;

public sealed record AuditEventQuery(
    DateTime? FromUtc = null, DateTime? ToUtc = null, string? Actor = null, string? PageKey = null,
    string? EventCode = null, string? Target = null, string? Outcome = null, string? Cursor = null, int PageSize = 50);

public sealed record AuditEventListItem(long Id, Guid EventId, DateTime OccurredAtUtc, string Environment,
    string Category, string EventCode, string Outcome, string? ReasonCode, string ActorType,
    int? ActorOperatorId, string? ActorAccount, string? ActorOperatorCode, string? ActorDisplayName,
    string? ActorRole, string? ActorServiceCode, string? PageKey, string? RouteTemplate,
    string? TargetType, string? TargetId, string? HttpMethod, int? HttpStatus, string Source,
    [property: JsonIgnore] string DetailsJson = "{}", string? TargetDisplayName = null, AuditEventSafeSummary? Summary = null);

public sealed record AuditEventSafeSummary(string[] ChangedFields, string[] PermissionCodes,
    string? PreviousStatus, string? NewStatus, long? RecordCount, long? SucceededCount,
    long? SkippedCount, long? FailedCount, long? Revision, long? VersionId);

public sealed record AuditEventPage(IReadOnlyList<AuditEventListItem> Items, string? NextCursor,
    DateTime FromUtc, DateTime ToUtc, int PageSize);

public sealed class AuditQueryValidationException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class AuditEventQueryService(AppDbContext db, TimeProvider clock)
{
    public const int MaxExportRows = 5000;
    private sealed record CursorValue(DateTime OccurredAtUtc, long Id);

    public async Task<AuditEventPage> QueryAsync(AuditEventQuery input, CancellationToken ct = default)
    {
        var (query, from, to) = Build(input);
        if (input.PageSize is < 1 or > 100) throw new AuditQueryValidationException("AUDIT_PAGE_SIZE_INVALID");
        var cursor = DecodeCursor(input.Cursor);
        if (cursor is not null)
            query = query.Where(x => x.OccurredAtUtc < cursor.OccurredAtUtc || x.OccurredAtUtc == cursor.OccurredAtUtc && x.Id < cursor.Id);
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Take(input.PageSize + 1).Select(x => new AuditEventListItem(x.Id, x.EventId, x.OccurredAtUtc,
                x.Environment, x.Category, x.EventCode, x.Outcome, x.ReasonCode, x.ActorType,
                x.ActorOperatorId, x.ActorAccountSnapshot, x.ActorOperatorCodeSnapshot, x.ActorDisplayNameSnapshot,
                x.ActorRoleSnapshot, x.ActorServiceCode, x.PageKey, x.RouteTemplate, x.TargetType, x.TargetId,
                x.HttpMethod, x.HttpStatus, x.Source, x.DetailsJson)).ToListAsync(ct);
        rows = await EnrichAsync(rows, ct);
        var hasMore = rows.Count > input.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(rows, hasMore && rows.Count > 0 ? EncodeCursor(rows[^1].OccurredAtUtc, rows[^1].Id) : null, from, to, input.PageSize);
    }

    public async Task<(byte[] Content, int Count, DateTime FromUtc, DateTime ToUtc)> ExportAsync(AuditEventQuery input, CancellationToken ct = default)
    {
        var (query, from, to) = Build(input with { Cursor = null });
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Take(MaxExportRows + 1).Select(x => new AuditEventListItem(x.Id, x.EventId, x.OccurredAtUtc,
                x.Environment, x.Category, x.EventCode, x.Outcome, x.ReasonCode, x.ActorType,
                x.ActorOperatorId, x.ActorAccountSnapshot, x.ActorOperatorCodeSnapshot, x.ActorDisplayNameSnapshot,
                x.ActorRoleSnapshot, x.ActorServiceCode, x.PageKey, x.RouteTemplate, x.TargetType, x.TargetId,
                x.HttpMethod, x.HttpStatus, x.Source, x.DetailsJson)).ToListAsync(ct);
        rows = await EnrichAsync(rows, ct);
        if (rows.Count > MaxExportRows) throw new AuditQueryValidationException("AUDIT_EXPORT_LIMIT_EXCEEDED");
        return (BuildWorkbook(rows), rows.Count, from, to);
    }

    internal static byte[] BuildWorkbook(IReadOnlyList<AuditEventListItem> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("稽核紀錄");
        var headers = new[] { "ID", "事件時間(UTC)", "環境", "分類", "事件", "結果", "原因", "人員帳號", "人員代碼", "人員姓名", "角色", "頁面", "路由", "目標類型", "目標ID", "目標名稱", "異動摘要", "方法", "狀態碼", "來源" };
        for (var column = 1; column <= headers.Length; column++) sheet.Cell(1, column).SetValue(headers[column - 1]);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var values = new object?[] { row.Id, row.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture), row.Environment,
                row.Category, row.EventCode, row.Outcome, row.ReasonCode, row.ActorAccount, row.ActorOperatorCode,
                row.ActorDisplayName, row.ActorRole, row.PageKey, row.RouteTemplate, row.TargetType, row.TargetId,
                row.TargetDisplayName, SummaryText(row.Summary), row.HttpMethod, row.HttpStatus, row.Source };
            for (var column = 1; column <= values.Length; column++)
            {
                var cell = sheet.Cell(index + 2, column);
                if (values[column - 1] is string text)
                {
                    cell.SetValue(SafeSpreadsheetText(text));
                }
                else if (values[column - 1] is not null) cell.Value = XLCellValue.FromObject(values[column - 1]!);
            }
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(8, 60);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    internal static string SafeSpreadsheetText(string value) => value.Length > 0 && value[0] is '=' or '+' or '-' or '@'
        ? "'" + value : value;

    private async Task<List<AuditEventListItem>> EnrichAsync(List<AuditEventListItem> rows, CancellationToken ct)
    {
        var ids = rows.Where(x => x.TargetType == "Operator" && int.TryParse(x.TargetId, out _))
            .Select(x => int.Parse(x.TargetId!, CultureInfo.InvariantCulture)).Distinct().ToArray();
        var targets = ids.Length == 0 ? new Dictionary<int, string>() : await db.Operators.IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.OperatorName + "（" + x.OperatorCode + "）", ct);
        return rows.Select(row => row with {
            TargetDisplayName = row.TargetType == "Operator" && int.TryParse(row.TargetId, out var id) && targets.TryGetValue(id, out var name) ? name : null,
            Summary = ParseSafeSummary(row.DetailsJson)
        }).ToList();
    }

    public static AuditEventSafeSummary? ParseSafeSummary(string? json)
    {
        try
        {
            using var document = JsonDocument.Parse(json ?? "{}"); var root = document.RootElement;
            string[] Strings(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => x.Length <= 64).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(64).ToArray() : [];
            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()!.Length <= 64 ? value.GetString() : null;
            long? Number(string name) => root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) && number >= 0 ? number : null;
            var result = new AuditEventSafeSummary(Strings("ChangedFields"), Strings("PermissionCodes"), Text("PreviousStatus"), Text("NewStatus"),
                Number("RecordCount"), Number("SucceededCount"), Number("SkippedCount"), Number("FailedCount"), Number("Revision"), Number("VersionId"));
            return result.ChangedFields.Length == 0 && result.PermissionCodes.Length == 0 && result.PreviousStatus is null && result.NewStatus is null
                && result.RecordCount is null && result.SucceededCount is null && result.SkippedCount is null && result.FailedCount is null && result.Revision is null && result.VersionId is null ? null : result;
        }
        catch (JsonException) { return null; }
    }

    private static string? SummaryText(AuditEventSafeSummary? value)
    {
        if (value is null) return null;
        var parts = new List<string>();
        if (value.ChangedFields.Length > 0) parts.Add("異動欄位：" + string.Join("、", value.ChangedFields));
        if (value.PermissionCodes.Length > 0) parts.Add("權限設為：" + string.Join("、", value.PermissionCodes));
        if (value.PreviousStatus is not null || value.NewStatus is not null) parts.Add($"狀態：{value.PreviousStatus ?? "-"} → {value.NewStatus ?? "-"}");
        if (value.RecordCount is not null) parts.Add("筆數：" + value.RecordCount);
        return string.Join("；", parts);
    }

    public static string[] FilterFields(AuditEventQuery input)
    {
        var fields = new List<string>();
        if (!string.IsNullOrWhiteSpace(input.Actor)) fields.Add("Actor");
        if (!string.IsNullOrWhiteSpace(input.PageKey)) fields.Add("PageKey");
        if (!string.IsNullOrWhiteSpace(input.EventCode)) fields.Add("EventCode");
        if (!string.IsNullOrWhiteSpace(input.Target)) fields.Add("Target");
        if (!string.IsNullOrWhiteSpace(input.Outcome)) fields.Add("Outcome");
        return fields.ToArray();
    }

    private (IQueryable<Domain.Entities.AuditEvent> Query, DateTime From, DateTime To) Build(AuditEventQuery input)
    {
        var to = Normalize(input.ToUtc ?? clock.GetUtcNow().UtcDateTime);
        var from = Normalize(input.FromUtc ?? to.AddDays(-7));
        if (from > to || to - from > TimeSpan.FromDays(31)) throw new AuditQueryValidationException("AUDIT_DATE_RANGE_INVALID");
        ValidateCode(input.PageKey, "AUDIT_PAGE_KEY_INVALID"); ValidateCode(input.EventCode, "AUDIT_EVENT_CODE_INVALID");
        ValidateCode(input.Outcome, "AUDIT_OUTCOME_INVALID");
        if (input.Actor?.Length > 200 || input.Target?.Length > 200) throw new AuditQueryValidationException("AUDIT_FILTER_INVALID");
        if (input.Outcome is not null && input.Outcome is not ("Succeeded" or "Denied" or "Failed"))
            throw new AuditQueryValidationException("AUDIT_OUTCOME_INVALID");
        var query = db.AuditEvents.AsNoTracking().Where(x => x.OccurredAtUtc >= from && x.OccurredAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(input.Actor))
            query = query.Where(x => x.ActorAccountSnapshot == input.Actor || x.ActorOperatorCodeSnapshot == input.Actor || x.ActorDisplayNameSnapshot == input.Actor || x.ActorServiceCode == input.Actor);
        if (!string.IsNullOrWhiteSpace(input.PageKey)) query = query.Where(x => x.PageKey == input.PageKey);
        if (!string.IsNullOrWhiteSpace(input.EventCode)) query = query.Where(x => x.EventCode == input.EventCode);
        if (!string.IsNullOrWhiteSpace(input.Target)) query = query.Where(x => x.TargetType == input.Target || x.TargetId == input.Target);
        if (!string.IsNullOrWhiteSpace(input.Outcome)) query = query.Where(x => x.Outcome == input.Outcome);
        return (query, from, to);
    }

    private static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
    private static void ValidateCode(string? value, string code)
    {
        if (value is null) return;
        if (value.Length is < 1 or > 100 || value.Any(x => !(char.IsLetterOrDigit(x) || x is '_' or '.' or ':' or '-')))
            throw new AuditQueryValidationException(code);
    }
    private static string EncodeCursor(DateTime occurredAtUtc, long id) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new CursorValue(occurredAtUtc, id))));
    private static CursorValue? DecodeCursor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            if (value.Length > 256) throw new FormatException();
            var cursor = JsonSerializer.Deserialize<CursorValue>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            if (cursor is null || cursor.Id <= 0 || cursor.OccurredAtUtc == default) throw new FormatException();
            return cursor with { OccurredAtUtc = Normalize(cursor.OccurredAtUtc) };
        }
        catch (Exception ex) when (ex is FormatException or JsonException) { throw new AuditQueryValidationException("AUDIT_CURSOR_INVALID"); }
    }
}
