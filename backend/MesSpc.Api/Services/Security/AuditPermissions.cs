using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Security;

/// <summary>Sensitive audit access always uses the active database identity and explicit grants.</summary>
public sealed class AuditPermissions(AppDbContext db, IConfiguration configuration)
{
    public const string Read = "admin.audit.read";
    public const string Export = "admin.audit.export";
    public const string Evidence = "admin.audit.evidence";
    public static readonly string[] Codes = [Read, Export, Evidence];

    public async Task<Operator?> CurrentAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("Auth:Enabled") || user.Identity?.IsAuthenticated != true
            || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0) return null;
        return await db.Operators.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive && !x.IsDeleted, ct);
    }

    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, string permission, CancellationToken ct = default)
    {
        if (!Codes.Contains(permission, StringComparer.Ordinal)) return false;
        var actor = await CurrentAsync(user, ct);
        if (actor is null || !TryExplicit(actor.PagePermissionsJson, out var codes)) return false;
        return codes.Contains(Read) && codes.Contains(permission);
    }

    internal static int[] ReadAdministrators(IConfiguration configuration)
    {
        var values = configuration.GetSection("Audit:SecurityAdministratorOperatorIds").GetChildren()
            .Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (values.Length > 64 || values.Any(x => !int.TryParse(x, out var id) || id <= 0))
            throw new InvalidOperationException("SECURITY_AUDIT_ADMINISTRATORS_INVALID");
        var ids = values.Select(x => int.Parse(x!)).ToArray();
        if (ids.Distinct().Count() != ids.Length) throw new InvalidOperationException("SECURITY_AUDIT_ADMINISTRATORS_INVALID");
        return ids;
    }

    public bool IsSecurityAdministrator(int id)
    {
        try { return ReadAdministrators(configuration).Contains(id); }
        catch (InvalidOperationException) { return false; }
    }

    internal int[] AdministratorIds => ReadAdministrators(configuration);

    public async Task<bool> CanChangeAsync(ClaimsPrincipal user, int targetId, IReadOnlyCollection<string> previous,
        IReadOnlyCollection<string> requested, CancellationToken ct = default)
    {
        var actor = await CurrentAsync(user, ct);
        if (actor is null || UserRoles.Normalize(actor.Role) != UserRoles.Editor) return false;
        var sensitive = requested.Where(x => x.StartsWith("admin.audit.", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        if (sensitive.Any(x => !Codes.Contains(x, StringComparer.Ordinal))
            || (sensitive.Contains(Export) || sensitive.Contains(Evidence)) && !sensitive.Contains(Read)) return false;
        var old = previous.Where(x => Codes.Contains(x, StringComparer.Ordinal)).ToHashSet(StringComparer.Ordinal);
        if (old.SetEquals(sensitive)) return true;
        return IsSecurityAdministrator(actor.Id)
            && (actor.Id != targetId || !sensitive.Except(old).Any());
    }

    public static bool TryExplicit(string? json, out HashSet<string> permissions)
    {
        permissions = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(json);
            if (values is null || values.Any(x => string.IsNullOrWhiteSpace(x))) return false;
            permissions = values.ToHashSet(StringComparer.Ordinal);
            return true;
        }
        catch (JsonException) { return false; }
    }
}
