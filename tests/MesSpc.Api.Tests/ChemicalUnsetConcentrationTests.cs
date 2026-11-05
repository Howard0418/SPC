using ChemicalFormulaMigration;
using System.Text.Json.Nodes;

namespace MesSpc.Api.Tests;

public sealed class ChemicalUnsetConcentrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \r\n ")]
    [InlineData("直接量測值")]
    public void MissingSource_DoesNotInventFormula(string? source) =>
        Assert.Equal("", ConcentrationSourcePolicy.Convert(source, 0m, 1m, 0m));

    [Fact]
    public void ExplicitIdentitySource_IsStillAFormula() =>
        Assert.Equal("ROUND((0)+(1*Primary)+(0*Secondary),8)", ConcentrationSourcePolicy.Convert("H8", 0m, 1m, 0m));

    [Fact]
    public void RealSource_PreservesConvertedCoefficients() =>
        Assert.Equal("ROUND((0)+(3.177*Primary)+(0*Secondary),8)", ConcentrationSourcePolicy.Convert("3.177*H8", 0m, 3.177m, 0m));

    [Fact]
    public void Cleanup_OnlyClearsProvenGeneratedFormulas_AndIsIdempotent()
    {
        const string json = """{"enabled":true,"migratedFrom":"C5:12","concentrationFormula":"ROUND((0)+(1*Primary)+(0*Secondary),8)","adjustmentFormula":"KEEP","decimalPlaces":2} """;
        var rules = new[] { new SourceFormulaRule("C5:12", "直接量測值") };
        var change = Assert.Single(UnsetConcentrationPlanner.Build(rules, [new(1, json)]));
        var result = JsonNode.Parse(change.AfterJson)!;
        Assert.False(result["enabled"]!.GetValue<bool>());
        Assert.Equal("", result["concentrationFormula"]!.GetValue<string>());
        Assert.Equal("KEEP", result["adjustmentFormula"]!.GetValue<string>());
        Assert.Equal(2, result["decimalPlaces"]!.GetValue<int>());
        Assert.Empty(UnsetConcentrationPlanner.Build(rules, [new(1, change.AfterJson)]));
    }

    [Theory]
    [InlineData("{}", "直接量測值")]
    [InlineData("""{"migratedFrom":"x","concentrationFormula":"Primary*2"}""", "直接量測值")]
    [InlineData("""{"migratedFrom":"x","concentrationFormula":"ROUND((0)+(1*Primary)+(0*Secondary),8)"}""", "H8")]
    public void Cleanup_PreservesActualOrUnprovenFormula(string json, string source) =>
        Assert.Empty(UnsetConcentrationPlanner.Build([new("x", source)], [new(1, json)]));

    [Fact]
    public void Cleanup_RejectsDifferentRecheckFormula() => Assert.Throws<InvalidOperationException>(() =>
        UnsetConcentrationPlanner.Build([new("x", "直接量測值")], [new(1, """{"migratedFrom":"x","concentrationFormula":"ROUND((0)+(1*Primary)+(0*Secondary),8)","recheckConcentrationFormula":"Primary*2"}""")]));
}
