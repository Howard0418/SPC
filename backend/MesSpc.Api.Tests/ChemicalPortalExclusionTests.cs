using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class ChemicalPortalExclusionTests
{
    [Fact]
    public void PortalFixedRemark_RemainsVisibleButExcludedFromStatistics()
    {
        Assert.Equal("ExcludedVisible", ChemicalPortalMeasurementService.PortalExclusionState);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("", false, null)]
    [InlineData(" CLOSE_LINE ", true, "CLOSE_LINE")]
    [InlineData("NO_OPEN_LINE_CURRENT_TANK", true, "NO_OPEN_LINE_CURRENT_TANK")]
    public void NormalizeExclusion_UsesFixedReasonAsSingleSource(string? reason, bool expectedExcluded, string? expectedReason)
    {
        var actual = ChemicalPortalMeasurementService.NormalizeExclusion(reason);

        Assert.Equal(expectedExcluded, actual.IsExcluded);
        Assert.Equal(expectedReason, actual.Reason);
    }

    [Fact]
    public void NormalizeExclusion_RejectsUnknownReason()
    {
        Assert.Throws<ArgumentException>(() => ChemicalPortalMeasurementService.NormalizeExclusion("自由備註"));
    }
}
