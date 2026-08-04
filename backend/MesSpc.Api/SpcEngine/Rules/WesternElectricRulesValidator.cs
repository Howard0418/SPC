using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Rules;

public static class WesternElectricRulesValidator
{
    /// <summary>
    /// Applies Western Electric Rules (Rules 1, 2, 3, 4) to a chronological list of SPC data points.
    /// </summary>
    public static void ApplyRules(
        List<SpcDataPoint> points,
        ControlLimits limits,
        IReadOnlySet<string>? enabledRuleCodes = null)
    {
        if (points.Count == 0)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var wasOutOfControl = p.IsOutOfControl;
            var v = p.Value;

            var cl = p.CL ?? limits.CL;
            var ucl = p.UCL ?? limits.UCL;
            var lcl = p.LCL ?? limits.LCL;

            if (!cl.HasValue || !ucl.HasValue || !lcl.HasValue)
                continue;

            var mean = cl.Value;
            var sigma = (ucl.Value - mean) / 3.0;
            if (sigma <= 0)
                continue;

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
                        var prevPt = points[i - j];
                        var prevCl = prevPt.CL ?? limits.CL;
                        if (!prevCl.HasValue || Math.Sign(prevPt.Value - prevCl.Value) != side)
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

            // Rule 5: 2 out of 3 consecutive points are > 2 sigma from center line (same side)
            if (i >= 2)
            {
                int aboveCount = 0;
                int belowCount = 0;
                bool valid = true;
                for (int j = 0; j < 3; j++)
                {
                    var pt = points[i - j];
                    var ptCl = pt.CL ?? limits.CL;
                    var ptUcl = pt.UCL ?? limits.UCL;
                    if (!ptCl.HasValue || !ptUcl.HasValue)
                    {
                        valid = false;
                        break;
                    }
                    var ptMean = ptCl.Value;
                    var ptSigma = (ptUcl.Value - ptMean) / 3.0;
                    if (ptSigma <= 0)
                    {
                        valid = false;
                        break;
                    }

                    if (pt.Value > ptMean + 2 * ptSigma) aboveCount++;
                    if (pt.Value < ptMean - 2 * ptSigma) belowCount++;
                }

                if (valid && (aboveCount >= 2 || belowCount >= 2))
                {
                    p.ViolatedRules.Add("Rule5_2Of3Over2Sigma");
                    p.IsOutOfControl = true;
                }
            }

            // Rule 6: 4 out of 5 consecutive points are > 1 sigma from center line (same side)
            if (i >= 4)
            {
                int aboveCount = 0;
                int belowCount = 0;
                bool valid = true;
                for (int j = 0; j < 5; j++)
                {
                    var pt = points[i - j];
                    var ptCl = pt.CL ?? limits.CL;
                    var ptUcl = pt.UCL ?? limits.UCL;
                    if (!ptCl.HasValue || !ptUcl.HasValue)
                    {
                        valid = false;
                        break;
                    }
                    var ptMean = ptCl.Value;
                    var ptSigma = (ptUcl.Value - ptMean) / 3.0;
                    if (ptSigma <= 0)
                    {
                        valid = false;
                        break;
                    }

                    if (pt.Value > ptMean + ptSigma) aboveCount++;
                    if (pt.Value < ptMean - ptSigma) belowCount++;
                }

                if (valid && (aboveCount >= 4 || belowCount >= 4))
                {
                    p.ViolatedRules.Add("Rule6_4Of5Over1Sigma");
                    p.IsOutOfControl = true;
                }
            }

            // Rule 7: 15 consecutive points within 1 sigma of center line (either side)
            if (i >= 14)
            {
                bool match = true;
                for (int j = 0; j < 15; j++)
                {
                    var pt = points[i - j];
                    var ptCl = pt.CL ?? limits.CL;
                    var ptUcl = pt.UCL ?? limits.UCL;
                    if (!ptCl.HasValue || !ptUcl.HasValue)
                    {
                        match = false;
                        break;
                    }
                    var ptMean = ptCl.Value;
                    var ptSigma = (ptUcl.Value - ptMean) / 3.0;
                    if (ptSigma <= 0 || Math.Abs(pt.Value - ptMean) >= ptSigma)
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    p.ViolatedRules.Add("Rule7_15Within1Sigma");
                    p.IsOutOfControl = true;
                }
            }

            // Rule 8: 8 consecutive points outside 1 sigma of center line (either side)
            if (i >= 7)
            {
                bool match = true;
                for (int j = 0; j < 8; j++)
                {
                    var pt = points[i - j];
                    var ptCl = pt.CL ?? limits.CL;
                    var ptUcl = pt.UCL ?? limits.UCL;
                    if (!ptCl.HasValue || !ptUcl.HasValue)
                    {
                        match = false;
                        break;
                    }
                    var ptMean = ptCl.Value;
                    var ptSigma = (ptUcl.Value - ptMean) / 3.0;
                    if (ptSigma <= 0 || Math.Abs(pt.Value - ptMean) <= ptSigma)
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    p.ViolatedRules.Add("Rule8_8Outside1Sigma");
                    p.IsOutOfControl = true;
                }
            }

            if (enabledRuleCodes is not null)
            {
                p.ViolatedRules.RemoveAll(ruleCode => !enabledRuleCodes.Contains(ruleCode));
                p.IsOutOfControl = wasOutOfControl || p.ViolatedRules.Count > 0;
            }
        }
    }
}
