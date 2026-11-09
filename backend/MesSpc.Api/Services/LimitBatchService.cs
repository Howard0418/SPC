using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record LimitBatchInputValues(object? Lsl, object? Target, object? Usl, object? Lcl, object? Cl, object? Ucl);
public sealed record LimitBatchEditRequest(int PartProcessCharacteristicId, LimitBatchInputValues Values);
public sealed record LimitBatchPreviewRequest(IReadOnlyList<LimitBatchEditRequest> Items);
public sealed record LimitBatchApplyRequest(string ApplyToken);
public sealed record LimitBatchPreviewItem(
    int PartProcessCharacteristicId,
    LimitBatchValues? Before,
    LimitBatchValues After,
    IReadOnlyList<LimitBatchValidationError> Errors);
public sealed record LimitBatchPreviewResult(bool IsValid, string? ApplyToken, IReadOnlyList<LimitBatchPreviewItem> Items);
public sealed record LimitBatchApplyResult(Guid BatchId, int UpdatedCount);

public sealed class LimitBatchConflictException(string message) : Exception(message);

internal sealed record LimitBatchToken(string Actor, DateTime ExpiresAtUtc, IReadOnlyList<LimitBatchTokenItem> Items);
internal sealed record LimitBatchTokenItem(int PartProcessCharacteristicId, LimitBatchValues Before, LimitBatchValues After);

/// <summary>Previews and atomically applies edits limited to the six specification/control limit fields.</summary>
public sealed class LimitBatchService(AppDbContext db, IDataProtectionProvider protection)
{
    private const int MaxItems = 500;
    private readonly IDataProtector protector = protection.CreateProtector("SPC.AnalysisLimits.Batch.v1");

    public async Task<LimitBatchPreviewResult> PreviewAsync(
        LimitBatchPreviewRequest request,
        string actor,
        CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count is < 1 or > MaxItems || request.Items.Select(x => x.PartProcessCharacteristicId).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("每批須包含1至500個不重複的管制項目。");

        var ids = request.Items.Select(x => x.PartProcessCharacteristicId).ToArray();
        var rows = await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                Entity = x,
                ChartTypeCode = x.ChartTypeId == null
                    ? null
                    : db.ControlChartTypes.Where(chart => chart.Id == x.ChartTypeId).Select(chart => chart.ChartTypeCode).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Entity.Id, ct);

        var previewItems = new List<LimitBatchPreviewItem>(request.Items.Count);
        var tokenItems = new List<LimitBatchTokenItem>(request.Items.Count);
        foreach (var edit in request.Items)
        {
            var parsed = ParseValues(edit.Values);
            if (!rows.TryGetValue(edit.PartProcessCharacteristicId, out var row))
            {
                previewItems.Add(new(edit.PartProcessCharacteristicId, null, parsed.Values,
                    [new("Id", "PPC_NOT_FOUND", "找不到管制項目。", "請重新載入清單後再預檢。")]));
                continue;
            }

            var before = ToValues(row.Entity);
            var errors = parsed.Errors.Concat(LimitBatchValidator.Validate(parsed.Values, row.ChartTypeCode)).ToList();
            if (errors.Count == 0 && before == parsed.Values)
                errors.Add(new("Id", "NO_CHANGE", "此列沒有界限異動。", "請移除未異動列或修改後再預檢。"));
            previewItems.Add(new(edit.PartProcessCharacteristicId, before, parsed.Values, errors));
            if (errors.Count == 0) tokenItems.Add(new(edit.PartProcessCharacteristicId, before, parsed.Values));
        }

        if (previewItems.Any(x => x.Errors.Count > 0)) return new(false, null, previewItems);
        var token = new LimitBatchToken(NormalizeActor(actor), DateTime.UtcNow.AddMinutes(10), tokenItems);
        return new(true, protector.Protect(JsonSerializer.Serialize(token)), previewItems);
    }

    public async Task<LimitBatchApplyResult> ApplyAsync(
        LimitBatchApplyRequest request,
        string actor,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ApplyToken))
            throw new LimitBatchConflictException("缺少預檢憑證，請重新預檢。");

        LimitBatchToken token;
        try
        {
            token = JsonSerializer.Deserialize<LimitBatchToken>(protector.Unprotect(request.ApplyToken))
                ?? throw new CryptographicException();
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            throw new LimitBatchConflictException("預檢憑證無效，請重新預檢。");
        }

        var normalizedActor = NormalizeActor(actor);
        if (!string.Equals(token.Actor, normalizedActor, StringComparison.Ordinal) || token.ExpiresAtUtc <= DateTime.UtcNow)
            throw new LimitBatchConflictException("預檢已過期或操作者不同，請重新預檢。");
        if (token.Items.Count is < 1 or > MaxItems || token.Items.Select(x => x.PartProcessCharacteristicId).Distinct().Count() != token.Items.Count)
            throw new LimitBatchConflictException("預檢內容不完整，請重新預檢。");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var ids = token.Items.Select(x => x.PartProcessCharacteristicId).Order().ToArray();
            if (db.Database.IsSqlServer())
            {
                foreach (var id in ids)
                    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}", ct);
            }
            var entities = await db.PartProcessCharacteristics.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            if (entities.Count != ids.Length || token.Items.Any(item =>
                    !entities.TryGetValue(item.PartProcessCharacteristicId, out var entity) || ToValues(entity) != item.Before))
                throw new LimitBatchConflictException("界限已被其他使用者修改，請重新載入並預檢。");

            var batchId = Guid.NewGuid();
            db.LimitChangeBatches.Add(new LimitChangeBatch
            {
                Id = batchId,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = normalizedActor,
                ItemCount = token.Items.Count
            });
            foreach (var item in token.Items)
            {
                ApplyValues(entities[item.PartProcessCharacteristicId], item.After);
                db.LimitChangeItems.Add(ToHistory(batchId, item));
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(batchId, token.Items.Count);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static string NormalizeActor(string actor) => string.IsNullOrWhiteSpace(actor) ? "System" : actor.Trim();
    private static (LimitBatchValues Values, IReadOnlyList<LimitBatchValidationError> Errors) ParseValues(LimitBatchInputValues input)
    {
        var errors = new List<LimitBatchValidationError>();
        double? Parse(object? raw, string field)
        {
            if (raw is null) return null;
            if (raw is JsonElement element)
            {
                if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
                raw = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
            }
            var text = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrEmpty(text)) return null;
            if (double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)) return value;
            errors.Add(new(field, "INVALID_NUMBER", $"{field} 不是有效數字。", "請只輸入半形數字與小數點；移除單位、百分比、逗號及 ± 文字。"));
            return null;
        }

        return (new(Parse(input.Lsl, "LSL"), Parse(input.Target, "Target"), Parse(input.Usl, "USL"),
            Parse(input.Lcl, "LCL"), Parse(input.Cl, "CL"), Parse(input.Ucl, "UCL")), errors);
    }
    private static LimitBatchValues ToValues(PartProcessCharacteristic x) => new(x.LSL, x.TargetValue, x.USL, x.LCL, x.CL, x.UCL);
    private static void ApplyValues(PartProcessCharacteristic x, LimitBatchValues values)
    {
        x.LSL = values.Lsl;
        x.TargetValue = values.Target;
        x.USL = values.Usl;
        x.LCL = values.Lcl;
        x.CL = values.Cl;
        x.UCL = values.Ucl;
    }
    private static LimitChangeItem ToHistory(Guid batchId, LimitBatchTokenItem item) => new()
    {
        LimitChangeBatchId = batchId,
        PartProcessCharacteristicId = item.PartProcessCharacteristicId,
        PreviousLsl = item.Before.Lsl, PreviousTarget = item.Before.Target, PreviousUsl = item.Before.Usl,
        PreviousLcl = item.Before.Lcl, PreviousCl = item.Before.Cl, PreviousUcl = item.Before.Ucl,
        NewLsl = item.After.Lsl, NewTarget = item.After.Target, NewUsl = item.After.Usl,
        NewLcl = item.After.Lcl, NewCl = item.After.Cl, NewUcl = item.After.Ucl
    };
}
