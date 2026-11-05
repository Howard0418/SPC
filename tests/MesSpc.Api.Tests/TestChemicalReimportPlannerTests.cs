using TestChemicalReimport;

namespace MesSpc.Api.Tests;

public sealed class TestChemicalReimportPlannerTests
{
    [Fact]
    public void Canon_matches_excel_multiline_item_to_master_name()
        => Assert.Equal("氫氧化鉀", TestChemicalReimportPlanner.CanonItem("氫氧化鉀 (g/L)\nKOH"));

    [Fact]
    public void Canon_preserves_tank_qualifier()
        => Assert.NotEqual(TestChemicalReimportPlanner.Canon("蝕刻(銅)"), TestChemicalReimportPlanner.Canon("蝕刻(鎳)"));

    [Fact]
    public void Resolve_requires_exactly_one_enabled_business_mapping()
    {
        var source = new SourceIdentity("N2", "表處", "氫氧化鉀 (g/L)\nKOH", "OPEN", new(2026, 9, 1));
        var resolved = TestChemicalReimportPlanner.Resolve([source], [new(255, "N2", "表處", "氫氧化鉀", true)]);
        Assert.Equal(255, resolved[source]);
    }

    [Fact]
    public void Resolve_rejects_pre_september_source()
    {
        var source = new SourceIdentity("N2", "表處", "PH", "OPEN", new(2026, 8, 31));
        Assert.Throws<InvalidOperationException>(() => TestChemicalReimportPlanner.Resolve([source], [new(1, "N2", "表處", "PH", true)]));
    }

    [Fact]
    public void Resolve_rejects_ambiguous_mapping()
    {
        var source = new SourceIdentity("C5", "清潔", "硫酸", "GENERAL", new(2026, 10, 1));
        Assert.Throws<InvalidOperationException>(() => TestChemicalReimportPlanner.Resolve([source],
            [new(1, "C5", "清潔", "硫酸", true), new(2, "C5", "清潔", "硫酸", true)]));
    }

    [Theory]
    [InlineData("EARLY", "OPEN")]
    [InlineData("MIDDLE", "MIDDLE")]
    public void Phase_maps_only_approved_shifts(string source, string expected)
        => Assert.Equal(expected, TestChemicalReimportPlanner.Phase(source));

    [Fact]
    public void Phase_rejects_night_shift()
        => Assert.Throws<InvalidOperationException>(() => TestChemicalReimportPlanner.Phase("NIGHT"));

    [Theory]
    [InlineData("舊線", "PT1", true)]
    [InlineData("舊線", "PT2", false)]
    [InlineData("新線", "QE2", true)]
    [InlineData("新線", "QE1", false)]
    public void LineMatches_maps_legacy_sheet_groups_to_actual_lines(string sheet, string machine, bool expected)
        => Assert.Equal(expected, TestChemicalReimportPlanner.LineMatches(sheet, machine));

    [Fact]
    public void NormalizeUploadBatch_replaces_orphaned_legacy_link_with_controlled_batch()
    {
        var controlled = Guid.NewGuid();
        Assert.Equal(controlled, TestChemicalReimportPlanner.NormalizeUploadBatch(Guid.NewGuid(), controlled));
    }

    [Fact]
    public void NormalizeUploadBatch_rejects_empty_controlled_batch()
        => Assert.Throws<InvalidOperationException>(() => TestChemicalReimportPlanner.NormalizeUploadBatch(Guid.NewGuid(), Guid.Empty));
}
