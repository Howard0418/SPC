using ChemicalFormulaMigration;
using System.Text.Json.Nodes;

namespace MesSpc.Api.Tests;

public sealed class ChemicalUnsetConcentrationTests
{
    [Theory]
    [InlineData(2515, "U13", "IF(Concentration=\"\",\"\",IF(Concentration<31,\"添加\",IF(Concentration>U13,\"稀釋\",\"\")))", "IF(Concentration=\"\",\"\",IF(Concentration<31,\"添加\",\"\"))")]
    [InlineData(2516, "U14", "IF(Concentration=\"\",\"\",IF(Concentration<5,\"添加\",IF(Concentration>U14,\"稀釋\",\"\")))", "IF(Concentration=\"\",\"\",IF(Concentration<5,\"添加\",\"\"))")]
    public void NaUpperLimit_RemovesOnlyUnsupportedUpperBranch(int ppcId, string token, string formula, string expected)
    {
        var json = new JsonObject
        {
            ["enabled"] = true,
            ["concentrationFormula"] = "Primary*2",
            ["adjustmentFormula"] = formula,
            ["adjustmentAmountFormula"] = $"IFERROR(IF(Concentration<1,\"ADD\",IF(Concentration>{token},\"DILUTE\",\"\")),\"\")",
            ["decimalPlaces"] = 2
        }.ToJsonString();
        var change = Assert.Single(NaAdjustmentPlanner.Build([new(ppcId, token, json)]));
        var result = JsonNode.Parse(change.AfterJson)!;
        Assert.Equal(expected, result["adjustmentFormula"]!.GetValue<string>());
        Assert.DoesNotContain(token, result["adjustmentAmountFormula"]!.GetValue<string>());
        Assert.Contains("ADD", result["adjustmentAmountFormula"]!.GetValue<string>());
        Assert.Equal("Primary*2", result["concentrationFormula"]!.GetValue<string>());
        Assert.Empty(NaAdjustmentPlanner.Build([new(ppcId, token, change.AfterJson)]));
    }

    [Fact]
    public void NaOnlyTankChange_BecomesUnset()
    {
        const string json = """{"enabled":true,"concentrationFormula":"Primary*2","adjustmentFormula":"IF(Concentration=\"\",\"\",IF(Concentration>U16,\"更槽\",\"\"))","adjustmentAmountFormula":"","decimalPlaces":2}""";
        var change = Assert.Single(NaAdjustmentPlanner.Build([new(2518, "U16", json)]));
        var result = JsonNode.Parse(change.AfterJson)!;
        Assert.Equal("", result["adjustmentFormula"]!.GetValue<string>());
        Assert.Equal("", result["adjustmentAmountFormula"]!.GetValue<string>());
    }

    [Fact]
    public void NaAdjustment_RejectsUnknownPpcOrUnexpectedFormula()
    {
        const string json = """{"adjustmentFormula":"Concentration>U13","adjustmentAmountFormula":""}""";
        Assert.Throws<InvalidOperationException>(() => NaAdjustmentPlanner.Build([new(9999, "U13", json)]));
        Assert.Throws<InvalidOperationException>(() => NaAdjustmentPlanner.Build([new(2515, "U13", json)]));
    }

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

    [Theory]
    [InlineData("production", "172.16.110.16", "PMR_SPC_2026", true, false)]
    [InlineData("test", "172.16.110.16", "PMR_SPC_2026", true, true)]
    [InlineData("production", "other", "PMR_SPC_2026", true, true)]
    public void ProductionTarget_RejectsUnreviewedOrWrongEnvironment(string env, string server, string database, bool apply, bool reviewed) =>
        Assert.Throws<InvalidOperationException>(() => UnsetProductionGuard.ValidateTarget(env, server, database, apply, reviewed));

    [Fact]
    public void ProductionTarget_AllowsApprovedTarget() =>
        UnsetProductionGuard.ValidateTarget("production", "172.16.110.16", "PMR_SPC_2026", true, true);

    [Fact]
    public void ReviewedRows_AllowBeforeAndAfter_ButRejectDrift()
    {
        var review = new[] { new UnsetFormulaChange(1, "source", "before", "after") };
        UnsetProductionGuard.ValidateRows(review, [new(1, "before")]);
        UnsetProductionGuard.ValidateRows(review, [new(1, "after")]);
        Assert.Throws<InvalidOperationException>(() => UnsetProductionGuard.ValidateRows(review, [new(1, "changed")]));
        Assert.Throws<InvalidOperationException>(() => UnsetProductionGuard.ValidateRows(review, []));
    }
}
