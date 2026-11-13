using System.Text.Json.Nodes;

namespace C1ExcelAlignment;

public sealed record FormulaSlice(DateTime FromUtc, DateTime? UntilUtc, long FormulaId, long? FId);
public sealed record ScheduleAlignment(bool Changed, IReadOnlyList<FormulaSlice> Periods);

public static class AlignmentPlan
{
    public static string AlignConfig(string source, bool tankOneSulfuric)
    {
        var root = JsonNode.Parse(source)?.AsObject()
            ?? throw new InvalidOperationException("Formula config is not an object.");
        root["decimalPlaces"] = 3;
        if (!tankOneSulfuric) return root.ToJsonString();

        root["adjustmentFormula"] = "IF(Concentration=\"\",\"\",IF(Concentration<180,\"添加\",IF(Concentration>195,\"稀釋\",\"\")))";
        root["adjustmentAmountFormula"] = "IFERROR(IF(Concentration<180,\"硫酸(50%)：\"&ROUND(((180-Concentration)*1900/0.714/1000),1)&\" L\",IF(Concentration=\"\",\"\",IF(Concentration>195,\"通知工程師\",\"\"))),\"\")";
        return root.ToJsonString();
    }

    public static ScheduleAlignment SplitAt(
        IEnumerable<FormulaSlice> source,
        DateTime cutoffUtc,
        long expectedFormulaId,
        long expectedFId,
        long replacementFormulaId)
    {
        if (expectedFormulaId <= 0 || expectedFId <= 0 || replacementFormulaId <= 0)
            throw new InvalidOperationException("Reviewed formulas and F history are required.");

        var rows = source.OrderBy(x => x.FromUtc).ToList();
        Validate(rows);
        if (rows.Count == 0) throw new InvalidOperationException("Current schedule is empty.");

        var current = rows.SingleOrDefault(x => x.FromUtc <= cutoffUtc && (x.UntilUtc is null || cutoffUtc < x.UntilUtc))
            ?? throw new InvalidOperationException("Cutoff is outside the reviewed schedule.");
        if (current.FormulaId == replacementFormulaId && current.FId == expectedFId && current.FromUtc == cutoffUtc)
            return new(false, rows);
        if (current.FormulaId != expectedFormulaId || current.FId != expectedFId || current.UntilUtc is not null)
            throw new InvalidOperationException("Current effectiveness differs from the reviewed source.");
        if (cutoffUtc <= current.FromUtc)
            throw new InvalidOperationException("Cutoff must split the current open period.");

        var index = rows.IndexOf(current);
        rows[index] = current with { UntilUtc = cutoffUtc };
        rows.Insert(index + 1, new FormulaSlice(cutoffUtc, null, replacementFormulaId, expectedFId));
        Validate(rows);
        return new(true, rows);
    }

    private static void Validate(IReadOnlyList<FormulaSlice> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var current = rows[index];
            if (current.UntilUtc <= current.FromUtc)
                throw new InvalidOperationException("Invalid schedule period.");
            if (index == 0) continue;
            var previous = rows[index - 1];
            if (previous.UntilUtc is null || previous.UntilUtc > current.FromUtc)
                throw new InvalidOperationException("Schedule periods must be non-overlapping.");
        }
    }
}
