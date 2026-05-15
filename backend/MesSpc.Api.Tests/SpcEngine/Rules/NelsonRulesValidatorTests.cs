using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Rules;

public class NelsonRulesValidatorTests
{
    private static ControlLimits GetStandardLimits() => new()
    {
        CL = 10,
        UCL = 13, // sigma = 1
        LCL = 7
    };

    private static SpcDataPoint P(double value) => new() { MeasuredAt = DateTime.UtcNow, Value = value };

    [Fact]
    public void Rule1_ShouldTrigger_WhenPointIsBeyond3Sigma()
    {
        var points = new List<SpcDataPoint> { P(10), P(11), P(13.1) }; // 13.1 > 13 (UCL)
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        Assert.Empty(points[0].ViolatedRules);
        Assert.Empty(points[1].ViolatedRules);
        Assert.Contains("Nelson1", points[2].ViolatedRules);
        Assert.True(points[2].IsOutOfControl);
    }

    [Fact]
    public void Rule2_ShouldTrigger_When9PointsOnSameSideOfMean()
    {
        var points = new List<SpcDataPoint>();
        for (int i = 0; i < 9; i++) points.Add(P(11)); // All above mean (10)
        
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        // Only the 9th point should trigger the rule
        for (int i = 0; i < 8; i++) Assert.Empty(points[i].ViolatedRules);
        Assert.Contains("Nelson2", points[8].ViolatedRules);
        Assert.True(points[8].IsOutOfControl);
    }

    [Fact]
    public void Rule3_ShouldTrigger_When6PointsContinuouslyIncreasing()
    {
        var points = new List<SpcDataPoint>
        {
            P(9), P(9.5), P(10), P(10.5), P(11), P(11.5) // 6 points increasing
        };
        
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        Assert.Contains("Nelson3", points[5].ViolatedRules);
        Assert.True(points[5].IsOutOfControl);
    }

    [Fact]
    public void Rule4_ShouldTrigger_When14PointsAlternating()
    {
        var points = new List<SpcDataPoint>();
        for (int i = 0; i < 14; i++)
        {
            points.Add(P(i % 2 == 0 ? 11 : 9)); // Alternates between 11 and 9
        }
        
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        Assert.Contains("Nelson4", points[13].ViolatedRules);
        Assert.True(points[13].IsOutOfControl);
    }

    [Fact]
    public void Rule5_ShouldTrigger_When2OutOf3PointsBeyond2Sigma()
    {
        // 2 sigma limit is 10 +/- 2 = 12 or 8
        var points = new List<SpcDataPoint>
        {
            P(12.5), // > 2 sigma
            P(10),   // normal
            P(12.1)  // > 2 sigma
        };
        
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        Assert.Contains("Nelson5", points[2].ViolatedRules);
        Assert.True(points[2].IsOutOfControl);
    }

    [Fact]
    public void Rule6_ShouldTrigger_When4OutOf5PointsBeyond1Sigma()
    {
        // 1 sigma limit is 10 +/- 1 = 11 or 9
        var points = new List<SpcDataPoint>
        {
            P(11.5), // > 1 sigma
            P(11.2), // > 1 sigma
            P(10),   // normal
            P(11.1), // > 1 sigma
            P(11.3)  // > 1 sigma
        };
        
        NelsonRulesValidator.ApplyRules(points, GetStandardLimits());
        
        Assert.Contains("Nelson6", points[4].ViolatedRules);
        Assert.True(points[4].IsOutOfControl);
    }

    [Fact]
    public void Validator_ShouldHandleEmptyList()
    {
        var points = new List<SpcDataPoint>();
        var exception = Record.Exception(() => NelsonRulesValidator.ApplyRules(points, GetStandardLimits()));
        Assert.Null(exception);
    }
}
