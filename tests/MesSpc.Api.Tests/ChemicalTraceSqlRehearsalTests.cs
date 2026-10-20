using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MesSpc.Api.Tests;

public class ChemicalTraceSqlRehearsalTests
{
#if CHEMICAL_TRACE_SQL_TESTS
    private static string Connection => Environment.GetEnvironmentVariable("SPC_FORMULA_SCHEMA_TEST_CONNECTION")
        ?? throw new InvalidOperationException("Explicit isolated connection required.");
    private static AppDbContext Open()
    {
        var cs = new SqlConnectionStringBuilder(Connection);
        if (!cs.InitialCatalog.StartsWith("SPC_FORMULA_SCHEMA_VERIFY_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing a non-isolated database.");
        return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Connection).Options);
    }
    private static async Task<object?> Scalar(AppDbContext db, string sql)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = sql;
        return await cmd.ExecuteScalarAsync();
    }
    private static async Task RejectSql(AppDbContext db, FormattableString sql, int number)
    {
        Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync(sql);
        await action.Should().ThrowAsync<SqlException>().Where(x => x.Number == number);
    }
    private static Task<object?> Fingerprint(AppDbContext db) => Scalar(db, """
        SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(
          (SELECT Id,PartProcessCharacteristicId,MeasuredAt,MeasuredValue,RecheckValue FROM VariableMeasurements ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),
          (SELECT Id,USL,LSL,UCL,CL,LCL,ChemicalAnalysisConfigJson FROM PartProcessCharacteristics ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES),
          (SELECT Id,ConfigJson FROM ChemicalAnalysisFormulaVersions ORDER BY Id FOR JSON PATH),
          (SELECT Id,CellsJson FROM ChemicalFTableVersionHistories ORDER BY Id FOR JSON PATH),
          (SELECT Id,NumericValue,TextValue FROM ChemicalFTableCells ORDER BY Id FOR JSON PATH,INCLUDE_NULL_VALUES))),2)
        """);

    [Fact]
    public async Task IsolatedSqlUpDownUpIntegrityConcurrencyAndFailClosedPermissions()
    {
        await using var db = Open();
        (await Scalar(db, "SELECT CAST(value AS nvarchar(100)) FROM sys.extended_properties WHERE class=0 AND name=N'SPC.FormulaTrace.Isolation'"))
            .Should().Be("TASK-002");
        var before = await Fingerprint(db);
        var oldMigration = "20261019235606_AddSpecificationLimitVersions";
        await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
        (await Fingerprint(db)).Should().Be(before);
        (await db.ChemicalFormulaPolicies.CountAsync()).Should().Be(0);
        await db.GetService<IMigrator>().MigrateAsync(oldMigration);
        (await Scalar(db, "SELECT COUNT(*) FROM sys.tables WHERE name='ChemicalCalculationReceipts'")).Should().Be(0);
        (await Fingerprint(db)).Should().Be(before);
        await db.Database.MigrateAsync(); (await Fingerprint(db)).Should().Be(before);

        var token = Guid.NewGuid().ToString("N")[..8];
        var process = new Process { ProcessCode = "VERIFY-" + token };
        var characteristic = new QualityCharacteristic { CharacteristicCode = "VERIFY-" + token };
        var part = new Part { PartNo = "VERIFY-" + token };
        var batch = new UploadBatch();
        db.AddRange(process, characteristic, part, batch); await db.SaveChangesAsync();
        var ppc = new PartProcessCharacteristic { ControlScope = "CHEM", ProcessId = process.Id, CharacteristicId = characteristic.Id };
        var other = new PartProcessCharacteristic { ControlScope = "CHEM", ProcessId = process.Id, CharacteristicId = characteristic.Id, Unit = "OTHER" };
        db.AddRange(ppc, other); await db.SaveChangesAsync();
        var formula = new ChemicalAnalysisFormulaVersion { PartProcessCharacteristicId = ppc.Id, VersionNo = 1, ConfigJson = "{}" };
        var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = ppc.Id, RevisionNo = 1 };
        var fHistory = new ChemicalFTableVersionHistory { VersionNo = 999991, VersionCode = "F-VERIFY", CellsJson = "[]" };
        db.AddRange(formula, revision, fHistory); await db.SaveChangesAsync();
        var period = new ChemicalFormulaEffectivity { PartProcessCharacteristicId = ppc.Id, FormulaVersionId = formula.Id, ScheduleRevisionId = revision.Id,
            FTableHistoryId = fHistory.Id, EffectiveFromUtc = new(2026, 1, 1), EffectiveUntilUtcExclusive = new(2026, 11, 1) };
        db.Add(period); await db.SaveChangesAsync();
        await RejectSql(db, $"INSERT ChemicalFormulaEffectivities(PartProcessCharacteristicId,ScheduleRevisionId,FormulaVersionId,EffectiveFromUtc) VALUES({ppc.Id},{revision.Id},{formula.Id},'2026-02-01')", 51102);
        await RejectSql(db, $"INSERT ChemicalFormulaEffectivities(PartProcessCharacteristicId,ScheduleRevisionId,FormulaVersionId,EffectiveFromUtc) VALUES({other.Id},{revision.Id},{formula.Id},'2026-12-01')", 547);
        var adjacent = new ChemicalFormulaEffectivity { PartProcessCharacteristicId = ppc.Id, FormulaVersionId = formula.Id,
            ScheduleRevisionId = revision.Id, EffectiveFromUtc = new(2026, 11, 1) };
        db.Add(adjacent); await db.SaveChangesAsync();
        revision.PublishedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync();
        await RejectSql(db, $"UPDATE ChemicalFormulaEffectivities SET EffectiveFromUtc='2026-01-02' WHERE Id={period.Id}", 51101);
        await RejectSql(db, $"UPDATE ChemicalAnalysisFormulaVersions SET ConfigJson='changed' WHERE Id={formula.Id}", 51104);
        await RejectSql(db, $"UPDATE ChemicalFTableVersionHistories SET CellsJson='changed' WHERE Id={fHistory.Id}", 51105);
        await RejectSql(db, $"UPDATE ChemicalFormulaScheduleRevisions SET Reason='changed' WHERE Id={revision.Id}", 51100);
        db.Add(new ChemicalFormulaPolicy { PartProcessCharacteristicId = ppc.Id, ActiveScheduleRevisionId = revision.Id, IsEnabled = true });
        await db.SaveChangesAsync();
        var receipt = new ChemicalCalculationReceipt { Id = Guid.NewGuid(), PartProcessCharacteristicId = ppc.Id, EffectivityId = period.Id,
            MeasuredAtUtc = new(2026, 10, 20), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5), InputHash = new('b', 64), SourceKeyHash = new('a', 64), PayloadJson = "{}" };
        db.Add(receipt); await db.SaveChangesAsync();
        await RejectSql(db, $"UPDATE ChemicalCalculationReceipts SET PayloadJson='changed' WHERE Id={receipt.Id}", 51106);
        await RejectSql(db, $"INSERT ChemicalCalculationReceipts(Id,PartProcessCharacteristicId,EffectivityId,MeasuredAtUtc,CreatedAtUtc,ExpiresAtUtc,Actor,InputHash,SourceKeyHash,EngineVersion,PrecisionPolicy,PayloadJson) SELECT NEWID(),PartProcessCharacteristicId,EffectivityId,'2028-01-01',CreatedAtUtc,ExpiresAtUtc,Actor,InputHash,SourceKeyHash,EngineVersion,PrecisionPolicy,PayloadJson FROM ChemicalCalculationReceipts WHERE Id={receipt.Id}", 51107);
        var vm = new VariableMeasurement { UploadBatchId = batch.UploadBatchId, PartId = part.Id, ProcessId = process.Id,
            CharacteristicId = characteristic.Id, PartProcessCharacteristicId = ppc.Id, MeasuredAt = receipt.MeasuredAtUtc,
            ChemicalReceiptId = receipt.Id, ChemicalSourceKeyHash = receipt.SourceKeyHash, MeasuredValue = 10 };
        db.Add(vm); await db.SaveChangesAsync();
        var trace = new ChemicalCalculationTrace { VariableMeasurementId = vm.Id, ReceiptId = receipt.Id, RawResultCanonical = "10.1234567890123456789012345678", StoredResultCanonical = "10.12" };
        var job = new ChemicalStatisticsJob { Id = Guid.NewGuid(), VariableMeasurementId = vm.Id, ReceiptId = receipt.Id };
        db.AddRange(trace, job); await db.SaveChangesAsync();
        await RejectSql(db, $"DELETE FROM ChemicalCalculationTraces WHERE Id={trace.Id}", 51108);
        await RejectSql(db, $"UPDATE VariableMeasurements SET ChemicalReceiptId=NULL WHERE Id={vm.Id}", 51110);
        await RejectSql(db, $"UPDATE VariableMeasurements SET ChemicalSourceKeyHash={new string('c',64)} WHERE Id={vm.Id}", 51111);
        await RejectSql(db, $"UPDATE VariableMeasurements SET MeasuredValue=999 WHERE Id={vm.Id}", 51115);
        job.Status = "Processing"; job.Attempts++; await db.SaveChangesAsync();
        var traceCount = await db.ChemicalCalculationTraces.CountAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.Add(new ChemicalCalculationTrace { VariableMeasurementId = vm.Id, ReceiptId = receipt.Id, Phase = "Recheck", VersionNo = 1 });
            await db.SaveChangesAsync(); await tx.RollbackAsync();
        }
        (await db.ChemicalCalculationTraces.AsNoTracking().CountAsync()).Should().Be(traceCount);
        db.ChangeTracker.Clear();
        var receiptCount = await db.ChemicalCalculationReceipts.CountAsync();
        var vmCount = await db.VariableMeasurements.CountAsync();
        var jobCount = await db.ChemicalStatisticsJobs.CountAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var freshReceipt = new ChemicalCalculationReceipt { Id = Guid.NewGuid(), PartProcessCharacteristicId = ppc.Id,
                EffectivityId = period.Id, MeasuredAtUtc = receipt.MeasuredAtUtc, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                InputHash = new('b',64), SourceKeyHash = new('d',64) };
            db.Add(freshReceipt); await db.SaveChangesAsync();
            var freshVm = new VariableMeasurement { UploadBatchId = batch.UploadBatchId, PartId = part.Id,
                ProcessId = process.Id, CharacteristicId = characteristic.Id, PartProcessCharacteristicId = ppc.Id,
                ChemicalReceiptId = freshReceipt.Id, ChemicalSourceKeyHash = freshReceipt.SourceKeyHash, MeasuredAt = receipt.MeasuredAtUtc };
            db.Add(freshVm); await db.SaveChangesAsync();
            db.AddRange(new ChemicalCalculationTrace { VariableMeasurementId = freshVm.Id, ReceiptId = freshReceipt.Id },
                new ChemicalStatisticsJob { Id = Guid.NewGuid(), VariableMeasurementId = freshVm.Id, ReceiptId = freshReceipt.Id });
            await db.SaveChangesAsync();
            db.Add(new ChemicalStatisticsJob { Id = Guid.NewGuid(), VariableMeasurementId = freshVm.Id, ReceiptId = freshReceipt.Id });
            Func<Task> duplicate = () => db.SaveChangesAsync();
            var error = await duplicate.Should().ThrowAsync<DbUpdateException>();
            error.Which.InnerException.Should().BeOfType<SqlException>().Which.Number.Should().BeOneOf(2601, 2627);
            await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        (await db.ChemicalCalculationReceipts.CountAsync()).Should().Be(receiptCount);
        (await db.VariableMeasurements.CountAsync()).Should().Be(vmCount);
        (await db.ChemicalCalculationTraces.CountAsync()).Should().Be(traceCount);
        (await db.ChemicalStatisticsJobs.CountAsync()).Should().Be(jobCount);
        var legacyVm = new VariableMeasurement { UploadBatchId = batch.UploadBatchId, PartId = part.Id,
            ProcessId = process.Id, CharacteristicId = characteristic.Id, PartProcessCharacteristicId = other.Id, MeasuredValue = 1 };
        db.Add(legacyVm); await db.SaveChangesAsync();
        legacyVm.ChemicalReceiptId.Should().BeNull();

        // Two concurrent writes claiming the same per-PPC revision must not both succeed.
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var separate = Open();
            separate.Add(new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = ppc.Id, RevisionNo = 99 });
            try { await separate.SaveChangesAsync(); return true; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException s && (s.Number == 2601 || s.Number == 2627)) { return false; }
        }));
        outcomes.Count(x => x).Should().Be(1);
        var concurrentRevision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = ppc.Id, RevisionNo = 100 };
        db.Add(concurrentRevision); await db.SaveChangesAsync();
        var periods = await Task.WhenAll(Enumerable.Range(0, 2).Select(async n =>
        {
            await using var separate = Open();
            separate.Add(new ChemicalFormulaEffectivity { PartProcessCharacteristicId = ppc.Id, ScheduleRevisionId = concurrentRevision.Id,
                FormulaVersionId = formula.Id, EffectiveFromUtc = new(2027, n + 1, 1) });
            try { await separate.SaveChangesAsync(); return true; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException s && (s.Number == 51102 || s.Number == 1205)) { return false; }
        }));
        periods.Count(x => x).Should().Be(1);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE USER FormulaSchemaProbe WITHOUT LOGIN;
            GRANT INSERT ON VariableMeasurements TO FormulaSchemaProbe;
            ALTER ROLE SpcChemicalDirectWriter ADD MEMBER FormulaSchemaProbe;
            """);
        (await Scalar(db, """
            EXECUTE AS USER='FormulaSchemaProbe';
            DECLARE @n int=0;
            BEGIN TRY INSERT VariableMeasurements DEFAULT VALUES; END TRY BEGIN CATCH SET @n=ERROR_NUMBER(); END CATCH;
            REVERT; SELECT @n;
            """)).Should().Be(229);
        await db.Database.ExecuteSqlRawAsync("ALTER ROLE SpcChemicalDirectWriter DROP MEMBER FormulaSchemaProbe; DROP USER FormulaSchemaProbe;");
    }
#else
    [Fact(Skip = "SQL isolation rehearsal is opt-in; use Invoke-ChemicalTraceSchemaRehearsal.ps1.")]
    public void RequiresExplicitIsolatedSqlDatabase() { }
#endif
}
