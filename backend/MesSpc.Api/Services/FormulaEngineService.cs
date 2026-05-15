namespace MesSpc.Api.Services;

public class FormulaEngineService
{
    public double EvaluateByExpression(string expression, IReadOnlyCollection<double> values, double? usl = null, double? lsl = null)
    {
        if (values.Count == 0) return 0;
        var formulaCode = expression.Split('(')[0].Trim().ToUpperInvariant();
        var arr = values.ToArray();
        var avg = arr.Average();
        var stdev = StdDev(arr);

        return formulaCode.ToUpperInvariant() switch
        {
            "AVG" => avg,
            "STDEV" => stdev,
            "RANGE" => arr.Max() - arr.Min(),
            "MAX" => arr.Max(),
            "MIN" => arr.Min(),
            "COUNT" => arr.Length,
            "SUM" => arr.Sum(),
            "CP" when usl.HasValue && lsl.HasValue && stdev > 0 => (usl.Value - lsl.Value) / (6 * stdev),
            "CPK" when usl.HasValue && lsl.HasValue && stdev > 0 =>
                Math.Min((usl.Value - avg) / (3 * stdev), (avg - lsl.Value) / (3 * stdev)),
            _ => throw new InvalidOperationException($"Unsupported formula code: {formulaCode}")
        };
    }

    private static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count <= 1) return 0;
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
