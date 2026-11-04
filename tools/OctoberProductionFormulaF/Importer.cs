using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace OctoberProductionFormulaF;

public sealed record ProductionFormulaFImportResult(
    int FHistoriesCreated,
    int FormulaVersionsCreated,
    int ScheduleRevisionsCreated,
    int EffectivitiesCreated)
{
    public int TotalCreated => FHistoriesCreated + FormulaVersionsCreated + ScheduleRevisionsCreated + EffectivitiesCreated;
}

/// <summary>Appends immutable October formula/F history without changing current PPC or active F master data.</summary>
public sealed class OctoberProductionFormulaFImporter(AppDbContext db, IDataProtectionProvider dataProtection)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, string> StandardSolutions = new Dictionary<string, string>
    {
        ["B3"] = "1N NaOH", ["B4"] = "0.2N HCl", ["B5"] = "0.4N HCl", ["B6"] = "0.05N EDTA",
        ["B7"] = "0.1N KMnO4", ["B8"] = "0.1N H2SO4", ["B9"] = "0.1N I2", ["B10"] = "0.1ml Na2S2O3"
    };

    public async Task<ProductionFormulaFImportResult> ApplyAsync(
        ProductionFormulaFPlan plan,
        string actor,
        string reason,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new ArgumentException("Reason is required.", nameof(reason));

        var desiredByPpc = plan.Periods.GroupBy(x => x.PpcId).ToDictionary(x => x.Key, x => x.ToList());
        var formulaByKey = plan.Formulas.ToDictionary(x => (x.PpcId, x.Signature));
        var fByKey = plan.FTables.ToDictionary(x => x.Signature, StringComparer.Ordinal);

        var activePolicies = await db.ChemicalFormulaPolicies.AsNoTracking()
            .Where(x => desiredByPpc.Keys.Contains(x.PartProcessCharacteristicId) && x.ActiveScheduleRevisionId != null)
            .ToListAsync(ct);
        var unchangedPpcs = new HashSet<int>();
        foreach (var policy in activePolicies)
        {
            var existing = await ExistingScheduleAsync(policy.PartProcessCharacteristicId, policy.ActiveScheduleRevisionId!.Value, ct);
            var desired = desiredByPpc[policy.PartProcessCharacteristicId]
                .Select(x => new ScheduleKey(x.PpcId, x.Day, x.FormulaSignature, x.FSignature));
            if (!OctoberProductionFormulaFPlanner.ScheduleEquals(existing, desired))
                throw new InvalidOperationException($"Existing production schedule conflict for PPC {policy.PartProcessCharacteristicId}.");
            unchangedPpcs.Add(policy.PartProcessCharacteristicId);
        }

        var existingF = await LoadExistingFHistoriesAsync(ct);
        var fCreated = 0;
        var nextFVersion = (await db.ChemicalFTableVersionHistories.MaxAsync(x => (int?)x.VersionNo, ct) ?? 0) + 1;
        foreach (var definition in plan.FTables.Where(x => !existingF.ContainsKey(x.Signature)))
        {
            var history = new ChemicalFTableVersionHistory
            {
                VersionNo = nextFVersion++,
                VersionCode = "OCT-2026-" + ShortHash(definition.Signature),
                DisplayName = "2026年10月藥液日報F快照",
                CellsJson = CellsJson(definition.Cells),
                ChangeType = "HistoricalExcelSnapshot",
                ChangedBy = actor.Trim(), Reason = reason.Trim(), ChangedAt = DateTime.UtcNow
            };
            db.Add(history);
            existingF[definition.Signature] = history;
            fCreated++;
        }
        if (fCreated > 0) await db.SaveChangesAsync(ct);

        var formulaCreated = 0;
        var versionByKey = new Dictionary<(int PpcId, string Signature), ChemicalAnalysisFormulaVersion>();
        foreach (var ppcGroup in plan.Formulas.GroupBy(x => x.PpcId))
        {
            var existing = await db.ChemicalAnalysisFormulaVersions
                .Where(x => x.PartProcessCharacteristicId == ppcGroup.Key).OrderBy(x => x.VersionNo).ToListAsync(ct);
            var byCanonical = existing.GroupBy(x => CanonicalJson(x.ConfigJson), StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.Last(), StringComparer.Ordinal);
            var next = existing.Select(x => x.VersionNo).DefaultIfEmpty().Max() + 1;
            foreach (var definition in ppcGroup)
            {
                var config = ConfigJson(definition.Formula);
                var canonical = CanonicalJson(config);
                if (!byCanonical.TryGetValue(canonical, out var version))
                {
                    version = new ChemicalAnalysisFormulaVersion
                    {
                        PartProcessCharacteristicId = definition.PpcId,
                        VersionNo = next++, ConfigJson = canonical,
                        PreviousConfigJson = null, ChangeType = "HistoricalSnapshot",
                        ChangedBy = actor.Trim(), Reason = reason.Trim(), ChangedAt = DateTime.UtcNow
                    };
                    db.Add(version);
                    byCanonical[canonical] = version;
                    formulaCreated++;
                }
                versionByKey[(definition.PpcId, definition.Signature)] = version;
            }
        }
        if (formulaCreated > 0) await db.SaveChangesAsync(ct);

        var scheduleService = new ChemicalFormulaScheduleService(
            db, dataProtection, TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
        var schedulesCreated = 0;
        var effectsCreated = 0;
        foreach (var (ppcId, desiredPeriods) in desiredByPpc.OrderBy(x => x.Key))
        {
            if (unchangedPpcs.Contains(ppcId)) continue;
            var policy = await db.ChemicalFormulaPolicies.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PartProcessCharacteristicId == ppcId, ct);
            if (policy?.ActiveScheduleRevisionId is not null)
                throw new InvalidOperationException($"Existing production schedule conflict for PPC {ppcId}.");
            var periods = desiredPeriods.OrderBy(x => x.Day).Select(period => new ChemicalSchedulePeriod(
                versionByKey[(ppcId, period.FormulaSignature)].Id,
                existingF[period.FSignature].Id,
                period.Day.ToString("yyyy-MM-dd"),
                period.Day.ToString("yyyy-MM-dd"))).ToArray();
            await scheduleService.PublishAsync(ppcId,
                new ChemicalPublishSchedule(null, reason.Trim(), periods), actor.Trim(), ct);
            schedulesCreated++;
            effectsCreated += periods.Length;
        }

        return new(fCreated, formulaCreated, schedulesCreated, effectsCreated);
    }

    public static string ConfigJson(string formula)
        => JsonSerializer.Serialize(new
        {
            enabled = true,
            version = "OCT-2026-HISTORICAL",
            primaryInputLabel = "原始滴定值",
            secondaryInputLabel = OctoberProductionFormulaFPlanner.FormulaSignature(formula).Contains("SECONDARY", StringComparison.Ordinal) ? "第二滴定值" : "",
            concentrationFormula = formula.Trim(),
            recheckConcentrationFormula = formula.Trim(),
            adjustmentFormula = (string?)null,
            adjustmentAmountFormula = (string?)null,
            decimalPlaces = 8
        });

    private async Task<List<ScheduleKey>> ExistingScheduleAsync(int ppcId, long revisionId, CancellationToken ct)
    {
        var effects = await db.ChemicalFormulaEffectivities.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == ppcId && x.ScheduleRevisionId == revisionId)
            .OrderBy(x => x.EffectiveFromUtc).ToListAsync(ct);
        var formulaIds = effects.Select(x => x.FormulaVersionId).Distinct().ToArray();
        var fIds = effects.Where(x => x.FTableHistoryId.HasValue).Select(x => x.FTableHistoryId!.Value).Distinct().ToArray();
        var formulas = await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
            .Where(x => formulaIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var histories = await db.ChemicalFTableVersionHistories.AsNoTracking()
            .Where(x => fIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var timezone = Taipei();
        var result = new List<ScheduleKey>();
        foreach (var effect in effects)
        {
            if (!effect.EffectiveUntilUtcExclusive.HasValue || !effect.FTableHistoryId.HasValue ||
                !formulas.TryGetValue(effect.FormulaVersionId, out var formula) ||
                !histories.TryGetValue(effect.FTableHistoryId.Value, out var history))
                throw new InvalidOperationException($"Existing production schedule conflict for PPC {ppcId}.");
            var from = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(effect.EffectiveFromUtc, DateTimeKind.Utc), timezone);
            var until = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(effect.EffectiveUntilUtcExclusive.Value, DateTimeKind.Utc), timezone);
            if (from.TimeOfDay != TimeSpan.Zero || until != from.AddDays(1))
                throw new InvalidOperationException($"Existing production schedule conflict for PPC {ppcId}.");
            using var config = JsonDocument.Parse(formula.ConfigJson);
            var expression = config.RootElement.GetProperty("concentrationFormula").GetString() ?? string.Empty;
            var cells = DeserializeFCells(history.CellsJson);
            result.Add(new(ppcId, DateOnly.FromDateTime(from),
                OctoberProductionFormulaFPlanner.FormulaSignature(expression),
                OctoberProductionFormulaFPlanner.FSignature(cells)));
        }
        return result;
    }

    private async Task<Dictionary<string, ChemicalFTableVersionHistory>> LoadExistingFHistoriesAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, ChemicalFTableVersionHistory>(StringComparer.Ordinal);
        foreach (var history in await db.ChemicalFTableVersionHistories.OrderBy(x => x.Id).ToListAsync(ct))
        {
            try
            {
                var signature = OctoberProductionFormulaFPlanner.FSignature(DeserializeFCells(history.CellsJson));
                result[signature] = history;
            }
            catch (ArgumentException)
            {
                // Older partial snapshots are not equivalent to a reviewed full eight-cell table.
            }
        }
        return result;
    }

    private static IReadOnlyDictionary<string, decimal> DeserializeFCells(string json)
    {
        var cells = JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(json, JsonOptions) ?? [];
        return cells.Where(x => x.NumericValue.HasValue).ToDictionary(
            x => x.CellAddress, x => x.NumericValue!.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string CellsJson(IReadOnlyDictionary<string, decimal> cells)
        => JsonSerializer.Serialize(cells.OrderBy(x => int.Parse(x.Key[1..], System.Globalization.CultureInfo.InvariantCulture))
            .Select(x => new ChemicalFTableHistoryCell("F!" + x.Key, StandardSolutions[x.Key], x.Value)));

    private static string ShortHash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12];

    private static string CanonicalJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(writer, document.RootElement);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static TimeZoneInfo Taipei()
    {
        foreach (var id in new[] { "Taipei Standard Time", "Asia/Taipei" })
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        throw new InvalidOperationException("Taipei time zone is unavailable.");
    }
}
