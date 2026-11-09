using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public sealed class LimitBatchServiceTests
{
    private static async Task<(SqliteConnection Connection, AppDbContext Db, LimitBatchService Service)> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var group = new ControlChartGroup { GroupCode = "VARIABLE", GroupName = "Variable" };
        db.ControlChartGroups.Add(group);
        await db.SaveChangesAsync();
        var chartType = new ControlChartType
        {
            ChartGroupId = group.Id,
            ChartTypeCode = "I_MR",
            ChartTypeName = "I-MR",
            DataCategory = "Variable",
            RequiredSampleSize = 1
        };
        var process = new Process { ProcessCode = "P1", ProcessName = "Process 1" };
        var characteristic = new QualityCharacteristic { CharacteristicCode = "Q1", CharacteristicName = "Quality 1" };
        db.AddRange(chartType, process, characteristic);
        await db.SaveChangesAsync();
        db.PartProcessCharacteristics.AddRange(
            new PartProcessCharacteristic
            {
                Id = 1, ProcessId = process.Id, CharacteristicId = characteristic.Id, ChartTypeId = chartType.Id,
                FormulaConfigJson = "{\"keep\":1}", LSL = 0, TargetValue = 5, USL = 10
            },
            new PartProcessCharacteristic
            {
                Id = 2, ProcessId = process.Id, CharacteristicId = characteristic.Id, ChartTypeId = chartType.Id,
                FormulaConfigJson = "{\"keep\":2}", LSL = 10, TargetValue = 15, USL = 20
            });
        await db.SaveChangesAsync();
        return (connection, db, new LimitBatchService(db, new EphemeralDataProtectionProvider()));
    }

    [Fact]
    public async Task Preview_InvalidRowDoesNotWriteOrIssueApplyToken()
    {
        var (connection, db, service) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            var preview = await service.PreviewAsync(new LimitBatchPreviewRequest([
                new(1, new(10, 5, 1, null, null, null))
            ]), "editor", CancellationToken.None);

            Assert.False(preview.IsValid);
            Assert.Null(preview.ApplyToken);
            Assert.Contains(preview.Items.Single().Errors, error => error.Code == "SPEC_ORDER");
            Assert.Empty(await db.LimitChangeBatches.ToListAsync());
            Assert.Equal(0, (await db.PartProcessCharacteristics.FindAsync(1))!.LSL);
        }
    }

    [Fact]
    public async Task Apply_WithoutPreviewTokenIsRejectedWithoutWriting()
    {
        var (connection, db, service) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            var error = await Assert.ThrowsAsync<LimitBatchConflictException>(() =>
                service.ApplyAsync(new(" "), "editor", CancellationToken.None));

            Assert.Contains("重新預檢", error.Message);
            Assert.Empty(await db.LimitChangeBatches.ToListAsync());
        }
    }

    [Fact]
    public async Task Preview_TextWithUnitReturnsActionableFieldError()
    {
        var (connection, db, service) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            var preview = await service.PreviewAsync(new LimitBatchPreviewRequest([
                new(1, new("95%", 5, 10, null, null, null))
            ]), "editor", CancellationToken.None);

            var error = Assert.Single(preview.Items.Single().Errors);
            Assert.Equal("LSL", error.Field);
            Assert.Equal("INVALID_NUMBER", error.Code);
            Assert.Contains("移除單位", error.Suggestion);
            Assert.Null(preview.ApplyToken);
        }
    }

    [Fact]
    public async Task Apply_UpdatesOnlySixLimitsAndCreatesImmutableBatchDetails()
    {
        var (connection, db, service) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            var versionsBefore = await db.SpecificationLimitVersions.CountAsync();
            var preview = await service.PreviewAsync(new LimitBatchPreviewRequest([
                new(1, new(1, 6, 11, 2, 6, 10)),
                new(2, new(9, 15, 21, null, null, null))
            ]), "editor", CancellationToken.None);

            var applied = await service.ApplyAsync(new(preview.ApplyToken!), "editor", CancellationToken.None);
            db.ChangeTracker.Clear();

            Assert.Equal(2, applied.UpdatedCount);
            var first = await db.PartProcessCharacteristics.FindAsync(1);
            Assert.Equal("{\"keep\":1}", first!.FormulaConfigJson);
            Assert.Equal(1, first.LSL);
            Assert.Equal(6, first.TargetValue);
            Assert.Equal(11, first.USL);
            Assert.Equal(2, first.LCL);
            Assert.Equal(6, first.CL);
            Assert.Equal(10, first.UCL);
            var batch = await db.LimitChangeBatches.Include(x => x.Items).SingleAsync();
            Assert.Equal("editor", batch.ChangedBy);
            Assert.Equal(2, batch.Items.Count);
            Assert.All(batch.Items, item => Assert.Equal(batch.Id, item.LimitChangeBatchId));
            Assert.Equal(versionsBefore + 2, await db.SpecificationLimitVersions.CountAsync());
        }
    }

    [Fact]
    public async Task Apply_WhenOneLimitIsStaleRejectsWholeBatch()
    {
        var (connection, db, service) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            var preview = await service.PreviewAsync(new LimitBatchPreviewRequest([
                new(1, new(1, 6, 11, null, null, null)),
                new(2, new(9, 15, 21, null, null, null))
            ]), "editor", CancellationToken.None);
            var second = await db.PartProcessCharacteristics.FindAsync(2);
            second!.USL = 22;
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<LimitBatchConflictException>(() =>
                service.ApplyAsync(new(preview.ApplyToken!), "editor", CancellationToken.None));
            db.ChangeTracker.Clear();

            Assert.Equal(0, (await db.PartProcessCharacteristics.FindAsync(1))!.LSL);
            Assert.Equal(22, (await db.PartProcessCharacteristics.FindAsync(2))!.USL);
            Assert.Empty(await db.LimitChangeBatches.ToListAsync());
        }
    }
}
