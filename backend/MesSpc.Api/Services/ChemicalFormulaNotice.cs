using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed class ChemicalFormulaNotice(
    AppDbContext db, IConfiguration configuration, IEmailNotificationService mail, ILogger<ChemicalFormulaNotice> logger)
{
    public static (bool Adjustment, bool Concentration) Changes(string? previous, string? current) =>
        (ReadFormula(previous, "adjustmentAmountFormula") != ReadFormula(current, "adjustmentAmountFormula"),
         ReadFormula(previous, "concentrationFormula") != ReadFormula(current, "concentrationFormula"));

    public static void AfterResponse(HttpContext? context, long? versionId)
    {
        if (context is null || versionId is not long id ||
            context.RequestServices?.GetService<IServiceScopeFactory>() is not { } scopes) return;
        context.Response.OnCompleted(async () =>
        {
            if (context.Response.StatusCode is < 200 or >= 300) return;
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ChemicalFormulaNotice>().SendCommittedAsync(id);
        });
    }

    public async Task SendCommittedAsync(long versionId)
    {
        try
        {
            var settings = AlertRecipientSelection.Read(configuration);
            if (settings is null || (settings.AdjustmentFormula.Length == 0 && settings.ConcentrationFormula.Length == 0)) return;
            var version = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleOrDefaultAsync(row => row.Id == versionId);
            if (version is null) return;
            var changes = Changes(version.PreviousConfigJson, version.ConfigJson);
            if (changes.Adjustment)
                await SendAsync(settings.AdjustmentFormula, "調整量公式修改", version.PartProcessCharacteristicId, version.VersionNo, versionId);
            if (changes.Concentration)
                await SendAsync(settings.ConcentrationFormula, "濃度公式修改", version.PartProcessCharacteristicId, version.VersionNo, versionId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Formula notification failed for version {VersionId}", versionId);
        }
    }

    private async Task SendAsync(int[] selected, string category, int itemId, int versionNumber, long versionId)
    {
        if (selected.Length == 0) return;
        var recipients = await db.Operators.AsNoTracking().Where(user => user.IsActive && selected.Contains(user.Id))
            .OrderBy(user => user.Id).Select(user => new { user.Email, user.OperatorName }).ToListAsync();
        var subject = $"【SPC】{category}";
        var body = $"<p>{category}已儲存。</p><p>管制項目 ID：{itemId}；公式版本：{versionNumber}。</p>";
        foreach (var recipient in recipients.Where(user => AlertRecipientSelection.IsValidEmail(user.Email))
                     .DistinctBy(user => user.Email!.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!await mail.SendHtmlEmailAsync(recipient.Email!.Trim(), recipient.OperatorName, subject, body))
                    logger.LogWarning("Formula notification delivery failed for version {VersionId} and category {Category}", versionId, category);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Formula notification delivery failed for version {VersionId} and category {Category}", versionId, category);
            }
        }
    }

    private static string ReadFormula(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty(key, out var formula)
            && formula.ValueKind == JsonValueKind.String ? formula.GetString()!.Trim() : "";
    }
}
