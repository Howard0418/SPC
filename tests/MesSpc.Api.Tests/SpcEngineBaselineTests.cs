using FluentAssertions;
using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Rules;

namespace MesSpc.Api.Tests;

public class SpcEngineBaselineTests
{
    [Fact]
    public void Imr_GoldenValues_ShouldMatchHandCalculation()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var points = new[] { 10d, 12d, 11d, 14d, 13d }
            .Select((value, index) => new SpcDataPoint
            {
                MeasuredAt = start.AddMinutes(index),
                Value = value
            })
            .ToList();

        var result = ImrChartCalculator.Calculate(points, new ControlLimits { LSL = 5, USL = 15 });
        var iLimits = Property(result.StatControlLimits!, "iControlLimitsStat");
        var mrLimits = Property(result.StatControlLimits!, "mrControlLimitsStat");

        Number(iLimits, "cl").Should().BeApproximately(12.0, 1e-12);
        Number(mrLimits, "cl").Should().BeApproximately(1.75, 1e-12);
        Number(iLimits, "ucl").Should().BeApproximately(12 + 3 * 1.75 / 1.128, 1e-12);
        Number(iLimits, "lcl").Should().BeApproximately(12 - 3 * 1.75 / 1.128, 1e-12);
        Number(mrLimits, "ucl").Should().BeApproximately(3.267 * 1.75, 1e-12);
        Number(mrLimits, "lcl").Should().Be(0);
    }

    [Fact]
    public void XbarR_GoldenValues_ShouldUseN3Constants()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var groups = new List<Subgroup>
        {
            new() { MeasuredAt = start, Values = [10, 12, 11] },
            new() { MeasuredAt = start.AddHours(1), Values = [14, 13, 12] },
            new() { MeasuredAt = start.AddHours(2), Values = [10, 9, 11] }
        };

        var result = XbarRChartCalculator.Calculate(groups, new ControlLimits(), 3);
        var xbar = Property(result.StatControlLimits!, "xbarControl");
        var range = Property(result.StatControlLimits!, "rControl");
        const double xDoubleBar = 34d / 3d;

        Number(xbar, "cl").Should().BeApproximately(xDoubleBar, 1e-12);
        Number(range, "cl").Should().Be(2);
        Number(xbar, "ucl").Should().BeApproximately(xDoubleBar + 1.023 * 2, 1e-12);
        Number(xbar, "lcl").Should().BeApproximately(xDoubleBar - 1.023 * 2, 1e-12);
        Number(range, "ucl").Should().BeApproximately(2.575 * 2, 1e-12);
        Number(range, "lcl").Should().Be(0);
    }

    [Fact]
    public void Capability_GoldenValues_ShouldSeparateWithinAndOverallSigma()
    {
        var groups = new List<Subgroup>
        {
            new() { Values = [9, 10, 11] },
            new() { Values = [10, 11, 12] }
        };

        var result = ProcessCapabilityCalculator.Calculate(groups, 14, 6)!;

        result.SigmaWithin!.Value.Should().BeApproximately(2d / 1.693, 0.0001);
        result.SigmaOverall!.Value.Should().BeApproximately(Math.Sqrt(5.5 / 5), 0.0001);
        result.Cp!.Value.Should().BeApproximately((14d - 6d) / (6 * (2d / 1.693)), 0.0001);
        result.Cpk!.Value.Should().BeApproximately((14d - 10.5d) / (3 * (2d / 1.693)), 0.0001);
        result.Pp!.Value.Should().BeApproximately((14d - 6d) / (6 * Math.Sqrt(5.5 / 5)), 0.0001);
        result.Ppk!.Value.Should().BeApproximately((14d - 10.5d) / (3 * Math.Sqrt(5.5 / 5)), 0.0001);
        result.Ppm.Should().Be(0);
    }

    [Fact]
    public void PChart_GoldenValues_ShouldWeightPBarByInspectedQuantity()
    {
        var points = new List<AttributeDataPoint>
        {
            new() { MeasuredAt = new DateTime(2026, 1, 1), InspectedQty = 100, DefectQty = 10 },
            new() { MeasuredAt = new DateTime(2026, 1, 2), InspectedQty = 300, DefectQty = 15 }
        };

        var result = AttributeChartCalculator.Calculate("P", points, new ControlLimits());

        Number(result.StatControlLimits!, "cl").Should().BeApproximately(25d / 400d, 1e-12);
        Number(result.StatControlLimits!, "nBar").Should().Be(200);
    }

    [Fact]
    public void WesternElectric_GoldenSignals_ShouldDetectPointAndRunRules()
    {
        var pointBeyondThreeSigma = new List<SpcDataPoint>
        {
            new() { Value = 13.1 }
        };
        WesternElectricRulesValidator.ApplyRules(
            pointBeyondThreeSigma,
            new ControlLimits { CL = 10, UCL = 13, LCL = 7 });

        pointBeyondThreeSigma[0].ViolatedRules.Should().Contain("Rule1_Over3Sigma");
        pointBeyondThreeSigma[0].IsOutOfControl.Should().BeTrue();

        var sameSideRun = Enumerable.Range(1, 9)
            .Select(index => new SpcDataPoint { Value = 10 + index * 0.01 })
            .ToList();
        WesternElectricRulesValidator.ApplyRules(
            sameSideRun,
            new ControlLimits { CL = 10, UCL = 13, LCL = 7 });

        sameSideRun[^1].ViolatedRules.Should().Contain("Rule2_9SameSide");
    }

    private static object Property(object source, string name) =>
        source.GetType().GetProperty(name)?.GetValue(source)
        ?? throw new InvalidOperationException($"Missing SPC result property '{name}'.");

    private static double Number(object source, string name) =>
        Convert.ToDouble(Property(source, name));
}

public class PhaseTwoSpcAcceptanceTests
{
    [Fact(Skip = "Phase 2: raw observations must drive OOS; subgroup means only drive Xbar OOC.")]
    public void Xbar_ShouldReportRawObservationOutsideSpecification() { }

    [Fact(Skip = "Phase 2: an excluded observation must break, not compress, the moving-range sequence.")]
    public void Imr_ShouldBreakMovingRangeAtExcludedObservation() { }

    [Fact(Skip = "Phase 2: mixed subgroup sizes must not share constants from the first subgroup.")]
    public void XbarR_ShouldRejectOrSeparateMixedSubgroupSizes() { }

    [Fact(Skip = "Phase 2: P/U run rules must use each point's sample-size-dependent limits.")]
    public void AttributeRunRules_ShouldUsePointSpecificLimits() { }
}
