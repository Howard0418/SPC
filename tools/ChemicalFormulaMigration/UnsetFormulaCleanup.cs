using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChemicalFormulaMigration;

internal static class UnsetFormulaCleanup
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static async Task RunAsync(string[] args)
    {
        string Arg(string name) => args.Single(x => x.StartsWith(name + "=", StringComparison.Ordinal))[(name.Length + 1)..];
        var configPath = Arg("--config"); var rulesPath = Arg("--rules"); var output = Arg("--output");
        var apply = args.Contains("--apply", StringComparer.Ordinal);
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var environment = config.RootElement.GetProperty("AppEnvironment").GetString();
        var sql = new SqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!
            .Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        if (!sql.DataSource.Equals("172.16.110.16", StringComparison.OrdinalIgnoreCase)
            || !((environment == "test" && sql.InitialCatalog == "PMR_SPC_TEST")
                || (!apply && environment == "production" && sql.InitialCatalog == "PMR_SPC_2026")))
            throw new InvalidOperationException("Only the test database may be changed; production is read-only for this task.");
        var sourceText = await File.ReadAllTextAsync(rulesPath);
        var rules = JsonSerializer.Deserialize<List<SourceFormulaRule>>(sourceText, JsonOptions) ?? throw new InvalidOperationException("Rules missing.");
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(sql.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        await using (var identity = db.Database.GetDbConnection().CreateCommand())
        {
            identity.CommandText = "SELECT DB_NAME()";
            if ((string?)await identity.ExecuteScalarAsync() != sql.InitialCatalog) throw new InvalidOperationException("Database identity differs.");
        }
        async Task<List<UnsetFormulaRow>> LoadAsync() => await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x => x.ControlScope == "CHEM" && x.ChemicalAnalysisConfigJson != null)
            .Select(x => new UnsetFormulaRow(x.Id, x.ChemicalAnalysisConfigJson!)).ToListAsync();
        async Task<Dictionary<string, string>> ProtectedAsync()
        {
            var state = new Dictionary<string, string>();
            foreach (var table in new[] { "VariableMeasurements", "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalFTableVersions", "ChemicalFTableCells", "ChemicalFormulaScheduleRevisions", "ChemicalFormulaEffectivities", "ControlLimitSegments" })
            {
                await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
                cmd.CommandText = $"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.[{table}]";
                await using var reader = await cmd.ExecuteReaderAsync(); await reader.ReadAsync();
                state[table] = $"{reader.GetInt64(0)}:{(reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1))}";
            }
            return state;
        }
        var beforeRows = await LoadAsync(); var plan = UnsetConcentrationPlanner.Build(rules, beforeRows);
        var ids = plan.Select(x => x.PpcId).ToHashSet();
        var labels = await db.PartProcessCharacteristics.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, Line = x.Machine == null ? "" : x.Machine.MachineCode, Tank = x.Tank == null ? "" : x.Tank.TankName,
                Item = x.Characteristic == null ? "" : x.Characteristic.CharacteristicName }).ToListAsync();
        var before = await ProtectedAsync(); var versionsBefore = await db.ChemicalAnalysisFormulaVersions.CountAsync();
        var run = DateTime.Now.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(output);
        var evidence = new { environment, database = sql.InitialCatalog, sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText))), plan, labels, before };
        await File.WriteAllTextAsync(Path.Combine(output, $"{run}-before.json"), JsonSerializer.Serialize(evidence, JsonOptions));
        if (apply && plan.Count > 0)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var entities = await db.PartProcessCharacteristics.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            var service = new ChemicalAnalysisFormulaVersionService(db);
            foreach (var change in plan)
            {
                var entity = entities[change.PpcId];
                if (entity.ChemicalAnalysisConfigJson != change.BeforeJson) throw new InvalidOperationException("Source config drifted.");
                await service.RecordChangeAsync(entity.Id, change.BeforeJson, change.AfterJson, Environment.UserName,
                    "依Excel來源直接量測值，清除舊轉換器自動生成恆等式；濃度公式未設定", default);
                entity.ChemicalAnalysisConfigJson = change.AfterJson;
            }
            await db.SaveChangesAsync();
            var afterInTx = await LoadAsync();
            if (UnsetConcentrationPlanner.Build(rules, afterInTx).Count != 0) throw new InvalidOperationException("Cleanup is not idempotent.");
            foreach (var old in beforeRows.Where(x => !ids.Contains(x.PpcId)))
                if (afterInTx.Single(x => x.PpcId == old.PpcId).ConfigJson != old.ConfigJson) throw new InvalidOperationException("Unrelated config changed.");
            var protectedInTx = await ProtectedAsync();
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(protectedInTx)) throw new InvalidOperationException("Protected data changed.");
            if (await db.ChemicalAnalysisFormulaVersions.CountAsync() - versionsBefore != plan.Count) throw new InvalidOperationException("Version count differs.");
            await tx.CommitAsync();
        }
        var remaining = UnsetConcentrationPlanner.Build(rules, await LoadAsync());
        await File.WriteAllTextAsync(Path.Combine(output, $"{run}-result.json"), JsonSerializer.Serialize(new {
            environment, database = sql.InitialCatalog, apply, changed = apply ? plan.Count : 0, planned = plan.Count, remaining = remaining.Count,
            versionsAdded = await db.ChemicalAnalysisFormulaVersions.CountAsync() - versionsBefore, before, after = await ProtectedAsync(), plan, labels
        }, JsonOptions));
        Console.WriteLine($"{environment}: planned={plan.Count}; changed={(apply ? plan.Count : 0)}; remaining={remaining.Count}");
        foreach (var row in labels) Console.WriteLine($"PPC {row.Id}: {row.Line} / {row.Tank} / {row.Item}");
    }
}
