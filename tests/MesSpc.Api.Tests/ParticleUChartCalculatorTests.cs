using FluentAssertions;
using MesSpc.Api.SpcEngine.Calculators;

namespace MesSpc.Api.Tests;

public class ParticleUChartCalculatorTests
{
    [Fact]
    public void Calculate_ShouldUseDecimalVolumeAndDynamicLimits()
    {
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(1, 20).Select(index => new ParticleUChartInput(
            index, start.AddHours(index), 10, index % 2 == 0 ? 2.5m : 5m, "L")).ToList();

        var result = ParticleUChartCalculator.Calculate(points);

        result.ChartType.Should().Be("U_CHART");
        result.ControlStatus.Should().Be("ready");
        result.Points[0].Value.Should().Be(2);
        result.Points[1].Value.Should().Be(4);
        result.Points[0].Ucl.Should().NotBe(result.Points[1].Ucl);
        result.SamplingVolumeUnit.Should().Be("L");
    }

    [Fact]
    public void Calculate_ShouldRejectMissingOrMixedVolumeUnits()
    {
        var start = DateTime.UtcNow;
        var missing = Enumerable.Range(1, 20).Select(index => new ParticleUChartInput(index, start.AddHours(index), 1, 1m, null)).ToList();
        var mixed = Enumerable.Range(1, 20).Select(index => new ParticleUChartInput(index, start.AddHours(index), 1, 1m, index == 20 ? "M3" : "L")).ToList();

        Action missingAction = () => ParticleUChartCalculator.Calculate(missing);
        Action mixedAction = () => ParticleUChartCalculator.Calculate(mixed);
        missingAction.Should().Throw<ArgumentException>().WithMessage("*SAMPLING_VOLUME_REQUIRED*");
        mixedAction.Should().Throw<ArgumentException>().WithMessage("*SAMPLING_VOLUME_UNIT_MISMATCH*");
    }
}
