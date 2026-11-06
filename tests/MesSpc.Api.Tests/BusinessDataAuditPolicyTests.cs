using MesSpc.Api.Controllers;
using MesSpc.Api.Services.Auditing;

namespace MesSpc.Api.Tests;

public sealed class BusinessDataAuditPolicyTests
{
    [Theory]
    [InlineData("Create", "POST", "measurement.record.create")]
    [InlineData("UpdateItem", "PUT", "measurement.record.update")]
    [InlineData("DeleteItem", "DELETE", "measurement.record.delete")]
    public void ManualMeasurementWritesUseFixedMeasurementCodes(string action, string method, string expected)
        => Assert.Equal(expected, BusinessDataAuditPolicy.Resolve(typeof(ManualMeasurementsV1Controller), action, method)!.EventCode);

    [Theory]
    [InlineData(typeof(ChemicalFormulaScheduleController), "Publish", "POST", "configuration.formula.update")]
    [InlineData(typeof(ChemicalFTableController), "Apply", "POST", "configuration.ftable.apply")]
    [InlineData(typeof(UploadsController), "Confirm", "POST", "import.batch.confirm")]
    [InlineData(typeof(EtchTestImportController), "Confirm", "POST", "import.batch.confirm")]
    [InlineData(typeof(PartProcessCharacteristicsController), "Delete", "DELETE", "masterdata.record.delete")]
    public void ApprovedHighRiskWritesAreMapped(Type controller, string action, string method, string expected)
        => Assert.Equal(expected, BusinessDataAuditPolicy.Resolve(controller, action, method)!.EventCode);

    [Fact]
    public void MasterSubresourceWriteIsAlsoCaptured()
        => Assert.Equal("masterdata.record.create", BusinessDataAuditPolicy.Resolve(typeof(MachinesController), "CreateTank", "POST")!.EventCode);

    [Fact]
    public void PreviewAndUnknownWritesAreNotSilentlyCaptured()
    {
        Assert.Null(BusinessDataAuditPolicy.Resolve(typeof(ChemicalFormulaScheduleController), "Preview", "POST"));
        Assert.Null(BusinessDataAuditPolicy.Resolve(typeof(UploadsController), "Calculate", "POST"));
    }
}
