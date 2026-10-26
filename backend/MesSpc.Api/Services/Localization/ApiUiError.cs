namespace MesSpc.Api.Services.Localization;

/// <summary>Safe additive descriptor for a known API failure; retains the legacy Traditional Chinese message.</summary>
public sealed record ApiUiError(string Message, string Code, string MessageKey, IReadOnlyDictionary<string,string> Args)
{
    /// <summary>Explicit safe child messages; never inner exceptions or executable templates.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ApiUiError>? Details { get; init; }
    /// <summary>Creates an argument-free descriptor from packaged resources, never from user input.</summary>
    public static ApiUiError Create(string code, string key) => new(ApiUiMessageCatalog.Shared.Format(key), code, key, ApiUiMessageCatalog.EmptyArguments);

    /// <summary>Copies raw display arguments, validates the packaged contract, and formats exactly once.</summary>
    public static ApiUiError Create(string code, string key, IReadOnlyDictionary<string,string> args)
    {
        var copy = new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(new Dictionary<string,string>(args, StringComparer.Ordinal));
        return new(ApiUiMessageCatalog.Shared.Format(key, args: copy), code, key, copy);
    }

    /// <summary>Adds display metadata without replacing a legacy string body, result instance or status.</summary>
    public static Microsoft.AspNetCore.Mvc.ObjectResult WithLegacyMessage(Microsoft.AspNetCore.Mvc.ObjectResult result, HttpContext? context, string key)
    {
        _ = ApiUiMessageCatalog.Shared.Format(key); // Validates a known argument-free resource before setting headers.
        if (context is not null) context.Response.Headers["X-SPC-Message-Key"] = key;
        return result;
    }

    /// <summary>Adds bounded base64url UTF8 JSON arguments while keeping the original legacy string response.</summary>
    public static Microsoft.AspNetCore.Mvc.ObjectResult WithLegacyMessage(Microsoft.AspNetCore.Mvc.ObjectResult result, HttpContext? context, string key, IReadOnlyDictionary<string,string> args)
    {
        var descriptor = Create("API_VALIDATION", key, args);
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(descriptor.Args));
        if (context is not null && encoded.Length <= 4096)
        {
            context.Response.Headers["X-SPC-Message-Key"] = key;
            context.Response.Headers["X-SPC-Message-Args"] = encoded;
        }
        return result;
    }
}
