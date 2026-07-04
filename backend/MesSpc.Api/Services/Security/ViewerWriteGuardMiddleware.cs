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
            await context.Response.WriteAsJsonAsync(new { message = "檢視者只能查看與查詢資料，無法執行新增、修改、刪除或匯入操作。" });
            return;
        }

        await next(context);
    }
}
