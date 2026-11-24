using MesSpc.Api.Controllers;
using Xunit;

namespace MesSpc.Api.Tests;

public sealed class ChemicalDailyAdjustmentTests
{
    private const string Initial = """{"result":{"Adjustment":"稀釋","AdjustmentAmount":"通知工程師"}}""";
    private const string Recheck = """{"result":{"Adjustment":"","AdjustmentAmount":""}}""";

    [Fact]
    public void RestoresInitialAdjustmentWhenBothSavedFieldsMatchTheRecheck()
    {
        Assert.Equal(("稀釋", "通知工程師"), ManualMeasurementsV1Controller.ResolveDailyAdjustment(null, "", Initial, Recheck));
    }

    [Theory]
    [InlineData(null, Recheck)]
    [InlineData(Initial, null)]
    [InlineData(Initial, "{}")]
    [InlineData("invalid", Recheck)]
    public void LeavesSavedFieldsWhenEvidenceIsMissingOrInvalid(string? initial, string? recheck)
    {
        Assert.Equal(("", (string?)null), ManualMeasurementsV1Controller.ResolveDailyAdjustment("", null, initial, recheck));
    }

    [Fact]
    public void LeavesManualDifferenceIntact()
    {
        Assert.Equal(("稀釋", "人工覆核"), ManualMeasurementsV1Controller.ResolveDailyAdjustment("稀釋", "人工覆核", Initial, Recheck));
    }
}
