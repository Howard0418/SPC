using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class EtchImportInspectionTests
{
    static EtchReportRequest Report(string line = "PT1") => new(new(2026, 10, 28), line, 1.5m, "fixture",
        (from side in new[] { "A", "B" } from r in Enumerable.Range(1, 5)
         from op in Enumerable.Range(1, EtchReportSyncService.Rule(line).Ops)
         select new EtchPoint(side, r, op, 2m, 1m)).ToList());

    static async Task<AppDbContext> Seed(SqliteConnection connection, string line)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new() { Id = 1, ProcessCode = line, ProcessName = line });
        db.Machines.Add(new() { Id = 1, MachineCode = line, MachineName = line, ProcessId = 1 });
        for (var i = 0; i < 3; i++)
        {
            db.QualityCharacteristics.Add(new() { Id = i + 1, CharacteristicCode = EtchReportSyncService.Codes[i], CharacteristicName = "fixture" });
            db.PartProcessCharacteristics.Add(new() { Id = i + 1, ProcessId = 1, MachineId = 1, CharacteristicId = i + 1,
                ControlScope = "PROCESS", SampleSize = i < 2 ? EtchReportSyncService.Rule(line).Ops * 5 : 1 });
        }
        await db.SaveChangesAsync(); return db;
    }

    [Theory] [InlineData("PT1", 25)] [InlineData("PT2", 50)]
    public async Task InspectAndInsert_CompleteSubgroupAndImmutableRetry(string line, int n)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, line); var svc = new EtchReportSyncService(db); var report = Report(line);
        var preview = await svc.InspectAsync(report);
        Assert.Equal("NEW", preview.State); Assert.False(preview.Verified); Assert.Equal(n, preview.SampleCountPerSide);
        Assert.Empty(await db.UploadBatches.ToListAsync());
        var saved = await svc.ImportOnlyAsync(report, "fixture");
        var ids = await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        var metadata = await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => new { x.Id, x.CreatedBy, x.CreatedAt, x.UpdatedBy, x.UpdatedAt }).ToListAsync();
        (await db.UploadBatches.SingleAsync()).FileHash = "legacy-hash-must-not-trigger-reimport"; await db.SaveChangesAsync();
        var inspection = await svc.InspectAsync(report);
        Assert.Equal("ALREADY_EXISTS", inspection.State); Assert.True(inspection.Verified);
        Assert.Equal(n * 2 + 1, inspection.StoredPointCount); Assert.Equal(n * 2 + 1, inspection.PointCount);
        var retry = await svc.ImportOnlyAsync(report, "another-actor");
        Assert.True(retry.Unchanged); Assert.Equal(saved.BatchId, retry.BatchId);
        Assert.Equal(ids, await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync());
        Assert.Equal(1, await db.UploadBatches.CountAsync());
        Assert.Equal(metadata, await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => new { x.Id, x.CreatedBy, x.CreatedAt, x.UpdatedBy, x.UpdatedAt }).ToListAsync());
    }

    [Fact]
    public void FullPairFingerprint_IsInvariantAndMatchesPortalContractVector()
    {
        var request=Report();
        Assert.Equal("E9431DC4B5FC0855358AC4875402264BF5DB64DEA850DD512E284A897C430A3A",EtchReportSyncService.ContentHash(request));
        Assert.Equal(EtchReportSyncService.ContentHash(request),EtchReportSyncService.ContentHash(request with {Points=request.Points.AsEnumerable().Reverse().ToList(),LineSpeed=1.500000m}));
    }

    [Theory]
    [InlineData("partial")] [InlineData("changed")] [InlineData("duplicate")] [InlineData("archived")]
    [InlineData("date")] [InlineData("machine")] [InlineData("other-mapping")]
    public async Task ExistingConflicts_NeverReimportOrClaimVerified(string kind)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT1"); var svc = new EtchReportSyncService(db); var report = Report();
        await svc.SyncAsync(report, "fixture", false); var row = await db.VariableMeasurements.OrderBy(x => x.Id).FirstAsync();
        if (kind == "partial") db.VariableMeasurements.Remove(row);
        if (kind == "changed") row.MeasuredValue = 7;
        if (kind == "archived") row.IsDeleted = true;
        if (kind == "date") row.MeasuredAt = row.MeasuredAt.AddDays(1);
        if (kind == "machine") row.MachineId = 2;
        if (kind == "other-mapping") row.PartProcessCharacteristicId = 99;
        if (kind == "duplicate") db.VariableMeasurements.Add(new() { PartProcessCharacteristicId = row.PartProcessCharacteristicId,
            MeasuredAt = row.MeasuredAt, MachineId = 1, ProcessId = 1, SourceReference = row.SourceReference, SampleNo = row.SampleNo, MeasuredValue = row.MeasuredValue });
        await db.SaveChangesAsync(); var count = await db.VariableMeasurements.IgnoreQueryFilters().CountAsync();
        var result = await svc.InspectAsync(report);
        Assert.Equal("CONFLICT", result.State); Assert.False(result.Verified);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ImportOnlyAsync(report, "fixture"));
        Assert.Equal(count, await db.VariableMeasurements.IgnoreQueryFilters().CountAsync()); Assert.Equal(1, await db.UploadBatches.CountAsync());
    }

    [Fact]
    public async Task LegacyLineSpeed_IsReportedAndPreserved()
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT1"); var svc = new EtchReportSyncService(db); var report = Report();
        await svc.SyncAsync(report, "fixture", false);
        db.QualityCharacteristics.Add(new() { Id = 4, CharacteristicCode = "ETCH_LINE_SPEED", CharacteristicName = "speed" });
        db.PartProcessCharacteristics.Add(new() { Id = 4, MachineId = 1, ProcessId = 1, CharacteristicId = 4, IsEnabled = false });
        db.VariableMeasurements.Add(new() { PartProcessCharacteristicId = 4, MachineId = 1, ProcessId = 1, MeasuredAt = report.ReportDate,
            SourceReference = "ETCH:2026-10-28:PT1", MeasuredValue = 1.5 }); await db.SaveChangesAsync();
        var result = await svc.InspectAsync(report);
        Assert.True(result.Verified); Assert.Equal(1, result.AdditionalExistingRowCount);
        Assert.True((await svc.ImportOnlyAsync(report, "fixture")).Unchanged); Assert.Equal(52, await db.VariableMeasurements.CountAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SecondMeasurement_ReplacesOnlyFirst_OnceAndWithHistory(bool sameValues)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT1"); var svc = new EtchReportSyncService(db); var first = Report();
        var saved = await svc.ImportGovernedAsync(first, "first");
        var ids = await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        var second = first with { MeasurementNo = 2, Points = first.Points.Select(p => p with { AfterValue = sameValues ? p.AfterValue : .8m }).ToList() };
        Assert.Equal("REPLACE_FIRST", (await svc.InspectAsync(second)).State);
        var replaced = await svc.ImportGovernedAsync(second, "second");
        Assert.False(replaced.Unchanged); Assert.NotEqual(saved.BatchId, replaced.BatchId);
        Assert.Equal(ids, await db.VariableMeasurements.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync());
        Assert.Equal(2, (await svc.InspectAsync(second)).StoredMeasurementNo);
        Assert.True((await svc.ImportGovernedAsync(second, "retry")).Unchanged);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ImportGovernedAsync(first, "old"));
        var changedSecond = second with { Points = second.Points.Select(p => p with { AfterValue = .7m }).ToList() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ImportGovernedAsync(changedSecond, "changed"));
        Assert.Equal(2, await db.UploadBatches.CountAsync());
        var detail = await db.UploadDetails.SingleAsync(x => x.UploadBatchId == replaced.BatchId);
        using var proof = System.Text.Json.JsonDocument.Parse(detail.PayloadJson);
        Assert.Equal(51, proof.RootElement.GetProperty("Before").GetArrayLength());
        Assert.All(proof.RootElement.GetProperty("Before").EnumerateArray().Where(x => x.GetProperty("PartProcessCharacteristicId").GetInt32() != 3), x => Assert.Equal(1d, x.GetProperty("MeasuredValue").GetDouble()));
    }

    [Theory]
    [InlineData("test", "PMR_SPC_TEST", false, true)] [InlineData("production", "PMR_SPC_2026", false, false)]
    [InlineData("production", "PMR_SPC_2026", true, true)] [InlineData("test", "PMR_SPC_2026", true, false)]
    [InlineData("production", "PMR_SPC_TEST", true, false)]
    public void StrictEnvironmentGuard_RequiresExactDatabaseAndProductionOptIn(string environment, string database, bool optIn, bool expected)
        => Assert.Equal(expected, EtchTestImportController.IsEnvironmentAllowed(environment,
            $"Server=172.16.110.16;Database={database};Integrated Security=true", optIn));
}
