using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;

namespace MesSpc.Api.SpcEngine.Calculators;

public sealed record ParticleCChartInput(long MeasurementId, DateTime MeasurementTime, long Count);
public sealed record ParticleControlLimits(double Ucl, double Cl, double Lcl, double? NormalizationBase = null);
public sealed record ParticleChartPoint(long MeasurementId, DateTime Time, long Count, double? Ucl, double? Cl,
    double? Lcl, bool IsOutOfControl, IReadOnlyList<string> ViolatedRules, double? Value = null, decimal? SamplingVolume = null);
public sealed record ParticleChartResult(string ChartType, string ControlStatus, ParticleControlLimits? StatControlLimits,
    IReadOnlyList<ParticleChartPoint> Points, string? SamplingVolumeUnit = null);

public static class ParticleCChartCalculator
{
    private const long MaxExactDoubleInteger = 9_007_199_254_740_991L;

    public static ParticleChartResult Calculate(IReadOnlyCollection<ParticleCChartInput> rawPoints)
    {
        var ordered = rawPoints.OrderBy(point => point.MeasurementTime).ThenBy(point => point.MeasurementId).ToList();
        if (ordered.Any(point => point.Count < 0)) throw new ArgumentOutOfRangeException(nameof(rawPoints), "COUNT_NEGATIVE");
        if (ordered.Any(point => point.Count > MaxExactDoubleInteger)) throw new ArgumentOutOfRangeException(nameof(rawPoints), "COUNT_PRECISION_EXCEEDED");

        if (ordered.Count < 20)
        {
            return new("C_CHART", "insufficientData", null, ordered.Select(point =>
                new ParticleChartPoint(point.MeasurementId, point.MeasurementTime, point.Count, null, null, null, false, [], point.Count)).ToList());
        }

        var cl = ordered.Average(point => (double)point.Count);
        var ucl = cl + 3 * Math.Sqrt(cl);
        var lcl = Math.Max(0, cl - 3 * Math.Sqrt(cl));
        var evaluated = ordered.Select(point => new SpcDataPoint
        {
            MeasuredAt = point.MeasurementTime,
            Value = point.Count,
            UCL = ucl,
            CL = cl,
            LCL = lcl,
            IsOutOfControl = point.Count > ucl || point.Count < lcl
        }).ToList();
        WesternElectricRulesValidator.ApplyRules(evaluated, new ControlLimits { UCL = ucl, CL = cl, LCL = lcl });

        var points = ordered.Select((point, index) => new ParticleChartPoint(
            point.MeasurementId, point.MeasurementTime, point.Count, ucl, cl, lcl,
            evaluated[index].IsOutOfControl, evaluated[index].ViolatedRules.ToList(), point.Count)).ToList();
        return new("C_CHART", "ready", new(ucl, cl, lcl), points);
    }
}
