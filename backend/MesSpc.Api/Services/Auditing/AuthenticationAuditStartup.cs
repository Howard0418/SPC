using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Auditing;

/// <summary>Rejects authentication collection when trusted identity or restricted persistence cannot be established.</summary>
public static class AuthenticationAuditStartup
{
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Audit:AuthenticationEnabled")) return;
        if (!configuration.GetValue<bool>("Auth:Enabled") || configuration.GetValue("Database:InitializeOnStartup", true)
            || configuration["AppEnvironment"] is not ("test" or "production")
            || !Strong(configuration["Auth:JwtKey"]) || !Strong(configuration["Auth:PortalSsoKey"]) || !Strong(configuration["Audit:AttemptedAccountHmacKey"]))
            throw new InvalidOperationException("AUTH_AUDIT_CONFIGURATION_INVALID");
    }

    /// <summary>Uses the actual runtime connection; never infers least privilege from role definitions alone.</summary>
    public static async Task ValidateDatabaseAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlServer()) throw new InvalidOperationException("AUTH_AUDIT_SQLSERVER_REQUIRED");
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT COALESCE(IS_SRVROLEMEMBER('sysadmin'),0), COALESCE(IS_MEMBER('db_owner'),0),
                COALESCE(IS_MEMBER('SpcAuditWriter'),0),
                COALESCE(HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','SELECT'),0),
                COALESCE(HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','INSERT'),0),
                COALESCE(HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','UPDATE'),0),
                COALESCE(HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','DELETE'),0),
                COALESCE(HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','ALTER'),0),
                (SELECT COUNT(*) FROM fn_my_permissions(NULL,'DATABASE') WHERE permission_name LIKE 'CREATE %'
                   OR permission_name IN ('CONTROL','ALTER','ALTER ANY ROLE','ALTER ANY USER','ALTER ANY SCHEMA'));
                """;
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                await reader.ReadAsync(ct);
                var expected = new[] { 0, 0, 1, 1, 1, 0, 0, 0, 0 };
                if (Enumerable.Range(0, expected.Length).Any(i => Convert.ToInt32(reader.GetValue(i)) != expected[i]))
                    throw new InvalidOperationException("AUTH_AUDIT_RUNTIME_PERMISSIONS_INVALID");
            }
            if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
                throw new InvalidOperationException("AUTH_AUDIT_PENDING_MIGRATIONS");
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static bool Strong(string? value) => value is { Length: >= 32 } && !value.StartsWith("REPLACE_", StringComparison.Ordinal)
        && !value.StartsWith("USE_THE_", StringComparison.Ordinal);
}
