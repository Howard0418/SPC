using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class PpcOptionalMachineEditTests
{
    private static PartProcessCharacteristic Request(int? machine=247) => new() {ControlScope="PROCESS", ProcessId=195, MachineId=machine, CharacteristicId=870, Unit="μm", ChartTypeId=1046, LSL=0.2, USL=0.4};
    private static async Task<AppDbContext> Setup(SqliteConnection c, bool required=false)
    {
        await c.OpenAsync();var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new(){Id=195,ProcessCode="PT2",ProcessName="粗化線",ControlScope="PROCESS"});
        db.Machines.Add(new(){Id=247,MachineCode="PT2",MachineName="PT2",ProcessId=195});
        db.QualityCharacteristics.Add(new(){Id=870,CharacteristicCode="ETCH",CharacteristicName="A side",ControlScope="PROCESS"});
        db.ControlChartGroups.Add(new(){Id=24,GroupCode="PROC",GroupName="製程",BusinessScopeCode="PROCESS",RequiresMachine=required});
        db.ControlChartTypes.Add(new(){Id=1046,ChartGroupId=24,ChartTypeCode="XBAR_S",ChartTypeName="XBAR S"});
        await db.SaveChangesAsync();
        var a=Request();a.Id=2620;a.LSL=0.19999999999999998;var b=Request(null);b.Id=2610;db.PartProcessCharacteristics.AddRange(a,b);await db.SaveChangesAsync();return db;
    }
    private static PartProcessCharacteristicsController Controller(AppDbContext db)=>new(db){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
    [Fact]public async Task EditLsl_OptionalSelectedMachine_PreservesKeyDespiteNullMachineSibling()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await using var db=await Setup(c);
        var result=await Controller(db).Update(2620,Request());Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();var item=await db.PartProcessCharacteristics.FindAsync(2620);Assert.Equal(247,item!.MachineId);Assert.Equal(0.2,item.LSL);Assert.Equal(2,await db.PartProcessCharacteristics.CountAsync());
    }
    [Fact]public async Task GenuineDuplicate_StillConflict()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await using var db=await Setup(c);
        Assert.IsType<ConflictObjectResult>(await Controller(db).Update(2620,Request(null)));
    }
    [Fact]public async Task OptionalNullMachine_SelfEditSucceeds()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await using var db=await Setup(c);
        Assert.IsType<OkObjectResult>(await Controller(db).Update(2610,Request(null)));
    }
    [Fact]public async Task RequiredMissingMachine_StillBadRequest()
    {
        await using var c=new SqliteConnection("Data Source=:memory:");await using var db=await Setup(c,true);
        Assert.IsType<BadRequestObjectResult>(await Controller(db).Update(2620,Request(null)));
    }
}
