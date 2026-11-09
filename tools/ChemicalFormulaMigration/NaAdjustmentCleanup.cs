using System.Data;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChemicalFormulaMigration;

internal static class NaAdjustmentCleanup
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task RunAsync(string[] args)
    {
        string Arg(string name) => args.Single(x => x.StartsWith(name + "=", StringComparison.Ordinal))[(name.Length + 1)..];
        var configPath = Arg("--config"); var output = Arg("--output"); var apply = args.Contains("--apply", StringComparer.Ordinal);
        var maintenanceConfigPath = args.SingleOrDefault(x => x.StartsWith("--maintenance-config=", StringComparison.Ordinal))?["--maintenance-config=".Length..];
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var environment = config.RootElement.GetProperty("AppEnvironment").GetString();
        var builder = new SqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!
            .Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        if (environment != "test" || builder.DataSource != "172.16.110.16" || builder.InitialCatalog != "PMR_SPC_TEST")
            throw new InvalidOperationException("Only the confirmed SPC test database is permitted.");
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(builder.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        await using (var identity = db.Database.GetDbConnection().CreateCommand())
        {
            identity.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
            await using var reader = await identity.ExecuteReaderAsync(); await reader.ReadAsync();
            if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16") throw new InvalidOperationException("Actual database target mismatch.");
        }
        var approved = new Dictionary<int, string> { [2515] = "U13", [2516] = "U14", [2518] = "U16", [2619] = "U13" };
        async Task<List<NaAdjustmentRow>> LoadAsync() => (await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x => approved.Keys.Contains(x.Id)).Select(x => new { x.Id, x.ChemicalAnalysisConfigJson }).ToListAsync())
            .Select(x => new NaAdjustmentRow(x.Id, approved[x.Id], x.ChemicalAnalysisConfigJson ?? "{}")).ToList();
        async Task<Dictionary<string, string>> ProtectedAsync()
        {
            var state = new Dictionary<string, string>();
            foreach (var table in new[] { "VariableMeasurements", "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalFTableVersions", "ChemicalFTableCells", "ChemicalFormulaScheduleRevisions", "ChemicalFormulaEffectivities", "ControlLimitSegments" })
            {
                await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
                cmd.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}]";
                await using var reader = await cmd.ExecuteReaderAsync(); await reader.ReadAsync(); state[table] = $"{reader.GetInt64(0)}:{(reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1))}";
            }
            return state;
        }
        var rows = await LoadAsync(); if (rows.Count != approved.Count) throw new InvalidOperationException("The four reviewed PPCs are not present.");
        var plan = NaAdjustmentPlanner.Build(rows); var before = await ProtectedAsync(); var versionsBefore = await db.ChemicalAnalysisFormulaVersions.CountAsync();
        Directory.CreateDirectory(output); var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss"); object? backup = null;
        await File.WriteAllTextAsync(Path.Combine(output, $"{stamp}-before.json"), JsonSerializer.Serialize(new { environment, database = builder.InitialCatalog, plan, before }, Json));
        if (apply && plan.Count > 0)
        {
            var backupConnection = maintenanceConfigPath is null
                ? builder.ConnectionString
                : LoadMaintenanceConnection(maintenanceConfigPath);
            backup = await BackupAsync(backupConnection, stamp, maintenanceConfigPath is not null);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var entities = await db.PartProcessCharacteristics.Where(x => approved.Keys.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            var versions = new ChemicalAnalysisFormulaVersionService(db);
            foreach (var change in plan)
            {
                var entity = entities[change.PpcId]; if (entity.ChemicalAnalysisConfigJson != change.BeforeJson) throw new InvalidOperationException("Config drifted.");
                await versions.RecordChangeAsync(entity.Id, change.BeforeJson, change.AfterJson, Environment.UserName, "Excel調整條件為NA；移除未定義上限／更槽分支，保留可證明的低值添加規則", default);
                entity.ChemicalAnalysisConfigJson = change.AfterJson;
            }
            await db.SaveChangesAsync();
            if (NaAdjustmentPlanner.Build(await LoadAsync()).Count != 0) throw new InvalidOperationException("Cleanup is not idempotent.");
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await ProtectedAsync())) throw new InvalidOperationException("Protected data changed.");
            if (await db.ChemicalAnalysisFormulaVersions.CountAsync() - versionsBefore != plan.Count) throw new InvalidOperationException("Version count differs.");
            await tx.CommitAsync();
        }
        var remaining = NaAdjustmentPlanner.Build(await LoadAsync()); var after = await ProtectedAsync();
        await File.WriteAllTextAsync(Path.Combine(output, $"{stamp}-result.json"), JsonSerializer.Serialize(new { environment, database = builder.InitialCatalog, apply, planned = plan.Count, changed = apply ? plan.Count : 0, remaining = remaining.Count, versionsAdded = await db.ChemicalAnalysisFormulaVersions.CountAsync() - versionsBefore, before, after, backup, plan }, Json));
        Console.WriteLine($"test: planned={plan.Count}; changed={(apply ? plan.Count : 0)}; remaining={remaining.Count}");
    }

    private static string LoadMaintenanceConnection(string path)
    {
        using var config = JsonDocument.Parse(File.ReadAllText(path));
        if (config.RootElement.GetProperty("AppEnvironment").GetString() != "test")
            throw new InvalidOperationException("Maintenance configuration must be test-only.");
        var builder = new SqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!
            .Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        if (builder.DataSource != "172.16.110.16" || builder.InitialCatalog != "PMR_SPC_TEST")
            throw new InvalidOperationException("Maintenance configuration target mismatch.");
        return builder.ConnectionString;
    }

    private static async Task<object> BackupAsync(string connection, string stamp, bool maintenanceIdentity)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var cmd = sql.CreateCommand();
        cmd.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            await reader.ReadAsync();
            if (reader.GetString(0) != "PMR_SPC_TEST" || reader.GetString(1) != "172.16.110.16")
                throw new InvalidOperationException("Maintenance connection actual target mismatch.");
        }
        cmd.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"; var dir = (string?)await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Backup directory missing.");
        var path = dir.TrimEnd('\\', '/') + $"\\PMR_SPC_TEST_pre_na_adjustment_{stamp}.bak"; cmd.CommandTimeout = 300;
        cmd.CommandText = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT"; cmd.Parameters.AddWithValue("@path", path); await cmd.ExecuteNonQueryAsync();
        cmd.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await cmd.ExecuteNonQueryAsync(); return new { path, copyOnly = true, checksum = true, verifyOnly = true, maintenanceIdentity };
    }
}
