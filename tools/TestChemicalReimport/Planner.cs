using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TestChemicalReimport;

public sealed record SourceIdentity(string Line, string Slot, string AnalysisItem, string SamplingStage, DateOnly Day);
public sealed record MasterIdentity(int PpcId, string MachineCode, string TankName, string CharacteristicName, bool IsEnabled);

public static partial class TestChemicalReimportPlanner
{
    [GeneratedRegex(@"\([^)]*\)|（[^）]*）|\[[^]]*\]", RegexOptions.CultureInvariant)]
    private static partial Regex Parenthetical();

    public static string Canon(string? value)
    {
        var first = (value ?? "").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "";
        var normalized = first.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    public static string CanonItem(string? value)
    {
        var first = (value ?? "").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "";
        return Canon(Parenthetical().Replace(first, ""));
    }

    public static IReadOnlyDictionary<SourceIdentity, int> Resolve(IEnumerable<SourceIdentity> sources, IEnumerable<MasterIdentity> masters)
    {
        var enabled = masters.Where(x => x.IsEnabled).ToArray();
        var result = new Dictionary<SourceIdentity, int>();
        foreach (var source in sources.Distinct())
        {
            if (source.Day < new DateOnly(2026, 9, 1)) throw new InvalidOperationException("9月前來源禁止匯入。");
            var candidates = enabled.Where(x =>
                LineMatches(source.Line, x.MachineCode) && Canon(x.TankName) == Canon(source.Slot) &&
                CanonItem(x.CharacteristicName) == CanonItem(source.AnalysisItem)).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException($"主檔無法唯一對應：{source.Line}/{source.Slot}/{source.AnalysisItem}，候選={candidates.Length}。");
            result[source] = candidates[0].PpcId;
        }
        return result;
    }

    public static bool LineMatches(string sourceLine, string machineCode)
    {
        var source = Canon(sourceLine); var machine = Canon(machineCode);
        if (source == machine) return true;
        return source switch
        {
            "舊線" => machine is "PT1" or "QE1" or "ST1" or "DV1",
            "新線" => machine is "PT2" or "QE2" or "ST2" or "DV2",
            _ => false
        };
    }

    public static string Phase(string shift) => shift.Trim().ToUpperInvariant() switch
    {
        "EARLY" => "OPEN",
        "MIDDLE" => "MIDDLE",
        _ => throw new InvalidOperationException("只接受早班或中班。")
    };

    public static decimal RequiredDecimal(object? value, string label)
    {
        if (value is null || !decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidOperationException(label + "不是有效數值。");
        return parsed;
    }

    public static Guid NormalizeUploadBatch(Guid legacyUploadBatchId, Guid controlledReimportBatchId)
        => controlledReimportBatchId == Guid.Empty
            ? throw new InvalidOperationException("受控重匯批次不得為空。")
            : controlledReimportBatchId;

    public static bool IsSeptember2026(DateTime? portalDailyDate, DateTime measuredAt)
    {
        var day = portalDailyDate ?? measuredAt;
        return day >= new DateTime(2026, 9, 1) && day < new DateTime(2026, 10, 1);
    }

    public static bool IsIsolatedReconciliationTarget(params string?[] labels)
        => labels.Any(x => !string.IsNullOrWhiteSpace(x) &&
            (x.Contains("十月隔離測試", StringComparison.OrdinalIgnoreCase) ||
             x.Contains("Excel藥液對帳", StringComparison.OrdinalIgnoreCase)));

    public static bool IsExplicitTestMasterLabel(params string?[] labels)
        => labels.Any(x => !string.IsNullOrWhiteSpace(x) && x.Contains("測試", StringComparison.OrdinalIgnoreCase));
}
