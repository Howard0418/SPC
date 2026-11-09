namespace MesSpc.Api.Services;

/// <summary>Six limit fields accepted by the dedicated batch editing contract.</summary>
public sealed record LimitBatchValues(
    double? Lsl,
    double? Target,
    double? Usl,
    double? Lcl,
    double? Cl,
    double? Ucl);

/// <summary>A field-level validation result with an actionable correction hint.</summary>
public sealed record LimitBatchValidationError(
    string Field,
    string Code,
    string Message,
    string Suggestion);

/// <summary>Validates specification and manual control limit relationships without mutating data.</summary>
public static class LimitBatchValidator
{
    private static readonly HashSet<string> SupportedChartTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "XBAR_R", "XBAR-R", "XBAR_S", "XBAR-S", "I_MR", "I-MR",
        "P", "P_CHART", "P-CHART", "NP", "NP_CHART", "NP-CHART",
        "C", "C_CHART", "C-CHART", "DUST_C",
        "U", "U_CHART", "U-CHART", "DUST_U"
    };

    public static IReadOnlyList<LimitBatchValidationError> Validate(
        LimitBatchValues values,
        string? chartTypeCode = null)
    {
        var errors = new List<LimitBatchValidationError>();
        CheckFinite(values.Lsl, "LSL", errors);
        CheckFinite(values.Target, "Target", errors);
        CheckFinite(values.Usl, "USL", errors);
        CheckFinite(values.Lcl, "LCL", errors);
        CheckFinite(values.Cl, "CL", errors);
        CheckFinite(values.Ucl, "UCL", errors);
        if (errors.Count > 0) return errors;

        if (values.Lsl.HasValue && values.Usl.HasValue && values.Lsl.Value >= values.Usl.Value)
        {
            errors.Add(new("LSL/USL", "SPEC_ORDER", "LSL 必須小於 USL。", "請分別輸入較小的 LSL 與較大的 USL。"));
        }

        if (values.Target.HasValue &&
            ((values.Lsl.HasValue && values.Target.Value < values.Lsl.Value) ||
             (values.Usl.HasValue && values.Target.Value > values.Usl.Value)))
        {
            errors.Add(new("Target", "TARGET_OUTSIDE_SPEC", "Target 必須位於已設定的規格範圍內。", "請調整 Target，或先修正 LSL／USL。"));
        }

        var controlCount = new[] { values.Lcl, values.Cl, values.Ucl }.Count(value => value.HasValue);
        if (controlCount is > 0 and < 3)
        {
            errors.Add(new("LCL/CL/UCL", "CONTROL_LIMITS_INCOMPLETE", "手動管制界限必須同時填寫 LCL、CL、UCL。", "若使用自動計算請三欄都留白；若使用手動值請三欄填妥。"));
        }
        else if (controlCount == 3 && !(values.Lcl < values.Cl && values.Cl < values.Ucl))
        {
            errors.Add(new("LCL/CL/UCL", "CONTROL_LIMITS_ORDER", "手動管制界限必須符合 LCL < CL < UCL。", "請依由小到大的順序調整三個管制界限。"));
        }

        if (controlCount > 0 && !string.IsNullOrWhiteSpace(chartTypeCode) &&
            !SupportedChartTypes.Contains(chartTypeCode.Trim()))
        {
            errors.Add(new("LCL/CL/UCL", "CONTROL_LIMITS_UNSUPPORTED_CHART", "此圖型尚未核准批次編輯手動管制界限。", "請回單筆管制項目設定頁維護，或先補齊圖型規則。"));
        }

        return errors;
    }

    private static void CheckFinite(double? value, string field, ICollection<LimitBatchValidationError> errors)
    {
        if (value.HasValue && !double.IsFinite(value.Value))
        {
            errors.Add(new(field, "NON_FINITE", $"{field} 必須是有限數值。", "請移除單位、百分比、逗號或其他非數字符號。"));
        }
    }
}
