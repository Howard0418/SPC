using System.Text.Json;
using FluentAssertions;
using ProductionChemicalMasterFormulaSync;

namespace MesSpc.Api.Tests;

public sealed class ProductionChemicalMasterFormulaSyncPlannerTests
{
    [Fact]
    public void Build_ReplacesOnlyInitialAndRecheckFormula()
    {
        var source = Row(1, "M|T|C|ml/L", """{"concentrationFormula":"Primary*F!$B$3","recheckConcentrationFormula":"Primary*2*F!$B$3","adjustmentFormula":"TEST","decimalPlaces":5}""");
        var target = Row(9, "M|T|C|ml/L", """{"concentrationFormula":"Primary*.99","adjustmentFormula":"PRODUCTION","decimalPlaces":2,"sourceRow":7}""");

        var change = ProductionChemicalMasterFormulaSyncPlanner.Build([source], [target]).Changes.Single();
        using var merged = JsonDocument.Parse(change.MergedConfigJson);
        merged.RootElement.GetProperty("concentrationFormula").GetString().Should().Be("Primary*F!$B$3");
        merged.RootElement.GetProperty("recheckConcentrationFormula").GetString().Should().Be("Primary*2*F!$B$3");
        merged.RootElement.GetProperty("adjustmentFormula").GetString().Should().Be("PRODUCTION");
        merged.RootElement.GetProperty("decimalPlaces").GetInt32().Should().Be(2);
        merged.RootElement.GetProperty("sourceRow").GetInt32().Should().Be(7);
    }

    [Fact]
    public void Build_RemovesRecheckWhenTestDoesNotHaveOne()
    {
        var plan = ProductionChemicalMasterFormulaSyncPlanner.Build(
            [Row(1, "K", """{"concentrationFormula":"Primary"}""")],
            [Row(2, "K", """{"concentrationFormula":"Old","recheckConcentrationFormula":"Old2","x":1}""")]);

        using var merged = JsonDocument.Parse(plan.Changes.Single().MergedConfigJson);
        merged.RootElement.TryGetProperty("recheckConcentrationFormula", out _).Should().BeFalse();
        merged.RootElement.GetProperty("x").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Build_IdenticalFormulaIsSkippedWithoutReformatRequirement()
    {
        var plan = ProductionChemicalMasterFormulaSyncPlanner.Build(
            [Row(1, "K", """{"concentrationFormula":"Primary*F!$B$3","x":999}""")],
            [Row(2, "K", """{"x":1,"concentrationFormula":"Primary*F!$B$3"}""")]);

        plan.Changes.Should().BeEmpty();
        plan.Unchanged.Should().ContainSingle();
    }

    [Fact]
    public void Build_RejectsDuplicateOrMismatchedNaturalKeys()
    {
        var row = Row(1, "K", """{"concentrationFormula":"Primary"}""");
        Action duplicate = () => ProductionChemicalMasterFormulaSyncPlanner.Build([row, row with { PpcId = 2 }], [row]);
        Action mismatch = () => ProductionChemicalMasterFormulaSyncPlanner.Build([row], [row with { NaturalKey = "OTHER" }]);

        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*duplicate*");
        mismatch.Should().Throw<InvalidOperationException>().WithMessage("*key set*");
    }

    [Fact]
    public void Build_RejectsNonChemicalDisabledOrInvalidJson()
    {
        var good = Row(1, "K", """{"concentrationFormula":"Primary"}""");
        Action nonChemical = () => ProductionChemicalMasterFormulaSyncPlanner.Build([good with { ControlScope = "PROCESS" }], [good]);
        Action disabled = () => ProductionChemicalMasterFormulaSyncPlanner.Build([good], [good with { IsEnabled = false }]);
        Action invalid = () => ProductionChemicalMasterFormulaSyncPlanner.Build([good with { ConfigJson = "{" }], [good]);

        nonChemical.Should().Throw<InvalidOperationException>();
        disabled.Should().Throw<InvalidOperationException>();
        invalid.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("test", "172.16.110.16", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.27", "PMR_SPC_2026", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_TEST", "production")]
    [InlineData("production", "172.16.110.16", "PMR_SPC_2026", "test")]
    public void ValidateProductionTarget_RejectsWrongTarget(string app, string server, string database, string api)
    {
        Action act = () => ProductionChemicalMasterFormulaSyncPlanner.ValidateProductionTarget(app, server, database, api);
        act.Should().Throw<InvalidOperationException>();
    }

    private static ChemicalMasterFormulaRow Row(int id, string key, string config) =>
        new(id, key, true, "CHEM", config);
}
