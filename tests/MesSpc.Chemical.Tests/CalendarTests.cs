using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class CalendarTests
{
    static async Task<IActionResult> Read(AppDbContext db,int machineId=1,int year=2026,int month=9,string phase="OPEN",string stage="OPEN")
    {
        var method=typeof(ManualMeasurementsV1Controller).GetMethod("GetCalendar");
        Assert.NotNull(method);
        return await (Task<IActionResult>)method.Invoke(new ManualMeasurementsV1Controller(db,null!),[machineId,year,month,phase,stage,CancellationToken.None])!;
    }
    [Fact]
    public async Task Calendar_SeparatesShiftStage_AndMarksLegacyWithoutCountingDeletedOrOtherLines()
    {
        await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Machines.AddRange(new Machine{Id=1,MachineCode="N1",MachineName="N1"},new Machine{Id=2,MachineCode="C1",MachineName="C1"});
        var id=0;
        VariableMeasurement Row(int day,string phase="OPEN",string stage="OPEN",int machine=1,bool deleted=false,bool daily=true,SourceType source=SourceType.Manual)=>new(){Id=++id,MachineId=machine,PartProcessCharacteristicId=id,MeasuredAt=new DateTime(2026,9,day),PortalDailyDate=daily?new DateTime(2026,9,day):null,SamplingPhase=phase,SamplingStage=stage,IsDeleted=deleted,SourceType=source};
        db.VariableMeasurements.AddRange(Row(1),Row(2,stage:"CLOSE"),Row(3,phase:"MIDDLE"),Row(4,stage:"GENERAL"),Row(5,phase:"GENERAL",stage:"GENERAL",daily:false),Row(6,deleted:true),Row(7,machine:2,stage:"GENERAL"),Row(8,stage:"GENERAL",daily:false,source:SourceType.Csv));
        db.VariableMeasurements.Add(new VariableMeasurement{Id=++id,MachineId=1,MeasuredAt=new DateTime(2026,10,1),PortalDailyDate=new DateTime(2026,10,1),SamplingPhase="OPEN",SamplingStage="OPEN",PartProcessCharacteristicId=id});
        await db.SaveChangesAsync();
        using var json=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(db)).Value));
        var days=json.RootElement.GetProperty("days").EnumerateArray().ToDictionary(x=>x.GetProperty("date").GetString()!);
        Assert.Equal(4,days.Count);Assert.True(days["2026-09-01"].GetProperty("hasData").GetBoolean());
        foreach(var date in new[]{"2026-09-04","2026-09-05","2026-09-08"}){Assert.True(days[date].GetProperty("hasHistory").GetBoolean());Assert.False(days[date].GetProperty("hasData").GetBoolean());}
        using var close=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(db,stage:"CLOSE")).Value));
        Assert.Contains(close.RootElement.GetProperty("days").EnumerateArray(),x=>x.GetProperty("date").GetString()=="2026-09-02" && x.GetProperty("hasData").GetBoolean());
        using var other=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(db,machineId:2,stage:"GENERAL")).Value));
        Assert.Single(other.RootElement.GetProperty("days").EnumerateArray());
        Assert.IsType<BadRequestObjectResult>(await Read(db,machineId:2));
        Assert.IsType<BadRequestObjectResult>(await Read(db,month:13));
        Assert.IsType<BadRequestObjectResult>(await Read(db,phase:"BAD"));
        Assert.IsType<NotFoundResult>(await Read(db,machineId:999));
    }
    [Fact]
    public async Task LeapMonth_UsesDailyDateAndExcludesNextMonth()
    {
        await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Machines.Add(new Machine{Id=1,MachineCode="N2",MachineName="N2"});
        db.VariableMeasurements.Add(new VariableMeasurement{Id=1,MachineId=1,MeasuredAt=new DateTime(2024,3,1),PortalDailyDate=new DateTime(2024,2,29),SamplingPhase="OPEN",SamplingStage="OPEN"});await db.SaveChangesAsync();
        using var feb=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(db,year:2024,month:2)).Value));
        Assert.Equal("2024-02-29",Assert.Single(feb.RootElement.GetProperty("days").EnumerateArray()).GetProperty("date").GetString());
        using var mar=JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Read(db,year:2024,month:3)).Value));Assert.Empty(mar.RootElement.GetProperty("days").EnumerateArray());
    }
}
