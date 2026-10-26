using MesSpc.Api.Services.Localization;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MesSpc.Api.Services;

public sealed record ChemicalSchedulePeriod(long FormulaVersionId, long? FTableHistoryId, string FromDate, string? ToDate);
public sealed record ChemicalUtcPeriod(long FormulaVersionId, long? FTableHistoryId, DateTime FromUtc, DateTime? UntilUtc);
public sealed record ChemicalCalculatedValue(string Raw, string Stored, string Adjustment, string AdjustmentAmount, string Judgment);

/// <summary>Explicit-offset dates, mandatory inputs and versioned Portal-compatible chemical calculation.</summary>
public static class ChemicalFormulaCalculation
{
    public const string EngineVersion = "portal-excel8-v1";
    public const string PrecisionPolicy = "decimal-excel8-away-from-zero-v1";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex Identifiers = new("[A-Za-z_$][A-Za-z0-9_$!.]*", RegexOptions.Compiled);

    public static string Canonical(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    public static DateTime ParseMeasuredAt(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$") ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw ApiUiException.Attach(new ArgumentException("量測日期須為有效ISO8601時間並含Z或時區，例如2026-11-01T00:00:00+08:00。"), "spc.api.chemicalMeasuredAtInvalid");
        return date.UtcDateTime;
    }

    public static List<ChemicalUtcPeriod> ValidatePeriods(IReadOnlyList<ChemicalSchedulePeriod> periods)
    {
        if (periods.Count is < 1 or > 200) throw ApiUiException.Attach(new ArgumentException("排程須有1至200個期間。"), "spc.api.schedulePeriodCount");
        var result = periods.Select(p => {
            if (string.IsNullOrWhiteSpace(p.FromDate) || p.FormulaVersionId <= 0) throw ApiUiException.Attach(new ArgumentException("每個期間須選公式版本與起日。"), "spc.api.schedulePeriodFieldsRequired");
            var range = VersionHistoryDateRange.Parse(p.FromDate, p.ToDate);
            return new ChemicalUtcPeriod(p.FormulaVersionId, p.FTableHistoryId, range.FromUtc!.Value, range.UntilUtc);
        }).OrderBy(p => p.FromUtc).ToList();
        for (var i = 1; i < result.Count; i++)
            if (!result[i-1].UntilUtc.HasValue || result[i].FromUtc < result[i-1].UntilUtc)
                throw ApiUiException.Attach(new ArgumentException("生效日期區間重疊，請調整起迄日；迄日包含當天。"), "spc.api.schedulePeriodOverlap");
        return result;
    }

    public static ChemicalCalculatedValue Calculate(string configJson, string primary, string? secondary, string fCellsJson,
        decimal? lsl, decimal? usl, decimal? target)
    {
        var config = JsonSerializer.Deserialize<ChemicalCalculationConfig>(configJson, JsonOptions)
            ?? throw ApiUiException.Attach(new ArgumentException("公式配置無效。"), "spc.api.chemicalConfigInvalid");
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ConcentrationFormula) || config.DecimalPlaces is < 0 or > 8)
            throw ApiUiException.Attach(new ArgumentException("請選啟用且有濃度公式、精度0至8的歷史版本。"), "spc.api.chemicalVersionPrecisionInvalid");
        var cells = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["PRIMARY"] = ReadDecimal(primary, "Primary") };
        if (!string.IsNullOrWhiteSpace(secondary)) cells["SECONDARY"] = ReadDecimal(secondary, "Secondary");
        if (lsl.HasValue) cells["LSL"] = lsl.Value;
        if (usl.HasValue) cells["USL"] = usl.Value;
        if (target.HasValue) cells["TARGET"] = target.Value;
        foreach (var cell in JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(fCellsJson, JsonOptions) ?? [])
            if (cell.NumericValue.HasValue) cells[ChemicalFTableService.NormalizeCellAddress("F!" + cell.CellAddress.Replace("F!", "", StringComparison.OrdinalIgnoreCase))] = cell.NumericValue.Value;
        decimal? raw = null;
        string Evaluate(string? formula, bool numeric)
        {
            if (string.IsNullOrWhiteSpace(formula)) return "";
            // Strip quoted literals before identifying variables. Do not silently coerce unknown input to zero.
            var withoutStrings = Regex.Replace(formula, "\"(?:\"\"|[^\"])*\"", "\"\"");
            foreach (Match match in Identifiers.Matches(withoutStrings))
            {
                var key = match.Value.Replace("$", "").ToUpperInvariant();
                if (Regex.IsMatch(withoutStrings[(match.Index+match.Length)..], @"^\s*\(")) continue;
                if (!cells.ContainsKey(key)) throw ApiUiException.Attach(new ArgumentException($"缺少公式所需輸入或F快照儲存格：{key}。"), "spc.api.chemicalInputCellMissing", new Dictionary<string,string> { ["cell"] = key });
            }
            return numeric ? Canonical(ChemicalExcelFormulaEvaluator.EvaluateNumber(formula, cells))
                : ChemicalExcelFormulaEvaluator.EvaluateText(formula, cells);
        }
        try
        {
            raw = ReadDecimal(Evaluate(config.ConcentrationFormula, true), "計算結果");
            cells["CONCENTRATION"] = raw.Value;
            var adjustment = Evaluate(config.AdjustmentFormula, false);
            var amount = Evaluate(config.AdjustmentAmountFormula, false);
            var judgment = lsl.HasValue && raw < lsl || usl.HasValue && raw > usl ? "NG" : lsl.HasValue || usl.HasValue ? "OK" : "Unknown";
            return new(Canonical(raw.Value), Canonical(decimal.Round(raw.Value, config.DecimalPlaces, MidpointRounding.AwayFromZero)), adjustment, amount, judgment);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or DivideByZeroException or NotSupportedException or InvalidOperationException)
        { throw ApiUiException.Attach(new ArgumentException("公式無法計算，請核對語法、除數與輸入。", ex), "spc.api.chemicalCalculationInvalid"); }
    }

    public static decimal ReadDecimal(string? text, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 128 || !Regex.IsMatch(text, @"^[+-]?\d+(\.\d+)?$") ||
            !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            throw ApiUiException.Attach(new ArgumentException($"{label}須提供未先取位的有效十進位原始字串。"), "spc.api.chemicalDecimalRequired", new Dictionary<string,string> { ["field"] = label });
        // decimal.TryParse rounds >28 digits: reject representations that cannot be retained exactly.
        var canonicalInput = text.TrimStart('+');
        if (!System.Numerics.BigInteger.TryParse(canonicalInput.Replace(".", ""), out var digits)) throw ApiUiException.Attach(new ArgumentException("原始值無效。"), "spc.api.chemicalRawInvalid");
        var scale = canonicalInput.Contains('.') ? canonicalInput.Length - canonicalInput.IndexOf('.') - 1 : 0;
        var bits = decimal.GetBits(value);
        var actual = ((System.Numerics.BigInteger)(uint)bits[2] << 64) + ((System.Numerics.BigInteger)(uint)bits[1] << 32) + (uint)bits[0];
        if (value < 0) actual = -actual;
        var actualScale = (bits[3] >> 16) & 0xff;
        if (digits * System.Numerics.BigInteger.Pow(10, actualScale) != actual * System.Numerics.BigInteger.Pow(10, scale))
            throw ApiUiException.Attach(new ArgumentException($"{label}超出可精確保存的decimal精度。"), "spc.api.chemicalDecimalPrecision", new Dictionary<string,string> { ["field"] = label });
        return value;
    }
}

public sealed record ChemicalCalculationConfig(bool Enabled = false, string ConcentrationFormula = "", string? AdjustmentFormula = null,
    string? AdjustmentAmountFormula = null, int DecimalPlaces = 2);
