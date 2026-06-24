using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Rules;

public static class NelsonRulesValidator
{
    /// <summary>
    /// Applies the standard 8 Nelson Rules to a dataset.
    /// This assumes the points are in chronological order.
    /// </summary>
    public static void ApplyRules(List<SpcDataPoint> points, ControlLimits limits)
    {
        if (points.Count == 0)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
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
                p.ViolatedRules.Add("Nelson1");
                p.IsOutOfControl = true;
            }

            // Rule 2: 9 (or more) points in a row are on the same side of the mean.
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
                        p.ViolatedRules.Add("Nelson2");
                        p.IsOutOfControl = true;
                    }
                }
            }

            // Rule 3: 6 (or more) points in a row are continually increasing (or decreasing).
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
                        p.ViolatedRules.Add("Nelson3");
                        p.IsOutOfControl = true;
                    }
                }
            }

            // Rule 4: 14 (or more) points in a row alternate in direction, increasing then decreasing.
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
                    p.ViolatedRules.Add("Nelson4");
                    p.IsOutOfControl = true;
                }
            }

            // Rule 5: 2 out of 3 points > 2 standard deviations from mean in same direction
            if (i >= 2)
            {
                int countPos = 0;
                int countNeg = 0;
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

                    var diff = pt.Value - ptMean;
                    if (diff > 2 * ptSigma) countPos++;
                    if (diff < -2 * ptSigma) countNeg++;
                }
                if (valid && (countPos >= 2 || countNeg >= 2))
                {
                    p.ViolatedRules.Add("Nelson5");
                    p.IsOutOfControl = true;
                }
            }
            
            // Rule 6: 4 out of 5 points > 1 standard deviation from mean in same direction
            if (i >= 4)
            {
                int countPos = 0;
                int countNeg = 0;
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

                    var diff = pt.Value - ptMean;
                    if (diff > ptSigma) countPos++;
                    if (diff < -ptSigma) countNeg++;
                }
                if (valid && (countPos >= 4 || countNeg >= 4))
                {
                    p.ViolatedRules.Add("Nelson6");
                    p.IsOutOfControl = true;
                }
            }
        }
    }
}
