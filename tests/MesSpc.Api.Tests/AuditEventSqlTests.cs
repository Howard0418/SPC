using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class AuditSqlFactAttribute : FactAttribute
{
    public AuditSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SPC_AUDIT_SQL_TESTS") != "1")
            Skip = "Requires explicitly enabled isolated SQL Server database (SPC_AUDIT_SQL_TESTS=1).";
    }
}

public class AuditEventSqlTests
{
    [AuditSqlFact]
    public async Task IsolatedMigrationRetryConcurrencyPermissionsAndAtomicity()
    {
        // Only credentials are read from local test configuration; no writes to the configured database.
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "release/test/backend/appsettings.json")));
        var connection = new SqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString());
        Assert.Equal("PMR_SPC_TEST", connection.InitialCatalog);
        var name = "SPC_AUDIT_VERIFY_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = "master"; connection.Pooling = false;
        await using var master = new SqlConnection(connection.ConnectionString); await master.OpenAsync();
        await using (var create = master.CreateCommand()) { create.CommandText = "CREATE DATABASE [" + name + "]"; await create.ExecuteNonQueryAsync(); }
        connection.InitialCatalog = name;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options;
        AppDbContext Open() => new(options);
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppEnvironment"] = "test" }).Build();
        AuditEventWriter Writer(AppDbContext db) => new(db, options, settings, TimeProvider.System, NullLogger<AuditEventWriter>.Instance);
        try
        {
            await using var db = Open();
            var migrator = db.GetService<IMigrator>();
            // Historical migrations cannot bootstrap an empty DB (pre-existing ControlScope gap).
            // Build the disposable baseline from the exact preceding EF migration model, not from current code.
            var assembly = db.GetService<IMigrationsAssembly>();
            const string previousId = "20261020052530_AddChemicalDirectWriterProtocol";
            var previous = assembly.CreateMigration(assembly.Migrations[previousId], db.Database.ProviderName!);
            var baseline = db.GetService<IModelRuntimeInitializer>().Initialize(previous.TargetModel, designTime: true);
            var operations = db.GetService<IMigrationsModelDiffer>().GetDifferences(null, baseline.GetRelationalModel());
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(operations, baseline))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            foreach (var id in assembly.Migrations.Keys.Where(id => string.CompareOrdinal(id, previousId) <= 0))
                await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.9")));
            var user = new Operator { OperatorCode = "AUDIT-QA", OperatorName = "Audit QA", Username = "audit-qa", Role = "Viewer" };
            db.Add(user); await db.SaveChangesAsync();
            var before = await FingerprintAsync(db);
            // Exercise the exact idempotent SQL form used by the release engine, including a second replay.
            var script = migrator.GenerateScript(previousId, "20261103012224_AddAuditEvents", MigrationsSqlGenerationOptions.Idempotent);
            var batches = System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            for (var replay = 0; replay < 2; replay++)
                foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
                    await db.Database.ExecuteSqlRawAsync(batch);
            Assert.Equal(before, await FingerprintAsync(db));
            await migrator.MigrateAsync("20261020052530_AddChemicalDirectWriterProtocol");
            Assert.Equal(before, await FingerprintAsync(db));
            await db.Database.MigrateAsync();
            Assert.Equal(before, await FingerprintAsync(db));

            var actor = AuditActor.User(user.Id, "Jwt", new string('a', 64));
            var draft = new AuditEventDraft("navigation.page.enter", "Succeeded") { PageKey = "analysis.spc.overall", ClientEventId = Guid.NewGuid() };
            async Task<Guid> Append() { await using var context = Open(); return (await Writer(context).AppendAsync(draft, actor)).EventId; }
            var writes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Append()));
            Assert.Single(writes.Distinct()); Assert.Equal(1, await db.AuditEvents.CountAsync());
            Assert.Equal(DateTimeKind.Utc, (await db.AuditEvents.AsNoTracking().SingleAsync()).OccurredAtUtc.Kind);
            await using (var metadata = db.Database.GetDbConnection().CreateCommand())
            {
                metadata.CommandText = "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditEvents') AND name IS NOT NULL";
                Assert.Equal(8, await metadata.ExecuteScalarAsync());
            }

            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("UPDATE dbo.AuditEvents SET Outcome=N'Denied'"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("DELETE dbo.AuditEvents"));
            await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync("20261020052530_AddChemicalDirectWriterProtocol"));
            Assert.Equal(1, await db.AuditEvents.CountAsync());

            await db.Database.ExecuteSqlRawAsync("CREATE USER AuditRuntimeQa WITHOUT LOGIN; ALTER ROLE SpcAuditWriter ADD MEMBER AuditRuntimeQa;");
            foreach (var sql in new[] { "UPDATE dbo.AuditEvents SET Outcome=N'Denied'", "DELETE dbo.AuditEvents", "ALTER TABLE dbo.AuditEvents ADD Unexpected int NULL" })
            {
                await using var runtime = new SqlConnection(connection.ConnectionString); await runtime.OpenAsync();
                await using var denied = runtime.CreateCommand(); denied.CommandText = "EXECUTE AS USER=N'AuditRuntimeQa'; " + sql;
                await Assert.ThrowsAsync<SqlException>(() => denied.ExecuteNonQueryAsync());
            }
            await using (var runtime = new SqlConnection(connection.ConnectionString))
            {
                await runtime.OpenAsync(); await using var read = runtime.CreateCommand();
                read.CommandText = "EXECUTE AS USER=N'AuditRuntimeQa'; SELECT COUNT_BIG(*) FROM dbo.AuditEvents; REVERT;";
                Assert.Equal(1L, await read.ExecuteScalarAsync());
                var columns = db.Model.FindEntityType(typeof(AuditEvent))!.GetProperties().Where(p => p.Name != "Id").Select(p => p.Name).ToArray();
                var list = string.Join(',', columns.Select(n => "[" + n + "]"));
                var expressions = string.Join(',', columns.Select(n => n == "EventId" ? "NEWID()" : n == "ClientEventId" ? "NULL" : "[" + n + "]"));
                read.CommandText = "EXECUTE AS USER=N'AuditRuntimeQa'; BEGIN TRAN; INSERT dbo.AuditEvents(" + list + ") SELECT " + expressions + " FROM dbo.AuditEvents; SELECT @@ROWCOUNT; ROLLBACK; REVERT;";
                Assert.Equal(1, await read.ExecuteScalarAsync());
            }

            await using (var atomic = Open())
            {
                await using var tx = await atomic.Database.BeginTransactionAsync();
                atomic.Operators.Add(new Operator { OperatorCode = "ROLLBACK", OperatorName = "ROLLBACK" });
                await Writer(atomic).StageAsync(new("security.operator.create", "Succeeded"), actor);
                await atomic.SaveChangesAsync(); await tx.RollbackAsync();
            }
            Assert.Equal(before, await FingerprintAsync(db)); Assert.Equal(1, await db.AuditEvents.CountAsync());

            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.TR_AuditQa_FailInsert ON dbo.AuditEvents AFTER INSERT AS BEGIN THROW 51304, 'isolated forced insert failure',1; END;");
            await using (var atomic = Open())
            {
                atomic.Operators.Add(new Operator { OperatorCode = "FAIL", OperatorName = "FAIL" });
                await Writer(atomic).StageAsync(new("security.operator.create", "Succeeded"), actor);
                await Assert.ThrowsAsync<DbUpdateException>(() => atomic.SaveChangesAsync());
            }
            Assert.Equal(before, await FingerprintAsync(db)); Assert.Equal(1, await db.AuditEvents.CountAsync());
            // These are known synthetic QA events only, removed solely in the disposable database.
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.TR_AuditQa_FailInsert; CREATE USER AuditMaintenanceQa WITHOUT LOGIN; ALTER ROLE SpcAuditMaintenance ADD MEMBER AuditMaintenanceQa; GRANT SELECT,DELETE ON dbo.AuditEvents TO AuditMaintenanceQa;");
            await db.Database.ExecuteSqlRawAsync("EXECUTE AS USER=N'AuditMaintenanceQa'; DELETE dbo.AuditEvents; REVERT;");
            await migrator.MigrateAsync("20261020052530_AddChemicalDirectWriterProtocol"); await db.Database.MigrateAsync();
            Assert.Equal(before, await FingerprintAsync(db));
        }
        finally
        {
            // Name is generated here, never accepts user/configured database names for deletion.
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^SPC_AUDIT_VERIFY_[a-f0-9]{32}$")) throw new InvalidOperationException("Isolation guard failed");
            await using var cleanup = master.CreateCommand();
            cleanup.CommandText = "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "]";
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task<string> FingerprintAsync(AppDbContext db)
    {
        // Exact sorted representations of all non-audit user tables, not a count-only comparison.
        await db.Database.OpenConnectionAsync();
        var connection = db.Database.GetDbConnection();
        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sys.tables WHERE is_ms_shipped=0 AND name NOT IN ('AuditEvents','__EFMigrationsHistory') ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync(); while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }
        var result = new StringBuilder();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM [dbo].[" + table.Replace("]", "]]", StringComparison.Ordinal) + "] FOR JSON PATH, INCLUDE_NULL_VALUES";
            await using var reader = await command.ExecuteReaderAsync(); var chunks = new StringBuilder();
            while (await reader.ReadAsync()) chunks.Append(reader.GetString(0));
            using var json = JsonDocument.Parse(chunks.Length == 0 ? "[]" : chunks.ToString());
            result.Append(table).Append(':').AppendJoin('\n', json.RootElement.EnumerateArray().Select(x => x.GetRawText()).Order(StringComparer.Ordinal));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.ToString())));
    }
}
