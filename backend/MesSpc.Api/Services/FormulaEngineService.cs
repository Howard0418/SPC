using NCalc;

namespace MesSpc.Api.Services;

public class FormulaEngineService
{
    public double EvaluateByExpression(string expression, IReadOnlyCollection<double> values, double? usl = null, double? lsl = null)
    {
        if (values.Count == 0 || string.IsNullOrWhiteSpace(expression)) return 0;

        var arr = values.ToArray();
        var avg = arr.Average();
        var stdev = StdDev(arr);
        var max = arr.Max();
        var min = arr.Min();
        var range = max - min;
        var sum = arr.Sum();
        var count = arr.Length;

        // Create NCalc expression
        var e = new Expression(expression);

        // Bind common statistical variables
        e.Parameters["AVG"] = avg;
        e.Parameters["STDEV"] = stdev;
        e.Parameters["MAX"] = max;
        e.Parameters["MIN"] = min;
        e.Parameters["RANGE"] = range;
        e.Parameters["SUM"] = sum;
        e.Parameters["COUNT"] = count;

        if (usl.HasValue) e.Parameters["USL"] = usl.Value;
        if (lsl.HasValue) e.Parameters["LSL"] = lsl.Value;

        // Optional: Pre-calculate common capability indices if they just ask for them by name
        if (expression.Trim().Equals("CP", StringComparison.OrdinalIgnoreCase))
        {
            if (usl.HasValue && lsl.HasValue && stdev > 0) return (usl.Value - lsl.Value) / (6 * stdev);
            return 0;
        }
        if (expression.Trim().Equals("CPK", StringComparison.OrdinalIgnoreCase))
        {
            if (usl.HasValue && lsl.HasValue && stdev > 0)
                return Math.Min((usl.Value - avg) / (3 * stdev), (avg - lsl.Value) / (3 * stdev));
            return 0;
        }

        var result = e.Evaluate();
        return Convert.ToDouble(result);
    }

    private static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count <= 1) return 0;
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
