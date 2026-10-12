using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public class RawDataCsvExporterTests
{
    [Fact]
    public void Build_ShouldKeepNumericValueLiteral_WhenValueLooksLikeExcelDateSerial()
    {
        var mapping = CreateMapping(2606);
        var rows = new object[]
        {
            new
            {
                PartProcessCharacteristicId = 2606,
                MeasuredAt = new DateTime(2026, 8, 14, 9, 10, 0),
                PortalDailyDate = (DateTime?)null,
                SamplingPhase = "GENERAL",
                SamplingStage = "GENERAL",
                Value = 7.92,
                Operator = "克里斯",
                VariableMeasurementId = 73553
            }
        };

        var csv = RawDataCsvExporter.Build(rows, mapping);

        csv.Should().Contain("\"7.92\"");
        csv.Should().Contain("\"2026-08-14 09:10:00\"");
        csv.Should().NotContain("0007-09-02");
        csv.Should().NotContain("2026-08-18");
    }

    [Fact]
    public void Build_ShouldFilterRowsForOtherPartProcessCharacteristics()
    {
        var mapping = CreateMapping(2606);
        var rows = new object[]
        {
            new { PartProcessCharacteristicId = 2606, MeasuredAt = new DateTime(2026, 8, 14, 9, 10, 0), Value = 7.92, VariableMeasurementId = 73553 },
            new { PartProcessCharacteristicId = 9999, MeasuredAt = new DateTime(2026, 8, 15, 9, 10, 0), Value = 99.99, VariableMeasurementId = 1 }
        };

        var csv = RawDataCsvExporter.Build(rows, mapping);

        csv.Should().Contain("\"2606\"");
        csv.Should().Contain("\"7.92\"");
        csv.Should().NotContain("\"9999\"");
        csv.Should().NotContain("\"99.99\"");
    }

    private static PartProcessCharacteristic CreateMapping(int id) => new()
    {
        Id = id,
        Process = new Process { ProcessName = "快蝕線(QE2)" },
        Characteristic = new QualityCharacteristic { CharacteristicName = "雙氧水" },
        Machine = new Machine { MachineName = "QE2" }
    };
}
