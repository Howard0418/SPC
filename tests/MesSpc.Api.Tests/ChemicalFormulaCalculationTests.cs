using FluentAssertions;
using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public class ChemicalFormulaCalculationTests
{
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
    [InlineData("Primary*2", "1.23456789", "2.46913578", "2.47")]
    [InlineData("1/3", "1", "0.33333333", "0.33")]
    [InlineData("ROUND(Primary,0)", "2.5", "3", "3")]
    [InlineData("IF(Primary>2,10,20)", "3", "10", "10")]
    [InlineData("Primary", "-1.235", "-1.235", "-1.24")]
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
