using System.Security.Claims;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Security;

/// <summary>Rechecks the signed SPC identity and projects the current grants without extending the session.</summary>
internal sealed class CurrentOperatorSession(AppDbContext db)
{
    public async Task<ClaimsPrincipal?> ResolveAsync(ClaimsPrincipal signed, CancellationToken ct = default)
    {
        if (signed.Identity is not ClaimsIdentity { IsAuthenticated: true } identity
            || string.IsNullOrWhiteSpace(identity.Name)
            || !int.TryParse(signed.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0)
            return null;

        var user = await db.Operators.AsNoTracking()
            .Where(x => x.Id == id && x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Username, x.OperatorCode, x.OperatorName, x.Role, x.PagePermissionsJson })
            .SingleOrDefaultAsync(ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Username)
            || NormalizeAccount(user.Username) != NormalizeAccount(identity.Name))
            return null;

        var claims = identity.Claims.Where(x => x.Type != identity.RoleClaimType
            && x.Type is not ("role" or "permission" or "permissions" or "operatorCode" or "displayName")).ToList();
        claims.Add(new(identity.RoleClaimType, UserRoles.Normalize(user.Role)));
        claims.Add(new("operatorCode", user.OperatorCode));
        claims.Add(new("displayName", user.OperatorName));
        claims.AddRange(SpcPagePermissions.Resolve(user.Role, user.PagePermissionsJson).Select(code => new Claim("permission", code)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType));
    }

    internal static string NormalizeAccount(string account)
    {
        var value = account.Trim();
        var slash = value.LastIndexOf('\\');
        if (slash >= 0 && slash < value.Length - 1) value = value[(slash + 1)..];
        var at = value.IndexOf('@');
        if (at > 0) value = value[..at];
        return value.ToLowerInvariant();
    }
}
