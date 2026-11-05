using FluentAssertions;
using OctoberProductionChemicalCorrection;

namespace MesSpc.Api.Tests;

public sealed class OctoberProductionChemicalCorrectionPlannerTests
{
    private static CorrectionSource Source(double value = 12.345, double? recheck = null, string id = "source-a") =>
        new(id, 2425, new DateOnly(2026, 10, 1), "OPEN", "GENERAL", value, recheck, "file.xlsx", "C5", 7);

    private static CorrectionCurrent Current(double value = 10, double? recheck = null, string? sourceHash = null, long id = 99) =>
        new(id, 2425, new DateOnly(2026, 10, 1), "OPEN", "GENERAL", value, recheck, sourceHash, "row-version-a");

    [Fact]
    public void Build_PairsOneNaturalKeyAndPlansExistingIdWithoutInsert()
    {
        var result = ChemicalCorrectionPlanner.Build([Source()], [Current()]);

        result.Corrections.Should().ContainSingle();
        result.Corrections[0].MeasurementId.Should().Be(99);
        result.Corrections[0].AfterInitial.Should().Be(12.345);
        result.AlreadyCorrect.Should().BeEmpty();
    }

    [Fact]
    public void Build_TreatsSameInitialAndRecheckAsIdempotent()
    {
        var result = ChemicalCorrectionPlanner.Build([Source(12.345, 12.5)], [Current(12.345, 12.5)]);

        result.Corrections.Should().BeEmpty();
        result.AlreadyCorrect.Should().ContainSingle(x => x.MeasurementId == 99);
    }

    [Fact]
    public void Build_ClearsFormalRecheckWhenExcelHasNone()
    {
        var result = ChemicalCorrectionPlanner.Build([Source()], [Current(12.345, 11.73)]);

        result.Corrections.Should().ContainSingle();
        result.Corrections[0].BeforeRecheck.Should().Be(11.73);
        result.Corrections[0].AfterRecheck.Should().BeNull();
    }

    [Fact]
    public void Build_RejectsMissingOrDuplicateFormalNaturalKey()
    {
        Action missing = () => ChemicalCorrectionPlanner.Build([Source()], []);
        Action duplicate = () => ChemicalCorrectionPlanner.Build([Source()], [Current(id: 1), Current(id: 2)]);

        missing.Should().Throw<InvalidOperationException>().WithMessage("*exactly one*");
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*exactly one*");
    }

    [Fact]
    public void Build_RejectsExistingImportedSourceHash()
    {
        Action act = () => ChemicalCorrectionPlanner.Build([Source()], [Current(sourceHash: "already-owned")]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*source key*");
    }

    [Fact]
    public void Build_RejectsDuplicateSourceNaturalKey()
    {
        Action act = () => ChemicalCorrectionPlanner.Build([Source(id: "a"), Source(id: "b")], [Current()]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate source natural key*");
    }
}
