#if SPC_PRODUCTION_SQL_TESTS
using System.Security.Cryptography;
using System.Text;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Chemical.Tests;

public sealed class ProductionFormulaMigrationRehearsalTests
{
    [Fact]
    public async Task FourPendingMigrationsPreserveExistingRowsAndAreIdempotent()
    {
        var connection = Environment.GetEnvironmentVariable("SPC_PRODUCTION_REHEARSAL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit isolated connection required");
        var builder = new SqlConnectionStringBuilder(connection);
        Assert.Matches("^PMR_SPC_RELEASE_VERIFY_20261021_[a-f0-9]{8}$", builder.InitialCatalog);
        await using var sql = new SqlConnection(connection);
        await sql.OpenAsync();
        using var name = sql.CreateCommand(); name.CommandText = "SELECT DB_NAME()";
        Assert.Equal(builder.InitialCatalog, (string)(await name.ExecuteScalarAsync())!);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
        await using var db = new AppDbContext(options);
        string[] expected = ["20261007090000_AddChemicalFTableVersionHistories",
            "20261019235606_AddSpecificationLimitVersions", "20261020040256_AddChemicalFormulaTraceSchema",
            "20261020052530_AddChemicalDirectWriterProtocol"];
        Assert.Equal(expected.Order(), (await db.Database.GetPendingMigrationsAsync()).Order());
        var tables = new List<(string Schema, string Name)>();
        using (var cmd = sql.CreateCommand())
        {
            cmd.CommandText = "SELECT SCHEMA_NAME(schema_id),name FROM sys.tables WHERE is_ms_shipped=0 AND name<>'__EFMigrationsHistory' ORDER BY schema_id,name";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
        }
        var before = new Dictionary<string, (string Columns, string Hash, long Rows)>();
        foreach (var table in tables)
        {
            var full = Quote(table.Schema) + "." + Quote(table.Name);
            using var cmd = sql.CreateCommand();
            cmd.CommandText = "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(@full) ORDER BY column_id";
            cmd.Parameters.AddWithValue("@full", full);
            var columns = new List<string>();
            using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync()) columns.Add("t." + Quote(reader.GetString(0)));
            var list = string.Join(",", columns);
            var data = await Fingerprint(sql, full, list);
            before[full] = (list, data.Hash, data.Rows);
        }
        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        foreach (var (table, original) in before)
        {
            var current = await Fingerprint(sql, table, original.Columns);
            Assert.True(original.Hash == current.Hash && original.Rows == current.Rows,
                "Original rows changed in " + table);
        }
        foreach (var table in new[] { "ChemicalFTableVersionHistories", "SpecificationLimitVersions",
            "ChemicalFormulaScheduleRevisions", "ChemicalFormulaEffectivities", "ChemicalFormulaPolicies",
            "ChemicalCalculationReceipts", "ChemicalCalculationTraces", "ChemicalStatisticsJobs" })
        {
            using var count = sql.CreateCommand(); count.CommandText = "SELECT COUNT_BIG(*) FROM dbo." + Quote(table);
            Assert.Equal(0L, (long)(await count.ExecuteScalarAsync())!);
        }
        using (var check = sql.CreateCommand())
        {
            check.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS"; check.CommandTimeout = 180;
            using var reader = await check.ExecuteReaderAsync();
            do { Assert.False(await reader.ReadAsync(), "Constraint violation in isolated database"); }
            while (await reader.NextResultAsync());
        }
        Console.WriteLine("ISOLATED_MIGRATIONS=4; IDEMPOTENT=PASS; ORIGINAL_TABLES=" + tables.Count + "; ORIGINAL_ROWS_PRESERVED=PASS; EMPTY_NEW_TABLES=8");
    }
    private static string Quote(string value) => "[" + value.Replace("]", "]]") + "]";
    private static async Task<(string Hash, long Rows)> Fingerprint(SqlConnection connection, string table, string columns)
    {
        using var cmd = connection.CreateCommand(); cmd.CommandTimeout = 180;
        cmd.CommandText = "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT " + columns +
            " FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)),2) h FROM " + table + " t ORDER BY h";
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var reader = await cmd.ExecuteReaderAsync(); long rows = 0;
        while (await reader.ReadAsync()) { hash.AppendData(Encoding.ASCII.GetBytes(reader.GetString(0))); rows++; }
        return (Convert.ToHexString(hash.GetHashAndReset()), rows);
    }
}
#endif
