using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NCalc;

namespace MesSpc.Api.Services;

public class ChemicalFTableService(AppDbContext db)
{
    private static readonly Regex CellReferenceRegex = new(
        @"(?i)(?:'?(F)'?!\$?([A-Z]{1,3})\$?(\d+))",
        RegexOptions.Compiled);

    private static readonly IReadOnlyList<ChemicalFTableSeedCell> DefaultSeedCells =
    [
        new("F!B3", "1N NaOH", 1.02m),
        new("F!B4", "0.2N HCl", 0.983m),
        new("F!B5", "0.4N HCl", 0.983m),
        new("F!B6", "0.05N EDTA", 1m),
        new("F!B7", "0.1N KMnO4", 1m),
        new("F!B8", "0.1N H2SO4", 1m),
        new("F!B9", "0.1N I2", 1m),
        new("F!B10", "0.1ml Na2S2O3", 1m)
    ];

    public async Task<ChemicalFTableSyncResult> SyncDefaultAsync(bool apply, string? versionCode, string? displayName, CancellationToken ct)
    {
        return await SyncCellsAsync(DefaultSeedCells, apply, versionCode, displayName, "SPC_DEFAULT_F_TABLE", ct);
    }

    public async Task<ChemicalFTableSyncResult> SyncCellsAsync(
        IReadOnlyList<ChemicalFTableSeedCell> seedCells,
        bool apply,
        string? versionCode,
        string? displayName,
        string sourceName,
        CancellationToken ct,
        string? changedBy = null,
        string? reason = null,
        long? restoredFromHistoryId = null)
    {
        var normalizedVersionCode = string.IsNullOrWhiteSpace(versionCode)
            ? $"F-{DateTime.Today:yyyyMMdd}"
            : versionCode.Trim();

        var activeVersion = await GetActiveVersionAsync(ct);
        var existingVersion = await db.ChemicalFTableVersions
            .FirstOrDefaultAsync(x => x.VersionCode == normalizedVersionCode, ct);
        var activeCells = activeVersion is null
            ? new Dictionary<string, ChemicalFTableCell>(StringComparer.OrdinalIgnoreCase)
            : await db.ChemicalFTableCells
                .Where(x => x.VersionId == activeVersion.Id)
                .ToDictionaryAsync(x => x.NormalizedCellAddress, StringComparer.OrdinalIgnoreCase, ct);

        var cells = seedCells.Select(seed =>
        {
            var normalized = NormalizeCellAddress(seed.CellAddress);
            activeCells.TryGetValue(normalized, out var current);
            return new ChemicalFTableCellPreview(
                normalized,
                seed.StandardSolution,
                seed.NumericValue,
                current?.NumericValue,
                current is null ? "Add" : current.NumericValue == seed.NumericValue && current.StandardSolution == seed.StandardSolution ? "Unchanged" : "Update");
        }).ToList();

        if (!apply)
        {
            return new ChemicalFTableSyncResult(false, normalizedVersionCode, activeVersion?.VersionCode, cells);
        }

        var hasCellChanges = cells.Any(x => x.Action != "Unchanged");
        var activeVersionChanged = !string.Equals(activeVersion?.VersionCode, normalizedVersionCode, StringComparison.OrdinalIgnoreCase);
        var shouldRecordHistory = hasCellChanges || activeVersionChanged || restoredFromHistoryId.HasValue;
        var previousVersionCode = activeVersion?.VersionCode;
        var previousCells = activeCells.Values
            .Select(x => new ChemicalFTableHistoryCell(x.NormalizedCellAddress, x.StandardSolution, x.NumericValue))
            .OrderBy(x => x.CellAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var nextCells = seedCells
            .Select(x => new ChemicalFTableHistoryCell(NormalizeCellAddress(x.CellAddress), x.StandardSolution, x.NumericValue))
            .OrderBy(x => x.CellAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var version = existingVersion ?? new ChemicalFTableVersion
        {
            VersionCode = normalizedVersionCode,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"F 表 {normalizedVersionCode}" : displayName.Trim(),
            SourceName = sourceName,
            EffectiveAt = DateTime.UtcNow,
            ImportedAt = DateTime.UtcNow,
            IsActive = true
        };

        if (existingVersion is null)
        {
            db.ChemicalFTableVersions.Add(version);
        }
        else
        {
            version.DisplayName = string.IsNullOrWhiteSpace(displayName) ? version.DisplayName : displayName.Trim();
            version.ImportedAt = DateTime.UtcNow;
            version.IsActive = true;
        }

        var otherActiveVersions = await db.ChemicalFTableVersions
            .Where(x => x.IsActive && x.VersionCode != normalizedVersionCode)
            .ToListAsync(ct);
        foreach (var item in otherActiveVersions)
        {
            item.IsActive = false;
        }

        await db.SaveChangesAsync(ct);

        var oldCells = await db.ChemicalFTableCells.Where(x => x.VersionId == version.Id).ToListAsync(ct);
        db.ChemicalFTableCells.RemoveRange(oldCells);
        foreach (var seed in seedCells)
        {
            var normalized = NormalizeCellAddress(seed.CellAddress);
            db.ChemicalFTableCells.Add(new ChemicalFTableCell
            {
                VersionId = version.Id,
                SheetName = "F",
                CellAddress = seed.CellAddress,
                NormalizedCellAddress = normalized,
                StandardSolution = seed.StandardSolution,
                NumericValue = seed.NumericValue,
                RawValue = seed.NumericValue.ToString(CultureInfo.InvariantCulture)
            });
        }

        await RebuildReferencesAsync(version.Id, ct);
        if (shouldRecordHistory)
        {
            var versionNo = (await db.ChemicalFTableVersionHistories
                .Select(x => (int?)x.VersionNo)
                .MaxAsync(ct) ?? 0) + 1;
            db.ChemicalFTableVersionHistories.Add(new ChemicalFTableVersionHistory
            {
                VersionNo = versionNo,
                FTableVersionId = version.Id,
                VersionCode = version.VersionCode,
                DisplayName = version.DisplayName,
                PreviousVersionCode = previousVersionCode,
                CellsJson = JsonSerializer.Serialize(nextCells),
                PreviousCellsJson = previousCells.Count == 0 ? null : JsonSerializer.Serialize(previousCells),
                ChangeType = restoredFromHistoryId.HasValue ? "Restore" : existingVersion is null ? "Create" : "Update",
                RestoredFromHistoryId = restoredFromHistoryId,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "system" : changedBy.Trim(),
                ChangedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ChemicalFTableSyncResult(true, normalizedVersionCode, previousVersionCode, cells);
    }

    public async Task<List<ChemicalFTableVersionHistorySummary>> GetVersionHistoryAsync(int take, CancellationToken ct)
    {
        var limit = Math.Clamp(take, 1, 100);
        return await db.ChemicalFTableVersionHistories
            .AsNoTracking()
            .OrderByDescending(x => x.VersionNo)
            .Take(limit)
            .Select(x => new ChemicalFTableVersionHistorySummary(
                x.Id,
                x.VersionNo,
                x.VersionCode,
                x.DisplayName,
                x.PreviousVersionCode,
                x.ChangeType,
                x.ChangedAt,
                x.ChangedBy,
                x.Reason,
                x.RestoredFromHistoryId))
            .ToListAsync(ct);
    }

    public async Task<ChemicalFTableVersionHistoryDetail?> GetVersionHistoryDetailAsync(long id, CancellationToken ct)
    {
        var history = await db.ChemicalFTableVersionHistories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (history is null) return null;

        return new ChemicalFTableVersionHistoryDetail(
            history.Id,
            history.VersionNo,
            history.VersionCode,
            history.DisplayName,
            history.PreviousVersionCode,
            history.ChangeType,
            history.ChangedAt,
            history.ChangedBy,
            history.Reason,
            history.RestoredFromHistoryId,
            DeserializeHistoryCells(history.CellsJson),
            string.IsNullOrWhiteSpace(history.PreviousCellsJson) ? [] : DeserializeHistoryCells(history.PreviousCellsJson));
    }

    public async Task<ChemicalFTableSyncResult> RestoreVersionHistoryAsync(long id, string? changedBy, string? reason, CancellationToken ct)
    {
        var history = await GetVersionHistoryDetailAsync(id, ct);
        if (history is null)
        {
            throw new ArgumentException("找不到指定的 F 表版本記錄。");
        }

        var seedCells = history.Cells
            .Select(x => new ChemicalFTableSeedCell(x.CellAddress, x.StandardSolution ?? "", x.NumericValue ?? 0m))
            .ToList();
        if (seedCells.Count == 0)
        {
            throw new ArgumentException("指定的 F 表版本記錄沒有可回復的儲存格。");
        }

        return await SyncCellsAsync(
            seedCells,
            true,
            history.VersionCode,
            history.DisplayName,
            "SPC_F_TABLE_RESTORE",
            ct,
            changedBy,
            string.IsNullOrWhiteSpace(reason) ? $"Restore from F table history #{history.VersionNo}" : reason,
            id);
    }

    public async Task<List<ChemicalFTableImpactItem>> GetImpactAsync(string cellAddress, CancellationToken ct)
    {
        var normalized = NormalizeCellAddress(cellAddress);
        var activeVersion = await GetActiveVersionAsync(ct);
        if (activeVersion is null) return [];

        var references = await db.ChemicalFTableReferences
            .AsNoTracking()
            .Where(x => x.VersionId == activeVersion.Id && x.NormalizedCellAddress == normalized)
            .OrderBy(x => x.PartProcessCharacteristicId)
            .ToListAsync(ct);
        if (references.Count == 0) return [];

        var ppcIds = references.Select(x => x.PartProcessCharacteristicId).Distinct().ToList();
        var mappings = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Where(x => ppcIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var processIds = mappings.Values.Select(x => x.ProcessId).Distinct().ToList();
        var machineIds = mappings.Values.Where(x => x.MachineId.HasValue).Select(x => x.MachineId!.Value).Distinct().ToList();
        var tankIds = mappings.Values.Where(x => x.TankId.HasValue).Select(x => x.TankId!.Value).Distinct().ToList();
        var characteristicIds = mappings.Values.Select(x => x.CharacteristicId).Distinct().ToList();

        var processes = await db.Processes.AsNoTracking().Where(x => processIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var machines = await db.Machines.AsNoTracking().Where(x => machineIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var tanks = await db.Tanks.AsNoTracking().Where(x => tankIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var characteristics = await db.QualityCharacteristics.AsNoTracking().Where(x => characteristicIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

        return references
            .Where(reference => mappings.ContainsKey(reference.PartProcessCharacteristicId))
            .Select(reference =>
            {
                var mapping = mappings[reference.PartProcessCharacteristicId];
                processes.TryGetValue(mapping.ProcessId, out var process);
                var machine = mapping.MachineId.HasValue && machines.TryGetValue(mapping.MachineId.Value, out var machineValue) ? machineValue : null;
                var tank = mapping.TankId.HasValue && tanks.TryGetValue(mapping.TankId.Value, out var tankValue) ? tankValue : null;
                characteristics.TryGetValue(mapping.CharacteristicId, out var characteristic);
                return new ChemicalFTableImpactItem(
                    mapping.Id,
                    process?.ProcessCode ?? "",
                    process?.ProcessName ?? "",
                    machine?.MachineCode ?? "",
                    machine?.MachineName ?? "",
                    tank?.TankCode ?? "",
                    tank?.TankName ?? "",
                    characteristic?.CharacteristicCode ?? "",
                    characteristic?.CharacteristicName ?? "",
                    reference.SourceFormula,
                    reference.ReferenceContext);
            })
            .OrderBy(x => x.ProcessCode)
            .ThenBy(x => x.MachineCode)
            .ThenBy(x => x.TankCode)
            .ThenBy(x => x.CharacteristicCode)
            .ToList();
    }

    public async Task<ChemicalFTableVersion?> GetActiveVersionAsync(CancellationToken ct) =>
        await db.ChemicalFTableVersions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.EffectiveAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<ChemicalFTableVersionDetail?> GetActiveDetailAsync(CancellationToken ct)
    {
        var version = await GetActiveVersionAsync(ct);
        if (version is null) return null;
        var cells = await db.ChemicalFTableCells
            .AsNoTracking()
            .Where(x => x.VersionId == version.Id)
            .OrderBy(x => x.NormalizedCellAddress)
            .Select(x => new ChemicalFTableCellDto(x.NormalizedCellAddress, x.StandardSolution, x.NumericValue, x.TextValue))
            .ToListAsync(ct);
        return new ChemicalFTableVersionDetail(version.Id, version.VersionCode, version.DisplayName, version.EffectiveAt, cells);
    }

    public async Task<ChemicalFormulaEvaluationResult> EvaluateChemicalFormulaAsync(string expression, Dictionary<string, decimal>? variables, CancellationToken ct)
    {
        var version = await GetActiveVersionAsync(ct)
            ?? throw new InvalidOperationException("尚未啟用 F 表版本，無法計算含 F 表參照的公式。");
        var refs = ExtractCellReferences(expression);
        var cells = refs.Count == 0
            ? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            : await db.ChemicalFTableCells
                .AsNoTracking()
                .Where(x => x.VersionId == version.Id && refs.Contains(x.NormalizedCellAddress) && x.NumericValue.HasValue)
                .ToDictionaryAsync(x => x.NormalizedCellAddress, x => x.NumericValue!.Value, StringComparer.OrdinalIgnoreCase, ct);

        var missing = refs.Where(x => !cells.ContainsKey(x)).OrderBy(x => x).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"F 表缺少儲存格：{string.Join(", ", missing)}");
        }

        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (variables is not null)
        {
            foreach (var pair in variables)
            {
                parameters[pair.Key] = pair.Value;
            }
        }

        var normalizedExpression = CellReferenceRegex.Replace(expression, match =>
        {
            var cell = NormalizeCellAddress(match.Value);
            var parameterName = ToParameterName(cell);
            parameters[parameterName] = cells[cell];
            return parameterName;
        });

        var ncalc = new Expression(normalizedExpression);
        foreach (var parameter in parameters)
        {
            ncalc.Parameters[parameter.Key] = parameter.Value;
        }

        var value = Convert.ToDecimal(ncalc.Evaluate(), CultureInfo.InvariantCulture);
        return new ChemicalFormulaEvaluationResult(version.VersionCode, expression, normalizedExpression, value, refs);
    }

    public static List<string> ExtractCellReferences(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return [];
        return CellReferenceRegex.Matches(expression)
            .Select(match => NormalizeCellAddress(match.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public static string NormalizeCellAddress(string cellAddress)
    {
        var match = CellReferenceRegex.Match(cellAddress.Trim());
        if (!match.Success) throw new ArgumentException($"F 表儲存格格式不正確：{cellAddress}", nameof(cellAddress));
        return $"F!{match.Groups[2].Value.ToUpperInvariant()}{match.Groups[3].Value}";
    }

    private static string ToParameterName(string normalizedCellAddress) =>
        normalizedCellAddress.Replace("!", "_", StringComparison.Ordinal).Replace("$", string.Empty, StringComparison.Ordinal);

    private async Task RebuildReferencesAsync(int versionId, CancellationToken ct)
    {
        var oldReferences = await db.ChemicalFTableReferences.Where(x => x.VersionId == versionId).ToListAsync(ct);
        db.ChemicalFTableReferences.RemoveRange(oldReferences);

        var mappings = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Where(x => x.ChemicalAnalysisConfigJson != null && x.ChemicalAnalysisConfigJson != "")
            .Select(x => new { x.Id, x.ChemicalAnalysisConfigJson })
            .ToListAsync(ct);

        foreach (var mapping in mappings)
        {
            var refs = ExtractFormulaReferencesFromConfig(mapping.ChemicalAnalysisConfigJson)
                .GroupBy(x => x.CellAddress, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());

            foreach (var reference in refs)
            {
                db.ChemicalFTableReferences.Add(new ChemicalFTableReference
                {
                    VersionId = versionId,
                    PartProcessCharacteristicId = mapping.Id,
                    CellAddress = reference.CellAddress,
                    NormalizedCellAddress = reference.CellAddress,
                    SourceFormula = reference.Formula,
                    ReferenceContext = reference.Context
                });
            }
        }
    }

    private static List<ChemicalFormulaReference> ExtractFormulaReferencesFromConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return [];
        try
        {
            using var document = JsonDocument.Parse(configJson);
            var result = new List<ChemicalFormulaReference>();
            VisitJson(document.RootElement, "$", result);
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<ChemicalFTableHistoryCell> DeserializeHistoryCells(string? cellsJson)
    {
        if (string.IsNullOrWhiteSpace(cellsJson)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(cellsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void VisitJson(JsonElement element, string path, List<ChemicalFormulaReference> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    VisitJson(property.Value, $"{path}.{property.Name}", result);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    VisitJson(item, $"{path}[{index++}]", result);
                }
                break;
            case JsonValueKind.String:
                var value = element.GetString();
                foreach (var cell in ExtractCellReferences(value))
                {
                    result.Add(new ChemicalFormulaReference(cell, value ?? string.Empty, path));
                }
                break;
        }
    }
}

public record ChemicalFTableSeedCell(string CellAddress, string StandardSolution, decimal NumericValue);
public record ChemicalFTableCellPreview(string CellAddress, string StandardSolution, decimal NumericValue, decimal? CurrentNumericValue, string Action);
public record ChemicalFTableSyncResult(bool Applied, string VersionCode, string? PreviousActiveVersionCode, List<ChemicalFTableCellPreview> Cells);
public record ChemicalFTableCellDto(string CellAddress, string? StandardSolution, decimal? NumericValue, string? TextValue);
public record ChemicalFTableVersionDetail(int Id, string VersionCode, string DisplayName, DateTime EffectiveAt, List<ChemicalFTableCellDto> Cells);
public record ChemicalFTableHistoryCell(string CellAddress, string? StandardSolution, decimal? NumericValue);
public record ChemicalFTableVersionHistorySummary(long Id, int VersionNo, string VersionCode, string DisplayName, string? PreviousVersionCode, string ChangeType, DateTime ChangedAt, string? ChangedBy, string? Reason, long? RestoredFromHistoryId);
public record ChemicalFTableVersionHistoryDetail(long Id, int VersionNo, string VersionCode, string DisplayName, string? PreviousVersionCode, string ChangeType, DateTime ChangedAt, string? ChangedBy, string? Reason, long? RestoredFromHistoryId, List<ChemicalFTableHistoryCell> Cells, List<ChemicalFTableHistoryCell> PreviousCells);
public record ChemicalFormulaEvaluationResult(string VersionCode, string SourceExpression, string EvaluatedExpression, decimal Value, List<string> CellReferences);
public record ChemicalFormulaReference(string CellAddress, string Formula, string Context);
public record ChemicalFTableImpactItem(int PartProcessCharacteristicId, string ProcessCode, string ProcessName, string MachineCode, string MachineName, string TankCode, string TankName, string CharacteristicCode, string CharacteristicName, string? SourceFormula, string? ReferenceContext);
