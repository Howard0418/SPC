namespace MesSpc.Api.Services;

/// <summary>
/// 3σ 管制圖常數（子組全距法與製程能力估計），n = 子組大小 2～25。
/// 參考 Montgomery《Introduction to Statistical Quality Control》常用表。
/// </summary>
public static class SpcConstants
{
    private static readonly Dictionary<int, (double A2, double D3, double D4, double d2, double c4)> Table = new()
    {
        [2] = (1.880, 0.000, 3.267, 1.128, 0.7979),
        [3] = (1.023, 0.000, 2.575, 1.693, 0.8862),
        [4] = (0.729, 0.000, 2.282, 2.059, 0.9213),
        [5] = (0.577, 0.000, 2.115, 2.326, 0.9400),
        [6] = (0.483, 0.000, 2.004, 2.534, 0.9515),
        [7] = (0.419, 0.076, 1.924, 2.704, 0.9594),
        [8] = (0.373, 0.136, 1.864, 2.847, 0.9650),
        [9] = (0.337, 0.184, 1.816, 2.970, 0.9693),
        [10] = (0.308, 0.223, 1.777, 3.078, 0.9727),
        [11] = (0.285, 0.256, 1.744, 3.173, 0.9754),
        [12] = (0.266, 0.283, 1.717, 3.258, 0.9776),
        [13] = (0.249, 0.307, 1.693, 3.336, 0.9794),
        [14] = (0.235, 0.328, 1.672, 3.407, 0.9810),
        [15] = (0.223, 0.347, 1.653, 3.472, 0.9823),
        [16] = (0.212, 0.363, 1.637, 3.532, 0.9835),
        [17] = (0.203, 0.378, 1.622, 3.588, 0.9845),
        [18] = (0.194, 0.391, 1.609, 3.640, 0.9854),
        [19] = (0.187, 0.403, 1.597, 3.689, 0.9862),
        [20] = (0.180, 0.415, 1.585, 3.735, 0.9869),
        [21] = (0.173, 0.425, 1.575, 3.778, 0.9876),
        [22] = (0.167, 0.434, 1.566, 3.819, 0.9882),
        [23] = (0.162, 0.443, 1.557, 3.858, 0.9887),
        [24] = (0.157, 0.451, 1.549, 3.895, 0.9892),
        [25] = (0.153, 0.459, 1.541, 3.931, 0.9896)
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

    public static bool TryGetCapabilityFactors(int subgroupSize, out double d2, out double c4)
    {
        d2 = c4 = 0;
        if (subgroupSize < 2 || !Table.TryGetValue(subgroupSize, out var row)) return false;
        d2 = row.d2;
        c4 = row.c4;
        return true;
    }

    public static bool TryGetXbarSFactors(int subgroupSize, out double a3, out double b3, out double b4)
    {
        a3 = b3 = b4 = 0;
        if (subgroupSize < 2 || !Table.TryGetValue(subgroupSize, out var row) || row.c4 <= 0) return false;

        var c4 = row.c4;
        var factor = 3 * Math.Sqrt(1 - c4 * c4) / c4;
        a3 = 3 / (c4 * Math.Sqrt(subgroupSize));
        b3 = Math.Max(0, 1 - factor);
        b4 = 1 + factor;
        return true;
    }
}
