namespace MesSpc.Api.Services.Localization;

/// <summary>Explicit index into a preserved legacy message array; the descriptor is display-only.</summary>
public sealed record IndexedApiUiMessage(int Index, ApiUiError Descriptor);

/// <summary>Tracks only deliberately annotated messages and rejects stale array entries.</summary>
public sealed class IndexedApiUiMessages
{
    private readonly Dictionary<int, (string Original, ApiUiError Descriptor)> messages = new();

    /// <summary>Appends the original text exactly once and captures an immutable safe descriptor.</summary>
    public void Add(List<string> target, string original, string key, IReadOnlyDictionary<string,string>? args = null)
    {
        var descriptor = ApiUiError.Create("IMPORT_VALIDATION", key, args ?? ApiUiMessageCatalog.EmptyArguments) with { Message = original };
        Add(target, original, descriptor);
    }

    /// <summary>Appends legacy evidence while allowing a deliberately sanitized public display descriptor.</summary>
    public void Add(List<string> target, string original, ApiUiError descriptor)
    {
        messages[target.Count] = (original, descriptor);
        target.Add(original);
    }

    /// <summary>Returns only indexes whose captured original text is still present; no key is inferred from text.</summary>
    public IReadOnlyList<IndexedApiUiMessage> Snapshot(IReadOnlyList<string> source) => messages
        .Where(pair => pair.Key < source.Count && string.Equals(pair.Value.Original, source[pair.Key], StringComparison.Ordinal))
        .OrderBy(pair => pair.Key).Select(pair => new IndexedApiUiMessage(pair.Key, pair.Value.Descriptor)).ToArray();
}
