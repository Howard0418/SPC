using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdjustmentParameterFormulaAlignment;

public enum FormulaAlignmentStatus
{
    NoFormula,
    AlreadyAligned,
    Update,
    MissingParameter,
    Ambiguous
}

public sealed record FormulaAlignmentResult(string Formula, FormulaAlignmentStatus Status, string? Reason = null);

/// <summary>
/// Converts only observable concentration action comparisons and tank-capacity
/// multiplication operands. Quoted text, unit divisors, targets and unrelated
/// multipliers are intentionally preserved.
/// </summary>
public static partial class FormulaParameterAlignment
{
    private const string Addition = "ADDITION_CONTROL_VALUE";
    private const string Dilution = "DILUTION_CONTROL_VALUE";
    private const string Capacity = "TANK_CAPACITY_LITERS";
    private const string Operand = @"(?:ADDITION_CONTROL_VALUE|DILUTION_CONTROL_VALUE|LSL|USL|[+-]?(?:\d+(?:\.\d*)?|\.\d+))";

    public static FormulaAlignmentResult AlignFormula(
        string? formula,
        decimal? tankCapacityLiters,
        decimal? additionControlValue,
        decimal? dilutionControlValue)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return new FormulaAlignmentResult(formula ?? string.Empty, FormulaAlignmentStatus.NoFormula);

        var changed = false;
        FormulaAlignmentStatus? blocked = null;
        string? reason = null;
        var semanticFormula = BlankThenAddRegex().Replace(formula, match =>
        {
            if (!additionControlValue.HasValue)
            {
                blocked = FormulaAlignmentStatus.MissingParameter;
                reason = "缺少添加管制值";
                return match.Value;
            }
            changed = true;
            return match.Groups["prefix"].Value + Addition + match.Groups["suffix"].Value;
        });
        var result = TransformOutsideQuotedText(semanticFormula, segment =>
        {
            var aligned = DirectComparisonRegex().Replace(segment, match => ReplaceComparison(match, reversed: false));
            aligned = ReversedComparisonRegex().Replace(aligned, match => ReplaceComparison(match, reversed: true));
            if (blocked.HasValue) return segment;
            var capacityResult = ReplaceCapacityOperands(aligned, tankCapacityLiters);
            changed |= capacityResult.Changed;
            return capacityResult.Formula;

            string ReplaceComparison(Match match, bool reversed)
            {
                if (blocked.HasValue) return match.Value;
                var op = match.Groups["op"].Value;
                var operand = match.Groups["operand"].Value;
                var additionDirection = reversed ? op.StartsWith('>') : op.StartsWith('<');
                var expectedName = additionDirection ? Addition : Dilution;
                var expectedValue = additionDirection ? additionControlValue : dilutionControlValue;

                if (operand.Equals(Addition, StringComparison.OrdinalIgnoreCase) ||
                    operand.Equals(Dilution, StringComparison.OrdinalIgnoreCase))
                    return match.Value;
                if (!expectedValue.HasValue)
                {
                    Block(FormulaAlignmentStatus.MissingParameter, $"缺少{(additionDirection ? "添加" : "稀釋")}管制值");
                    return match.Value;
                }
                if (!operand.Equals("LSL", StringComparison.OrdinalIgnoreCase) &&
                    !operand.Equals("USL", StringComparison.OrdinalIgnoreCase) &&
                    !decimal.TryParse(operand, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture, out _))
                {
                    Block(FormulaAlignmentStatus.Ambiguous,
                        $"門檻 {operand} 不等於目前{(additionDirection ? "添加" : "稀釋")}管制值 {expectedValue.Value.ToString(CultureInfo.InvariantCulture)}");
                    return match.Value;
                }

                changed = true;
                return reversed
                    ? expectedName + match.Groups["between1"].Value + op + match.Groups["between2"].Value + match.Groups["concentration"].Value
                    : match.Groups["concentration"].Value + match.Groups["between1"].Value + op + match.Groups["between2"].Value + expectedName;
            }
        });

        if (blocked.HasValue) return new FormulaAlignmentResult(formula, blocked.Value, reason);
        return new FormulaAlignmentResult(result, changed ? FormulaAlignmentStatus.Update : FormulaAlignmentStatus.AlreadyAligned);

        void Block(FormulaAlignmentStatus status, string detail)
        {
            blocked = status;
            reason = detail;
        }
    }

    private static (string Formula, bool Changed) ReplaceCapacityOperands(string segment, decimal? capacity)
    {
        if (!capacity.HasValue) return (segment, false);
        var changed = false;
        var formula = NumberRegex().Replace(segment, match =>
        {
            if (!decimal.TryParse(match.Value, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var value) || value != capacity.Value)
                return match.Value;
            var previous = PreviousNonWhitespace(segment, match.Index - 1);
            var next = NextNonWhitespace(segment, match.Index + match.Length);
            if (previous != '*' && next != '*') return match.Value;
            changed = true;
            return Capacity;
        });
        return (formula, changed);
    }

    private static char? PreviousNonWhitespace(string source, int index)
    {
        for (; index >= 0; index--) if (!char.IsWhiteSpace(source[index])) return source[index];
        return null;
    }

    private static char? NextNonWhitespace(string source, int index)
    {
        for (; index < source.Length; index++) if (!char.IsWhiteSpace(source[index])) return source[index];
        return null;
    }

    private static string TransformOutsideQuotedText(string formula, Func<string, string> transform)
    {
        var output = new StringBuilder(formula.Length + 32);
        var segment = new StringBuilder();
        var inString = false;
        for (var index = 0; index < formula.Length; index++)
        {
            var character = formula[index];
            if (character != '"')
            {
                segment.Append(character);
                continue;
            }
            if (!inString)
            {
                output.Append(transform(segment.ToString()));
                segment.Clear();
                inString = true;
                segment.Append(character);
                continue;
            }
            segment.Append(character);
            if (index + 1 < formula.Length && formula[index + 1] == '"')
            {
                segment.Append(formula[++index]);
                continue;
            }
            output.Append(segment);
            segment.Clear();
            inString = false;
        }
        output.Append(inString ? segment : transform(segment.ToString()));
        return output.ToString();
    }

    [GeneratedRegex(@"(?<concentration>Concentration)(?<between1>\s*)(?<op><=|>=|<|>)(?<between2>\s*)(?<operand>" + Operand + @")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DirectComparisonRegex();

    [GeneratedRegex(@"(?<operand>" + Operand + @")(?<between1>\s*)(?<op><=|>=|<|>)(?<between2>\s*)(?<concentration>Concentration)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReversedComparisonRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9_.])[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?![A-Za-z0-9_.])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    [GeneratedRegex("(?<prefix>Concentration\\s*(?:>=|>)\\s*)(?:[+-]?(?:\\d+(?:\\.\\d*)?|\\.\\d+))(?<suffix>\\s*,\\s*\"\"\\s*,\\s*\"添加\")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlankThenAddRegex();
}
