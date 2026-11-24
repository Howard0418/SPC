using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class ChemicalTimedScheduleTests
{
    [Fact]
    public void TimedCutoverPreservesExactBoundaryAndEarlierCalendarPeriod()
    {
        var periods = ChemicalFormulaCalculation.ValidatePeriods([
            new(291, 247, "2026-11-24", "2026-11-24"),
            new(291, 247, "2026-11-25T00:00:00+08:00", "2026-11-25T10:30:00+08:00"),
            new(1544, 247, "2026-11-25T10:30:00+08:00", null)
        ]);

        var at = new DateTime(2026, 11, 25, 2, 30, 0, DateTimeKind.Utc);
        Assert.Equal(at, periods[1].UntilUtc);
        Assert.Equal(at, periods[2].FromUtc);
        Assert.Equal(periods[0].UntilUtc, periods[1].FromUtc);
    }

    [Fact]
    public void PortalUsesCurrentAdjustmentsAndHistoricalConcentration()
    {
        const string historical = """{"enabled":true,"concentrationFormula":"Primary*2","adjustmentFormula":"\"舊\"","adjustmentAmountFormula":"\"舊值\"","tankCapacityLiters":20}""";
        const string current = """{"enabled":true,"concentrationFormula":"Primary*3","adjustmentFormula":"\"稀釋\"","adjustmentAmountFormula":"TANK_CAPACITY_LITERS-Concentration","tankCapacityLiters":100}""";
        var legacy = ChemicalFormulaCalculation.Calculate(historical,"8",null,"[]",null,null,null);
        var portal = ChemicalFormulaCalculation.Calculate(historical,"8",null,"[]",null,null,null,
            adjustmentSettings: ChemicalFormulaCalculation.ReadCurrentAdjustment(current));
        Assert.Equal("16", portal.Stored);
        Assert.Equal("稀釋", portal.Adjustment);
        Assert.Equal("84", portal.AdjustmentAmount);
        Assert.Equal("舊值", legacy.AdjustmentAmount);
        var blank = ChemicalFormulaCalculation.Calculate(historical,"8",null,"[]",null,null,null,
            adjustmentSettings: ChemicalFormulaCalculation.ReadCurrentAdjustment(null));
        Assert.Empty(blank.Adjustment);
        Assert.Empty(blank.AdjustmentAmount);
    }

    [Theory]
    [InlineData("2026-11-25", "2026-11-25T10:30:00+08:00")]
    [InlineData("2026-11-25T10:30:00", null)]
    [InlineData("2026-11-25T10:30:00+08:00", "2026-11-25T10:29:00+08:00")]
    public void RejectsAmbiguousOrReversedTimedIntervals(string start, string? end)
    {
        Assert.Throws<ArgumentException>(() =>
            ChemicalFormulaCalculation.ValidatePeriods([new(291, 247, start, end)]));
    }
}
