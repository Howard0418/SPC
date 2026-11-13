using FluentAssertions;
using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public class ChemicalFormulaCalculationTests
{
    [Theory]
    [InlineData("0.3524", "53.504")]
    [InlineData("0.3525", "53.656")]
    [InlineData("-0.3525", "-53.656")]
    public void TitrationIsRoundedToThreeDecimalsBeforeFormulaAndResultIsThreeDecimals(string primary, string expected)
    {
        const string config = "{\"enabled\":true,\"concentrationFormula\":\"Primary*152\",\"decimalPlaces\":2}";

        var result = ChemicalFormulaCalculation.Calculate(config, primary, null, "[]", null, null, null);

        result.Raw.Should().Be(expected);
        result.Stored.Should().Be(expected);
    }

    [Fact]
    public void InitialRecheckAndSecondaryUseSameThreeDecimalInputPolicy()
    {
        const string config = "{\"enabled\":true,\"concentrationFormula\":\"Primary+Secondary\",\"recheckConcentrationFormula\":\"Primary*Secondary\",\"decimalPlaces\":8}";

        var initial = ChemicalFormulaCalculation.Calculate(config, "1.2344", "2.3455", "[]", null, null, null, "Initial");
        var recheck = ChemicalFormulaCalculation.Calculate(config, "1.2344", "2.3455", "[]", null, null, null, "Recheck");

        initial.Raw.Should().Be("3.58");
        initial.Stored.Should().Be("3.58");
        recheck.Raw.Should().Be("2.894964");
        recheck.Stored.Should().Be("2.895");
    }

    [Theory]
    [InlineData("IF(Primary=0,7,1/Primary)","0","7")]
    [InlineData("IF(Primary=0,1/(Primary-1),7)","1","7")]
    [InlineData("IFERROR(IF(Primary=0,1/Primary,9),7)","0","7")]
    public void ExcelConditionalBranchesAreLazy(string formula,string primary,string expected)
    {
        var config=System.Text.Json.JsonSerializer.Serialize(new{enabled=true,concentrationFormula=formula});
        ChemicalFormulaCalculation.Calculate(config,primary,null,"[]",null,null,null).Raw.Should().Be(expected);
    }

    [Fact]
    public void ActualC5ZeroDoesNotEvaluateUnselectedDilutionDivision()
    {
        var config=System.Text.Json.JsonSerializer.Serialize(new{enabled=true,concentrationFormula="Primary*8.7*F!B3",
            adjustmentAmountFormula="IFERROR(IF(Concentration>USL,\"排液：\"&ROUND(1100*(1-Target/Concentration),0)&\" L\",IF(Concentration<LSL,\"DP333：\"&ROUND((Target-Concentration)*1100/1000,1)&\" L\",\"\")),\"\")"});
        var result=ChemicalFormulaCalculation.Calculate(config,"0",null,"[{\"CellAddress\":\"B3\",\"NumericValue\":0.99}]",80,120,100);
        result.Raw.Should().Be("0");result.AdjustmentAmount.Should().Be("DP333：110 L");
        Action missing=()=>ChemicalFormulaCalculation.Calculate(config,"0",null,"[]",80,120,100);
        missing.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Initial", "6.29046")]
    [InlineData("Recheck", "31.4523")]
    public void ExcelPhaseKeepsOriginalCoefficientAndFSnapshot(string phase, string expected)
    {
        const string config = "{\"enabled\":true,\"concentrationFormula\":\"Primary*0.6354*F!B6\",\"recheckConcentrationFormula\":\"Primary*3.177*F!B6\"}";
        const string cells = "[{\"CellAddress\":\"B6\",\"NumericValue\":0.99}]";
        ChemicalFormulaCalculation.Calculate(config,"10",null,cells,null,null,null,phase).Raw.Should().Be(expected);
    }

    [Fact]
    public void LegacyRecheckUsesInitialFormulaButInvalidPhaseIsRejected()
    {
        const string config = "{\"enabled\":true,\"concentrationFormula\":\"Primary*8.7*F!B3\"}";
        const string cells = "[{\"CellAddress\":\"B3\",\"NumericValue\":0.99}]";
        ChemicalFormulaCalculation.Calculate(config,"10",null,cells,null,null,null,"Recheck").Raw.Should().Be("86.13");
        Action act=()=>ChemicalFormulaCalculation.Calculate(config,"10",null,cells,null,null,null,"typo");
        act.Should().Throw<ArgumentException>();
        ChemicalFormulaCalculation.Calculate(config,"0",null,cells,null,null,null).Raw.Should().Be("0");
    }

    [Theory]
    [InlineData("2026-11-01T00:00:00+08:00", "2026-10-31T16:00:00Z")]
    [InlineData("2026-11-01T08:00:00Z", "2026-11-01T16:00:00+08:00")]
    public void OffsetDatesRepresentSameInstant(string a, string b) => ChemicalFormulaCalculation.ParseMeasuredAt(a).Should().Be(ChemicalFormulaCalculation.ParseMeasuredAt(b));

    [Theory]
    [InlineData("2026-11-01T00:00:00")]
    [InlineData("2026-02-30T00:00:00+08:00")]
    [InlineData("")]
    public void InvalidDatesAreRejected(string date)
    { Action act = () => ChemicalFormulaCalculation.ParseMeasuredAt(date); act.Should().Throw<ArgumentException>(); }

    [Theory]
    [InlineData("Primary*2", "1.23456789", "2.47", "2.47")]
    [InlineData("1/3", "1", "0.33333333", "0.333")]
    [InlineData("ROUND(Primary,0)", "2.5", "3", "3")]
    [InlineData("IF(Primary>2,10,20)", "3", "10", "10")]
    [InlineData("Primary", "-1.235", "-1.235", "-1.235")]
    public void PortalCompatiblePrecision(string formula, string primary, string raw, string stored)
    {
        var result = ChemicalFormulaCalculation.Calculate("{\"enabled\":true,\"concentrationFormula\":\"" + formula + "\",\"decimalPlaces\":2}", primary, null, "[]", null, null, null);
        result.Raw.Should().Be(raw); result.Stored.Should().Be(stored);
    }

    [Fact]
    public void FSnapshotAndMandatoryInputsAreValidated()
    {
        const string config = "{\"enabled\":true,\"concentrationFormula\":\"Primary*F!$B$3+Secondary\",\"decimalPlaces\":2}";
        const string cells = "[{\"CellAddress\":\"B3\",\"StandardSolution\":\"test\",\"NumericValue\":2}]";
        ChemicalFormulaCalculation.Calculate(config,"1.234","3",cells,null,null,null).Raw.Should().Be("5.468");
        Action act = () => ChemicalFormulaCalculation.Calculate(config,"1",null,cells,null,null,null);
        act.Should().Throw<ArgumentException>();
        act = () => ChemicalFormulaCalculation.Calculate(config,"1","3","[]",null,null,null);
        act.Should().Throw<ArgumentException>();
        act = () => ChemicalFormulaCalculation.Calculate(config,"NaN","3",cells,null,null,null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnknownIdentifierDoesNotBecomeZero()
    {
        Action act = () => ChemicalFormulaCalculation.Calculate("{\"enabled\":true,\"concentrationFormula\":\"Typo+Primary\"}","1",null,"[]",null,null,null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DateRangeIsHalfOpenAndOverlapRejected()
    {
        var ranges = new[] { new ChemicalSchedulePeriod(1,null,"2026-10-01","2026-10-31"), new ChemicalSchedulePeriod(2,null,"2026-11-01",null) };
        var converted = ChemicalFormulaCalculation.ValidatePeriods(ranges);
        converted[0].UntilUtc.Should().Be(converted[1].FromUtc);
        Action act = () => ChemicalFormulaCalculation.ValidatePeriods([ranges[0],new(2,null,"2026-10-31",null)]);
        act.Should().Throw<ArgumentException>();
    }
    [Theory]
    [InlineData("1.23456789012345678901234567891")]
    [InlineData("1,234")]
    [InlineData("1e2")]
    public void RawInputCannotBeSilentlyRoundedOrReformatted(string value)
    {
        Action act=()=>ChemicalFormulaCalculation.ReadDecimal(value,"Primary");act.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void ActualFHistoryAddressesAndNullableCellsAreSupported()
    {
        var result=ChemicalFormulaCalculation.Calculate("{\"enabled\":true,\"concentrationFormula\":\"Primary*F!B3\"}","1.234",null,
            "[{\"CellAddress\":\"F!B3\",\"NumericValue\":2},{\"CellAddress\":\"F!B4\",\"NumericValue\":null}]",null,null,null);
        result.Raw.Should().Be("2.468");
    }
}
