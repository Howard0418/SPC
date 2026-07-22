using FluentAssertions;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MesSpc.Api.Tests;

public class NormalityTest
{
    [Fact]
    public void TestNormalDataset_SucceedsAndPassesNormality()
    {
        // GIVEN: A standard normally distributed dataset
        var values = new List<double> { 10.0, 10.1, 9.9, 10.2, 9.8, 10.05, 9.95, 10.02, 9.98, 10.0 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 12, LSL = 8, Target = 10 };
        var initialResult = new ControlChartResult();

        // WHEN: Calling PopulateNormalityAndCurve
        var result = SpcService.PopulateNormalityAndCurve(initialResult, rawPoints, limits);

        // THEN: It should calculate stats
        result.Should().NotBeNull();
        result.Normality.Should().NotBeNull();
        result.Normality!.TestName.Should().Be("Jarque-Bera");
        result.Normality.PValue.Should().NotBeNull();
        
        // P-value should be high (>= 0.05) because the dataset is symmetric
        result.Normality.PValue.Value.Should().BeGreaterThanOrEqualTo(0.05);
        result.Normality.IsNormal.Should().BeTrue();
        result.Normality.Note.Should().BeNull();

        // Skewness should be close to 0 (symmetric)
        Math.Abs(result.Normality.Skewness).Should().BeLessThan(0.5);

        // Kurtosis should be close to 3
        Math.Abs(result.Normality.Kurtosis - 3.0).Should().BeLessThan(1.5);

        // Curve should be generated
        result.NormalCurve.Should().NotBeNull();
        result.NormalCurve!.Count.Should().Be(101);
    }

    [Fact]
    public void TestSkewedDataset_FailsNormalityTest()
    {
        // GIVEN: A highly skewed/non-normal dataset with extreme outliers
        var values = Enumerable.Repeat(10.0, 20).Append(50.0).ToList();
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 12, LSL = 8, Target = 10 };
        var initialResult = new ControlChartResult();

        // WHEN: Calling PopulateNormalityAndCurve
        var result = SpcService.PopulateNormalityAndCurve(initialResult, rawPoints, limits);

        // THEN: Normality test should fail (p-value < 0.05)
        result.Normality.Should().NotBeNull();
        result.Normality!.PValue.Should().NotBeNull();
        result.Normality.PValue.Value.Should().BeLessThan(0.05);
        result.Normality.IsNormal.Should().BeFalse();
        
        // Skewness and kurtosis should reflect non-normal distributions
        Math.Abs(result.Normality.Skewness).Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void TestDegenerateDataset_ReturnsNullPValueWithNote()
    {
        // GIVEN: A dataset with zero variance (all values are identical)
        var values = new List<double> { 10.0, 10.0, 10.0, 10.0, 10.0 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 12, LSL = 8, Target = 10 };
        var initialResult = new ControlChartResult();

        // WHEN: Calling PopulateNormalityAndCurve
        var result = SpcService.PopulateNormalityAndCurve(initialResult, rawPoints, limits);

        // THEN: P-value should be null, note should explain why
        result.Normality.Should().NotBeNull();
        result.Normality!.PValue.Should().BeNull();
        result.Normality.Note.Should().Contain("數據無變異");
        result.NormalCurve.Should().BeNullOrEmpty();
    }
}
