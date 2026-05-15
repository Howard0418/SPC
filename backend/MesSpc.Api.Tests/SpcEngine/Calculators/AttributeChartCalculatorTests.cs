using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Calculators;

public class AttributeChartCalculatorTests
{
    private static ControlLimits GetEmptyLimits() => new();

    [Fact]
    public void Calculate_PChart_ShouldComputeCorrectLimits()
    {
        var rawData = new List<AttributeDataPoint>
        {
            new() { MeasuredAt = DateTime.UtcNow, InspectedQty = 100, DefectQty = 5 }, // p = 0.05
            new() { MeasuredAt = DateTime.UtcNow, InspectedQty = 100, DefectQty = 10 }, // p = 0.10
            new() { MeasuredAt = DateTime.UtcNow, InspectedQty = 100, DefectQty = 15 }  // p = 0.15
        };

        var result = AttributeChartCalculator.Calculate("P", rawData, GetEmptyLimits());

        // Total defects = 30, Total inspected = 300, pBar = 0.1
        dynamic stats = result.StatControlLimits!;
        Assert.Equal(0.1, (double)stats.cl, 2);
        Assert.Equal(100.0, (double)stats.nBar, 2);

        // static UCL = pBar + 3 * sqrt(pBar*(1-pBar)/nBar) = 0.1 + 3 * sqrt(0.09/100) = 0.1 + 3 * 0.03 = 0.19
        Assert.Equal(0.19, (double)stats.ucl, 2);
    }

    [Fact]
    public void Calculate_NpChart_ShouldComputeCorrectLimits()
    {
        var rawData = new List<AttributeDataPoint>
        {
            new() { MeasuredAt = DateTime.UtcNow, InspectedQty = 100, DefectQty = 5 }, 
            new() { MeasuredAt = DateTime.UtcNow, InspectedQty = 100, DefectQty = 15 }
        };

        var result = AttributeChartCalculator.Calculate("NP", rawData, GetEmptyLimits());

        // npBar = 10, nBar = 100, pBar = 0.1
        // UCL = npBar + 3 * sqrt(npBar * (1-pBar)) = 10 + 3 * sqrt(10 * 0.9) = 10 + 3 * 3 = 19
        dynamic stats = result.StatControlLimits!;
        Assert.Equal(10.0, (double)stats.cl, 2);
        Assert.Equal(19.0, (double)stats.ucl, 2);
    }

    [Fact]
    public void Calculate_CChart_ShouldComputeCorrectLimits()
    {
        var rawData = new List<AttributeDataPoint>
        {
            new() { MeasuredAt = DateTime.UtcNow, DefectCount = 10 },
            new() { MeasuredAt = DateTime.UtcNow, DefectCount = 20 },
            new() { MeasuredAt = DateTime.UtcNow, DefectCount = 15 }
        };

        var result = AttributeChartCalculator.Calculate("C", rawData, GetEmptyLimits());

        // cBar = 15
        // UCL = cBar + 3 * sqrt(cBar) = 15 + 3 * 3.87 = 26.6
        dynamic stats = result.StatControlLimits!;
        Assert.Equal(15.0, (double)stats.cl, 2);
        Assert.Equal(15.0 + 3 * Math.Sqrt(15.0), (double)stats.ucl, 2);
    }

    [Fact]
    public void Calculate_UChart_ShouldComputeCorrectLimits()
    {
        var rawData = new List<AttributeDataPoint>
        {
            new() { MeasuredAt = DateTime.UtcNow, UnitCount = 2, DefectCount = 10 }, // u = 5
            new() { MeasuredAt = DateTime.UtcNow, UnitCount = 2, DefectCount = 20 }  // u = 10
        };

        var result = AttributeChartCalculator.Calculate("U", rawData, GetEmptyLimits());

        // Total defects = 30, Total units = 4. uBar = 7.5. nBar = 2.
        // UCL = uBar + 3 * sqrt(uBar/nBar) = 7.5 + 3 * sqrt(7.5/2) = 7.5 + 3 * 1.936 = 13.3
        dynamic stats = result.StatControlLimits!;
        Assert.Equal(7.5, (double)stats.cl, 2);
        Assert.Equal(7.5 + 3 * Math.Sqrt(7.5 / 2.0), (double)stats.ucl, 2);
    }
}
