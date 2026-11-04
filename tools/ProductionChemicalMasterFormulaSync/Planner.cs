using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProductionChemicalMasterFormulaSync;

public sealed record ChemicalMasterFormulaRow(
    int PpcId,
    string NaturalKey,
    bool IsEnabled,
    string ControlScope,
    string ConfigJson);

public sealed record ChemicalMasterFormulaChange(
    int SourcePpcId,
    int TargetPpcId,
    string NaturalKey,
    string OriginalConfigJson,
    string MergedConfigJson,
    string? ConcentrationFormula,
    string? RecheckConcentrationFormula);

public sealed record ChemicalMasterFormulaSyncPlan(
    IReadOnlyList<ChemicalMasterFormulaChange> Changes,
    IReadOnlyList<string> Unchanged);

public static class ProductionChemicalMasterFormulaSyncPlanner
{
    public static ChemicalMasterFormulaSyncPlan Build(
        IReadOnlyCollection<ChemicalMasterFormulaRow> sourceRows,
        IReadOnlyCollection<ChemicalMasterFormulaRow> targetRows)
    {
        ValidateRows(sourceRows, "source");
        ValidateRows(targetRows, "target");

        var source = ToUniqueDictionary(sourceRows, "source");
        var target = ToUniqueDictionary(targetRows, "target");
        if (!source.Keys.Order().SequenceEqual(target.Keys.Order()))
        {
            var sourceOnly = source.Keys.Except(target.Keys, StringComparer.Ordinal).Order().ToArray();
            var targetOnly = target.Keys.Except(source.Keys, StringComparer.Ordinal).Order().ToArray();
            throw new InvalidOperationException($"Source and target natural key sets differ. source-only=[{string.Join(",", sourceOnly)}]; target-only=[{string.Join(",", targetOnly)}]");
        }

        var changes = new List<ChemicalMasterFormulaChange>();
        var unchanged = new List<string>();
        foreach (var key in source.Keys.Order(StringComparer.Ordinal))
        {
            var sourceRow = source[key];
            var targetRow = target[key];
            var sourceConfig = JsonNode.Parse(sourceRow.ConfigJson)!.AsObject();
            var targetConfig = JsonNode.Parse(targetRow.ConfigJson)!.AsObject();
            var concentration = ReadOptionalString(sourceConfig, "concentrationFormula");
            var recheck = ReadOptionalString(sourceConfig, "recheckConcentrationFormula");
            var targetConcentration = ReadOptionalString(targetConfig, "concentrationFormula");
            var targetRecheck = ReadOptionalString(targetConfig, "recheckConcentrationFormula");

            if (concentration == targetConcentration && recheck == targetRecheck)
            {
                unchanged.Add(key);
                continue;
            }

            SetOptionalString(targetConfig, "concentrationFormula", concentration);
            SetOptionalString(targetConfig, "recheckConcentrationFormula", recheck);
            changes.Add(new(
                sourceRow.PpcId,
                targetRow.PpcId,
                key,
                targetRow.ConfigJson,
                targetConfig.ToJsonString(),
                concentration,
                recheck));
        }

        return new(changes, unchanged);
    }

    public static void ValidateProductionTarget(string appEnvironment, string dataSource, string database, string apiEnvironment)
    {
        var validServer = dataSource.Contains("172.16.110.16", StringComparison.OrdinalIgnoreCase)
            || dataSource.Contains("PMR-MESSQL", StringComparison.OrdinalIgnoreCase);
        if (!appEnvironment.Equals("production", StringComparison.OrdinalIgnoreCase)
            || !apiEnvironment.Equals("production", StringComparison.OrdinalIgnoreCase)
            || !database.Equals("PMR_SPC_2026", StringComparison.OrdinalIgnoreCase)
            || !validServer)
        {
            throw new InvalidOperationException("Production target guard rejected the supplied environment.");
        }
    }

    public static void ValidateTestSource(string appEnvironment, string dataSource, string database)
    {
        if (!appEnvironment.Equals("test", StringComparison.OrdinalIgnoreCase)
            || !dataSource.Contains("172.16.110.16", StringComparison.OrdinalIgnoreCase)
            || !database.Equals("PMR_SPC_TEST", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Test source guard rejected the supplied environment.");
        }
    }

    private static Dictionary<string, ChemicalMasterFormulaRow> ToUniqueDictionary(
        IEnumerable<ChemicalMasterFormulaRow> rows,
        string label)
    {
        var result = new Dictionary<string, ChemicalMasterFormulaRow>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!result.TryAdd(row.NaturalKey, row))
            {
                throw new InvalidOperationException($"{label} contains duplicate natural key '{row.NaturalKey}'.");
            }
        }

        return result;
    }

    private static void ValidateRows(IEnumerable<ChemicalMasterFormulaRow> rows, string label)
    {
        foreach (var row in rows)
        {
            if (!row.IsEnabled || !row.ControlScope.Equals("CHEM", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{label} row '{row.NaturalKey}' is not an enabled CHEM mapping.");
            }

            using var document = JsonDocument.Parse(row.ConfigJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{label} row '{row.NaturalKey}' config is not a JSON object.");
            }
        }
    }

    private static string? ReadOptionalString(JsonObject config, string property)
    {
        if (!config.TryGetPropertyValue(property, out var node) || node is null)
        {
            return null;
        }

        return node.GetValue<string>();
    }

    private static void SetOptionalString(JsonObject config, string property, string? value)
    {
        if (value is null)
        {
            config.Remove(property);
        }
        else
        {
            config[property] = value;
        }
    }
}
