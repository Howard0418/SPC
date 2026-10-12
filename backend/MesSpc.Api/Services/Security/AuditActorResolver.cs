using System.Security.Claims;

namespace MesSpc.Api.Services.Security;

public static class AuditActorResolver
{
    private static readonly string[] ClaimTypesInPriority =
    [
        ClaimTypes.Name,
        "name",
        "preferred_username",
        "username",
        ClaimTypes.NameIdentifier,
        "sub"
    ];

    public static string Resolve(ClaimsPrincipal? user, string fallback = "System")
    {
        var identityName = Normalize(user?.Identity?.Name);
        if (identityName is not null) return identityName;

        foreach (var claimType in ClaimTypesInPriority)
        {
            var value = Normalize(user?.FindFirst(claimType)?.Value);
            if (value is not null) return value;
        }

        return fallback;
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
