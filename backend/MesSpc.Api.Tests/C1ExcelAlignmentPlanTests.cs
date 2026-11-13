using System.Text.Json;
using C1ExcelAlignment;

namespace MesSpc.Api.Tests;

public sealed class C1ExcelAlignmentPlanTests
{
    [Fact]
    public void PrecisionOnly_ChangesOnlyDecimalPlaces()
    {
        const string source = "{\"enabled\":true,\"concentrationFormula\":\"Primary*6.25*F!$B$6\",\"decimalPlaces\":2,\"adjustmentFormula\":\"x\"}";

        var result = AlignmentPlan.AlignConfig(source, false);

        using var json = JsonDocument.Parse(result);
        Assert.Equal(3, json.RootElement.GetProperty("decimalPlaces").GetInt32());
        Assert.Equal("Primary*6.25*F!$B$6", json.RootElement.GetProperty("concentrationFormula").GetString());
        Assert.Equal("x", json.RootElement.GetProperty("adjustmentFormula").GetString());
    }

    [Fact]
    public void TankOneSulfuric_UsesExcelTargets()
    {
        const string source = "{\"enabled\":true,\"concentrationFormula\":\"Primary*24.5*F!$B$3\",\"decimalPlaces\":2,\"adjustmentFormula\":\"IF(Concentration=\\\"\\\",\\\"\\\",IF(Concentration<185,\\\"添加\\\",IF(Concentration>205,\\\"稀釋\\\",\\\"\\\")))\",\"adjustmentAmountFormula\":\"IFERROR(IF(Concentration<185,\\\"硫酸(50%)：\\\"&ROUND(((185-Concentration)*1900/0.714/1000),1)&\\\" L\\\",IF(Concentration=\\\"\\\",\\\"\\\",IF(Concentration>205,\\\"通知工程師\\\",\\\"\\\"))),\\\"\\\")\"}";

        var result = AlignmentPlan.AlignConfig(source, true);

        using var json = JsonDocument.Parse(result);
        Assert.Equal(3, json.RootElement.GetProperty("decimalPlaces").GetInt32());
        Assert.Contains("Concentration<180", json.RootElement.GetProperty("adjustmentFormula").GetString());
        Assert.Contains("Concentration>195", json.RootElement.GetProperty("adjustmentFormula").GetString());
        Assert.Contains("(180-Concentration)", json.RootElement.GetProperty("adjustmentAmountFormula").GetString());
    }

    [Fact]
    public void SplitAt_PreservesHistoryAndCreatesContiguousCurrentPeriod()
    {
        var cutoff = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
        var periods = new[]
        {
            new FormulaSlice(new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc), null, 277, 247)
        };

        var result = AlignmentPlan.SplitAt(periods, cutoff, 277, 247, 900);

        Assert.True(result.Changed);
        Assert.Equal(2, result.Periods.Count);
        Assert.Equal(cutoff, result.Periods[0].UntilUtc);
        Assert.Equal(new FormulaSlice(cutoff, null, 900, 247), result.Periods[1]);
    }

    [Fact]
    public void SplitAt_RejectsUnexpectedCurrentFormula()
    {
        var cutoff = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
        var periods = new[] { new FormulaSlice(new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc), null, 999, 247) };

        Assert.Throws<InvalidOperationException>(() => AlignmentPlan.SplitAt(periods, cutoff, 277, 247, 900));
    }

    [Fact]
    public void SplitAt_PreservesReviewedHistoricalGaps()
    {
        var cutoff = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
        var periods = new[]
        {
            new FormulaSlice(new DateTime(2026, 10, 1, 16, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 2, 16, 0, 0, DateTimeKind.Utc), 100, 247),
            new FormulaSlice(new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc), null, 277, 247)
        };

        var result = AlignmentPlan.SplitAt(periods, cutoff, 277, 247, 900);

        Assert.Equal(3, result.Periods.Count);
        Assert.Equal(periods[0], result.Periods[0]);
        Assert.Equal(cutoff, result.Periods[1].UntilUtc);
    }
}
