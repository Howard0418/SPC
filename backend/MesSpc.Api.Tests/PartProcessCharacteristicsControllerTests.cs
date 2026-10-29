using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Tests;

public class PartProcessCharacteristicsControllerTests
{
    [Fact]
    public void HasSameBusinessKey_ReturnsTrue_ForSameNullableProcessKey()
    {
        var left = new PartProcessCharacteristic
        {
            ControlScope = "PROCESS",
            ProcessId = 195,
            CharacteristicId = 871,
            Unit = " um "
        };
        var right = new PartProcessCharacteristic
        {
            ControlScope = "process",
            ProcessId = 195,
            CharacteristicId = 871,
            Unit = "um"
        };

        Assert.True(PartProcessCharacteristicsController.HasSameBusinessKey(left, right));
    }

    [Fact]
    public void HasSameBusinessKey_ReturnsFalse_WhenMachineDiffers()
    {
        var left = new PartProcessCharacteristic
        {
            ControlScope = "PROCESS",
            ProcessId = 195,
            CharacteristicId = 871,
            Unit = "um",
            MachineId = null
        };
        var right = new PartProcessCharacteristic
        {
            ControlScope = "PROCESS",
            ProcessId = 195,
            CharacteristicId = 871,
            Unit = "um",
            MachineId = 10
        };

        Assert.False(PartProcessCharacteristicsController.HasSameBusinessKey(left, right));
    }

    [Fact]
    public void HasFormulaConfigChanged_ReturnsTrue_WhenFormulaParametersChange()
    {
        Assert.True(PartProcessCharacteristicsController.HasFormulaConfigChanged(
            "{\"XbarCalculationMethod\":\"SYSTEM_STANDARD\",\"SigmaMultiplier\":3}",
            "{\"XbarCalculationMethod\":\"SYSTEM_STANDARD\",\"SigmaMultiplier\":2}"));
    }

    [Fact]
    public void HasFormulaConfigChanged_ReturnsFalse_ForEquivalentJsonFormatting()
    {
        Assert.False(PartProcessCharacteristicsController.HasFormulaConfigChanged(
            "{ \"SigmaMultiplier\": 3, \"XbarCalculationMethod\": \"SYSTEM_STANDARD\" }",
            "{\"SigmaMultiplier\":3,\"XbarCalculationMethod\":\"SYSTEM_STANDARD\"}"));
    }
}
