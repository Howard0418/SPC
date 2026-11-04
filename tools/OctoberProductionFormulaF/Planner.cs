using System.Globalization;
using System.Text.RegularExpressions;

namespace OctoberProductionFormulaF;

public sealed record SourceScheduleRow(
    int PpcId,
    string BusinessKey,
    DateOnly Day,
    string Formula,
    IReadOnlyDictionary<string, decimal> FCells,
    string SourceId);

public sealed record FormulaDefinition(int PpcId, string Signature, string Formula);
public sealed record FTableDefinition(string Signature, IReadOnlyDictionary<string, decimal> Cells);
public sealed record ScheduleKey(int PpcId, DateOnly Day, string FormulaSignature, string FSignature);
public sealed record PlannedPeriod(
    int PpcId,
    string BusinessKey,
    DateOnly Day,
    string FormulaSignature,
    string FSignature,
    IReadOnlyList<string> SourceIds);
public sealed record ScheduleConflict(
    int PpcId,
    string BusinessKey,
    DateOnly Day,
    IReadOnlyList<string> SourceIds,
    IReadOnlyList<ScheduleKey> Variants);
public sealed record ProductionFormulaFPlan(
    IReadOnlyList<FormulaDefinition> Formulas,
    IReadOnlyList<FTableDefinition> FTables,
    IReadOnlyList<PlannedPeriod> Periods,
    IReadOnlyList<ScheduleConflict> Conflicts,
    int IncludedRowCount,
    int ExcludedRowCount);

/// <summary>Pure planning and identity guards for the one-time October production formula/F import.</summary>
public static class OctoberProductionFormulaFPlanner
{
    private static readonly string[] RequiredFCells = ["B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10"];

    public static void ValidateTarget(string? appEnvironment, string? dataSource, string? database, string? apiEnvironment)
    {
        if (!string.Equals(appEnvironment, "production", StringComparison.Ordinal) ||
            !string.Equals(dataSource, "172.16.110.16", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(database, "PMR_SPC_2026", StringComparison.Ordinal) ||
            !string.Equals(apiEnvironment, "production", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The October formula/F runner is restricted to the verified SPC production target.");
        }
    }

    public static ProductionFormulaFPlan Build(IReadOnlyCollection<SourceScheduleRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0) throw new ArgumentException("At least one reviewed October row is required.", nameof(rows));

        var prepared = rows.Select(row =>
        {
            if (row.PpcId <= 0 || string.IsNullOrWhiteSpace(row.BusinessKey) || string.IsNullOrWhiteSpace(row.SourceId))
                throw new ArgumentException("Every row requires a positive PPC, business key and source identifier.", nameof(rows));
            if (row.Day.Year != 2026 || row.Day.Month != 10)
                throw new ArgumentException("Only reviewed October 2026 report days are allowed.", nameof(rows));
            if (string.IsNullOrWhiteSpace(row.Formula))
                throw new ArgumentException("Every row requires a concentration formula.", nameof(rows));

            var cells = NormalizeFCells(row.FCells);
            var formulaSignature = FormulaSignature(row.Formula);
            var fSignature = FSignature(cells);
            return new PreparedRow(row, cells, formulaSignature, fSignature);
        }).ToList();

        var periods = new List<PlannedPeriod>();
        var conflicts = new List<ScheduleConflict>();
        var includedRows = new List<PreparedRow>();
        var excluded = 0;

        foreach (var group in prepared.GroupBy(x => new { x.Row.PpcId, x.Row.BusinessKey, x.Row.Day })
                     .OrderBy(x => x.Key.PpcId).ThenBy(x => x.Key.Day))
        {
            var variants = group.Select(x => new ScheduleKey(x.Row.PpcId, x.Row.Day, x.FormulaSignature, x.FSignature))
                .Distinct().ToList();
            var sources = group.Select(x => x.Row.SourceId).Distinct(StringComparer.Ordinal).Order().ToList();
            if (variants.Count != 1)
            {
                conflicts.Add(new(group.Key.PpcId, group.Key.BusinessKey, group.Key.Day, sources, variants));
                excluded += group.Count();
                continue;
            }

            var first = group.First();
            periods.Add(new(group.Key.PpcId, group.Key.BusinessKey, group.Key.Day,
                first.FormulaSignature, first.FSignature, sources));
            includedRows.AddRange(group);
        }

        var formulas = includedRows
            .GroupBy(x => new { x.Row.PpcId, x.FormulaSignature })
            .Select(x => new FormulaDefinition(x.Key.PpcId, x.Key.FormulaSignature, x.First().Row.Formula.Trim()))
            .OrderBy(x => x.PpcId).ThenBy(x => x.Signature, StringComparer.Ordinal).ToList();
        var fTables = includedRows
            .GroupBy(x => x.FSignature, StringComparer.Ordinal)
            .Select(x => new FTableDefinition(x.Key, x.First().Cells))
            .OrderBy(x => x.Signature, StringComparer.Ordinal).ToList();

        return new(formulas, fTables, periods, conflicts, includedRows.Count, excluded);
    }

    public static bool ScheduleEquals(IEnumerable<ScheduleKey> left, IEnumerable<ScheduleKey> right)
        => left.Select(ScheduleText).Order(StringComparer.Ordinal)
            .SequenceEqual(right.Select(ScheduleText).Order(StringComparer.Ordinal), StringComparer.Ordinal);

    public static string FormulaSignature(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula)) throw new ArgumentException("Formula is required.", nameof(formula));
        return Regex.Replace(formula.Replace("$", string.Empty, StringComparison.Ordinal), @"\s+", string.Empty)
            .ToUpperInvariant();
    }

    public static IReadOnlyDictionary<string, decimal> NormalizeFCells(IReadOnlyDictionary<string, decimal> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var normalized = cells.ToDictionary(
            x => x.Key.Replace("$", string.Empty, StringComparison.Ordinal)
                .Replace("F!", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant(),
            x => x.Value,
            StringComparer.OrdinalIgnoreCase);
        if (normalized.Count != RequiredFCells.Length || RequiredFCells.Any(x => !normalized.ContainsKey(x)))
            throw new ArgumentException("A complete F table with exactly B3 through B10 is required.", nameof(cells));
        return RequiredFCells.ToDictionary(x => x, x => normalized[x], StringComparer.OrdinalIgnoreCase);
    }

    public static string FSignature(IReadOnlyDictionary<string, decimal> cells)
    {
        var normalized = NormalizeFCells(cells);
        return string.Join(";", RequiredFCells.Select(x => $"{x}={normalized[x].ToString("0.############################", CultureInfo.InvariantCulture)}"));
    }

    private static string ScheduleText(ScheduleKey value)
        => $"{value.PpcId}|{value.Day:yyyy-MM-dd}|{value.FormulaSignature}|{value.FSignature}";

    private sealed record PreparedRow(
        SourceScheduleRow Row,
        IReadOnlyDictionary<string, decimal> Cells,
        string FormulaSignature,
        string FSignature);
}
