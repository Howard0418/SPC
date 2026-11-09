using System.Text.Json.Nodes;

namespace ChemicalFormulaMigration;

public sealed record NaAdjustmentRow(int PpcId, string Token, string ConfigJson);
public sealed record NaAdjustmentChange(int PpcId, string Token, string BeforeJson, string AfterJson);

/// <summary>Removes only adjustment branches whose referenced Excel condition is explicitly NA.</summary>
public static class NaAdjustmentPlanner
{
    private static readonly IReadOnlyDictionary<int, string> Approved = new Dictionary<int, string>
    {
        [2515] = "U13",
        [2516] = "U14",
        [2518] = "U16",
        [2619] = "U13"
    };

    public static IReadOnlyList<NaAdjustmentChange> Build(IReadOnlyCollection<NaAdjustmentRow> rows)
    {
        if (rows.Select(x => x.PpcId).Distinct().Count() != rows.Count)
            throw new InvalidOperationException("Duplicate PPC rows.");
        var changes = new List<NaAdjustmentChange>();
        foreach (var row in rows)
        {
            if (!Approved.TryGetValue(row.PpcId, out var token) || token != row.Token)
                throw new InvalidOperationException($"PPC {row.PpcId} is not approved for {row.Token} NA cleanup.");
            var config = JsonNode.Parse(row.ConfigJson)?.AsObject() ?? throw new InvalidOperationException("Invalid config.");
            var adjustment = config["adjustmentFormula"]?.GetValue<string>() ?? "";
            var amount = config["adjustmentAmountFormula"]?.GetValue<string>() ?? "";
            if (!adjustment.Contains(token, StringComparison.Ordinal) && !amount.Contains(token, StringComparison.Ordinal))
                continue;

            if (row.PpcId == 2518)
            {
                const string expected = "IF(Concentration=\"\",\"\",IF(Concentration>U16,\"更槽\",\"\"))";
                if (adjustment != expected || !string.IsNullOrWhiteSpace(amount))
                    throw new InvalidOperationException("PPC 2518 formula differs from the reviewed NA-only tank-change rule.");
                config["adjustmentFormula"] = "";
                config["adjustmentAmountFormula"] = "";
            }
            else
            {
                var newAdjustment = RemoveUnsupportedElseIf(adjustment, token);
                var newAmount = RemoveUnsupportedElseIf(amount, token);
                if (newAdjustment.Contains(token, StringComparison.Ordinal) || newAmount.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"PPC {row.PpcId} still references {token}.");
                config["adjustmentFormula"] = newAdjustment;
                config["adjustmentAmountFormula"] = newAmount;
            }
            changes.Add(new(row.PpcId, token, row.ConfigJson, config.ToJsonString()));
        }
        return changes;
    }

    private static string RemoveUnsupportedElseIf(string formula, string token)
    {
        var marker = $",IF(Concentration>{token},";
        var comma = formula.IndexOf(marker, StringComparison.Ordinal);
        if (comma < 0) throw new InvalidOperationException($"Reviewed formula does not contain the expected {token} else branch.");
        var start = comma + 1;
        var depth = 0;
        var quoted = false;
        for (var i = start; i < formula.Length; i++)
        {
            var ch = formula[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < formula.Length && formula[i + 1] == '"') { i++; continue; }
                quoted = !quoted;
                continue;
            }
            if (quoted) continue;
            if (ch == '(') depth++;
            else if (ch == ')' && --depth == 0)
                return formula[..comma] + ",\"\"" + formula[(i + 1)..];
        }
        throw new InvalidOperationException($"Reviewed {token} formula is unbalanced.");
    }
}
