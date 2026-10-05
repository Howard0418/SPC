using EtchTrialImport;

namespace MesSpc.Chemical.Tests;

public class EtchImportTargetOptionsTests
{
    [Fact]
    public void DefaultsToTestDryRun()
    {
        var target = ImportTargetOptions.Parse([]);

        Assert.Equal("test", target.Environment);
        Assert.False(target.Apply);
        Assert.Equal("PMR_PORTAL_TEST", target.PortalDatabase);
        Assert.Equal("PMR_SPC_TEST", target.SpcDatabase);
    }

    [Fact]
    public void ProductionDryRunUsesConfirmedProductionTargets()
    {
        var target = ImportTargetOptions.Parse(["--environment=production"]);

        Assert.True(target.IsProduction);
        Assert.False(target.Apply);
        Assert.Equal("PMR_PORTAL_UAT", target.PortalDatabase);
        Assert.Equal("PMR_SPC_2026", target.SpcDatabase);
        Assert.Equal("Production", target.PortalConnectionName);
    }

    [Theory]
    [InlineData("--confirm-portal=PMR_PORTAL_UAT")]
    [InlineData("--confirm-spc=PMR_SPC_2026")]
    public void ProductionApplyRequiresBothExplicitConfirmations(string oneConfirmation)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            ImportTargetOptions.Parse(
            [
                "--environment=production",
                "--apply",
                oneConfirmation
            ]));

        Assert.Contains("confirm", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionApplyRequiresBackupProofPath()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            ImportTargetOptions.Parse(
            [
                "--environment=production",
                "--apply",
                "--confirm-portal=PMR_PORTAL_UAT",
                "--confirm-spc=PMR_SPC_2026"
            ]));

        Assert.Contains("backup", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionApplyAcceptsExactConfirmationsAndBackupProofPath()
    {
        var target = ImportTargetOptions.Parse(
        [
            "--environment=production",
            "--apply",
            "--confirm-portal=PMR_PORTAL_UAT",
            "--confirm-spc=PMR_SPC_2026",
            "--backup-proof=D:\\evidence\\backup-proof.json"
        ]);

        Assert.True(target.Apply);
        Assert.Equal(@"D:\evidence\backup-proof.json", target.BackupProofPath);
    }

    [Theory]
    [InlineData("172.16.110.16", "PMR_SPC_2026")]
    [InlineData("tcp:172.16.110.16", "PMR_SPC_2026")]
    public void GuardAcceptsExpectedServerAndDatabase(string server, string database)
    {
        var target = ImportTargetOptions.Parse(["--environment=production"]);

        target.GuardConnection(
            $"Server={server};Database={database};User Id=x;Password=x;TrustServerCertificate=True",
            target.SpcDatabase);
    }

    [Theory]
    [InlineData("172.16.110.99", "PMR_SPC_2026")]
    [InlineData("172.16.110.16", "PMR_SPC_TEST")]
    public void GuardRejectsUnexpectedServerOrDatabase(string server, string database)
    {
        var target = ImportTargetOptions.Parse(["--environment=production"]);

        Assert.Throws<InvalidOperationException>(() =>
            target.GuardConnection(
                $"Server={server};Database={database};User Id=x;Password=x;TrustServerCertificate=True",
                target.SpcDatabase));
    }
}
