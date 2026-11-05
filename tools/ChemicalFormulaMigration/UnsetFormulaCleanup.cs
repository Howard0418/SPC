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
        var productionReviewed = args.Contains("--production-reviewed", StringComparer.Ordinal);
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var environment = config.RootElement.GetProperty("AppEnvironment").GetString();
        var sql = new SqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!
            .Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        UnsetProductionGuard.ValidateTarget(environment, sql.DataSource, sql.InitialCatalog, apply, productionReviewed);
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
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText)));
        ReviewedPlan? review = null;
        object? apiState = null;
        if (productionReviewed)
        {
            var reviewBytes = await File.ReadAllBytesAsync(Arg("--reviewed"));
            if (Convert.ToHexString(SHA256.HashData(reviewBytes)) != "311DA9E3E3FB64BCC2FE685AD2BE578097C7C313A1170E032D94DE3A841662B0")
                throw new InvalidOperationException("The authorized review evidence hash differs.");
            review = JsonSerializer.Deserialize<ReviewedPlan>(reviewBytes, JsonOptions) ?? throw new InvalidOperationException("Review missing.");
            if (review.SourceHash != sourceHash || !review.Plan.Select(x => x.PpcId).Order().SequenceEqual(new[] { 2442, 2579, 2580, 2581, 2582, 2599, 2600 }))
                throw new InvalidOperationException("Source or approved seven PPCs differ.");
            UnsetProductionGuard.ValidateRows(review.Plan, beforeRows);
            foreach (var change in plan)
                if (!review.Plan.Contains(change)) throw new InvalidOperationException("A change is outside the reviewed plan.");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var version = await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/version");
            var health = await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/health");
            using var versionDoc = JsonDocument.Parse(version);
            if (versionDoc.RootElement.GetProperty("environment").GetString() != "production") throw new InvalidOperationException("Production API environment differs.");
            apiState = new { version = JsonSerializer.Deserialize<object>(version), health = JsonSerializer.Deserialize<object>(health) };
        }
        var labelIds = review?.Plan.Select(x => x.PpcId).ToHashSet() ?? ids;
        var labels = await db.PartProcessCharacteristics.AsNoTracking().Where(x => labelIds.Contains(x.Id))
            .Select(x => new UnsetLabel(x.Id, x.Machine == null ? "" : x.Machine.MachineCode, x.Tank == null ? "" : x.Tank.TankName,
                x.Characteristic == null ? "" : x.Characteristic.CharacteristicName)).ToListAsync();
        if (review is not null && !labels.OrderBy(x => x.Id).SequenceEqual(review.Labels.OrderBy(x => x.Id)))
            throw new InvalidOperationException("Reviewed business identities differ.");
        var before = await ProtectedAsync(); var versionsBefore = await db.ChemicalAnalysisFormulaVersions.CountAsync();
        var run = DateTime.Now.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(output);
        var evidence = new { environment, database = sql.InitialCatalog, sourceHash, plan, labels, before, apiState };
        await File.WriteAllTextAsync(Path.Combine(output, $"{run}-before.json"), JsonSerializer.Serialize(evidence, JsonOptions));
        object? backup = null;
        if (apply && plan.Count > 0)
        {
            if (productionReviewed) backup = await BackupProductionAsync(sql.ConnectionString, output, run);
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
        if (review is not null) UnsetProductionGuard.ValidateRows(review.Plan, await LoadAsync());
        await File.WriteAllTextAsync(Path.Combine(output, $"{run}-result.json"), JsonSerializer.Serialize(new {
            environment, database = sql.InitialCatalog, apply, changed = apply ? plan.Count : 0, planned = plan.Count, remaining = remaining.Count,
            versionsAdded = await db.ChemicalAnalysisFormulaVersions.CountAsync() - versionsBefore, before, after = await ProtectedAsync(), plan, labels, backup, apiState
        }, JsonOptions));
        Console.WriteLine($"{environment}: planned={plan.Count}; changed={(apply ? plan.Count : 0)}; remaining={remaining.Count}");
        foreach (var row in labels) Console.WriteLine($"PPC {row.Id}: {row.Line} / {row.Tank} / {row.Item}");
    }

    private static async Task<object> BackupProductionAsync(string connection, string output, string run)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync(); await using var cmd = sql.CreateCommand();
        cmd.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
        var directory = (string?)await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Backup directory missing.");
        var path = directory.TrimEnd('\\', '/') + $"\\PMR_SPC_2026_pre_unset_formula_{run}.bak";
        cmd.CommandTimeout = 300; cmd.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@path WITH COPY_ONLY,CHECKSUM,INIT";
        cmd.Parameters.AddWithValue("@path", path); await cmd.ExecuteNonQueryAsync();
        cmd.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM"; await cmd.ExecuteNonQueryAsync();
        var proof = new { path, checksum = true, verifyOnly = true };
        await File.WriteAllTextAsync(Path.Combine(output, $"{run}-backup.json"), JsonSerializer.Serialize(proof, JsonOptions));
        return proof;
    }

    private sealed record UnsetLabel(int Id, string Line, string Tank, string Item);
    private sealed record ReviewedPlan(string SourceHash, List<UnsetFormulaChange> Plan, List<UnsetLabel> Labels);
}
