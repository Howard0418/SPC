namespace MesSpc.Api.Services;

/// <summary>
/// 3σ 管制圖常數（子組全距法），n = 子組大小 2～10。
/// 參考 Montgomery《Introduction to Statistical Quality Control》常用表。
/// </summary>
public static class SpcConstants
{
    private static readonly Dictionary<int, (double A2, double D3, double D4)> Table = new()
    {
        [2] = (1.880, 0.000, 3.267),
        [3] = (1.023, 0.000, 2.575),
        [4] = (0.729, 0.000, 2.282),
        [5] = (0.577, 0.000, 2.115),
        [6] = (0.483, 0.000, 2.004),
        [7] = (0.419, 0.076, 1.924),
        [8] = (0.373, 0.136, 1.864),
        [9] = (0.337, 0.184, 1.816),
        [10] = (0.308, 0.223, 1.777)
    };

    public static bool TryGetFactors(int subgroupSize, out double a2, out double d3, out double d4)
    {
        a2 = d3 = d4 = 0;
        if (subgroupSize < 2 || !Table.TryGetValue(subgroupSize, out var row)) return false;
        a2 = row.A2;
        d3 = row.D3;
        d4 = row.D4;
        return true;
    }
}
