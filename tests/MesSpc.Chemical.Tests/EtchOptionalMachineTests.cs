using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class EtchOptionalMachineTests
{
    static EtchReportRequest Report(string line)
    {
        var (ops, _) = EtchReportSyncService.Rule(line);
        return new(new(2026, 10, 28), line, 1.5m, "fixture",
            (from side in new[] { "A", "B" } from repeat in Enumerable.Range(1, 5)
             from op in Enumerable.Range(1, ops) select new EtchPoint(side, repeat, op, 2m, 1.5m)).ToList());
    }

    static async Task<AppDbContext> Seed(SqliteConnection connection, string line)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new() { Id = 1, ProcessCode = line, ProcessName = line });
        db.Machines.Add(new() { Id = 1, MachineCode = line, MachineName = line, ProcessId = 1 });
        for (var i = 0; i < 3; i++)
        {
            db.QualityCharacteristics.Add(new() { Id = i + 1, CharacteristicCode = EtchReportSyncService.Codes[i], CharacteristicName = "fixture" });
            db.PartProcessCharacteristics.Add(new() { Id = i + 1, ProcessId = 1, CharacteristicId = i + 1,
                ControlScope = "PROCESS", SampleSize = i < 2 ? EtchReportSyncService.Rule(line).Ops * 5 : 1 });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Theory]
    [InlineData("PT1", 25)] [InlineData("QE1", 25)] [InlineData("PT2", 50)] [InlineData("QE2", 50)]
    public async Task UniqueProcessMapping_PreservesMastersAndCompleteSubgroups(string line, int n)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, line);
        var svc = new EtchReportSyncService(db); var report = Report(line);
        Assert.False((await svc.PreviewAsync(report)).Unchanged);
        Assert.Empty(await db.VariableMeasurements.ToListAsync());
        var first = await svc.SyncAsync(report, "fixture", false);
        var second = await svc.SyncAsync(report, "fixture", false);
        Assert.True(second.Unchanged); Assert.Equal(first.BatchId, second.BatchId);
        Assert.Equal(n * 2 + 1, await db.VariableMeasurements.CountAsync());
        Assert.Equal(n, await db.VariableMeasurements.CountAsync(x => x.PartProcessCharacteristicId == 1));
        Assert.All(await db.VariableMeasurements.ToListAsync(), x => Assert.Equal(1, x.MachineId));
        Assert.All(await db.PartProcessCharacteristics.ToListAsync(), x => Assert.Null(x.MachineId));
        Assert.Equal(1, await db.UploadBatches.CountAsync());
    }

    [Fact]
    public async Task ExactMachineMapping_WinsOverUnboundMapping()
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT2");
        for (var i = 0; i < 3; i++) db.PartProcessCharacteristics.Add(new() { Id = 10 + i, ProcessId = 1, MachineId = 1,
            CharacteristicId = i + 1, ControlScope = "PROCESS", SampleSize = i < 2 ? 50 : 1 });
        await db.SaveChangesAsync();
        await new EtchReportSyncService(db).SyncAsync(Report("PT2"), "fixture", false);
        Assert.All(await db.VariableMeasurements.ToListAsync(), x => Assert.InRange(x.PartProcessCharacteristicId, 10, 12));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task ExistingBusinessKeyOutsideActiveMapping_IsNeverReimported(bool deleted)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT1");
        db.VariableMeasurements.Add(new() { PartProcessCharacteristicId = 99, MeasuredAt = new(2026, 10, 28),
            SourceReference = "ETCH:2026-10-28:PT1", MeasuredValue = 9, IsDeleted = deleted });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EtchReportSyncService(db).SyncAsync(Report("PT1"), "fixture", false));
        Assert.Equal(1, await db.VariableMeasurements.IgnoreQueryFilters().CountAsync());
        Assert.Empty(await db.UploadBatches.ToListAsync());
    }

    [Theory]
    [InlineData("partial-exact")] [InlineData("duplicate-unbound")] [InlineData("shared-process")]
    [InlineData("wrong-process-code")] [InlineData("wrong-count")] [InlineData("cross-process-exact")]
    public async Task UnsafeMapping_RejectsWithoutAnyWrites(string kind)
    {
        await using var c = new SqliteConnection("Data Source=:memory:;Foreign Keys=False"); await c.OpenAsync();
        await using var db = await Seed(c, "PT1");
        if (kind == "partial-exact") (await db.PartProcessCharacteristics.FindAsync(1))!.MachineId = 1;
        if (kind == "duplicate-unbound") db.PartProcessCharacteristics.Add(new() { Id = 4, ProcessId = 1, CharacteristicId = 1, ControlScope = "PROCESS", SampleSize = 25 });
        if (kind == "shared-process") db.Machines.Add(new() { Id = 2, MachineCode = "OTHER", MachineName = "OTHER", ProcessId = 1 });
        if (kind == "wrong-process-code") (await db.Processes.FindAsync(1))!.ProcessCode = "OTHER";
        if (kind == "wrong-count") (await db.PartProcessCharacteristics.FindAsync(1))!.SampleSize = 20;
        if (kind == "cross-process-exact") foreach (var m in await db.PartProcessCharacteristics.ToListAsync()) { m.MachineId = 1; m.ProcessId = 2; }
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EtchReportSyncService(db).SyncAsync(Report("PT1"), "fixture", false));
        Assert.Empty(await db.VariableMeasurements.ToListAsync()); Assert.Empty(await db.UploadBatches.ToListAsync());
    }
}
