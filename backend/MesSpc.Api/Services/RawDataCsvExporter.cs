using System.Globalization;
using System.Reflection;
using System.Text;
using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services;

internal static class RawDataCsvExporter
{
    public static string Build(IEnumerable<object> rows, PartProcessCharacteristic mapping)
    {
        var sb = new StringBuilder();
        AppendCsvRow(sb,
            "PartProcessCharacteristicId",
            "Process",
            "Characteristic",
            "Machine",
            "MeasuredAt",
            "PortalDailyDate",
            "SamplingPhase",
            "SamplingStage",
            "Value",
            "SpecLSL",
            "SpecTarget",
            "SpecUSL",
            "Unit",
            "LotNo",
            "SerialNo",
            "Operator",
            "LineId",
            "TankId",
            "SlotId",
            "SideCode",
            "IsExcluded",
            "IsOutOfSpec",
            "IsOutOfControl",
            "VariableMeasurementId");

        foreach (var row in rows.Where(row => BelongsToMapping(row, mapping.Id)))
        {
            AppendCsvRow(sb,
                mapping.Id,
                mapping.Process?.ProcessName ?? mapping.Process?.ProcessCode ?? "",
                mapping.Characteristic?.CharacteristicName ?? mapping.Characteristic?.CharacteristicCode ?? "",
                mapping.Machine?.MachineName ?? mapping.Machine?.MachineCode ?? "",
                FormatDateTime(ReadProperty(row, "MeasuredAt")),
                FormatDateTime(ReadProperty(row, "PortalDailyDate")),
                ReadProperty(row, "SamplingPhase") ?? "",
                ReadProperty(row, "SamplingStage") ?? "",
                ReadProperty(row, "Value") ?? "",
                FormatNumber(mapping.LSL),
                FormatNumber(mapping.TargetValue),
                FormatNumber(mapping.USL),
                mapping.Unit ?? "",
                ReadProperty(row, "LotNo") ?? "",
                ReadProperty(row, "SerialNo") ?? "",
                ReadProperty(row, "Operator") ?? "",
                ReadProperty(row, "LineId") ?? "",
                ReadProperty(row, "TankId") ?? "",
                ReadProperty(row, "SlotId") ?? "",
                ReadProperty(row, "SideCode") ?? "",
                ReadProperty(row, "IsExcluded") ?? "",
                ReadProperty(row, "IsOutOfSpec") ?? "",
                ReadProperty(row, "IsOutOfControl") ?? "",
                ReadProperty(row, "VariableMeasurementId") ?? "");
        }

        return sb.ToString();
    }

    private static bool BelongsToMapping(object row, int mappingId)
    {
        var rowPpcId = ReadProperty(row, "PartProcessCharacteristicId");
        return string.IsNullOrWhiteSpace(rowPpcId) ||
               int.TryParse(rowPpcId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == mappingId;
    }

    private static string? ReadProperty(object source, string name)
    {
        var prop = source.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(source) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

    private static string FormatDateTime(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : value ?? "";

    private static string FormatNumber(double? value) =>
        value.HasValue ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : "";

    private static void AppendCsvRow(StringBuilder sb, params object?[] values)
    {
        sb.AppendLine(string.Join(",", values.Select(value =>
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        })));
    }
}
