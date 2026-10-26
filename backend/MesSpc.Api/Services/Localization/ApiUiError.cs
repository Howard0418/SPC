namespace MesSpc.Api.Services.Localization;

/// <summary>Safe additive descriptor for a known API failure; retains the legacy Traditional Chinese message.</summary>
public sealed record ApiUiError(string Message, string Code, string MessageKey, IReadOnlyDictionary<string,string> Args)
{
    /// <summary>Creates an argument-free descriptor from packaged resources, never from user input.</summary>
    public static ApiUiError Create(string code, string key) => new(ApiUiMessageCatalog.Shared.Format(key), code, key, ApiUiMessageCatalog.EmptyArguments);
}
