using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class HistoryTests
{
    // Invoke optional query parameters by name so the old implementation fails behavior tests, not compilation.
    static async Task<IActionResult> Read(ProcessesController controller, int skip = 0, DateTime? start = null, DateTime? end = null)
    {
        var method = typeof(ProcessesController).GetMethod("GetMeasurements")!;
        var values = new Dictionary<string, object?> { ["id"] = 1, ["type"] = "variable", ["take"] = 500, ["skip"] = skip, ["start"] = start, ["end"] = end };
        var args = method.GetParameters().Select(p => values.TryGetValue(p.Name!, out var v) ? v : p.DefaultValue).ToArray();
        return await (Task<IActionResult>)method.Invoke(controller, args)!;
    }

    [Fact]
    public async Task MoreThan500_SameTimestamp_NoMissingOrDuplicateRows()
    {
        await using var f = await Fixture.Create();
        var ids = new List<long>();
        foreach (var skip in new[] { 0, 500 })
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(f.Controller, skip)).Value));
            Assert.Equal(605, json.RootElement.GetProperty("total").GetInt32());
            ids.AddRange(json.RootElement.GetProperty("rows").EnumerateArray().Select(x => x.GetProperty("Id").GetInt64()));
        }
        Assert.Equal(605, ids.Count);
        Assert.Equal(605, ids.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 605).Reverse().Select(x => (long)x), ids);
    }

    [Fact]
    public async Task DateFilter_PrecedesLimit_AndTotalMatchesIncludingBoundaries()
    {
        await using var f = await Fixture.Create();
        var day = new DateTime(2026, 8, 1);
        f.Db.VariableMeasurements.AddRange(new VariableMeasurement { Id=1001,ProcessId=1,MeasuredAt=day },
            new VariableMeasurement { Id=1002,ProcessId=1,MeasuredAt=day.AddDays(1).AddMilliseconds(-1) });
        await f.Db.SaveChangesAsync();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(f.Controller, start:day,end:day.AddDays(1).AddMilliseconds(-1))).Value));
        Assert.Equal(2,json.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(2,json.RootElement.GetProperty("rows").GetArrayLength());
        using var empty = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(f.Controller,start:day.AddYears(-1),end:day.AddYears(-1))).Value));
        Assert.Equal(0,empty.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task InvalidRangeOrOffsetRejected_DefaultContractRetained()
    {
        await using var f = await Fixture.Create();
        Assert.IsType<BadRequestObjectResult>(await Read(f.Controller,-1));
        Assert.IsType<BadRequestObjectResult>(await Read(f.Controller,start:DateTime.Today,end:DateTime.Today.AddDays(-1)));
        using var json=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await f.Controller.GetMeasurements(1)).Value));
        Assert.Equal(200,json.RootElement.GetProperty("rows").GetArrayLength());
        Assert.Equal(605,json.RootElement.GetProperty("total").GetInt32());
    }

    sealed class Fixture : IAsyncDisposable
    {
        readonly SqliteConnection connection;
        public AppDbContext Db { get; }
        public ProcessesController Controller => new(Db);
        Fixture(SqliteConnection c) { connection=c; Db=new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options); }
        public static async Task<Fixture> Create()
        {
            var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
            var f=new Fixture(c);await f.Db.Database.EnsureCreatedAsync();
            f.Db.Processes.Add(new Process { Id=1,ProcessCode="N2",ProcessName="N2" });
            f.Db.VariableMeasurements.AddRange(Enumerable.Range(1,605).Select(id=>new VariableMeasurement { Id=id,ProcessId=1,MeasuredAt=new DateTime(2026,9,1),MeasuredValue=id }));
            f.Db.VariableMeasurements.Add(new VariableMeasurement { Id=999,ProcessId=2,MeasuredAt=new DateTime(2026,9,1) });
            await f.Db.SaveChangesAsync();return f;
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
}
