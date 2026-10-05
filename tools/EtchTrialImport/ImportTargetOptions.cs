using Microsoft.Data.SqlClient;

namespace EtchTrialImport;

public sealed record ImportTargetOptions(
    string Environment,
    bool Apply,
    string SpcConfigPath,
    string PortalConfigPath,
    string SpcDatabase,
    string PortalDatabase,
    string PortalConnectionName,
    string? BackupProofPath)
{
    public const string ExpectedServer = "172.16.110.16";

    public bool IsProduction =>
        Environment.Equals("production", StringComparison.OrdinalIgnoreCase);

    public static ImportTargetOptions Parse(string[] args)
    {
        var environment = Value(args, "--environment")?.ToLowerInvariant() ?? "test";
        var apply = Has(args, "--apply");

        var target = environment switch
        {
            "test" => new ImportTargetOptions(
                "test",
                apply,
                @"D:\SPC\release\test\backend\appsettings.json",
                @"D:\PmrPortal\release\test\portal-api\appsettings.json",
                "PMR_SPC_TEST",
                "PMR_PORTAL_TEST",
                "Test",
                null),
            "production" => new ImportTargetOptions(
                "production",
                apply,
                @"D:\SPC\release\production\backend\appsettings.json",
                @"D:\PmrPortal\release\production\portal-api\appsettings.json",
                "PMR_SPC_2026",
                "PMR_PORTAL_UAT",
                "Production",
                Value(args, "--backup-proof")),
            _ => throw new InvalidOperationException(
                $"Unsupported environment '{environment}'. Use test or production.")
        };

        if (target.IsProduction && apply)
        {
            var portal = Value(args, "--confirm-portal");
            var spc = Value(args, "--confirm-spc");
            if (!string.Equals(portal, target.PortalDatabase, StringComparison.Ordinal) ||
                !string.Equals(spc, target.SpcDatabase, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Production apply requires exact --confirm-portal and --confirm-spc values.");
            }

            if (string.IsNullOrWhiteSpace(target.BackupProofPath))
            {
                throw new InvalidOperationException(
                    "Production apply requires --backup-proof with COPY_ONLY and VERIFYONLY evidence.");
            }
        }

        return target;
    }

    public void GuardConnection(string connectionString, string expectedDatabase)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var server = NormalizeServer(builder.DataSource);

        if (!string.Equals(server, ExpectedServer, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                builder.InitialCatalog,
                expectedDatabase,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unexpected database target '{builder.DataSource}/{builder.InitialCatalog}'.");
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

    private static bool Has(string[] args, string name) =>
        args.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string? Value(string[] args, string name)
    {
        var prefix = name + "=";
        var arg = args.FirstOrDefault(x =>
            x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return arg?[prefix.Length..];
    }
}
