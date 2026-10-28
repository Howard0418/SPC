using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.Services;

public sealed record OverallChart(int PpcId, ControlChartResult Chart);
public sealed record OverallSummary(int ControlItemCount, int DisabledItemCount, int OocItemCount,
    int OocCount, int OosItemCount, int OosCount, int AbnormalCount, int HandledCount, int UnhandledCount)
{
    public int RegisteredItemCount => ControlItemCount + DisabledItemCount;
    public int UnanalyzedItemCount { get; init; }
}

/// <summary>Counts chart positions once, merging primary/secondary signals and current alert workflow states.</summary>
public static class OverallStatistics
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed class Case(int ppcId)
    {
        public int PpcId { get; } = ppcId;
        public bool Ooc { get; set; }
        public bool Oos { get; set; }
        public HashSet<string> MeasurementKeys { get; } = [];
    }

    /// <summary>Aggregates current graph results. Alert identity is never inferred from an ambiguous master-data tuple.</summary>
    public static OverallSummary Calculate(int enabledCount, int disabledCount,
        IEnumerable<OverallChart> charts, IEnumerable<AlertEvent> alerts)
    {
        var latest = alerts.SelectMany(a => Keys(a).Select(k => (Key:k, Alert:a)))
            .GroupBy(x => x.Key).ToDictionary(g => g.Key,
                g => g.OrderByDescending(x => x.Alert.OccurredAt).ThenByDescending(x => x.Alert.Id).First().Alert);
        var cases = new Dictionary<string, Case>();
        foreach (var snapshot in charts)
        {
            var chart = snapshot.Chart;
            var raw = Elements(chart.RawDataPoints);
            var primary = ChartPoints(chart.ChartData);
            if (primary.Count == 0) primary = raw;
            var secondary = ChartPoints(chart.SecondaryChartData);
            var secondaryIndices = secondary.Where(p=>Number(p,"index").HasValue && !Flag(p,"isExcluded") && Ooc(p))
                .Select(p=>Number(p,"index")!.Value).ToHashSet();
            var rawByDate = raw.GroupBy(p=>Text(p,"measuredAt") ?? "").ToDictionary(g=>g.Key,g=>g.ToList());
            for (var i = 0; i < primary.Count; i++)
            {
                var point = primary[i];
                if (Flag(point,"isExcluded")) continue;
                var ooc = Ooc(point);
                if (chart.ChartType is "I_MR" or "I-MR")
                    ooc |= secondaryIndices.Contains(i+1);
                else if (i < secondary.Count && !Flag(secondary[i],"isExcluded"))
                    ooc |= Ooc(secondary[i]);
                var oos = Flag(point,"outOfSpec") || Flag(point,"isOutOfSpec");
                if (!ooc && !oos) continue;
                var keys = MeasurementKeys(point).ToList();
                // A subgroup is one chart position. Include every associated alert, not just the first sample's alert.
                if (chart.ChartType.StartsWith("XBAR",StringComparison.OrdinalIgnoreCase)
                    && rawByDate.TryGetValue(Text(point,"measuredAt") ?? "",out var members))
                    keys.AddRange(members.Where(p => !Flag(p,"isExcluded"))
                        .SelectMany(MeasurementKeys));
                var identity = MeasurementKeys(point).FirstOrDefault() ?? $"point:{i}";
                var key = $"{snapshot.PpcId}:{identity}";
                if (!cases.TryGetValue(key,out var item)) cases[key] = item = new(snapshot.PpcId);
                item.Ooc |= ooc; item.Oos |= oos;
                item.MeasurementKeys.UnionWith(keys);
            }
        }
        var abnormal = cases.Values.ToList();
        var handled = abnormal.Count(item => {
            var associated = item.MeasurementKeys.Where(latest.ContainsKey).Select(k => latest[k]).ToList();
            return associated.Count > 0 && associated.All(a => a.IsAcknowledged ||
                string.Equals(a.Status,"Closed",StringComparison.OrdinalIgnoreCase));
        });
        return new(enabledCount,disabledCount,
            abnormal.Where(x=>x.Ooc).Select(x=>x.PpcId).Distinct().Count(),abnormal.Count(x=>x.Ooc),
            abnormal.Where(x=>x.Oos).Select(x=>x.PpcId).Distinct().Count(),abnormal.Count(x=>x.Oos),
            abnormal.Count,handled,abnormal.Count-handled);
    }

    private static IEnumerable<string> Keys(AlertEvent a)
    {
        if(a.VariableMeasurementId.HasValue) yield return $"v:{a.VariableMeasurementId}";
        if(a.AttributeMeasurementId.HasValue) yield return $"a:{a.AttributeMeasurementId}";
    }
    private static IEnumerable<string> MeasurementKeys(JsonElement point)
    {
        if(Number(point,"variableMeasurementId") is long v) yield return $"v:{v}";
        if(Number(point,"attributeMeasurementId") is long a) yield return $"a:{a}";
    }
    private static List<JsonElement> Elements(object? value)
    {
        if(value is null) return [];
        var node=JsonSerializer.SerializeToElement(value,JsonOptions);
        return node.ValueKind==JsonValueKind.Array?node.EnumerateArray().ToList():[];
    }
    private static List<JsonElement> ChartPoints(object? data)
    {
        if(data is null) return [];
        var node=JsonSerializer.SerializeToElement(data,JsonOptions);
        return node.ValueKind==JsonValueKind.Object && node.TryGetProperty("points",out var points)
            && points.ValueKind==JsonValueKind.Array?points.EnumerateArray().ToList():[];
    }
    private static bool Flag(JsonElement p,string name) => p.TryGetProperty(name,out var v) && v.ValueKind==JsonValueKind.True;
    private static long? Number(JsonElement p,string name) => p.TryGetProperty(name,out var v) && v.TryGetInt64Safe(out var n)?n:null;
    private static string? Text(JsonElement p,string name) => p.TryGetProperty(name,out var v) && v.ValueKind==JsonValueKind.String?v.GetString():null;
    private static bool Ooc(JsonElement p) => Flag(p,"outOfControl") || Flag(p,"isOutOfControl") ||
        (p.TryGetProperty("violatedRules",out var rules) && rules.ValueKind==JsonValueKind.Array && rules.GetArrayLength()>0);
    private static bool TryGetInt64Safe(this JsonElement value,out long number)
    {
        number=0;
        return value.ValueKind==JsonValueKind.Number && value.TryGetInt64(out number);
    }
}
