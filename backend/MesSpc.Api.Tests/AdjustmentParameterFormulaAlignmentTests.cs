using AdjustmentParameterFormulaAlignment;

namespace MesSpc.Api.Tests;

public sealed class AdjustmentParameterFormulaAlignmentTests
{
    [Fact]
    public void AlignFormula_ReplacesActionThresholdsAndCapacityButPreservesUnitConversion()
    {
        const string formula = "IFERROR(IF(Concentration>USL,\"排液：\"&ROUND(1100*(1-Target/Concentration),0)&\" L\"&\" 補水：\"&ROUND(1100*(1-Target/Concentration),0)&\" L\",IF(Concentration<LSL,\"DP333：\"&ROUND((Target-Concentration)*1100/1000,1)&\" L\",\"\")),\"\")";

        var result = FormulaParameterAlignment.AlignFormula(formula, 1100m, 48m, 72m);

        Assert.Equal(FormulaAlignmentStatus.Update, result.Status);
        Assert.Equal("IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE,\"排液：\"&ROUND(TANK_CAPACITY_LITERS*(1-Target/Concentration),0)&\" L\"&\" 補水：\"&ROUND(TANK_CAPACITY_LITERS*(1-Target/Concentration),0)&\" L\",IF(Concentration<ADDITION_CONTROL_VALUE,\"DP333：\"&ROUND((Target-Concentration)*TANK_CAPACITY_LITERS/1000,1)&\" L\",\"\")),\"\")", result.Formula);
    }

    [Fact]
    public void AlignFormula_WhenCapacityIsOneThousand_DoesNotReplaceUnitDivisor()
    {
        const string formula = "IF(Concentration<20,ROUND((20-Concentration)*1000/1000,1),\"\")";

        var result = FormulaParameterAlignment.AlignFormula(formula, 1000m, 20m, null);

        Assert.Equal(FormulaAlignmentStatus.Update, result.Status);
        Assert.Contains("*TANK_CAPACITY_LITERS/1000", result.Formula);
        Assert.DoesNotContain("/TANK_CAPACITY_LITERS", result.Formula);
    }

    [Theory]
    [InlineData("IF(48>Concentration,\"添加\",\"\")", "IF(ADDITION_CONTROL_VALUE>Concentration,\"添加\",\"\")")]
    [InlineData("IF(72<Concentration,\"稀釋\",\"\")", "IF(DILUTION_CONTROL_VALUE<Concentration,\"稀釋\",\"\")")]
    public void AlignFormula_HandlesReversedNumericComparisons(string formula, string expected)
    {
        var result = FormulaParameterAlignment.AlignFormula(formula, null, 48m, 72m);

        Assert.Equal(FormulaAlignmentStatus.Update, result.Status);
        Assert.Equal(expected, result.Formula);
    }

    [Fact]
    public void AlignFormula_DoesNotRewriteNumbersInsideText()
    {
        const string formula = "IF(Concentration<48,\"槽容1100，換算/1000\",\"\")";

        var result = FormulaParameterAlignment.AlignFormula(formula, 1100m, 48m, 72m);

        Assert.Equal("IF(Concentration<ADDITION_CONTROL_VALUE,\"槽容1100，換算/1000\",\"\")", result.Formula);
    }

    [Fact]
    public void AlignFormula_IsIdempotent()
    {
        const string formula = "IF(Concentration<ADDITION_CONTROL_VALUE,ROUND((Target-Concentration)*TANK_CAPACITY_LITERS/1000,1),\"\")";

        var result = FormulaParameterAlignment.AlignFormula(formula, 1000m, 20m, null);

        Assert.Equal(FormulaAlignmentStatus.AlreadyAligned, result.Status);
        Assert.Equal(formula, result.Formula);
    }

    [Fact]
    public void AlignFormula_MissingReferencedParameterIsIsolated()
    {
        var result = FormulaParameterAlignment.AlignFormula("IF(Concentration<LSL,\"添加\",\"\")", 1000m, null, 72m);

        Assert.Equal(FormulaAlignmentStatus.MissingParameter, result.Status);
        Assert.Equal("IF(Concentration<LSL,\"添加\",\"\")", result.Formula);
    }

    [Fact]
    public void AlignFormula_DifferentLegacyNumericThresholdUsesCurrentControlParameter()
    {
        const string formula = "IF(Concentration<47,\"添加\",IF(Concentration>72,\"稀釋\",\"\"))";

        var result = FormulaParameterAlignment.AlignFormula(formula, 1000m, 48m, 72m);

        Assert.Equal(FormulaAlignmentStatus.Update, result.Status);
        Assert.Equal("IF(Concentration<ADDITION_CONTROL_VALUE,\"添加\",IF(Concentration>DILUTION_CONTROL_VALUE,\"稀釋\",\"\"))", result.Formula);
    }

    [Fact]
    public void AlignFormula_BlankThenAddInverseBranchUsesAdditionParameter()
    {
        const string formula = "IF(Concentration=\"\",\"\",IF(Concentration>15,\"\",\"添加\"))";

        var result = FormulaParameterAlignment.AlignFormula(formula, null, 20m, 30m);

        Assert.Equal(FormulaAlignmentStatus.Update, result.Status);
        Assert.Equal("IF(Concentration=\"\",\"\",IF(Concentration>ADDITION_CONTROL_VALUE,\"\",\"添加\"))", result.Formula);
    }
}
