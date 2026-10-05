using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;

namespace MesSpc.Api.SpcEngine.Calculators;

public sealed record ParticleUChartInput(long MeasurementId, DateTime MeasurementTime, long Count, decimal? SamplingVolume, string? SamplingVolumeUnit);

public static class ParticleUChartCalculator
{
    private const long MaxExactDoubleInteger = 9_007_199_254_740_991L;

    public static ParticleChartResult Calculate(IReadOnlyCollection<ParticleUChartInput> rawPoints)
    {
        var ordered = rawPoints.OrderBy(point => point.MeasurementTime).ThenBy(point => point.MeasurementId).ToList();
        if (ordered.Any(point => point.Count < 0)) throw new ArgumentOutOfRangeException(nameof(rawPoints), "COUNT_NEGATIVE");
        if (ordered.Any(point => point.Count > MaxExactDoubleInteger)) throw new ArgumentOutOfRangeException(nameof(rawPoints), "COUNT_PRECISION_EXCEEDED");
        if (ordered.Any(point => !point.SamplingVolume.HasValue || point.SamplingVolume <= 0 || string.IsNullOrWhiteSpace(point.SamplingVolumeUnit)))
            throw new ArgumentException("SAMPLING_VOLUME_REQUIRED", nameof(rawPoints));
        var units = ordered.Select(point => point.SamplingVolumeUnit!.Trim().ToUpperInvariant()).Distinct().ToList();
        if (units.Count != 1) throw new ArgumentException("SAMPLING_VOLUME_UNIT_MISMATCH", nameof(rawPoints));

        if (ordered.Count < 20)
        {
            return new("U_CHART", "insufficientData", null, ordered.Select(point => new ParticleChartPoint(
                point.MeasurementId, point.MeasurementTime, point.Count, null, null, null, false, [],
                (double)point.Count / (double)point.SamplingVolume!.Value, point.SamplingVolume)).ToList(), units[0]);
        }

        var totalCount = ordered.Sum(point => (double)point.Count);
        var totalVolume = ordered.Sum(point => (double)point.SamplingVolume!.Value);
        var uBar = totalCount / totalVolume;
        var nBar = totalVolume / ordered.Count;
        var staticUcl = uBar + 3 * Math.Sqrt(uBar / nBar);
        var staticLcl = Math.Max(0, uBar - 3 * Math.Sqrt(uBar / nBar));
        var evaluated = ordered.Select(point =>
        {
            var volume = (double)point.SamplingVolume!.Value;
            var value = point.Count / volume;
            var ucl = uBar + 3 * Math.Sqrt(uBar / volume);
            var lcl = Math.Max(0, uBar - 3 * Math.Sqrt(uBar / volume));
            return new SpcDataPoint { MeasuredAt = point.MeasurementTime, Value = value, UCL = ucl, CL = uBar, LCL = lcl, IsOutOfControl = value > ucl || value < lcl };
        }).ToList();
        WesternElectricRulesValidator.ApplyRules(evaluated, new ControlLimits { UCL = staticUcl, CL = uBar, LCL = staticLcl });
        var points = ordered.Select((point, index) => new ParticleChartPoint(
            point.MeasurementId, point.MeasurementTime, point.Count, evaluated[index].UCL, uBar, evaluated[index].LCL,
            evaluated[index].IsOutOfControl, evaluated[index].ViolatedRules.ToList(), evaluated[index].Value, point.SamplingVolume)).ToList();
        return new("U_CHART", "ready", new(staticUcl, uBar, staticLcl, nBar), points, units[0]);
    }
}
