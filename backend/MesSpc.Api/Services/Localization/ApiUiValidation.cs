using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Services.Localization;

/// <summary>Adds safe display metadata while retaining the framework validation response.</summary>
public static class ApiUiValidation
{
    /// <summary>Wraps the existing MVC factory; no validation, status or field names are replaced.</summary>
    public static void ConfigureOptions(ApiBehaviorOptions options)
    {
        var original = options.InvalidModelStateResponseFactory
            ?? throw new InvalidOperationException("MVC validation must be configured before API display metadata.");
        options.InvalidModelStateResponseFactory = context => DecorateResult(original(context));
    }

    /// <summary>Decorates only standard validation problems that have no existing typed error.</summary>
    public static IActionResult DecorateResult(IActionResult result)
    {
        if (result is not ObjectResult { Value: ValidationProblemDetails problem }
            || problem.Extensions.ContainsKey("code") || problem.Extensions.ContainsKey("messageKey")) return result;
        var catalog = ApiUiMessageCatalog.Shared;
        problem.Extensions["code"] = "VALIDATION_FAILED";
        problem.Extensions["messageKey"] = "spc.api.validationFailed";
        problem.Extensions["args"] = ApiUiMessageCatalog.EmptyArguments;
        problem.Extensions.TryAdd("message", catalog.Format("spc.api.validationFailed"));
        problem.Extensions["fieldMessages"] = problem.Errors.ToDictionary(pair => pair.Key, pair => pair.Value.Select(_ =>
        {
            IReadOnlyDictionary<string, string> args = new Dictionary<string, string> { ["field"] = pair.Key };
            return new ApiFieldMessage("VALIDATION_FIELD_INVALID", "spc.api.invalidField", args, catalog.Format("spc.api.invalidField", args: args));
        }).ToArray(), StringComparer.Ordinal);
        return result;
    }
}

/// <summary>Safe field display metadata. Arguments contain field paths, never attempted values.</summary>
public sealed record ApiFieldMessage(string Code, string MessageKey, IReadOnlyDictionary<string, string> Args, string Message);
