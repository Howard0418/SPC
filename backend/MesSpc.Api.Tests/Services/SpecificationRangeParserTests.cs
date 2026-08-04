using MesSpc.Api.Services;

namespace MesSpc.Api.Tests.Services;

public class SpecificationRangeParserTests
{
    [Theory]
    [InlineData("15", "13 ~ 17", 15, 13, 17)]
    [InlineData("15", "17～13", 15, 13, 17)]
    [InlineData("15", "13-17", 15, 13, 17)]
    [InlineData("15±2", null, 15, 13, 17)]
    [InlineData("15 +/- 2", "", 15, 13, 17)]
    public void Parse_ExtractsTargetAndLimits(
        string specification,
        string? range,
        double target,
        double lsl,
        double usl)
    {
        var result = SpecificationRangeParser.Parse(specification, range);

        Assert.Equal(target, result.TargetValue);
        Assert.Equal(lsl, result.Lsl);
        Assert.Equal(usl, result.Usl);
    }

    [Fact]
    public void Parse_BlankValues_ReturnsNoLimits()
    {
        var result = SpecificationRangeParser.Parse(null, null);

        Assert.Null(result.TargetValue);
        Assert.Null(result.Lsl);
        Assert.Null(result.Usl);
    }
}
