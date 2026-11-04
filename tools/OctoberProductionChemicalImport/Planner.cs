using System.Text.Json;

namespace OctoberProductionChemicalImport;

public sealed record ProductionChemicalSourceRow(
    int PpcId,
    DateOnly Day,
    string SourceId,
    string FileHash,
    string FileName,
    string Sheet,
    int SourceRow,
    string MachineCode,
    string SamplingPhase,
    string SamplingStage,
    string Primary,
    string? Secondary,
    string? RecheckPrimary,
    string? RecheckSecondary,
    string Excel,
    string? RecheckExcel);

public readonly record struct ProductionChemicalDayKey(int PpcId, DateOnly Day);
public readonly record struct ProductionChemicalNaturalKey(int PpcId, DateOnly Day, string SamplingPhase, string SamplingStage);
public sealed record PlannedChemicalMeasurement(ProductionChemicalSourceRow Source, string MeasuredAt, string SourceKey);
public sealed record ProductionChemicalImportPlan(
    IReadOnlyList<PlannedChemicalMeasurement> Included,
    IReadOnlyList<ProductionChemicalSourceRow> SkippedExisting,
    IReadOnlyList<ProductionChemicalSourceRow> ExcludedConflict);

public static class OctoberProductionChemicalImportPlanner
{
    public const string TimeBasis = "approved historical report-day 12:00 Asia/Taipei; not actual sampling time";

    public static ProductionChemicalImportPlan Build(
        IReadOnlyCollection<ProductionChemicalSourceRow> sourceRows,
        IReadOnlySet<ProductionChemicalDayKey> existingBeforeImport,
        IReadOnlySet<string> conflictSourceIds)
    {
        if (sourceRows.Count == 0) throw new ArgumentException("Source rows are required.");
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        var naturalKeys = new HashSet<ProductionChemicalNaturalKey>();
        var included = new List<PlannedChemicalMeasurement>();
        var skipped = new List<ProductionChemicalSourceRow>();
        var conflicts = new List<ProductionChemicalSourceRow>();

        foreach (var row in sourceRows.OrderBy(x => x.Day).ThenBy(x => x.PpcId).ThenBy(x => x.SourceId, StringComparer.Ordinal))
        {
            Validate(row);
            if (!sourceIds.Add(row.SourceId)) throw new ArgumentException("Duplicate source id in reviewed manifest.");
            if (conflictSourceIds.Contains(row.SourceId))
            {
                conflicts.Add(row);
                continue;
            }

            var naturalKey = new ProductionChemicalNaturalKey(row.PpcId, row.Day, row.SamplingPhase, row.SamplingStage);
            if (!naturalKeys.Add(naturalKey)) throw new ArgumentException("Duplicate formal natural key in reviewed source.");
            if (existingBeforeImport.Contains(new(row.PpcId, row.Day)))
            {
                skipped.Add(row);
                continue;
            }

            var measuredAt = row.Day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "T12:00:00+08:00";
            var sourceKey = JsonSerializer.Serialize(new
            {
                kind = "TransFilesChemical-v1",
                fileHash = row.FileHash,
                sheet = row.Sheet,
                row = row.SourceRow,
                samplingPhase = row.SamplingPhase,
                samplingStage = row.SamplingStage,
                task = "SPC-HISTORICAL-IMPORT-1102-TASK-004-CHEM-OCT",
                timeBasis = TimeBasis,
                sourceId = row.SourceId
            });
            included.Add(new(row, measuredAt, sourceKey));
        }

        return new(included, skipped, conflicts);
    }

    public static void ValidateTarget(string? appEnvironment, string? server, string? database, string? apiEnvironment)
    {
        if (!string.Equals(appEnvironment, "production", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(server, "172.16.110.16", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(database, "PMR_SPC_2026", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(apiEnvironment, "production", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production target identity mismatch.");
    }

    private static void Validate(ProductionChemicalSourceRow row)
    {
        if (row.PpcId <= 0 || row.Day.Year != 2026 || row.Day.Month != 10 || row.SourceRow < 2 ||
            row.FileHash.Length != 64 || string.IsNullOrWhiteSpace(row.SourceId) || string.IsNullOrWhiteSpace(row.Sheet))
            throw new ArgumentException("Invalid reviewed October source row.");
        if (row.SamplingPhase is not ("OPEN" or "MIDDLE"))
            throw new ArgumentException("SamplingPhase must be OPEN or MIDDLE.");
        var isN1N2 = row.MachineCode is "N1" or "N2";
        if (isN1N2 ? row.SamplingStage is not ("OPEN" or "CLOSE") : row.SamplingStage != "GENERAL")
            throw new ArgumentException("SamplingStage does not match the approved line rule.");
    }
}
