using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Rules;

public static class WesternElectricRulesValidator
{
    /// <summary>
    /// Applies Western Electric Rules (Rules 1, 2, 3, 4) to a chronological list of SPC data points.
    /// </summary>
    public static void ApplyRules(List<SpcDataPoint> points, ControlLimits limits)
    {
        if (points.Count == 0 || !limits.CL.HasValue || !limits.UCL.HasValue || !limits.LCL.HasValue)
            return;

        var mean = limits.CL.Value;
        var sigma = (limits.UCL.Value - mean) / 3.0;
        if (sigma <= 0) return;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var v = p.Value;

            // Rule 1: 1 point is > 3 standard deviations from the mean (Out of Control)
            if (Math.Abs(v - mean) > 3 * sigma)
            {
                p.ViolatedRules.Add("Rule1_Over3Sigma");
                p.IsOutOfControl = true;
            }

            // Rule 2: 9 consecutive points on the same side of the mean.
            if (i >= 8)
            {
                var side = Math.Sign(v - mean);
                if (side != 0)
                {
                    bool match = true;
                    for (int j = 1; j <= 8; j++)
                    {
                        if (Math.Sign(points[i - j].Value - mean) != side)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        p.ViolatedRules.Add("Rule2_9SameSide");
                        p.IsOutOfControl = true;
                    }
                }
            }

            // Rule 3: 6 consecutive points steadily increasing or decreasing.
            if (i >= 5)
            {
                var direction = Math.Sign(v - points[i - 1].Value);
                if (direction != 0)
                {
                    bool match = true;
                    for (int j = 1; j <= 4; j++)
                    {
                        if (Math.Sign(points[i - j].Value - points[i - j - 1].Value) != direction)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        p.ViolatedRules.Add("Rule3_6Trend");
                        p.IsOutOfControl = true;
                    }
                }
            }

            // Rule 4: 14 consecutive points alternating up and down.
            if (i >= 13)
            {
                bool match = true;
                for (int j = 0; j < 12; j++)
                {
                    var dir1 = Math.Sign(points[i - j].Value - points[i - j - 1].Value);
                    var dir2 = Math.Sign(points[i - j - 1].Value - points[i - j - 2].Value);
                    if (dir1 == 0 || dir2 == 0 || dir1 == dir2)
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    p.ViolatedRules.Add("Rule4_14Alternating");
                    p.IsOutOfControl = true;
                }
            }
        }
    }
}
