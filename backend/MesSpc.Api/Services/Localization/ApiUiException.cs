namespace MesSpc.Api.Services.Localization;

/// <summary>Attaches explicit display metadata while retaining the original exception and all its semantics.</summary>
public static class ApiUiException
{
    private static readonly object Marker = new();

    /// <summary>Tags only deliberately selected public failures; never derives keys from message text.</summary>
    public static T Attach<T>(T exception, string key, IReadOnlyDictionary<string,string>? args = null) where T : Exception
    {
        var descriptor = ApiUiError.Create("API_VALIDATION", key, args ?? ApiUiMessageCatalog.EmptyArguments);
        exception.Data[Marker] = descriptor with { Message = exception.Message };
        return exception;
    }

    /// <summary>Reads only the explicit marker created by this module.</summary>
    public static ApiUiError? Read(Exception exception) => exception.Data[Marker] as ApiUiError;

    /// <summary>Keeps the original legacy message-only response for untagged failures.</summary>
    public static object Describe(Exception exception) => Read(exception) is { } descriptor ? descriptor : new { message = exception.Message };

    /// <summary>Keeps the pre-existing failed response envelope and adds metadata only when explicitly tagged.</summary>
    public static object DescribeFailure(Exception exception) => Read(exception) is { } descriptor
        ? new { success = false, data = (object?)null, message = exception.Message, code = descriptor.Code, messageKey = descriptor.MessageKey, args = descriptor.Args }
        : new { success = false, data = (object?)null, message = exception.Message };
}
