using System.Globalization;
using System.Text.Json.Nodes;

namespace ChemicalFormulaMigration;

/// <summary>Only source formulas may create concentration calculation settings.</summary>
public static class ConcentrationSourcePolicy
{
    public static bool IsUnset(string? source) => string.IsNullOrWhiteSpace(source)
        || source.Trim().Equals("直接量測值", StringComparison.Ordinal);

    public static string Convert(string? source, decimal constant, decimal primary, decimal secondary)
    {
        if (IsUnset(source)) return "";
        static string Fmt(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
        return $"ROUND(({Fmt(constant)})+({Fmt(primary)}*Primary)+({Fmt(secondary)}*Secondary),8)";
    }
}

public sealed record SourceFormulaRule(string Id, string? SourceFormula);
public sealed record UnsetFormulaRow(int PpcId, string ConfigJson);
public sealed record UnsetFormulaChange(int PpcId, string SourceRuleId, string BeforeJson, string AfterJson);

/// <summary>Clears only generated identity formulas with explicit missing-source evidence.</summary>
public static class UnsetConcentrationPlanner
{
    private const string GeneratedIdentity = "ROUND((0)+(1*Primary)+(0*Secondary),8)";

    public static IReadOnlyList<UnsetFormulaChange> Build(IReadOnlyCollection<SourceFormulaRule> rules,
        IReadOnlyCollection<UnsetFormulaRow> rows)
    {
        var sources = rules.ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (rows.Select(x => x.PpcId).Distinct().Count() != rows.Count)
            throw new InvalidOperationException("Duplicate PPC rows.");
        var changes = new List<UnsetFormulaChange>();
        foreach (var row in rows)
        {
            var config = JsonNode.Parse(row.ConfigJson)?.AsObject() ?? throw new InvalidOperationException("Invalid config.");
            var id = config["migratedFrom"]?.GetValue<string>();
            if (id is null || !sources.TryGetValue(id, out var source) || !ConcentrationSourcePolicy.IsUnset(source.SourceFormula)) continue;
            if (config["concentrationFormula"]?.GetValue<string>() != GeneratedIdentity) continue;
            var recheck = config["recheckConcentrationFormula"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(recheck) && recheck != GeneratedIdentity)
                throw new InvalidOperationException($"PPC {row.PpcId} has a distinct recheck formula; source confirmation required.");
            config["concentrationFormula"] = "";
            if (config.ContainsKey("recheckConcentrationFormula")) config["recheckConcentrationFormula"] = "";
            config["enabled"] = false;
            changes.Add(new(row.PpcId, id, row.ConfigJson, config.ToJsonString()));
        }
        return changes;
    }
}
