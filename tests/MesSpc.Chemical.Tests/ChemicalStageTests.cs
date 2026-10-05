using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class ChemicalStageTests
{
    [Fact]
    public async Task Upgrade_ThreeDecimalDailyRoundtrip_PreservesRecheckAndStage()
    {
        await using var f=await Fixture.Create("N2");
        var payload=f.Payload("MIDDLE","CLOSE",10.004);
        payload["RecheckValue"]="9.999";
        var batch=await f.Upload.CreateVariableBatchAsync([payload],"PortalDaily","compatibility",null);
        Assert.Equal(0,batch.ErrorRows);
        await f.Upload.ConfirmAsync(batch.UploadBatchId);
        var row=await f.Db.VariableMeasurements.SingleAsync();
        Assert.Equal(10.004,row.MeasuredValue,8);Assert.Equal(9.999,row.RecheckValue!.Value,8);
        Assert.Equal("MIDDLE",row.SamplingPhase);Assert.Equal("CLOSE",row.SamplingStage);
        var id=row.Id;
        var json=JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"MIDDLE",samplingStage:"CLOSE")).Value);
        Assert.Contains("10.004",json);Assert.Contains("9.999",json);
        payload["MeasuredValue"]="10.003";
        batch=await f.Upload.CreateVariableBatchAsync([payload],"PortalDaily","compatibility",null);
        await f.Upload.ConfirmAsync(batch.UploadBatchId);
        var updated=await f.Db.VariableMeasurements.SingleAsync();
        Assert.Equal(id,updated.Id);Assert.Equal(10.003,updated.MeasuredValue,8);
    }

    [Fact]
    public async Task MissingDailyDate_UsesMeasurementDate_AndUpdatesOriginalId()
    {
        await using var f=await Fixture.Create("C1");await f.Save("OPEN","GENERAL",20);
        var old=await f.Db.VariableMeasurements.SingleAsync();var id=old.Id;
        old.PortalDailyDate=null;old.SamplingPhase="GENERAL";old.MeasuredAt=new DateTime(2026,9,17,23,59,59);await f.Db.SaveChangesAsync();
        var daily=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN"));
        Assert.Contains("\"exists\":true",JsonSerializer.Serialize(daily.Value));
        var next=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,18),"OPEN"));
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(next.Value));
        var middle=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"MIDDLE"));
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(middle.Value));
        var calendar=Assert.IsType<OkObjectResult>(await f.Controller.GetCalendar(1,2026,9,"OPEN","GENERAL"));
        Assert.Contains("\"hasData\":true",JsonSerializer.Serialize(calendar.Value));
        Assert.Contains("\"hasHistory\":false",JsonSerializer.Serialize(calendar.Value));
        var preview=await f.Upload.CreateVariableBatchAsync([f.Payload("OPEN","GENERAL",25)],"PortalDaily","test",null);
        var detail=await f.Db.UploadDetails.SingleAsync(x=>x.UploadBatchId==preview.UploadBatchId);
        using var payload=JsonDocument.Parse(detail.PayloadJson);
        Assert.Equal("正式資料已存在",payload.RootElement.GetProperty("DuplicateStatus").GetString());
        await f.Upload.ConfirmAsync(preview.UploadBatchId);
        var saved=await f.Db.VariableMeasurements.SingleAsync();Assert.Equal(id,saved.Id);Assert.Equal(25,saved.MeasuredValue);
        Assert.Equal(new DateTime(2026,9,17),saved.PortalDailyDate);
    }

    [Fact]
    public async Task MissingDateAndDailyDuplicate_RejectReadAndWrite()
    {
        await using var f=await Fixture.Create("C1");await f.Save("OPEN","GENERAL",20);
        var row=await f.Db.VariableMeasurements.AsNoTracking().SingleAsync();row.Id=0;row.PortalDailyDate=null;
        row.MeasuredAt=new DateTime(2026,9,17,12,0,0);f.Db.VariableMeasurements.Add(row);await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN"));
        var batch=await f.Upload.CreateVariableBatchAsync([f.Payload("OPEN","GENERAL",30)],"PortalDaily","test",null);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Upload.ConfirmAsync(batch.UploadBatchId));
        Assert.Equal(2,await f.Db.VariableMeasurements.CountAsync());Assert.All(await f.Db.VariableMeasurements.ToListAsync(),x=>Assert.Equal(20,x.MeasuredValue));
    }

    [Fact]
    public async Task Fallback_PreservesDailyDatePriority_SourceAndLegacyCloseBoundaries()
    {
        await using var f=await Fixture.Create("N2");await f.Save("OPEN","OPEN",20);
        var row=await f.Db.VariableMeasurements.SingleAsync();row.MeasuredAt=new DateTime(2026,9,18);await f.Db.SaveChangesAsync();
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,18),"OPEN",samplingStage:"OPEN")).Value));
        row.PortalDailyDate=null;row.SourceType=MesSpc.Api.Domain.Enums.SourceType.Csv;await f.Db.SaveChangesAsync();
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,18),"OPEN",samplingStage:"OPEN")).Value));
        row.SourceType=MesSpc.Api.Domain.Enums.SourceType.Manual;row.SamplingPhase="CLOSE";row.SamplingStage="GENERAL";await f.Db.SaveChangesAsync();
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,18),"OPEN",samplingStage:"CLOSE")).Value));
    }

    [Theory]
    [InlineData("GENERAL")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LegacyUnspecifiedShift_IsMorning_LoadsAndUpdatesOriginalId(string legacyPhase)
    {
        await using var f=await Fixture.Create("C1");
        await f.Save("OPEN","GENERAL",20);
        var old=await f.Db.VariableMeasurements.SingleAsync();var id=old.Id;
        old.SamplingPhase=legacyPhase;await f.Db.SaveChangesAsync();
        var result=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN"));
        Assert.Contains("\"exists\":true",JsonSerializer.Serialize(result.Value));
        var middle=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"MIDDLE"));
        Assert.Contains("\"exists\":false",JsonSerializer.Serialize(middle.Value));
        var calendar=Assert.IsType<OkObjectResult>(await f.Controller.GetCalendar(1,2026,9,"OPEN","GENERAL"));
        Assert.Contains("\"hasData\":true",JsonSerializer.Serialize(calendar.Value));
        var middleCalendar=Assert.IsType<OkObjectResult>(await f.Controller.GetCalendar(1,2026,9,"MIDDLE","GENERAL"));
        Assert.Contains("\"days\":[]",JsonSerializer.Serialize(middleCalendar.Value));
        await f.Save("OPEN","GENERAL",25);
        Assert.Equal(id,(await f.Db.VariableMeasurements.SingleAsync()).Id);
        Assert.Equal(25,(await f.Db.VariableMeasurements.SingleAsync()).MeasuredValue);
    }

    [Fact]
    public async Task ConflictingLegacyAndMorning_AreNotSilentlyCombined()
    {
        await using var f=await Fixture.Create("C1");await f.Save("OPEN","GENERAL",20);
        var row=await f.Db.VariableMeasurements.AsNoTracking().SingleAsync();row.Id=0;row.SamplingPhase="GENERAL";f.Db.VariableMeasurements.Add(row);await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN"));
        var batch=await f.Upload.CreateVariableBatchAsync([f.Payload("OPEN","GENERAL",30)],"PortalDaily","test",null);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Upload.ConfirmAsync(batch.UploadBatchId));
        Assert.Equal(2,await f.Db.VariableMeasurements.CountAsync());
        Assert.All(await f.Db.VariableMeasurements.ToListAsync(),x=>Assert.Equal(20,x.MeasuredValue));
    }

    [Theory]
    [InlineData("N1","OPEN",true)] [InlineData("N2","CLOSE",true)]
    [InlineData(" n1 "," close ",true)] [InlineData("N10","OPEN",false)]
    [InlineData("ST1","CLOSE",false)] [InlineData("N1","MIDDLE",false)]
    [InlineData("N1",null,true)] [InlineData("ST1","GENERAL",true)]
    public void StagePolicy_OnlyN1N2(string machine,string? stage,bool valid)
        => Assert.Equal(valid,ChemicalSamplingStage.TryNormalize(machine,stage,out _));

    [Theory] [InlineData("N1")] [InlineData("N2")]
    public async Task FourCombinations_AreIndependent_UpsertAndLegacyPreserved(string code)
    {
        await using var f=await Fixture.Create(code);
        await f.Save("OPEN",null,9);
        var expected=10d;
        foreach(var shift in new[]{"OPEN","MIDDLE"}) foreach(var stage in new[]{"OPEN","CLOSE"})
        {
            await f.Save(shift,stage,expected++);
            var result=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),shift,samplingStage:stage));
            using var json=JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            Assert.Single(json.RootElement.GetProperty("rows").EnumerateArray());
            Assert.Equal(expected-1,json.RootElement.GetProperty("rows")[0].GetProperty("MeasuredValue").GetDouble());
        }
        Assert.Equal(5,await f.Db.VariableMeasurements.CountAsync());
        await f.Save("MIDDLE","CLOSE",99);
        Assert.Equal(5,await f.Db.VariableMeasurements.CountAsync());
        Assert.Equal(99,(await f.Db.VariableMeasurements.SingleAsync(x=>x.SamplingPhase=="MIDDLE"&&x.SamplingStage=="CLOSE")).MeasuredValue);
        Assert.Equal(9,(await f.Db.VariableMeasurements.SingleAsync(x=>x.SamplingStage=="GENERAL")).MeasuredValue);
        var legacy=Assert.IsType<OkObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN"));
        Assert.Contains("\"MeasuredValue\":9",JsonSerializer.Serialize(legacy.Value));
        var current=await f.Db.VariableMeasurements.AsNoTracking().FirstAsync(x=>x.SamplingStage=="CLOSE");
        current.Id=0;f.Db.VariableMeasurements.Add(current);
        await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync()); // relational unique key still blocks true duplicates
    }

    [Fact]
    public async Task OtherLine_TwoShifts_RejectStageDuringPreviewAndRead()
    {
        await using var f=await Fixture.Create("ST1");
        await f.Save("OPEN","GENERAL",20);await f.Save("MIDDLE","GENERAL",30);
        Assert.Equal(2,await f.Db.VariableMeasurements.CountAsync());
        Assert.IsType<BadRequestObjectResult>(await f.Controller.GetDaily(1,new DateOnly(2026,9,17),"OPEN",samplingStage:"OPEN"));
        var invalid=await f.Upload.CreateVariableBatchAsync([f.Payload("OPEN","CLOSE",1)],"PortalDaily","test",null);
        Assert.Equal(1,invalid.ErrorRows);
        Assert.Contains(await f.Db.UploadErrors.ToListAsync(),x=>x.FieldName=="SamplingStage");
    }

    [Fact]
    public void Migration_OnlyAddsStageAndReplacesDailyUniqueIndex()
    {
        var migration=new MesSpc.Api.Migrations.AddChemicalSamplingStage();
        var operations=migration.UpOperations;
        Assert.Equal(3,operations.Count);
        var column=Assert.Single(operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>());
        Assert.Equal("VariableMeasurements",column.Table);Assert.Equal("SamplingStage",column.Name);Assert.Equal("GENERAL",column.DefaultValue);
        var index=Assert.Single(operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>());
        Assert.True(index.IsUnique);Assert.Equal(new[]{"PartProcessCharacteristicId","PortalDailyDate","SamplingPhase","SamplingStage"},index.Columns);
    }

    sealed class Fixture : IAsyncDisposable
    {
        readonly SqliteConnection connection;
        readonly string code;
        public AppDbContext Db {get;}
        public UploadService Upload {get;}
        public ManualMeasurementsV1Controller Controller {get;}
        Fixture(SqliteConnection c,AppDbContext db,string machine)
        {connection=c;Db=db;code=machine;var spc=new SpcService(db,new FakeEmail(),new ConfigurationBuilder().Build());Upload=new(db,spc);Controller=new(db,spc);}
        public static async Task<Fixture> Create(string code)
        {
            var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
            var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);
            await db.Database.EnsureCreatedAsync();
            db.Processes.Add(new(){Id=1,ProcessCode=code,ProcessName=code,ControlScope="CHEM"});
            db.Machines.Add(new(){Id=1,MachineCode=code,MachineName=code,ProcessId=1});
            db.ProductionLines.Add(new(){Id=1,LineCode=code,LineName=code});
            db.Tanks.Add(new(){Id=1,LineId=1,TankCode="T1",TankName="Tank"});
            db.QualityCharacteristics.Add(new(){Id=1,CharacteristicCode="C1",CharacteristicName="Concentration",ControlScope="CHEM"});
            db.ControlChartGroups.Add(new(){Id=1,GroupCode="CHEMICAL",GroupName="Chemical",BusinessScopeCode="CHEM",RequiresMachine=true,RequiresTank=true});
            db.PartProcessCharacteristics.Add(new(){Id=1,ProcessId=1,MachineId=1,TankId=1,CharacteristicId=1,ControlScope="CHEM",DisplayMode="RECORD_ONLY"});
            await db.SaveChangesAsync();return new(c,db,code);
        }
        public Dictionary<string,string?> Payload(string shift,string? stage,double value)=>new(){
            ["ControlScope"]="CHEM",["ProcessCode"]=code,["MachineCode"]=code,["TankCode"]="T1",["CharacteristicCode"]="C1",
            ["MeasuredValue"]=value.ToString(System.Globalization.CultureInfo.InvariantCulture),["MeasuredAt"]="2026-09-17T08:00:00+08:00",
            ["SamplingPhase"]=shift,["SamplingStage"]=stage,["SampleNo"]="1",["Operator"]="test"};
        public async Task Save(string shift,string? stage,double value)
        {
            var batch=await Upload.CreateVariableBatchAsync([Payload(shift,stage,value)],"PortalDaily","test",null);
            Assert.Equal(0,batch.ErrorRows);await Upload.ConfirmAsync(batch.UploadBatchId);
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    sealed class FakeEmail:IEmailNotificationService
    {
        public Task<bool> SendAlertEmailAsync(AlertEvent a,string e,string n,SmtpSettingsOverride? o=null)=>Task.FromResult(true);
        public Task<bool> SendTestEmailAsync(string e,SmtpSettingsOverride? o=null)=>Task.FromResult(true);
        public Task<bool> SendHtmlEmailAsync(string e,string n,string s,string h,SmtpSettingsOverride? o=null)=>Task.FromResult(true);
        public Task<bool> SendReportEmailAsync(string e,string n,string s,string h,byte[] b,string f)=>Task.FromResult(true);
    }
}

