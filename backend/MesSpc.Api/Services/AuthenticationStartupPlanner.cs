namespace MesSpc.Api.Services;

public static class AuthenticationStartupPlanner
{
    public static bool ShouldConfigureJwt(string? jwtKey) => !string.IsNullOrWhiteSpace(jwtKey);

    public static bool RequiresJwtKey(bool authEnabled, string? jwtKey) =>
        authEnabled && !ShouldConfigureJwt(jwtKey);
}
