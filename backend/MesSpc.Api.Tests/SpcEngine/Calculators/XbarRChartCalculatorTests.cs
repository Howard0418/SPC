using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class XbarRChartCalculatorTests
{
    private static List<Subgroup> CreateAsymmetricSubgroups()
    {
        var startedAt = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);
        return
        [
            new() { MeasuredAt = startedAt, Values = { 9.5, 10.5 } },
            new() { MeasuredAt = startedAt.AddMinutes(1), Values = { 9.7, 10.7 } },
            new() { MeasuredAt = startedAt.AddMinutes(2), Values = { 9.6, 10.6 } },
            new() { MeasuredAt = startedAt.AddMinutes(3), Values = { 11.5, 12.5 } },
            new() { MeasuredAt = startedAt.AddMinutes(4), Values = { 9.7, 10.7 } }
        ];
    }

    [Fact]
    public void Calculate_ShouldComputeCorrectLimitsAndPoints()
    {
        // Arrange
        var subgroups = new List<Subgroup>
        {
            new() { MeasuredAt = DateTime.UtcNow, Values = { 10, 12, 11 } },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(1), Values = { 14, 13, 12 } },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(2), Values = { 10, 9, 11 } }
        };

        var configuredLimits = new ControlLimits { USL = 15, LSL = 5 };

        // Act
        var result = XbarRChartCalculator.Calculate(subgroups, configuredLimits, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("XBAR_R", result.ChartType);
        Assert.Equal(3, result.SubgroupSize);
        Assert.Equal(15, result.Limits.USL);
        Assert.Equal(5, result.Limits.LSL);

        // Check Stat Control Limits
        // Means: 11, 13, 10 => X-DoubleBar = 11.333
        // Ranges: 2, 2, 2 => RBar = 2
        dynamic statControl = result.StatControlLimits!;
        Assert.NotNull(statControl);
        
        Assert.Equal(11.333, (double)statControl.xbarControl.cl, 2);
        Assert.Equal(2.0, (double)statControl.rControl.cl);

        // For n=3, A2=1.023, D3=0, D4=2.574
        Assert.Equal(11.333 + 1.023 * 2, (double)statControl.xbarControl.ucl, 2);
        Assert.Equal(11.333 - 1.023 * 2, (double)statControl.xbarControl.lcl, 2);
        Assert.Equal(2.574 * 2, (double)statControl.rControl.ucl, 2);

        // Check Points
        dynamic xbarChartData = result.ChartData;
        Assert.Equal(3, xbarChartData.points.Count);

        dynamic rChartData = result.SecondaryChartData!;
        Assert.Equal(3, rChartData.points.Count);
    }

    [Fact]
    public void Calculate_SigmaMethod_ShouldUseCalculatedLimitsForRenderedPoints()
    {
        var configuredLimits = new ControlLimits { UCL = 99, CL = 50, LCL = 1, USL = 120, LSL = 0 };
        const string formula = "{\"XbarCalculationMethod\":\"SIGMA_METHOD\",\"Multiplier\":3}";

        var result = XbarRChartCalculator.Calculate(CreateAsymmetricSubgroups(), configuredLimits, 2, formula);

        dynamic stat = result.StatControlLimits!;
        dynamic chart = result.ChartData;
        Assert.Equal("SIGMA_METHOD", (string)stat.xbarControl.calculationMethod);
        Assert.Equal((double)stat.xbarControl.ucl, (double)chart.points[0].uclStat, 10);
        Assert.Equal((double)stat.xbarControl.cl, (double)chart.points[0].clStat, 10);
        Assert.Equal((double)stat.xbarControl.lcl, (double)chart.points[0].lclStat, 10);
        Assert.NotEqual(99d, (double)chart.points[0].uclStat);
        Assert.NotEqual(1d, (double)chart.points[0].lclStat);
    }

    [Fact]
    public void Calculate_SelectedMethods_ShouldProduceDifferentRenderedLimits()
    {
        var configuredLimits = new ControlLimits { UCL = 99, CL = 50, LCL = 1 };
        const string rangeFormula = "{\"XbarCalculationMethod\":\"STANDARD_RANGE\"}";
        const string movingRangeFormula = "{\"XbarCalculationMethod\":\"MOVING_RANGE_OF_XBAR\",\"MrMultiplier\":2.66}";
        const string sigmaFormula = "{\"XbarCalculationMethod\":\"SIGMA_METHOD\",\"Multiplier\":3}";

        dynamic rangeChart = XbarRChartCalculator.Calculate(CreateAsymmetricSubgroups(), configuredLimits, 2, rangeFormula).ChartData;
        dynamic movingRangeChart = XbarRChartCalculator.Calculate(CreateAsymmetricSubgroups(), configuredLimits, 2, movingRangeFormula).ChartData;
        dynamic sigmaChart = XbarRChartCalculator.Calculate(CreateAsymmetricSubgroups(), configuredLimits, 2, sigmaFormula).ChartData;

        var rangeUcl = (double)rangeChart.points[0].uclStat;
        var movingRangeUcl = (double)movingRangeChart.points[0].uclStat;
        var sigmaUcl = (double)sigmaChart.points[0].uclStat;
        Assert.NotEqual(rangeUcl, movingRangeUcl, 6);
        Assert.NotEqual(rangeUcl, sigmaUcl, 6);
    }

    [Fact]
    public void Calculate_WithoutSelectedMethod_ShouldKeepConfiguredLimitsForCompatibility()
    {
        var configuredLimits = new ControlLimits { UCL = 99, CL = 50, LCL = 1 };

        dynamic chart = XbarRChartCalculator.Calculate(CreateAsymmetricSubgroups(), configuredLimits, 2).ChartData;

        Assert.Equal(99d, (double)chart.points[0].uclStat);
        Assert.Equal(50d, (double)chart.points[0].clStat);
        Assert.Equal(1d, (double)chart.points[0].lclStat);
    }
}
