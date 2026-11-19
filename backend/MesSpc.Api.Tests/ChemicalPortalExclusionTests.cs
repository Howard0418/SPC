using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class ChemicalPortalExclusionTests
{
    [Fact]
    public void PortalFixedRemark_DoesNotCreateAnExclusion()
    {
        Assert.Equal("CLOSE_LINE", ChemicalPortalMeasurementService.NormalizeEntryRemark(" CLOSE_LINE "));
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("", false, null)]
    [InlineData(" CLOSE_LINE ", true, "CLOSE_LINE")]
    [InlineData("NO_OPEN_LINE_CURRENT_TANK", true, "NO_OPEN_LINE_CURRENT_TANK")]
    public void NormalizeEntryRemark_AcceptsOnlyFixedValues(string? reason, bool expectedExcluded, string? expectedReason)
    {
        var actual = ChemicalPortalMeasurementService.NormalizeEntryRemark(reason);

        Assert.Equal(expectedExcluded, actual is not null);
        Assert.Equal(expectedReason, actual);
    }

    [Fact]
    public void NormalizeEntryRemark_RejectsUnknownReason()
    {
        Assert.Throws<ArgumentException>(() => ChemicalPortalMeasurementService.NormalizeEntryRemark("自由備註"));
    }

    [Fact]
    public void ReadEntryRemarkPayload_ReturnsRemarkForMatchingPpcOnly()
    {
        const string payload="{\"PpcId\":2437,\"ExclusionReason\":\"CLOSE_LINE\"}";
        Assert.Equal("CLOSE_LINE",ChemicalPortalMeasurementService.ReadEntryRemarkPayload(payload,2437));
        Assert.Null(ChemicalPortalMeasurementService.ReadEntryRemarkPayload(payload,1));
        Assert.Null(ChemicalPortalMeasurementService.ReadEntryRemarkPayload("{\"PpcId\":2437,\"ExclusionReason\":\"舊自由文字\"}",2437));
    }
}
