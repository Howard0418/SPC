using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MesSpc.Api.Services.Localization;

/// <summary>Immutable, shared API display resources. This class never changes parsing culture.</summary>
public sealed class ApiUiMessageCatalog
{
    /// <summary>Explicit UI display preference header; never an identity or authorization input.</summary>
    public const string LocaleHeader = "X-SPC-UI-Locale";
    private static readonly string[] Locales = ["zh-TW", "zh-CN", "en-US"];
    private static readonly Regex Parameter = new(@"\{([a-z][a-zA-Z0-9]*)\}", RegexOptions.CultureInvariant);
    private static readonly Regex Segment = new(@"^[a-z][a-zA-Z0-9]*$", RegexOptions.CultureInvariant);
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> messages;
    /// <summary>Validated immutable resources shared across concurrent requests.</summary>
    public static ApiUiMessageCatalog Shared { get; } = new();

    /// <summary>Loads the packaged single-source resources, without configuration or network access.</summary>
    public ApiUiMessageCatalog() : this(ReadEmbeddedResources()) { }

    private ApiUiMessageCatalog(IReadOnlyDictionary<string, string> resources)
    {
        if (resources.Count != Locales.Length || Locales.Any(locale => !resources.ContainsKey(locale)))
            throw new InvalidOperationException("API resources must contain all canonical locales.");
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var locale in Locales)
        {
            using var document = JsonDocument.Parse(resources[locale], new JsonDocumentOptions { MaxDepth = 32 });
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            Flatten(document.RootElement, "", entries);
            if (entries.Keys.Any(key => !key.StartsWith("spc.api.", StringComparison.Ordinal)))
                throw new InvalidOperationException("Only SPC API display resources are allowed.");
            result.Add(locale, new ReadOnlyDictionary<string, string>(entries));
        }
        var source = result["zh-TW"];
        foreach (var target in result.Values)
        {
            if (!source.Keys.Order(StringComparer.Ordinal).SequenceEqual(target.Keys.Order(StringComparer.Ordinal)))
                throw new InvalidOperationException("API resource key mismatch.");
            foreach (var key in source.Keys)
                if (!Parameters(source[key]).SequenceEqual(Parameters(target[key])))
                    throw new InvalidOperationException("API resource parameter mismatch.");
        }
        messages = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(result);
    }

    /// <summary>Creates a validated catalog from JSON modules, used by resource contract tests.</summary>
    public static ApiUiMessageCatalog FromJson(IReadOnlyDictionary<string, string> resources) => new(resources);

    /// <summary>Accepts only explicit canonical values; browser aliases never override the default.</summary>
    public static string NormalizeLocale(string? locale) => locale is "zh-TW" or "zh-CN" or "en-US" ? locale : "zh-TW";

    /// <summary>Reads the request display header without touching CurrentCulture or Accept-Language.</summary>
    public static string RequestLocale(HttpRequest request) => NormalizeLocale(request.Headers[LocaleHeader].ToString());

    /// <summary>Interpolates preformatted plain-text arguments once, with an exact parameter contract.</summary>
    public string Format(string key, string? locale = null, IReadOnlyDictionary<string, string>? args = null)
    {
        if (!messages["zh-TW"].TryGetValue(key, out var source)) throw new KeyNotFoundException("Unknown API message key.");
        var template = messages[NormalizeLocale(locale)].GetValueOrDefault(key, source);
        var required = Parameter.Matches(template).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        args ??= EmptyArguments;
        if (args.Count != required.Count || required.Any(name => !args.ContainsKey(name) || args[name] is null))
            throw new ArgumentException("API message parameters do not match.", nameof(args));
        return Parameter.Replace(template, match => args[match.Groups[1].Value]);
    }

    /// <summary>Immutable arguments for messages without substitutions.</summary>
    public static IReadOnlyDictionary<string, string> EmptyArguments { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    private static IEnumerable<string> Parameters(string value) => Parameter.Matches(value)
        .Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal);

    private static void Flatten(JsonElement node, string prefix, Dictionary<string, string> output)
    {
        if (node.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Invalid API resource object.");
        var properties = node.EnumerateObject().ToArray();
        if (properties.Length == 0) throw new InvalidOperationException("Empty API resource object.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (!names.Add(property.Name) || !Segment.IsMatch(property.Name)
                || property.Name is "constructor" or "prototype" or "__proto__")
                throw new InvalidOperationException("Invalid or duplicate API resource key.");
            var key = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            if (property.Value.ValueKind == JsonValueKind.Object) { Flatten(property.Value, key, output); continue; }
            if (property.Value.ValueKind != JsonValueKind.String) throw new InvalidOperationException("Invalid API resource leaf.");
            var value = property.Value.GetString()!;
            var remainder = Parameter.Replace(value, "");
            if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(['<', '>', '@', '|']) >= 0 || remainder.IndexOfAny(['{', '}']) >= 0)
                throw new InvalidOperationException("Nonportable API resource message.");
            if (!output.TryAdd(key, value)) throw new InvalidOperationException("Duplicate API resource leaf.");
        }
    }

    private static IReadOnlyDictionary<string, string> ReadEmbeddedResources()
    {
        var resources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var locale in Locales)
        {
            // Embedded assembly bytes only: never read user files, settings or remote dictionaries.
            using var stream = typeof(ApiUiMessageCatalog).Assembly.GetManifestResourceStream($"MesSpc.Api.Localization.{locale}.json")
                ?? throw new InvalidOperationException("Missing packaged API resources.");
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            resources.Add(locale, reader.ReadToEnd());
        }
        return resources;
    }
}
