using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed class ChemicalAnalysisFormulaVersionService(AppDbContext db)
{
    public async Task<ChemicalAnalysisFormulaVersionChangeResult> RecordChangeAsync(
        int partProcessCharacteristicId,
        string? previousConfigJson,
        string? newConfigJson,
        string? changedBy,
        string? reason,
        CancellationToken ct)
    {
        if (ConfigEquals(previousConfigJson, newConfigJson))
        {
            return new ChemicalAnalysisFormulaVersionChangeResult(false, null);
        }

        var mapping = await GetChemicalMappingAsync(partProcessCharacteristicId, ct);
        var version = await CreateVersionAsync(
            mapping.Id,
            NormalizeConfig(newConfigJson),
            NormalizeConfig(previousConfigJson),
            "Update",
            null,
            changedBy,
            reason,
            ct);

        return new ChemicalAnalysisFormulaVersionChangeResult(true, version);
    }

    public async Task<ChemicalAnalysisFormulaRestoreResult> RestoreAsync(
        int partProcessCharacteristicId,
        long versionId,
        string? changedBy,
        string? reason,
        CancellationToken ct)
    {
        var mapping = await GetChemicalMappingAsync(partProcessCharacteristicId, ct);
        var targetVersion = await db.ChemicalAnalysisFormulaVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId && x.PartProcessCharacteristicId == partProcessCharacteristicId, ct)
            ?? throw new KeyNotFoundException($"Chemical analysis formula version {versionId} was not found.");

        if (ConfigEquals(mapping.ChemicalAnalysisConfigJson, targetVersion.ConfigJson))
        {
            return new ChemicalAnalysisFormulaRestoreResult(false, null);
        }

        var previousConfigJson = NormalizeConfig(mapping.ChemicalAnalysisConfigJson);
        mapping.ChemicalAnalysisConfigJson = NormalizeConfig(targetVersion.ConfigJson);
        mapping.UpdatedAt = DateTime.UtcNow;
        mapping.UpdatedBy = NormalizeActor(changedBy);

        var restoreVersion = await CreateVersionAsync(
            mapping.Id,
            mapping.ChemicalAnalysisConfigJson,
            previousConfigJson,
            "Restore",
            targetVersion.Id,
            changedBy,
            reason,
            ct);

        await db.SaveChangesAsync(ct);
        return new ChemicalAnalysisFormulaRestoreResult(true, restoreVersion);
    }

    private async Task<PartProcessCharacteristic> GetChemicalMappingAsync(int partProcessCharacteristicId, CancellationToken ct)
    {
        var mapping = await db.PartProcessCharacteristics
            .FirstOrDefaultAsync(x => x.Id == partProcessCharacteristicId, ct)
            ?? throw new KeyNotFoundException($"PartProcessCharacteristic {partProcessCharacteristicId} was not found.");

        if (!string.Equals(ControlScopeCodes.Normalize(mapping.ControlScope), ControlScopeCodes.Chemical, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chemical analysis formula versioning only supports CHEM mappings.");
        }

        return mapping;
    }

    private async Task<ChemicalAnalysisFormulaVersion> CreateVersionAsync(
        int partProcessCharacteristicId,
        string configJson,
        string? previousConfigJson,
        string changeType,
        long? restoredFromVersionId,
        string? changedBy,
        string? reason,
        CancellationToken ct)
    {
        var latestVersionNo = await db.ChemicalAnalysisFormulaVersions
            .Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId)
            .Select(x => (int?)x.VersionNo)
            .MaxAsync(ct) ?? 0;

        var version = new ChemicalAnalysisFormulaVersion
        {
            PartProcessCharacteristicId = partProcessCharacteristicId,
            VersionNo = latestVersionNo + 1,
            ConfigJson = configJson,
            PreviousConfigJson = previousConfigJson,
            ChangeType = changeType,
            RestoredFromVersionId = restoredFromVersionId,
            ChangedBy = NormalizeActor(changedBy),
            Reason = NormalizeOptional(reason),
            ChangedAt = DateTime.UtcNow
        };
        db.ChemicalAnalysisFormulaVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return version;
    }

    private static bool ConfigEquals(string? left, string? right)
        => string.Equals(NormalizeConfig(left), NormalizeConfig(right), StringComparison.Ordinal);

    private static string NormalizeConfig(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeActor(string? value)
        => string.IsNullOrWhiteSpace(value) ? "System" : value.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ChemicalAnalysisFormulaVersionChangeResult(bool Created, ChemicalAnalysisFormulaVersion? Version);

public sealed record ChemicalAnalysisFormulaRestoreResult(bool Restored, ChemicalAnalysisFormulaVersion? Version);
