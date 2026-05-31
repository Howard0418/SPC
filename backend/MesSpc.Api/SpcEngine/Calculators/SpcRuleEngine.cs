using MesSpc.Api.Domain.Entities;
using System.Text.Json;

namespace MesSpc.Api.SpcEngine.Calculators;

public class SpcRuleViolation
{
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? PointIndex { get; set; } // The index of the point that triggered the rule
}

public static class SpcRuleEngine
{
    /// <summary>
    /// Evaluates SPC rules against a set of values.
    /// The values should be in chronological order, with the newest value at the end.
    /// </summary>
    public static List<SpcRuleViolation> EvaluateRules(List<double> values, List<SpcRule> rules, double cl, double ucl, double lcl)
    {
        var violations = new List<SpcRuleViolation>();
        if (values.Count == 0 || rules.Count == 0) return violations;

        // Calculate sigma assuming UCL/LCL are 3 sigma away from CL
        double sigma = (ucl - cl) / 3.0;
        if (sigma <= 0) return violations; // Invalid limits

        foreach (var rule in rules)
        {
            if (!rule.IsEnabled) continue;

            var violation = rule.RuleCode switch
            {
                "Rule1_Over3Sigma" => EvaluateRule1(values, cl, sigma, rule),
                "Rule2_9SameSide" => EvaluateRule2(values, cl, sigma, rule),
                "Rule3_6Trend" => EvaluateRule3(values, cl, sigma, rule),
                "Rule4_14Alternating" => EvaluateRule4(values, cl, sigma, rule),
                "Rule5_2Of3Over2Sigma" => EvaluateRule5(values, cl, sigma, rule),
                "Rule6_4Of5Over1Sigma" => EvaluateRule6(values, cl, sigma, rule),
                "Rule7_15Within1Sigma" => EvaluateRule7(values, cl, sigma, rule),
                "Rule8_8Outside1Sigma" => EvaluateRule8(values, cl, sigma, rule),
                _ => null
            };

            if (violation != null)
            {
                violations.Add(violation);
            }
        }

        return violations;
    }

    // Rule 1: 1 point is more than 3 standard deviations from the mean
    private static SpcRuleViolation? EvaluateRule1(List<double> values, double cl, double sigma, SpcRule rule)
    {
        if (values.Count < 1) return null;
        var lastValue = values.Last();
        if (lastValue > cl + 3 * sigma || lastValue < cl - 3 * sigma)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = $"1 point > 3 sigma from center line. Value: {lastValue:F3}",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 2: 9 (or more) points in a row are on the same side of the mean
    private static SpcRuleViolation? EvaluateRule2(List<double> values, double cl, double sigma, SpcRule rule)
    {
        int requiredPoints = ParseRequiredPoints(rule.RuleConfigJson, 9);
        if (values.Count < requiredPoints) return null;

        var lastN = values.Skip(values.Count - requiredPoints).ToList();
        bool allAbove = lastN.All(v => v > cl);
        bool allBelow = lastN.All(v => v < cl);

        if (allAbove || allBelow)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = $"{requiredPoints} points in a row on same side of center line.",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 3: 6 (or more) points in a row are continually increasing (or decreasing)
    private static SpcRuleViolation? EvaluateRule3(List<double> values, double cl, double sigma, SpcRule rule)
    {
        int requiredPoints = ParseRequiredPoints(rule.RuleConfigJson, 6);
        if (values.Count < requiredPoints) return null;

        var lastN = values.Skip(values.Count - requiredPoints).ToList();
        bool increasing = true;
        bool decreasing = true;

        for (int i = 1; i < lastN.Count; i++)
        {
            if (lastN[i] <= lastN[i - 1]) increasing = false;
            if (lastN[i] >= lastN[i - 1]) decreasing = false;
        }

        if (increasing || decreasing)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = $"{requiredPoints} points in a row continually {(increasing ? "increasing" : "decreasing")}.",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 4: 14 (or more) points in a row alternate in direction, increasing then decreasing
    private static SpcRuleViolation? EvaluateRule4(List<double> values, double cl, double sigma, SpcRule rule)
    {
        int requiredPoints = ParseRequiredPoints(rule.RuleConfigJson, 14);
        if (values.Count < requiredPoints) return null;

        var lastN = values.Skip(values.Count - requiredPoints).ToList();
        bool alternating = true;
        
        for (int i = 2; i < lastN.Count; i++)
        {
            double diff1 = lastN[i - 1] - lastN[i - 2];
            double diff2 = lastN[i] - lastN[i - 1];
            
            // They must alternate signs
            if (diff1 * diff2 >= 0) 
            {
                alternating = false;
                break;
            }
        }

        if (alternating)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = $"{requiredPoints} points in a row alternating up and down.",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 5: 2 out of 3 consecutive points are > 2 sigma from center line (same side)
    private static SpcRuleViolation? EvaluateRule5(List<double> values, double cl, double sigma, SpcRule rule)
    {
        if (values.Count < 3) return null;
        var last3 = values.Skip(values.Count - 3).ToList();
        
        int aboveCount = last3.Count(v => v > cl + 2 * sigma);
        int belowCount = last3.Count(v => v < cl - 2 * sigma);

        if (aboveCount >= 2 || belowCount >= 2)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = "2 out of 3 consecutive points > 2 sigma from center line (same side).",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 6: 4 out of 5 consecutive points are > 1 sigma from center line (same side)
    private static SpcRuleViolation? EvaluateRule6(List<double> values, double cl, double sigma, SpcRule rule)
    {
        if (values.Count < 5) return null;
        var last5 = values.Skip(values.Count - 5).ToList();
        
        int aboveCount = last5.Count(v => v > cl + 1 * sigma);
        int belowCount = last5.Count(v => v < cl - 1 * sigma);

        if (aboveCount >= 4 || belowCount >= 4)
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = "4 out of 5 consecutive points > 1 sigma from center line (same side).",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 7: 15 consecutive points are within 1 sigma of center line (either side)
    private static SpcRuleViolation? EvaluateRule7(List<double> values, double cl, double sigma, SpcRule rule)
    {
        if (values.Count < 15) return null;
        var last15 = values.Skip(values.Count - 15).ToList();
        
        if (last15.All(v => v > cl - 1 * sigma && v < cl + 1 * sigma))
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = "15 points in a row within 1 sigma of center line.",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    // Rule 8: 8 consecutive points are > 1 sigma from center line (either side)
    private static SpcRuleViolation? EvaluateRule8(List<double> values, double cl, double sigma, SpcRule rule)
    {
        if (values.Count < 8) return null;
        var last8 = values.Skip(values.Count - 8).ToList();
        
        if (last8.All(v => v > cl + 1 * sigma || v < cl - 1 * sigma))
        {
            return new SpcRuleViolation
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Message = "8 points in a row > 1 sigma from center line.",
                PointIndex = values.Count - 1
            };
        }
        return null;
    }

    private static int ParseRequiredPoints(string? json, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(json)) return defaultValue;
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("RequiredPoints", out var prop) && prop.TryGetInt32(out int val))
            {
                return val;
            }
        }
        catch { }
        return defaultValue;
    }
}
