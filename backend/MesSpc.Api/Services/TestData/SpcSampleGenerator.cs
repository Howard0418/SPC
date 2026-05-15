using MathNet.Numerics.Distributions;

namespace MesSpc.Api.Services.TestData;

public enum SpcPattern
{
    Normal,
    AboveUsl,
    BelowLsl,
    AboveUcl,
    BelowLcl,
    TrendUp,
    TrendDown,
    Cyclic,
    HighVariance,
    Spike
}

public class SpcSampleGenerator
{
    private readonly Random _random = Random.Shared;

    public List<double> GenerateSeries(int count, double mean, double sigma, double? lsl, double? usl, double? lcl, double? ucl, bool withAnomalies)
    {
        var series = Enumerable.Range(0, count)
            .Select(_ => Normal.Sample(_random, mean, sigma))
            .ToList();

        if (!withAnomalies || count < 10) return series;

        InjectPattern(series, SpcPattern.AboveUsl, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.BelowLsl, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.AboveUcl, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.BelowLcl, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.TrendUp, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.TrendDown, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.Cyclic, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.HighVariance, lsl, usl, lcl, ucl, mean, sigma);
        InjectPattern(series, SpcPattern.Spike, lsl, usl, lcl, ucl, mean, sigma);
        return series;
    }

    private void InjectPattern(List<double> series, SpcPattern pattern, double? lsl, double? usl, double? lcl, double? ucl, double mean, double sigma)
    {
        var start = _random.Next(0, Math.Max(1, series.Count - 8));
        var take = Math.Min(8, series.Count - start);
        if (take <= 0) return;

        for (var i = 0; i < take; i++)
        {
            var idx = start + i;
            var v = series[idx];
            switch (pattern)
            {
                case SpcPattern.AboveUsl when usl.HasValue:
                    series[idx] = usl.Value + Math.Abs(sigma * (1.2 + _random.NextDouble()));
                    break;
                case SpcPattern.BelowLsl when lsl.HasValue:
                    series[idx] = lsl.Value - Math.Abs(sigma * (1.2 + _random.NextDouble()));
                    break;
                case SpcPattern.AboveUcl when ucl.HasValue:
                    series[idx] = ucl.Value + Math.Abs(sigma * (0.8 + _random.NextDouble()));
                    break;
                case SpcPattern.BelowLcl when lcl.HasValue:
                    series[idx] = lcl.Value - Math.Abs(sigma * (0.8 + _random.NextDouble()));
                    break;
                case SpcPattern.TrendUp:
                    series[idx] = v + (i + 1) * sigma * 0.25;
                    break;
                case SpcPattern.TrendDown:
                    series[idx] = v - (i + 1) * sigma * 0.25;
                    break;
                case SpcPattern.Cyclic:
                    series[idx] = mean + Math.Sin(i * Math.PI / 2.0) * sigma * 2.0;
                    break;
                case SpcPattern.HighVariance:
                    series[idx] = Normal.Sample(_random, mean, sigma * 3.5);
                    break;
                case SpcPattern.Spike:
                    if (i == take / 2) series[idx] = mean + sigma * (5.0 + _random.NextDouble() * 2.0);
                    break;
            }
        }
    }
}

