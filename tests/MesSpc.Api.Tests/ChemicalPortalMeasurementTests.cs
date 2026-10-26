using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class ChemicalPortalMeasurementTests
{
    [Fact]
    public async Task InitialAndRecheckAreAtomicTrustedAndIdempotent()
    {
        await using var f=await Fixture.Create();
        var row=await f.Row("1.2345","2.3456"); row["MeasuredValue"]="999"; row["RecheckValue"]="888";
        var id=Guid.NewGuid(); await f.Service.ConfirmAsync(new(id,[row]),"editor",default);
        var vm=await f.Db.VariableMeasurements.SingleAsync();vm.MeasuredValue.Should().Be(2.47);vm.RecheckValue.Should().Be(4.69);
        (await f.Db.ChemicalCalculationReceipts.CountAsync()).Should().Be(2);
        (await f.Db.UploadDetails.Select(x=>x.PayloadJson).ToListAsync()).Should().OnlyContain(x=>!x.Contains("ChemicalPreviewToken") && !x.Contains("ChemicalRecheckPreviewToken"));
        (await f.Db.ChemicalCalculationTraces.Select(x=>x.Phase).ToListAsync()).Should().BeEquivalentTo("Initial","Recheck");
        await f.Service.ConfirmAsync(new(id,[row]),"editor",default);
        (await f.Db.VariableMeasurements.CountAsync()).Should().Be(1);(await f.Db.ChemicalCalculationTraces.CountAsync()).Should().Be(2);
        row["TitrationValue"]="9";
        Func<Task> act=()=>f.Service.ConfirmAsync(new(id,[row]),"editor",default);await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
    }

    [Fact]
    public async Task InvalidRecheckRollsBackInitialReceiptAndBatch()
    {
        await using var f=await Fixture.Create();var row=await f.Row("1","2");row["ChemicalRecheckPreviewToken"]="forged";
        Func<Task> act=()=>f.Service.ConfirmAsync(new(Guid.NewGuid(),[row]),"editor",default);await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        f.Db.ChangeTracker.Clear();(await f.Db.ChemicalCalculationReceipts.CountAsync()).Should().Be(0);
        (await f.Db.UploadBatches.CountAsync()).Should().Be(0);(await f.Db.VariableMeasurements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ChangedDateAndMissingRawAreRejectedWithoutWriting()
    {
        await using var f=await Fixture.Create();var row=await f.Row("1",null);row["MeasuredAt"]="2026-10-21T12:00:00+08:00";
        Func<Task> act=()=>f.Service.ConfirmAsync(new(Guid.NewGuid(),[row]),"editor",default);await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        row=await f.Row("1",null);row["TitrationValue"]="";
        await act.Should().ThrowAsync<ArgumentException>();f.Db.ChangeTracker.Clear();(await f.Db.VariableMeasurements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DailyUpdateAppendsTraceAndPreservesOriginalRawValues()
    {
        await using var f=await Fixture.Create();await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1",null)]),"editor",default);
        var original=(await f.Db.VariableMeasurements.SingleAsync()).Id;
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1.12345678","2.7654321")]),"editor",default);
        (await f.Db.VariableMeasurements.SingleAsync()).Id.Should().Be(original);
        (await f.Db.ChemicalCalculationTraces.CountAsync()).Should().Be(3);
        (await f.Db.ChemicalCalculationTraces.Where(x=>x.Phase=="Initial").MaxAsync(x=>x.VersionNo)).Should().Be(2);
        (await f.Db.ChemicalCalculationReceipts.Where(x=>x.PayloadJson.Contains("1.12345678")).CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RawFormulaSpecJudgmentAndNotificationsRespectCommit()
    {
        await using var f=await Fixture.Create();
        f.Db.Add(new ControlChartType{Id=1,ChartGroupId=1,ChartTypeCode="I_MR"});
        var mapping=await f.Db.PartProcessCharacteristics.SingleAsync();mapping.ChartTypeId=1;mapping.USL=1.002;
        await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("0.502",null)]),"editor",default);
        (await f.Db.VariableMeasurements.SingleAsync()).MeasuredValue.Should().Be(1.0);
        var statistic=await f.Db.SpcCalculationResults.SingleAsync();statistic.StatisticValue.Should().Be(1.0);statistic.IsOutOfSpec.Should().BeTrue();
        (await f.Db.AlertEvents.SingleAsync()).ActualValue.Should().Be(1.004);
        f.Mail.Sent.Should().Be(1);f.Mail.BeforeCommit.Should().BeFalse();
    }
    [Fact]
    public async Task MixedBatchKeepsUnscheduledLegacyValuesAndRejectsInvalidRowsAtomically()
    {
        await using var f=await Fixture.Create();
        f.Db.AddRange(new QualityCharacteristic{Id=2,CharacteristicCode="DIRECT",ControlScope="CHEM"},
            new PartProcessCharacteristic{Id=2,ProcessId=1,MachineId=1,TankId=1,CharacteristicId=2,ControlScope="CHEM"});
        await f.Db.SaveChangesAsync();
        var a=await f.Row("1.2345",null);var b=new Dictionary<string,string?>(a){["PpcId"]="2",["CharacteristicCode"]="DIRECT",["MeasuredValue"]="10.004",["RecheckValue"]="9.999",["TitrationValue"]=""};
        b.Remove("ChemicalPreviewToken");
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[a,b]),"editor",default);
        var legacy=await f.Db.VariableMeasurements.SingleAsync(x=>x.PartProcessCharacteristicId==2);
        legacy.MeasuredValue.Should().Be(10.004);legacy.RecheckValue.Should().Be(9.999);legacy.ChemicalReceiptId.Should().BeNull();
        (await f.Db.ChemicalCalculationTraces.CountAsync()).Should().Be(1);
        a=await f.Row("2.5",null);b["MeasuredValue"]="invalid";
        var failed=Guid.NewGuid();Func<Task> act=()=>f.Service.ConfirmAsync(new(failed,[a,b]),"editor",default);
        await act.Should().ThrowAsync<ArgumentException>();
        (await f.Db.UploadBatches.CountAsync(x=>x.UploadBatchId==failed)).Should().Be(0);
        (await f.Db.ChemicalCalculationReceipts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void PortalConfirmRequiresEditorEvenWhenOtherLegacyRoutesAreAnonymous()
    {
        var type=typeof(MesSpc.Api.Controllers.ChemicalPortalMeasurementsController);
        type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true).Should().NotBeEmpty();
        type.GetMethod("Confirm")!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Roles.Should().Be("Admin,Editor");
    }

    internal sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public AppDbContext Db {get;}
        public FakeEmail Mail {get;}
        public ChemicalPortalMeasurementService Service {get;}
        private Fixture(SqliteConnection connection,AppDbContext db)
        {
            this.connection=connection;Db=db;Mail=new(){IsTransaction=()=>db.Database.CurrentTransaction is not null};
            var schedule=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
            Service=new(db,schedule,new UploadService(db,new SpcService(db,Mail,new ConfigurationBuilder().Build())),NullLogger<ChemicalPortalMeasurementService>.Instance);
        }
        public static async Task<Fixture> Create()
        {
            var connection=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await connection.OpenAsync();
            var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
            db.AddRange(new Process{Id=1,ProcessCode="C1",ControlScope="CHEM"},new Machine{Id=1,MachineCode="C1",ProcessId=1},
                new ProductionLine{Id=1,LineCode="C1"},new Tank{Id=1,LineId=1,TankCode="T1"},
                new QualityCharacteristic{Id=1,CharacteristicCode="CONC",ControlScope="CHEM",InputMode="FORMULA"},
                new ControlChartGroup{Id=1,GroupCode="CHEMICAL",BusinessScopeCode="CHEM",RequiresMachine=true,RequiresTank=true},
                new PartProcessCharacteristic{Id=1,ProcessId=1,MachineId=1,TankId=1,CharacteristicId=1,ControlScope="CHEM"},
                new ChemicalAnalysisFormulaVersion{Id=1,PartProcessCharacteristicId=1,VersionNo=1,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\",\"decimalPlaces\":2}"});
            await db.SaveChangesAsync();var f=new Fixture(connection,db);
            var schedule=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
            await schedule.PublishAsync(1,new(null,"fixture",[new(1,null,"2026-10-01",null)]),"editor",default);
            return f;
        }
        public async Task<Dictionary<string,string?>> Row(string primary,string? recheck)
        {
            var input=new ChemicalPortalPreviewRequest(1,"2026-10-20T12:00:00+08:00",primary,null,"OPEN","GENERAL");
            var preview=await Service.PreviewAsync(input,"editor",default);
            var row=new Dictionary<string,string?> { ["PpcId"]="1",["ControlScope"]="CHEM",["ProcessCode"]="C1",["MachineCode"]="C1",["TankCode"]="T1",["CharacteristicCode"]="CONC",
                ["MeasuredAt"]=input.MeasuredAt,["TitrationValue"]=primary,["MeasuredValue"]="0",["SampleNo"]="1",["SamplingPhase"]="OPEN",["SamplingStage"]="GENERAL",["Operator"]="editor",["ChemicalPreviewToken"]=preview.Token };
            if(recheck is not null){var p=await Service.PreviewAsync(input with{Primary=recheck},"editor",default);row["RecheckTitrationValue"]=recheck;row["RecheckValue"]="0";row["ChemicalRecheckPreviewToken"]=p.Token;}
            return row;
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    internal sealed class FakeEmail : IEmailNotificationService
    { public int Sent {get;private set;} public bool BeforeCommit {get;private set;} public Func<bool>? IsTransaction {get;set;}
      public Task<bool> SendAlertEmailAsync(AlertEvent a,string e,string n,SmtpSettingsOverride? o=null, string? uiLocale = null){Sent++;BeforeCommit|=IsTransaction?.Invoke()??false;return Task.FromResult(true);}
      public Task<bool> SendTestEmailAsync(string e,SmtpSettingsOverride? o=null, string? uiLocale = null)=>Task.FromResult(true);
      public Task<bool> SendHtmlEmailAsync(string e,string n,string s,string h,SmtpSettingsOverride? o=null)=>Task.FromResult(true);
      public Task<bool> SendReportEmailAsync(string e,string n,string s,string h,byte[] b,string f)=>Task.FromResult(true); }
}
