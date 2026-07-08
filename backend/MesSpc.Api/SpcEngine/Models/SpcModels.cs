namespace MesSpc.Api.SpcEngine.Models;

public record SpcDataPoint
{
    public DateTime MeasuredAt { get; init; }
    public double Value { get; init; }
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    public bool IsOutOfSpec { get; set; }
    public bool IsOutOfControl { get; set; }
    public List<string> ViolatedRules { get; init; } = new();
    public string? LotNo { get; init; }
    public string? SerialNo { get; init; }
    public string? Operator { get; init; }
    public int? LineId { get; init; }
    public int? TankId { get; init; }
    public int? SlotId { get; init; }
    public string? SideCode { get; init; }
    public bool IsExcluded { get; init; }
    public string? RootCause { get; init; }
    public string? CorrectiveAction { get; init; }
    public int? MeasurementBatchId { get; init; }
    public long? VariableMeasurementId { get; init; }
    public int? AlertId { get; init; }
    public string? AlertStatus { get; init; }
    public string? ResponsibleUser { get; init; }
    public double? RecheckValue { get; init; }
    public string? AdjustAction { get; init; }
    public double? AdjustAmount { get; init; }
}

public record Subgroup
{
    public DateTime MeasuredAt { get; init; }
    public List<double> Values { get; init; } = new();
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    
    public int N => Values.Count;
    public double Mean => Values.Count > 0 ? Values.Average() : 0;
    public double Range => Values.Count > 0 ? Values.Max() - Values.Min() : 0;
    public string? LotNo { get; init; }
    public string? SerialNo { get; init; }
    public string? Operator { get; init; }
    public int? LineId { get; init; }
    public int? TankId { get; init; }
    public int? SlotId { get; init; }
    public string? SideCode { get; init; }
    public bool IsExcluded { get; init; }
    public bool OutOfSpec { get; init; }
    public bool OutOfControl { get; init; }
    public string? RootCause { get; init; }
    public string? CorrectiveAction { get; init; }
    public int? MeasurementBatchId { get; init; }
    public long? VariableMeasurementId { get; init; }
    public int? AlertId { get; init; }
    public string? AlertStatus { get; init; }
    public string? ResponsibleUser { get; init; }
    public double? RecheckValue { get; init; }
    public string? AdjustAction { get; init; }
    public double? AdjustAmount { get; init; }
}

public record AttributeDataPoint
{
    public DateTime MeasuredAt { get; init; }
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    public int? InspectedQty { get; init; }
    public int? DefectQty { get; init; }
    public int? UnitCount { get; init; }
    public int? DefectCount { get; init; }
    
    public bool IsOutOfControl { get; set; }
    public List<string> ViolatedRules { get; init; } = new();
    public string? LotNo { get; init; }
    public string? Operator { get; init; }
    public int? LineId { get; init; }
    public int? TankId { get; init; }
    public int? SlotId { get; init; }
    public string? SideCode { get; init; }
    public bool IsExcluded { get; init; }
}
