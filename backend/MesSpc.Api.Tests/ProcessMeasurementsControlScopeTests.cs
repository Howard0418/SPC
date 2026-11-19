using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ProcessMeasurementsControlScopeTests
{
    [Fact]
    public async Task GetMeasurements_WithChemicalScope_ExcludesProcessEtchMeasurements()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new Process { Id = 1, ProcessCode = "QE2", ProcessName = "QE2" });
        db.QualityCharacteristics.AddRange(
            new QualityCharacteristic { Id = 1, CharacteristicCode = "CHEM", CharacteristicName = "藥液", ControlScope = "CHEM" },
            new QualityCharacteristic { Id = 2, CharacteristicCode = "ETCH", CharacteristicName = "咬蝕量", ControlScope = "PROCESS" });
        db.PartProcessCharacteristics.AddRange(
            new PartProcessCharacteristic { Id = 1, ProcessId = 1, CharacteristicId = 1, ControlScope = "CHEM" },
            new PartProcessCharacteristic { Id = 2, ProcessId = 1, CharacteristicId = 2, ControlScope = "PROCESS" });
        db.VariableMeasurements.AddRange(NewMeasurement(1, 1), NewMeasurement(2, 2));
        await db.SaveChangesAsync();

        var result = await new ProcessesController(db).GetMeasurements(1, controlScope: "CHEM");

        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("rows")[0].GetProperty("PartProcessCharacteristicId").GetInt32());
    }

    private static VariableMeasurement NewMeasurement(long id, int ppcId) => new()
    {
        Id = id, UploadBatchId = Guid.NewGuid(), ProcessId = 1, CharacteristicId = ppcId,
        PartProcessCharacteristicId = ppcId, MeasuredValue = id, MeasuredAt = new DateTime(2026, 11, 9)
    };
}
