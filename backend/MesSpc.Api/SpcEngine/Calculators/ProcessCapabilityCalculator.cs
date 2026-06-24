using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.SpcEngine.Calculators;

public static class ProcessCapabilityCalculator
{
    public static CapabilityResult? Calculate(List<Subgroup> subgroups, double? usl, double? lsl)
    {
        var allValues = subgroups.SelectMany(x => x.Values).ToList();
        if (allValues.Count < 2) return null;

        double mean = allValues.Average();
        double varianceSum = allValues.Sum(x => (x - mean) * (x - mean));
        double sigmaOverall = Math.Sqrt(varianceSum / (allValues.Count - 1));

        if (sigmaOverall <= 0) return null;

        double sigmaWithin = sigmaOverall; // fallback
        var nRef = subgroups.First().N;
        if (nRef >= 2)
        {
            var validSubgroups = subgroups.Where(x => x.N == nRef).ToList();
            if (validSubgroups.Count > 0 && SpcConstants.TryGetCapabilityFactors(nRef, out var d2, out _))
            {
                double rBar = validSubgroups.Average(x => x.Range);
                if (d2 > 0) sigmaWithin = rBar / d2;
            }
        }
        else
        {
            // Moving range (N=1)
            double mrSum = 0;
            int mrCount = 0;
            for (int i = 1; i < allValues.Count; i++)
            {
                mrSum += Math.Abs(allValues[i] - allValues[i - 1]);
                mrCount++;
            }
            if (mrCount > 0)
            {
                double mrBar = mrSum / mrCount;
                sigmaWithin = mrBar / 1.128; // d2 for n=2 is 1.128
            }
        }

        if (sigmaWithin <= 0) sigmaWithin = sigmaOverall;

        double? ca = null, cp = null, cpk = null, pp = null, ppk = null, ppm = null;

        if (usl.HasValue && lsl.HasValue)
        {
            cp = (usl.Value - lsl.Value) / (6 * sigmaWithin);
            pp = (usl.Value - lsl.Value) / (6 * sigmaOverall);

            double cpkUpper = (usl.Value - mean) / (3 * sigmaWithin);
            double cpkLower = (mean - lsl.Value) / (3 * sigmaWithin);
            cpk = Math.Min(cpkUpper, cpkLower);

            double ppkUpper = (usl.Value - mean) / (3 * sigmaOverall);
            double ppkLower = (mean - lsl.Value) / (3 * sigmaOverall);
            ppk = Math.Min(ppkUpper, ppkLower);

            var halfWidth = (usl.Value - lsl.Value) / 2;
            if (halfWidth > 0)
            {
                var target = (usl.Value + lsl.Value) / 2;
                ca = Math.Abs(mean - target) / halfWidth;
            }

            ppm = (double)allValues.Count(x => x > usl.Value || x < lsl.Value) / allValues.Count * 1_000_000;
        }
        else if (usl.HasValue)
        {
            cpk = (usl.Value - mean) / (3 * sigmaWithin);
            ppk = (usl.Value - mean) / (3 * sigmaOverall);
            ppm = (double)allValues.Count(x => x > usl.Value) / allValues.Count * 1_000_000;
        }
        else if (lsl.HasValue)
        {
            cpk = (mean - lsl.Value) / (3 * sigmaWithin);
            ppk = (mean - lsl.Value) / (3 * sigmaOverall);
            ppm = (double)allValues.Count(x => x < lsl.Value) / allValues.Count * 1_000_000;
        }

        return new CapabilityResult
        {
            Ca = Round(ca),
            Cp = Round(cp),
            Cpk = Round(cpk),
            Pp = Round(pp),
            Ppk = Round(ppk),
            Ppm = Round(ppm),
            SigmaWithin = Round(sigmaWithin),
            SigmaOverall = Round(sigmaOverall)
        };
    }

    private static double? Round(double? val) => val.HasValue && !double.IsNaN(val.Value) && !double.IsInfinity(val.Value) ? Math.Round(val.Value, 4) : null;
}
