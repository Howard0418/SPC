using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Records auth JSON/model-validation rejection without reading the body.</summary>
public sealed class AuthenticationAuditResultFilter(AuthenticationAudit audit) : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (!context.ModelState.IsValid && context.Result is ObjectResult { StatusCode: 400 })
        {
            var code = context.HttpContext.Request.Path == "/api/v1/auth/login" ? "auth.local_login.request" : "auth.sso.exchange";
            await audit.FailureAsync("AUTH_REQUEST_INVALID", 400, null, eventCode: code);
        }
        await next();
    }
}
