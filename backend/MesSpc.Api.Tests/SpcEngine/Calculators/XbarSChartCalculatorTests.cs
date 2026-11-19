using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class XbarSChartCalculatorTests
{
    private static List<Subgroup> CreateAsymmetricSubgroups()
    {
        var startedAt = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);
        return
        [
            new() { MeasuredAt = startedAt, Values = { 0.22, 0.26 } },
            new() { MeasuredAt = startedAt.AddDays(1), Values = { 0.29, 0.33 } },
            new() { MeasuredAt = startedAt.AddDays(2), Values = { 0.26, 0.30 } },
            new() { MeasuredAt = startedAt.AddDays(3), Values = { 0.37, 0.41 } },
            new() { MeasuredAt = startedAt.AddDays(4), Values = { 0.16, 0.20 } },
            new() { MeasuredAt = startedAt.AddDays(5), Values = { 0.20, 0.24 } }
        ];
    }

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

    [Fact]
    public void Calculate_SelectedMethod_ShouldChangeXbarLimitsButKeepSChartLimits()
    {
        var configuredLimits = new ControlLimits { UCL = 0.31, CL = 0.28, LCL = 0.24 };
        const string rangeFormula = "{\"XbarCalculationMethod\":\"STANDARD_RANGE\"}";
        const string sigmaFormula = "{\"XbarCalculationMethod\":\"SIGMA_METHOD\",\"Multiplier\":3}";

        var rangeResult = XbarSChartCalculator.Calculate(
            CreateAsymmetricSubgroups(), configuredLimits, 2, formulaConfigJson: rangeFormula);
        var sigmaResult = XbarSChartCalculator.Calculate(
            CreateAsymmetricSubgroups(), configuredLimits, 2, formulaConfigJson: sigmaFormula);

        dynamic rangeStat = rangeResult.StatControlLimits!;
        dynamic sigmaStat = sigmaResult.StatControlLimits!;
        dynamic rangeChart = rangeResult.ChartData;
        dynamic sigmaChart = sigmaResult.ChartData;

        Assert.NotEqual((double)rangeStat.xbarControl.ucl, (double)sigmaStat.xbarControl.ucl, 6);
        Assert.NotEqual((double)rangeStat.xbarControl.lcl, (double)sigmaStat.xbarControl.lcl, 6);
        Assert.Equal((double)sigmaStat.xbarControl.ucl, (double)sigmaChart.points[0].uclStat, 10);
        Assert.Equal((double)sigmaStat.xbarControl.lcl, (double)sigmaChart.points[0].lclStat, 10);
        Assert.Equal((double)rangeStat.sControl.ucl, (double)sigmaStat.sControl.ucl, 10);
        Assert.Equal((double)rangeStat.sControl.lcl, (double)sigmaStat.sControl.lcl, 10);
        Assert.NotEqual((double)rangeChart.points[0].uclStat, (double)sigmaChart.points[0].uclStat, 6);
    }
}
