using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class ImrChartCalculatorTests
{
    [Fact]
    public void Calculate_ShouldComputeCorrectLimitsAndPoints()
    {
        // Arrange
        var rawData = new List<SpcDataPoint>
        {
            new() { MeasuredAt = DateTime.UtcNow, Value = 10 },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(1), Value = 12 },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(2), Value = 11 },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(3), Value = 14 },
            new() { MeasuredAt = DateTime.UtcNow.AddMinutes(4), Value = 13 }
        };

        var configuredLimits = new ControlLimits { USL = 15, LSL = 5 };

        // Act
        var result = ImrChartCalculator.Calculate(rawData, configuredLimits);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("I-MR", result.ChartType);
        Assert.Equal(1, result.SubgroupSize);
        Assert.Equal(15, result.Limits.USL);
        Assert.Equal(5, result.Limits.LSL);

        // Check Stat Control Limits
        // MR values: |12-10|=2, |11-12|=1, |14-11|=3, |13-14|=1
        // MR Bar: (2 + 1 + 3 + 1) / 4 = 1.75
        // I Bar: (10 + 12 + 11 + 14 + 13) / 5 = 12
        dynamic statControl = result.StatControlLimits!;
        Assert.NotNull(statControl);
        
        Assert.Equal(12.0, (double)statControl.iControlLimitsStat.cl);
        Assert.Equal(1.75, (double)statControl.mrControlLimitsStat.cl);

        // Check Points
        dynamic iChartData = result.ChartData;
        Assert.Equal(5, iChartData.points.Count);

        dynamic mrChartData = result.SecondaryChartData!;
        Assert.Equal(4, mrChartData.points.Count);
    }
}
