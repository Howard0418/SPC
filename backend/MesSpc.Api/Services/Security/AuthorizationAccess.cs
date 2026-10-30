using System.Security.Claims;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Security;

public sealed record CurrentUserAccess(string? Name, string Role, string? PagePermissionsJson, bool IsAuthenticated, bool IsActive);

public sealed class AuthorizationAccess(AppDbContext db)
{
    public async Task<CurrentUserAccess> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(user.Identity.Name))
            return new CurrentUserAccess(null, UserRoles.Viewer, null, false, false);

        var name = user.Identity.Name;
        var role = UserRoles.Normalize(user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value);
        var permissions = user.FindFirst("permissions")?.Value ?? user.FindFirst("permission")?.Value;

        var op = await db.Operators
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Username == name || x.OperatorCode == name, ct);

        if (op is null)
            return new CurrentUserAccess(name, role, permissions, true, false);

        return new CurrentUserAccess(
            name,
            UserRoles.Normalize(op.Role),
            string.IsNullOrWhiteSpace(op.PagePermissionsJson) ? permissions : op.PagePermissionsJson,
            true,
            op.IsActive);
    }

    public async Task<bool> CanManageCalibrationAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var access = await ResolveAsync(user, ct);
        return access is { IsAuthenticated: true, IsActive: true }
            && CalibrationRules.CanManage(access.Role, access.PagePermissionsJson);
    }
}
