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
        result.Normality!.TestName.Should().Be("Adjusted Jarque-Bera");
        result.Normality.MethodNote.Should().Contain("Minitab");
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
    public void TestSmallSampleAdjustedJarqueBera_FailsWhenClassicPValueWouldPass()
    {
        // GIVEN: Small raw dataset where the asymptotic Jarque-Bera p-value is about 0.10,
        // but the adjusted Jarque-Bera test detects non-normality.
        var values = new List<double> { 0.1602, -1.2352, 0.4644, -0.5592, -2.4591, -0.2133, -0.9788, -0.5206, -0.1523, 3.7510 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 5, LSL = -5, Target = 0 };
        var initialResult = new ControlChartResult();

        // WHEN: Calling PopulateNormalityAndCurve
        var result = SpcService.PopulateNormalityAndCurve(initialResult, rawPoints, limits);

        // THEN: The adjusted statistic should mark it as non-normal.
        result.Normality.Should().NotBeNull();
        result.Normality!.PValue.Should().NotBeNull();
        result.Normality.PValue.Value.Should().BeLessThan(0.05);
        result.Normality.IsNormal.Should().BeFalse();
        result.Normality.MethodNote.Should().Contain("Adjusted Jarque-Bera");
    }

    [Fact]
    public void FixedMinitabComparisonSample_DisclosesDifferentMethodAndStableResult()
    {
        var values = new List<double> { 7.92, 7.75, 7.87, 7.81, 7.95, 7.77, 7.83, 7.86, 7.9, 7.79, 7.84, 7.88 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 8.5, LSL = 7.5, Target = 8.0 };

        var result = SpcService.PopulateNormalityAndCurve(new ControlChartResult(), rawPoints, limits);

        result.Normality.Should().NotBeNull();
        result.Normality!.TestName.Should().Be("Adjusted Jarque-Bera");
        result.Normality.MethodNote.Should().Contain("Anderson-Darling");
        result.Normality.PValue.Should().NotBeNull();
        result.Normality.PValue!.Value.Should().BeInRange(0, 1);
        result.NormalCurve.Should().NotBeNull();
    }

    [Fact]
    public void AndersonDarlingMethod_ReturnsAdTestNameAndPValue()
    {
        var values = new List<double> { 7.92, 7.75, 7.87, 7.81, 7.95, 7.77, 7.83, 7.86, 7.9, 7.79, 7.84, 7.88 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 8.5, LSL = 7.5, Target = 8.0 };

        var result = SpcService.PopulateNormalityAndCurve(new ControlChartResult(), rawPoints, limits, "AD");

        result.Normality.Should().NotBeNull();
        result.Normality!.TestName.Should().Be("Anderson-Darling");
        result.Normality.MethodNote.Should().Contain("Anderson-Darling");
        result.Normality.PValue.Should().NotBeNull();
        result.Normality.PValue!.Value.Should().BeInRange(0, 1);
        result.Normality.Statistic.Should().BeGreaterThan(0);
        result.NormalCurve.Should().NotBeNull();
    }

    [Fact]
    public void AndersonDarlingMethod_DegenerateDataset_ReturnsNullPValueWithNote()
    {
        var values = new List<double> { 10.0, 10.0, 10.0, 10.0, 10.0 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var limits = new ControlLimits { USL = 12, LSL = 8, Target = 10 };

        var result = SpcService.PopulateNormalityAndCurve(new ControlChartResult(), rawPoints, limits, "Anderson-Darling");

        result.Normality.Should().NotBeNull();
        result.Normality!.TestName.Should().Be("Anderson-Darling");
        result.Normality.PValue.Should().BeNull();
        result.Normality.Note.Should().Contain("數據無變異");
        result.NormalCurve.Should().BeNullOrEmpty();
    }

    [Fact]
    public void NormalCurveScale_UsesMeasurementRangeWhenSpecLimitsAreDistant()
    {
        var values = new List<double> { 7.91, 7.92, 7.925, 7.93, 7.935, 7.94, 7.945, 7.95 };
        var rawPoints = values.Select(v => new SpcDataPoint { Value = v }).ToList();
        var nearLimits = new ControlLimits { USL = 8, LSL = 7.8, Target = 7.93 };
        var distantLimits = new ControlLimits { USL = 100, LSL = -100, Target = 0 };

        var nearResult = SpcService.PopulateNormalityAndCurve(new ControlChartResult(), rawPoints, nearLimits);
        var distantResult = SpcService.PopulateNormalityAndCurve(new ControlChartResult(), rawPoints, distantLimits);

        nearResult.NormalCurve.Should().NotBeNull();
        distantResult.NormalCurve.Should().NotBeNull();
        distantResult.NormalCurve!.Max(x => x.ScaledPdf)
            .Should().BeApproximately(nearResult.NormalCurve!.Max(x => x.ScaledPdf), 0.000001);
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
