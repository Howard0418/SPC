using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class XbarSChartCalculatorTests
{
    [Fact]
    public void Calculate_ShouldComputeCorrectLimitsAndPointsForXbarS()
    {
        // Arrange
        var subgroups = new List<Subgroup>
        {
            new() { MeasuredAt = DateTime.UtcNow, Values = new List<double> { 10, 11, 12, 10, 11, 10, 12, 11, 10, 11 } }, // Mean: 10.8
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(1), Values = new List<double> { 12, 13, 14, 12, 13, 12, 14, 13, 12, 13 } }, // Mean: 12.8
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(2), Values = new List<double> { 9, 10, 11, 9, 10, 9, 11, 10, 9, 10 } } // Mean: 9.8
        };

        var configuredLimits = new ControlLimits { USL = 15, LSL = 5 };

        // Act
        var result = XbarSChartCalculator.Calculate(subgroups, configuredLimits, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("XBAR_S", result.ChartType);
        Assert.Equal(10, result.SubgroupSize);
        Assert.Equal(15, result.Limits.USL);
        Assert.Equal(5, result.Limits.LSL);

        dynamic statControl = result.StatControlLimits!;
        Assert.NotNull(statControl);
        
        // Means: 10.8, 12.8, 9.8 => Average of means (xDoubleBar) = 11.1333
        Assert.Equal(11.133, (double)statControl.xbarControl.cl, 3);
        Assert.True((double)statControl.sControl.cl > 0);

        dynamic xbarChartData = result.ChartData;
        Assert.Equal(3, xbarChartData.points.Count);

        dynamic sChartData = result.SecondaryChartData!;
        Assert.Equal(3, sChartData.points.Count);
    }
}
