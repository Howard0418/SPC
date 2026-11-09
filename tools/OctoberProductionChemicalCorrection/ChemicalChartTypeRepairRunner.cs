using System.Data;
using System.Globalization;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OctoberProductionChemicalCorrection;

internal static class ChemicalChartTypeRepairRunner
{
    private const string DatabaseName = "PMR_SPC_2026";
    private const string ServerName = "172.16.110.16";
    private const string TaskCode = "SPC-CHEM-OCT-PROD-VALUE-CORRECTION-20261105";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 3 || args[2] is not ("--plan" or "--apply" or "--verify"))
            throw new ArgumentException("chart-type-repair CONFIG EVIDENCE_DIR --plan|--apply|--verify");

        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[0])).Build();
        var raw = config.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("SQL connection is missing.");
        var builder = new SqlConnectionStringBuilder(raw.Replace("Trust Server Certificate=", "TrustServerCertificate=", StringComparison.Ordinal));
        if (config["AppEnvironment"] != "production" || builder.DataSource != ServerName || builder.InitialCatalog != DatabaseName)
            throw new InvalidOperationException("Formal chart repair target guard failed.");

        Directory.CreateDirectory(args[1]);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await RequireActualTargetAsync(connection);

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        if (args[2] == "--verify")
        {
            var completed = await CountJobsAsync(connection, "Completed");
            var repairedPpcs = await CountRepairedPpcsAsync(connection);
            await WriteAsync(args[1], stamp, "verify", new { mode = "verify", database = DatabaseName, completed, repairedPpcs });
            if (repairedPpcs != 25 || completed != 493)
                throw new InvalidOperationException($"Repair verification incomplete: repairedPpcs={repairedPpcs}, completedJobs={completed}.");
            Console.WriteLine("VERIFIED: ppcs=25 I_MR; jobs=493 Completed");
            return 0;
        }

        var snapshot = await LoadSnapshotAsync(connection);
        var plan = ChemicalChartTypeRepairPlanner.Build(snapshot.Candidates);
        RequireFixedScope(snapshot, plan);

        if (args[2] == "--plan")
        {
            await WriteAsync(args[1], stamp, "plan", new { mode = "plan", database = DatabaseName, snapshot, plan });
            Console.WriteLine($"PLAN: ppcs={plan.ToUpdate.Count}; already={plan.AlreadyCorrect.Count}; failedJobs={plan.FailedJobCount}");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("SPC_ALLOW_FORMAL_CHEM_CHART_REPAIR") != "YES")
            throw new InvalidOperationException("Formal apply requires SPC_ALLOW_FORMAL_CHEM_CHART_REPAIR=YES after explicit authorization.");

        var backup = await BackupAsync(connection, stamp);
        var actor = WindowsIdentity.GetCurrent()?.Name ?? Environment.UserName;
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var chartTypeId = await ScalarIntAsync(connection, transaction,
                "SELECT Id FROM ControlChartTypes WHERE ChartTypeCode='I_MR' AND IsEnabled=1 AND IsDeleted=0");
            var changedPpcs = await ExecuteAsync(connection, transaction, ApplyPpcSql,
                new("@ChartTypeId", chartTypeId), new("@Actor", actor), new("@TaskCode", TaskCode));
            if (changedPpcs != 25)
                throw new InvalidOperationException($"Formal apply scope drift: ppcs={changedPpcs}.");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var repaired = await CountRepairedPpcsAsync(connection);
        if (repaired != 25) throw new InvalidOperationException($"Post-apply PPC verification failed: {repaired}.");
        await WriteAsync(args[1], stamp, "apply", new { mode = "apply", database = DatabaseName, backup, before = snapshot, repairedPpcs = repaired });
        Console.WriteLine("APPLIED: ppcs=25 I_MR; existing failed jobs will be retried by the statistics worker; no measurements imported");
        return 0;
    }

    private static void RequireFixedScope(RepairSnapshot snapshot, ChemicalChartTypeRepairPlan plan)
    {
        if (snapshot.Candidates.Count != 25 || snapshot.TotalJobs != 493 || plan.FailedJobCount != 160)
            throw new InvalidOperationException($"Reviewed scope drift: ppcs={snapshot.Candidates.Count}, jobs={snapshot.TotalJobs}, failed={plan.FailedJobCount}.");
    }

    private static async Task<RepairSnapshot> LoadSnapshotAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 90;
        command.CommandText = SnapshotSql;
        command.Parameters.AddWithValue("@TaskCode", TaskCode);
        var candidates = new List<ChemicalChartTypeRepairCandidate>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var evidence = reader.IsDBNull(3) ? Array.Empty<string>() : reader.GetString(3).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            candidates.Add(new(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1), evidence, reader.GetInt32(2)));
        }
        await reader.NextResultAsync();
        await reader.ReadAsync();
        return new(candidates, reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    }

    private static async Task RequireActualTargetAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(100),CONNECTIONPROPERTY('local_net_address'))";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (reader.GetString(0) != DatabaseName || reader.GetString(1) != ServerName)
            throw new InvalidOperationException("Actual formal database target mismatch.");
    }

    private static async Task<object> BackupAsync(SqlConnection connection, string stamp)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 300;
        command.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";
        var directory = Convert.ToString(await command.ExecuteScalarAsync()) ?? throw new InvalidOperationException("Backup path unavailable.");
        var path = Path.Combine(directory, $"PMR_SPC_2026_pre_chart_type_repair_{stamp}.bak");
        command.CommandText = "BACKUP DATABASE [PMR_SPC_2026] TO DISK=@Path WITH COPY_ONLY,CHECKSUM,INIT; RESTORE VERIFYONLY FROM DISK=@Path WITH CHECKSUM";
        command.Parameters.AddWithValue("@Path", path);
        await command.ExecuteNonQueryAsync();
        return new { path, copyOnly = true, checksum = true, verifyOnly = true };
    }

    private static async Task<int> CountJobsAsync(SqlConnection connection, string status)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = CountJobsSql;
        command.Parameters.AddWithValue("@TaskCode", TaskCode);
        command.Parameters.AddWithValue("@Status", status);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountRepairedPpcsAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM PartProcessCharacteristics p JOIN ControlChartTypes c ON c.Id=p.ChartTypeId WHERE p.Id IN ({TargetPpcIds}) AND c.ChartTypeCode='I_MR' AND c.IsEnabled=1 AND c.IsDeleted=0";
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<int> ScalarIntAsync(SqlConnection connection, SqlTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Required chart type missing."), CultureInfo.InvariantCulture);
    }

    private static async Task<int> ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; command.CommandTimeout = 90;
        command.Parameters.AddRange(parameters); return await command.ExecuteNonQueryAsync();
    }

    private static Task WriteAsync(string directory, string stamp, string suffix, object payload) =>
        File.WriteAllTextAsync(Path.Combine(directory, $"{stamp}-{suffix}.json"), JsonSerializer.Serialize(payload, JsonOptions));

    private sealed record RepairSnapshot(IReadOnlyList<ChemicalChartTypeRepairCandidate> Candidates, int TotalJobs, int CompletedJobs, int FailedJobs);

    private const string SnapshotSql = """
WITH blockers AS (
 SELECT p.Id, ct.ChartTypeCode CurrentCode, COUNT_BIG(*) FailedJobs, q.DefaultChartTypeId, q.CharacteristicCode, p.Unit, p.ControlScope
 FROM ChemicalStatisticsJobs j JOIN ChemicalCalculationReceipts r ON r.Id=j.ReceiptId
 JOIN VariableMeasurements v ON v.Id=j.VariableMeasurementId JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
 JOIN QualityCharacteristics q ON q.Id=p.CharacteristicId LEFT JOIN ControlChartTypes ct ON ct.Id=p.ChartTypeId
 WHERE r.PayloadJson LIKE '%'+@TaskCode+'%' AND j.Status='Failed'
 GROUP BY p.Id,ct.ChartTypeCode,q.DefaultChartTypeId,q.CharacteristicCode,p.Unit,p.ControlScope
)
SELECT b.Id,b.CurrentCode,CONVERT(int,b.FailedJobs),e.Codes
FROM blockers b OUTER APPLY (
 SELECT STRING_AGG(CONVERT(nvarchar(max),x.ChartTypeCode),',') Codes FROM (
   SELECT DISTINCT ct.ChartTypeCode FROM ControlChartTypes ct WHERE ct.Id=b.DefaultChartTypeId AND ct.IsEnabled=1 AND ct.IsDeleted=0
   UNION
   SELECT DISTINCT ct.ChartTypeCode FROM PartProcessCharacteristics p JOIN QualityCharacteristics q ON q.Id=p.CharacteristicId
   JOIN ControlChartTypes ct ON ct.Id=p.ChartTypeId AND ct.IsEnabled=1 AND ct.IsDeleted=0
   WHERE p.IsDeleted=0 AND p.Id<>b.Id AND q.CharacteristicCode=b.CharacteristicCode AND ISNULL(p.Unit,'')=ISNULL(b.Unit,'') AND ISNULL(p.ControlScope,'')=ISNULL(b.ControlScope,'')
 ) x
) e ORDER BY b.Id;
SELECT COUNT(*),SUM(CASE WHEN j.Status='Completed' THEN 1 ELSE 0 END),SUM(CASE WHEN j.Status='Failed' THEN 1 ELSE 0 END)
FROM ChemicalStatisticsJobs j JOIN ChemicalCalculationReceipts r ON r.Id=j.ReceiptId WHERE r.PayloadJson LIKE '%'+@TaskCode+'%';
""";

    private const string ApplyPpcSql = """
WITH targets AS (
 SELECT DISTINCT p.Id FROM ChemicalStatisticsJobs j JOIN ChemicalCalculationReceipts r ON r.Id=j.ReceiptId
 JOIN VariableMeasurements v ON v.Id=j.VariableMeasurementId JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
 WHERE r.PayloadJson LIKE '%'+@TaskCode+'%' AND j.Status='Failed' AND p.ChartTypeId IS NULL
)
UPDATE p SET ChartTypeId=@ChartTypeId,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@Actor
FROM PartProcessCharacteristics p JOIN targets t ON t.Id=p.Id WHERE p.ChartTypeId IS NULL;
""";

    private const string CountJobsSql = """
SELECT COUNT(*) FROM ChemicalStatisticsJobs j JOIN ChemicalCalculationReceipts r ON r.Id=j.ReceiptId
WHERE r.PayloadJson LIKE '%'+@TaskCode+'%' AND j.Status=@Status;
""";

    private const string TargetPpcIds = "2425,2426,2430,2431,2432,2438,2440,2444,2457,2459,2463,2473,2475,2478,2480,2481,2528,2539,2544,2546,2547,2555,2556,2557,2558";
}
