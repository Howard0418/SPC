using System.Text.Json.Nodes;
using MesSpc.Api.Services;
using N2CapacityAlignment;

namespace MesSpc.Api.Tests;

public sealed class N2CapacityAlignmentPlanTests
{
    [Fact]
    public void Definitions_AreTheSevenApprovedN2CapacityCorrections()
    {
        Assert.Equal(new[] { 2512, 2514, 2515, 2516, 2519, 2550, 2594 }, AlignmentPlan.Definitions.Select(x => x.PpcId));
        Assert.All(AlignmentPlan.Definitions, definition => Assert.Equal("N2", definition.Machine));
    }

    [Theory]
    [MemberData(nameof(CurrentConfigs))]
    public void AlignConfig_ChangesOnlyReviewedCapacityTokens(int ppcId, string configJson)
    {
        var definition = AlignmentPlan.Definitions.Single(x => x.PpcId == ppcId);

        var result = AlignmentPlan.AlignConfig(configJson, definition);

        Assert.True(result.Changed);
        Assert.Equal(definition.AfterFormulaHash, AlignmentPlan.Hash(result.AmountFormula));
        var before = JsonNode.Parse(configJson)!.AsObject();
        var after = JsonNode.Parse(result.ConfigJson)!.AsObject();
        after["adjustmentAmountFormula"] = before["adjustmentAmountFormula"]?.DeepClone();
        Assert.True(JsonNode.DeepEquals(before, after));
        Assert.False(AlignmentPlan.AlignConfig(result.ConfigJson, definition).Changed);
    }

    [Theory]
    [MemberData(nameof(CurrentConfigs))]
    public void CandidateFormula_CalculatesWithApprovedN2Capacity(int ppcId, string configJson)
    {
        var definition = AlignmentPlan.Definitions.Single(x => x.PpcId == ppcId);
        var aligned = AlignmentPlan.AlignConfig(configJson, definition);
        var root = JsonNode.Parse(aligned.ConfigJson)!.AsObject();
        root["enabled"] = true;
        root["concentrationFormula"] = "Primary";

        var result = ChemicalFormulaCalculation.Calculate(root.ToJsonString(), definition.SampleConcentration, null, "[]", null, null, null);

        Assert.Equal(definition.ExpectedAdjustmentAmount, result.AdjustmentAmount);
    }

    public static IEnumerable<object[]> CurrentConfigs()
    {
        foreach (var definition in AlignmentPlan.Definitions)
        {
            var formula = definition.SampleBeforeFormula;
            yield return new object[]
            {
                definition.PpcId,
                $$"""{"enabled":true,"decimalPlaces":3,"concentrationFormula":"Primary","adjustmentFormula":"","adjustmentAmountFormula":{{System.Text.Json.JsonSerializer.Serialize(formula)}},"tankCapacityLiters":{{definition.Capacity}}}"""
            };
        }
    }
}
