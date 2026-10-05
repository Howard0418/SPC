using System.Text.Json;
using EtchTrialImport;

namespace MesSpc.Chemical.Tests;

public class ProductionBackupProofTests
{
    [Fact]
    public void ValidProofMatchesBothProductionTargets()
    {
        var path = WriteProof();
        try
        {
            var target = ImportTargetOptions.Parse(["--environment=production"]);
            var proof = ProductionBackupProof.LoadAndValidate(path, target, DateTimeOffset.UtcNow);

            Assert.Equal("PMR_PORTAL_UAT", proof.Portal.Database);
            Assert.Equal("PMR_SPC_2026", proof.Spc.Database);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsUnverifiedBackup()
    {
        var path = WriteProof(portalVerified: false);
        try
        {
            var target = ImportTargetOptions.Parse(["--environment=production"]);
            var error = Assert.Throws<InvalidOperationException>(() =>
                ProductionBackupProof.LoadAndValidate(path, target, DateTimeOffset.UtcNow));

            Assert.Contains("VERIFYONLY", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsWrongDatabase()
    {
        var path = WriteProof(portalDatabase: "PMR_PORTAL_TEST");
        try
        {
            var target = ImportTargetOptions.Parse(["--environment=production"]);
            Assert.Throws<InvalidOperationException>(() =>
                ProductionBackupProof.LoadAndValidate(path, target, DateTimeOffset.UtcNow));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsStaleProof()
    {
        var path = WriteProof(createdAtUtc: DateTimeOffset.UtcNow.AddHours(-25));
        try
        {
            var target = ImportTargetOptions.Parse(["--environment=production"]);
            var error = Assert.Throws<InvalidOperationException>(() =>
                ProductionBackupProof.LoadAndValidate(path, target, DateTimeOffset.UtcNow));

            Assert.Contains("24", error.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteProof(
        bool portalVerified = true,
        string portalDatabase = "PMR_PORTAL_UAT",
        DateTimeOffset? createdAtUtc = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-backup-proof.json");
        var proof = new
        {
            Version = 1,
            Server = "172.16.110.16",
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            Portal = new
            {
                Database = portalDatabase,
                BackupPath = @"D:\SQLBackup\portal.bak",
                CopyOnly = true,
                Checksum = true,
                VerifyOnly = portalVerified
            },
            Spc = new
            {
                Database = "PMR_SPC_2026",
                BackupPath = @"D:\SQLBackup\spc.bak",
                CopyOnly = true,
                Checksum = true,
                VerifyOnly = true
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(proof));
        return path;
    }
}
