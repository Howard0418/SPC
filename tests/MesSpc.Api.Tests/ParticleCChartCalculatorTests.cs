using FluentAssertions;
using MesSpc.Api.SpcEngine.Calculators;

namespace MesSpc.Api.Tests;

public class ParticleCChartCalculatorTests
{
    [Fact]
    public void Calculate_WithFewerThanTwentyPoints_ShouldReturnInsufficientData()
    {
        var result = ParticleCChartCalculator.Calculate(Points(19, _ => 4));

        result.ControlStatus.Should().Be("insufficientData");
        result.StatControlLimits.Should().BeNull();
        result.Points.Should().OnlyContain(point => !point.IsOutOfControl && point.ViolatedRules.Count == 0);
    }

    [Fact]
    public void Calculate_ShouldKeepSameTimeMeasurementsAndClampLclToZero()
    {
        var time = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(1, 20).Select(id => new ParticleCChartInput(id, time, 1)).ToList();

        var result = ParticleCChartCalculator.Calculate(points);

        result.ControlStatus.Should().Be("ready");
        result.Points.Select(point => point.MeasurementId).Should().Equal(Enumerable.Range(1, 20).Select(id => (long)id));
        result.StatControlLimits!.Cl.Should().Be(1);
        result.StatControlLimits.Lcl.Should().Be(0);
        result.StatControlLimits.Ucl.Should().Be(4);
    }

    [Fact]
    public void Calculate_WhenCountExceedsExactDoubleRange_ShouldReject()
    {
        var points = Points(20, _ => 1);
        points[0] = points[0] with { Count = 9_007_199_254_740_992L };

        var action = () => ParticleCChartCalculator.Calculate(points);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*COUNT_PRECISION_EXCEEDED*");
    }

    private static List<ParticleCChartInput> Points(int count, Func<int, long> value) => Enumerable.Range(1, count)
        .Select(index => new ParticleCChartInput(index, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(index), value(index)))
        .ToList();
}
