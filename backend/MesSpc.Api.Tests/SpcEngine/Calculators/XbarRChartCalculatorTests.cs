using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class XbarRChartCalculatorTests
{
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
}
