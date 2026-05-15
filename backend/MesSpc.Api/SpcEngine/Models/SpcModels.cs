namespace MesSpc.Api.SpcEngine.Models;

public record SpcDataPoint
{
    public DateTime MeasuredAt { get; init; }
    public double Value { get; init; }
    public bool IsOutOfSpec { get; set; }
    public bool IsOutOfControl { get; set; }
    public List<string> ViolatedRules { get; init; } = new();
}

public record Subgroup
{
    public DateTime MeasuredAt { get; init; }
    public List<double> Values { get; init; } = new();
    
    public int N => Values.Count;
    public double Mean => Values.Count > 0 ? Values.Average() : 0;
    public double Range => Values.Count > 0 ? Values.Max() - Values.Min() : 0;
}
