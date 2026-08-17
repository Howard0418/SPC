namespace MesSpc.Api.Services;

public static class TankNameMatcher
{
    public const double AutoMatchThreshold = 0.90;
    public const double SuggestionThreshold = 0.70;
    public const double MinimumWinnerMargin = 0.08;

    public static string Normalize(string? value)
    {
        var normalized = string.Concat((value ?? "").Trim().ToUpperInvariant().Where(char.IsLetterOrDigit))
            .Replace("TANK", "", StringComparison.OrdinalIgnoreCase)
            .Replace("槽體", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        if (normalized.EndsWith('槽')) normalized = normalized[..^1];
        return normalized == "表處" ? "表面處理" : normalized;
    }

    public static double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1;
        var distance = LevenshteinDistance(a, b);
        return 1d - (double)distance / Math.Max(a.Length, b.Length);
    }

    public static IReadOnlyList<(T Item, double Score)> Rank<T>(
        string? input,
        IEnumerable<T> candidates,
        Func<T, string?> nameSelector,
        Func<T, string?>? codeSelector = null)
        => candidates
            .Select(item => (
                Item: item,
                Score: Math.Max(
                    Similarity(input, nameSelector(item)),
                    codeSelector is null ? 0 : Similarity(input, codeSelector(item)))))
            .OrderByDescending(x => x.Score)
            .ToList();

    public static T? SelectUniqueAutoMatch<T>(IReadOnlyList<(T Item, double Score)> ranked)
    {
        if (ranked.Count == 0 || ranked[0].Score < AutoMatchThreshold) return default;
        if (ranked.Count > 1 && ranked[0].Score - ranked[1].Score < MinimumWinnerMargin) return default;
        return ranked[0].Item;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        var current = new int[b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
