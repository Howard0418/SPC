using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MesSpc.Api.Tests;

public class ManualMeasurementsChemicalScopeTests
{
    [Fact]
    public async Task Daily_IgnoresMultipleProcessMeasurements_OnChemicalEntryPage()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddMeasurements(ppcId: 2, count: 50);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.GetDaily(1, new DateOnly(2026, 11, 5), "OPEN", samplingStage: "GENERAL");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("exists = False", ok.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Daily_StillRejectsDuplicateChemicalMeasurements()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddMeasurements(ppcId: 1, count: 2);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.GetDaily(1, new DateOnly(2026, 11, 5), "OPEN", samplingStage: "GENERAL");

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("MEASUREMENT_MORNING_DUPLICATE", conflict.Value!.ToString());
    }

    [Fact]
    public async Task Calendar_IgnoresProcessMeasurements()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddMeasurements(ppcId: 2, count: 50);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.GetCalendar(1, 2026, 11, "OPEN", "GENERAL");

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"days\":[]", json);
    }

    [Fact]
    public async Task Daily_ReturnsSavedJudgmentWithExistingValue()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddMeasurements(ppcId: 1, count: 1);
        await fixture.Db.SaveChangesAsync();
        var row = await fixture.Db.VariableMeasurements.SingleAsync();
        fixture.Db.UploadDetails.Add(new UploadDetail { UploadBatchId=row.UploadBatchId, RowNo=1, IsValid=false, CreatedBy="test",
            PayloadJson=JsonSerializer.Serialize(new { action="PortalChemicalSpecificationSavedV1", actor="test", recordedAt=DateTime.UtcNow,
                details=new[]{new PortalSavedSpecificationSnapshot(1,row.Id,row.PartProcessCharacteristicId,row.UploadBatchId,row.MeasuredAt,row.MeasuredValue,null,0d,2d,false,(long?)null)}})});
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.GetDaily(1, new DateOnly(2026, 11, 5), "OPEN", samplingStage: "GENERAL");
        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("\"initialJudgment\":\"OK\"", json);
        Assert.Contains("\"judgmentSource\":\"SavedSpecification\"", json);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public AppDbContext Db { get; }
        public ManualMeasurementsV1Controller Controller { get; }

        private Fixture(SqliteConnection connection, AppDbContext db)
        {
            this.connection = connection;
            Db = db;
            Controller = new ManualMeasurementsV1Controller(db, new SpcService(db, null!, new ConfigurationBuilder().Build()));
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            db.Processes.Add(new Process { Id = 1, ProcessCode = "QE2", ProcessName = "QE2" });
            db.Machines.Add(new Machine { Id = 1, ProcessId = 1, MachineCode = "QE2", MachineName = "QE2" });
            db.QualityCharacteristics.AddRange(
                new QualityCharacteristic { Id = 1, CharacteristicCode = "CHEM", CharacteristicName = "藥液", ControlScope = "CHEM" },
                new QualityCharacteristic { Id = 2, CharacteristicCode = "ETCH", CharacteristicName = "咬蝕量", ControlScope = "PROCESS" });
            db.PartProcessCharacteristics.AddRange(
                new PartProcessCharacteristic { Id = 1, ProcessId = 1, MachineId = 1, CharacteristicId = 1, ControlScope = "CHEM" },
                new PartProcessCharacteristic { Id = 2, ProcessId = 1, MachineId = 1, CharacteristicId = 2, ControlScope = "PROCESS" });
            await db.SaveChangesAsync();
            return new Fixture(connection, db);
        }

        public void AddMeasurements(int ppcId, int count)
        {
            Db.VariableMeasurements.AddRange(Enumerable.Range(1, count).Select(i => new VariableMeasurement
            {
                Id = ppcId * 1000 + i,
                UploadBatchId = Guid.NewGuid(),
                ProcessId = 1,
                MachineId = 1,
                CharacteristicId = ppcId,
                PartProcessCharacteristicId = ppcId,
                SampleNo = i,
                MeasuredValue = i,
                MeasuredAt = new DateTime(2026, 11, 5).AddSeconds(i),
                PortalDailyDate = null,
                SamplingPhase = "OPEN",
                SamplingStage = "GENERAL",
                SourceType = SourceType.Manual
            }));
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
