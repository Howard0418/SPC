using System.Text.Json;
using MesSpc.Api.Services;
using Xunit;

namespace MesSpc.Api.Tests;

public sealed class ChemicalPortalRecheckInputTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" 	 ")]
    public void OptionalSecondValueMatchesOmittedPreview(string? submittedSecondary)
    {
        var initial = new ChemicalPreviewInput("2026-11-23T00:00:00+08:00", "18.75", "1.25", "PortalDaily|2454|20261123|OPEN|GENERAL", "Initial");
        var preview = initial with { Primary = "17.6", Secondary = null, Phase = "Recheck" };
        var confirmation = ChemicalPortalMeasurementService.CreateRecheckInput(initial, "17.6", submittedSecondary);
        Assert.Equal(JsonSerializer.Serialize(preview), JsonSerializer.Serialize(confirmation));
    }

    [Fact]
    public void NonEmptySecondValueRemainsPartOfTheProtectedInput()
    {
        var initial = new ChemicalPreviewInput("2026-11-23T00:00:00+08:00", "18.75", null, "key", "Initial");
        var confirmation = ChemicalPortalMeasurementService.CreateRecheckInput(initial, "17.60", "2.030");
        Assert.Equal("17.60", confirmation.Primary);
        Assert.Equal("2.030", confirmation.Secondary);
        Assert.NotEqual(confirmation, ChemicalPortalMeasurementService.CreateRecheckInput(initial, "17.60", "2.031"));
    }
}
