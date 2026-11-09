using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class LimitBatchValidatorTests
{
    [Fact]
    public void Validate_AcceptsSingleSidedSpecificationAndAutomaticControlLimits()
    {
        var errors = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: null, Target: 100, Usl: 120,
            Lcl: null, Cl: null, Ucl: null));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RejectsNonFiniteValues()
    {
        var errors = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: double.NaN, Target: null, Usl: double.PositiveInfinity,
            Lcl: null, Cl: null, Ucl: null));

        Assert.Contains(errors, error => error.Field == "LSL" && error.Code == "NON_FINITE");
        Assert.Contains(errors, error => error.Field == "USL" && error.Code == "NON_FINITE");
    }

    [Fact]
    public void Validate_RejectsInvalidSpecificationOrderAndTargetOutsideRange()
    {
        var orderErrors = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: 10, Target: 10, Usl: 10,
            Lcl: null, Cl: null, Ucl: null));
        var targetErrors = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: 10, Target: 21, Usl: 20,
            Lcl: null, Cl: null, Ucl: null));

        Assert.Contains(orderErrors, error => error.Code == "SPEC_ORDER");
        Assert.Contains(targetErrors, error => error.Field == "Target" && error.Code == "TARGET_OUTSIDE_SPEC");
    }

    [Fact]
    public void Validate_RequiresCompleteOrderedManualControlLimits()
    {
        var incomplete = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: null, Target: null, Usl: null,
            Lcl: 1, Cl: null, Ucl: 3));
        var unordered = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: null, Target: null, Usl: null,
            Lcl: 3, Cl: 2, Ucl: 1));

        Assert.Contains(incomplete, error => error.Code == "CONTROL_LIMITS_INCOMPLETE");
        Assert.Contains(unordered, error => error.Code == "CONTROL_LIMITS_ORDER");
    }

    [Theory]
    [InlineData("XBAR_R")]
    [InlineData("XBAR_S")]
    [InlineData("I_MR")]
    [InlineData("P")]
    [InlineData("NP")]
    [InlineData("C")]
    [InlineData("U")]
    public void Validate_AcceptsCompleteManualLimitsForSupportedChartTypes(string chartType)
    {
        var errors = LimitBatchValidator.Validate(new LimitBatchValues(
            Lsl: 0, Target: 5, Usl: 10,
            Lcl: 1, Cl: 5, Ucl: 9), chartType);

        Assert.Empty(errors);
    }
}
