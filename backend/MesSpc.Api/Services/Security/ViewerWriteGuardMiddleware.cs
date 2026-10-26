using MesSpc.Api.Services.Localization;
using System.Security.Claims;

namespace MesSpc.Api.Services.Security;

public sealed class ViewerWriteGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isReadRequest = HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method);
        var isAnonymousAuthEndpoint = context.Request.Path.StartsWithSegments("/api/v1/auth/login");

        if (!isReadRequest
            && !isAnonymousAuthEndpoint
            && context.User.Identity?.IsAuthenticated == true
            && context.User.IsInRole(UserRoles.Viewer))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiUiError.Create("VIEWER_WRITE_DENIED", "spc.api.viewerWriteDenied"));
            return;
        }

        await next(context);
    }
}
