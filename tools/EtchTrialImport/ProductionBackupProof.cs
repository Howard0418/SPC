using System.Text.Json;

namespace EtchTrialImport;

public sealed record BackupDatabaseProof(
    string Database,
    string BackupPath,
    bool CopyOnly,
    bool Checksum,
    bool VerifyOnly);

public sealed record ProductionBackupProof(
    int Version,
    string Server,
    DateTimeOffset CreatedAtUtc,
    BackupDatabaseProof Portal,
    BackupDatabaseProof Spc)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProductionBackupProof LoadAndValidate(
        string path,
        ImportTargetOptions target,
        DateTimeOffset now)
    {
        if (!target.IsProduction)
        {
            throw new InvalidOperationException(
                "Backup proof validation is only valid for the production target.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Backup proof file does not exist: {path}");
        }

        ProductionBackupProof proof;
        try
        {
            proof = JsonSerializer.Deserialize<ProductionBackupProof>(
                File.ReadAllText(path),
                JsonOptions) ?? throw new InvalidOperationException("Backup proof is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Backup proof JSON is invalid.", ex);
        }

        if (proof.Version != 1 ||
            !string.Equals(
                NormalizeServer(proof.Server),
                ImportTargetOptions.ExpectedServer,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup proof target server or version is invalid.");
        }

        var age = now - proof.CreatedAtUtc;
        if (age < TimeSpan.FromMinutes(-5) || age > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException(
                "Backup proof must be created within 24 hours of production apply.");
        }

        ValidateDatabase(proof.Portal, target.PortalDatabase, "Portal");
        ValidateDatabase(proof.Spc, target.SpcDatabase, "SPC");
        return proof;
    }

    private static void ValidateDatabase(
        BackupDatabaseProof proof,
        string expectedDatabase,
        string label)
    {
        if (!string.Equals(
                proof.Database,
                expectedDatabase,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{label} backup proof database does not match {expectedDatabase}.");
        }

        if (!proof.CopyOnly || !proof.Checksum || !proof.VerifyOnly)
        {
            throw new InvalidOperationException(
                $"{label} backup proof must pass COPY_ONLY, CHECKSUM, and RESTORE VERIFYONLY.");
        }

        if (string.IsNullOrWhiteSpace(proof.BackupPath) ||
            !proof.BackupPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{label} backup path is invalid.");
        }
    }

    private static string NormalizeServer(string value)
    {
        var server = value.Trim();
        if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            server = server[4..];
        }

        var comma = server.IndexOf(',');
        return comma >= 0 ? server[..comma] : server;
    }
}
