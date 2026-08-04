using System.Globalization;
using System.Text.RegularExpressions;

namespace MesSpc.Api.Services;

internal readonly record struct ParsedSpecificationRange(
    double? TargetValue,
    double? Lsl,
    double? Usl);

internal static partial class SpecificationRangeParser
{
    [GeneratedRegex(@"[-+]?\d+(?:\.\d+)?")]
    private static partial Regex NumberPattern();

    public static ParsedSpecificationRange Parse(string? specification, string? range)
    {
        var specificationNumbers = ExtractNumbers(specification);
        var rangeNumbers = ExtractNumbers(range);

        double? target = specificationNumbers.Count > 0 ? specificationNumbers[0] : null;
        double? lsl = null;
        double? usl = null;

        if (rangeNumbers.Count >= 2)
        {
            lsl = Math.Min(rangeNumbers[0], rangeNumbers[1]);
            usl = Math.Max(rangeNumbers[0], rangeNumbers[1]);
        }
        else if (target.HasValue &&
                 specificationNumbers.Count >= 2 &&
                 (specification?.Contains('±') == true || specification?.Contains("+/-", StringComparison.OrdinalIgnoreCase) == true))
        {
            var tolerance = Math.Abs(specificationNumbers[1]);
            lsl = target.Value - tolerance;
            usl = target.Value + tolerance;
        }

        return new ParsedSpecificationRange(target, lsl, usl);
    }

    private static List<double> ExtractNumbers(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        // 「13-17」中的連字號是區間分隔符，不應被解析成負號。
        var normalized = Regex.Replace(text, @"(?<=\d)\s*-\s*(?=\d)", "~");
        return NumberPattern().Matches(normalized)
            .Select(x => double.Parse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture))
            .ToList();
    }
}
