using MesSpc.Api.Services.Localization;
// Portal ExcelFormulaEvaluator compatibility snapshot (2026-10-20). Engine: portal-excel8-v1.
// TASK-004 clients use the SPC contract; keep semantics covered by cross-repository fixtures.
using System.Globalization;

namespace MesSpc.Api.Services;

internal static class ChemicalExcelFormulaEvaluator
{
    public static string EvaluateText(string? formula, IReadOnlyDictionary<string, decimal> cells)
    {
        if (string.IsNullOrWhiteSpace(formula)) return string.Empty;
        return new Parser(formula, cells).Parse().AsText(); // Invalid formulas are rejected by the new contract.
    }

    public static decimal EvaluateNumber(string? formula, IReadOnlyDictionary<string, decimal> cells)
    {
        if (string.IsNullOrWhiteSpace(formula)) throw ApiUiException.Attach(new ArgumentException("公式不可為空白。", nameof(formula)), "spc.api.excelFormulaRequired");
        var text = new Parser(formula, cells).Parse().AsText();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            throw ApiUiException.Attach(new InvalidOperationException("公式結果不是數字。"), "spc.api.excelResultNotNumeric");
        return value;
    }

    private readonly record struct Value(decimal? Number = null, string? Text = null, bool? Boolean = null)
    {
        public decimal AsNumber() => Number ?? (Boolean == true ? 1m : 0m);
        public bool AsBoolean() => Boolean ?? (Number.HasValue ? Number.Value != 0 : !string.IsNullOrEmpty(Text));
        public string AsText() => Text ?? (Boolean.HasValue ? (Boolean.Value ? "TRUE" : "FALSE") : Number?.ToString("0.########", CultureInfo.InvariantCulture) ?? "");
    }

    private sealed class Parser(string source, IReadOnlyDictionary<string, decimal> cells)
    {
        private int _position;

        public Value Parse()
        {
            var value = ParseComparison();
            Skip();
            if (_position != source.Length) throw ApiUiException.Attach(new FormatException("公式包含未支援的語法。"), "spc.api.excelSyntaxUnsupported");
            return value;
        }

        private Value ParseComparison()
        {
            var left = ParseConcat();
            Skip();
            var op = ReadOperator("<=", ">=", "<>", "=", "<", ">");
            if (op is null) return left;
            var right = ParseConcat();
            var leftBlank = left.Text == string.Empty && !left.Number.HasValue && !left.Boolean.HasValue;
            var rightBlank = right.Text == string.Empty && !right.Number.HasValue && !right.Boolean.HasValue;
            if ((leftBlank || rightBlank) && op is not "=" and not "<>") return new(Boolean: false);
            var numeric = left.Number.HasValue && right.Number.HasValue;
            var compare = numeric
                ? left.AsNumber().CompareTo(right.AsNumber())
                : string.Compare(left.AsText(), right.AsText(), StringComparison.OrdinalIgnoreCase);
            return new(Boolean: op switch { "=" => compare == 0, "<>" => compare != 0, "<" => compare < 0, ">" => compare > 0, "<=" => compare <= 0, ">=" => compare >= 0, _ => false });
        }

        private Value ParseConcat()
        {
            var value = ParseAdditive();
            while (Try('&')) value = new(Text: value.AsText() + ParseAdditive().AsText());
            return value;
        }

        private Value ParseAdditive()
        {
            var value = ParseMultiplicative();
            while (true)
            {
                if (Try('+')) value = new(Number: value.AsNumber() + ParseMultiplicative().AsNumber());
                else if (Try('-')) value = new(Number: value.AsNumber() - ParseMultiplicative().AsNumber());
                else return value;
            }
        }

        private Value ParseMultiplicative()
        {
            var value = ParseUnary();
            while (true)
            {
                if (Try('*')) value = new(Number: value.AsNumber() * ParseUnary().AsNumber());
                else if (Try('/')) value = new(Number: value.AsNumber() / ParseUnary().AsNumber());
                else return value;
            }
        }

        private Value ParseUnary()
        {
            if (Try('+')) return ParseUnary();
            if (Try('-')) return new(Number: -ParseUnary().AsNumber());
            var value = ParsePrimary();
            while (Try('%')) value = new(Number: value.AsNumber() / 100m);
            return value;
        }

        private Value ParsePrimary()
        {
            Skip();
            if (Try('(')) { var value = ParseComparison(); Expect(')'); return value; }
            if (Peek() == '"') return new(Text: ReadString());
            if (char.IsDigit(Peek()) || Peek() == '.') return new(Number: ReadNumber());
            var identifier = ReadIdentifier();
            Skip();
            if (Try('(')) return ParseFunction(identifier);
            var key = identifier.Replace("$", "", StringComparison.Ordinal).ToUpperInvariant();
            return cells.TryGetValue(key, out var number) ? new(Number: number) : new(Text: string.Empty);
        }

        private Value ParseFunction(string name)
        {
            name = name.ToUpperInvariant();
            if (name == "IF")
            {
                var condition = ParseComparison().AsBoolean(); Expect(',');
                var whenTrue = ParseComparison(); Expect(',');
                var whenFalse = ParseComparison(); Expect(')');
                return condition ? whenTrue : whenFalse;
            }
            if (name == "IFERROR")
            {
                Value value;
                try { value = ParseComparison(); } catch { SkipArgument(); value = default; }
                Expect(','); var fallback = ParseComparison(); Expect(')');
                return value.Number.HasValue || value.Text is not null || value.Boolean.HasValue ? value : fallback;
            }
            if (name == "ROUND")
            {
                var number = ParseComparison().AsNumber(); Expect(',');
                var digits = (int)ParseComparison().AsNumber(); Expect(')');
                return new(Number: decimal.Round(number, Math.Clamp(digits, 0, 28), MidpointRounding.AwayFromZero));
            }
            throw ApiUiException.Attach(new NotSupportedException($"不支援函數 {name}。"), "spc.api.excelFunctionUnsupported", new Dictionary<string,string> { ["name"] = name });
        }

        private void SkipArgument()
        {
            var depth = 0; var quoted = false;
            while (_position < source.Length)
            {
                var ch = source[_position];
                if (ch == '"') quoted = !quoted;
                if (!quoted) { if (ch == '(') depth++; else if (ch == ')') depth--; else if (ch == ',' && depth == 0) return; }
                _position++;
            }
        }

        private string ReadIdentifier()
        {
            Skip(); var start = _position;
            while (_position < source.Length && (char.IsLetterOrDigit(source[_position]) || source[_position] is '$' or '!' or '_' or '.')) _position++;
            if (start == _position) throw ApiUiException.Attach(new FormatException("缺少識別字。"), "spc.api.excelIdentifierRequired");
            return source[start.._position];
        }

        private string ReadString()
        {
            Expect('"'); var result = new System.Text.StringBuilder();
            while (_position < source.Length)
            {
                var ch = source[_position++];
                if (ch != '"') { result.Append(ch); continue; }
                if (_position < source.Length && source[_position] == '"') { result.Append('"'); _position++; continue; }
                return result.ToString();
            }
            throw ApiUiException.Attach(new FormatException("字串未結束。"), "spc.api.excelStringUnterminated");
        }

        private decimal ReadNumber()
        {
            var start = _position;
            while (_position < source.Length && (char.IsDigit(source[_position]) || source[_position] == '.')) _position++;
            return decimal.Parse(source[start.._position], CultureInfo.InvariantCulture);
        }

        private string? ReadOperator(params string[] candidates)
        {
            Skip();
            foreach (var candidate in candidates)
                if (source.AsSpan(_position).StartsWith(candidate, StringComparison.Ordinal)) { _position += candidate.Length; return candidate; }
            return null;
        }

        private bool Try(char expected) { Skip(); if (Peek() != expected) return false; _position++; return true; }
        private void Expect(char expected) { if (!Try(expected)) throw ApiUiException.Attach(new FormatException($"缺少 {expected}。"), "spc.api.excelTokenMissing", new Dictionary<string,string> { ["token"] = expected.ToString() }); }
        private char Peek() => _position < source.Length ? source[_position] : '\0';
        private void Skip() { while (_position < source.Length && char.IsWhiteSpace(source[_position])) _position++; }
    }
}
