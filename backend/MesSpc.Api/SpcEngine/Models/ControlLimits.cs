namespace MesSpc.Api.SpcEngine.Models;

public record ControlLimits
{
    public double? UCL { get; init; }
    public double? CL { get; init; }
    public double? LCL { get; init; }
    
    // Specifications
    public double? USL { get; init; }
    public double? LSL { get; init; }
    public double? Target { get; init; }
}

public record CapabilityResult
{
    public double? Cp { get; init; }
    public double? Cpk { get; init; }
    public double? Pp { get; init; }
    public double? Ppk { get; init; }
    public double? SigmaWithin { get; init; }
    public double? SigmaOverall { get; init; }
}

public record ControlChartResult
{
    public string ChartType { get; init; } = string.Empty;
    public ControlLimits Limits { get; init; } = new();
    public object? StatControlLimits { get; init; }
    public object ChartData { get; init; } = new();
    public object? SecondaryChartData { get; init; }
    public int SubgroupSize { get; init; }
    public string? SubgroupSizeNote { get; init; }
    public CapabilityResult? Capability { get; init; }
    public object? RawDataPoints { get; init; }
}
