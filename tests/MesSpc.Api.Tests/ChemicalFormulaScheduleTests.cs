using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public sealed class ChemicalFormulaScheduleTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ChemicalFormulaScheduleService Service(AppDbContext db) => new(db, new EphemeralDataProtectionProvider(), TimeProvider.System, NullLogger<ChemicalFormulaScheduleService>.Instance);
    private static async Task Seed(AppDbContext db)
    {
        db.Add(new PartProcessCharacteristic { Id=1, ControlScope="CHEM", IsEnabled=true });
        db.AddRange(new ChemicalAnalysisFormulaVersion { Id=1,PartProcessCharacteristicId=1,VersionNo=1, ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\"}" },
            new ChemicalAnalysisFormulaVersion { Id=2,PartProcessCharacteristicId=1,VersionNo=2, ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*3\"}" });
        await db.SaveChangesAsync();
    }
    private static ChemicalPublishSchedule Request(long? revision = null) => new(revision,"文件DOC-001",[new(1,null,"2026-10-01","2026-10-31"),new(2,null,"2026-11-01",null)]);
    private static ChemicalPreviewInput Input(string date="2026-11-01T00:00:00+08:00") => new(date,"1.2345",null,"source-1");

    [Fact]
    public async Task CrossDayBatchUsesDifferentVersionWithoutEnablingLegacyGate()
    {
        await using var db=Db(); await Seed(db); var svc=Service(db);
        await svc.PublishAsync(1,Request(),"editor",default);
        var a=await svc.PreviewAsync(1,Input("2026-10-31T23:59:59+08:00"),"editor",default);
        var b=await svc.PreviewAsync(1,Input(),"editor",default);
        a.FormulaVersionId.Should().Be(1); b.FormulaVersionId.Should().Be(2);
        a.Result.Raw.Should().Be("2.469"); b.Result.Raw.Should().Be("3.7035");
        (await db.ChemicalFormulaPolicies.SingleAsync()).IsEnabled.Should().BeFalse();
        (await db.ChemicalCalculationReceipts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task StaleRevisionAndChangedInputOrActorAreRejected()
    {
        await using var db=Db(); await Seed(db); var svc=Service(db);
        var first=await svc.PublishAsync(1,Request(),"editor",default);
        var preview=await svc.PreviewAsync(1,Input(),"editor",default);
        Func<Task> act=()=>svc.PrepareAsync(1,new(Input() with { Primary="2" },preview.Token),"editor",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        act=()=>svc.PrepareAsync(1,new(Input(),preview.Token),"other",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        await svc.PublishAsync(1,Request(first),"editor",default);
        act=()=>svc.PrepareAsync(1,new(Input(),preview.Token),"editor",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        act=()=>svc.PublishAsync(1,Request(first),"editor",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        (await db.ChemicalCalculationReceipts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PreparedReceiptFreezesReferencedPeriodAndNeverWritesMeasurement()
    {
        await using var db=Db(); await Seed(db); var svc=Service(db);
        var revision=await svc.PublishAsync(1,Request(),"editor",default);
        var preview=await svc.PreviewAsync(1,Input(),"editor",default);
        var receipt=await svc.PrepareAsync(1,new(Input(),preview.Token),"editor",default);
        receipt.EngineVersion.Should().Be(ChemicalFormulaCalculation.EngineVersion);
        receipt.PayloadJson.Should().Contain("1.2345");
        (await db.VariableMeasurements.CountAsync()).Should().Be(0);
        Func<Task> act=()=>svc.PublishAsync(1,new(revision,"不能回溯",[new(1,null,"2026-10-01",null)]),"editor",default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MissingVersionAndCrossItemFormulaRejected()
    {
        await using var db=Db(); await Seed(db); var svc=Service(db);
        Func<Task> act=()=>svc.PreviewAsync(1,Input(),"editor",default);
        await act.Should().ThrowAsync<ArgumentException>();
        db.Add(new ChemicalAnalysisFormulaVersion { Id=3,PartProcessCharacteristicId=2,VersionNo=1,ConfigJson="{}" }); await db.SaveChangesAsync();
        act=()=>svc.PublishAsync(1,new(null,"bad",[new(3,null,"2026-11-01",null)]),"editor",default);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Current { get; set; } = new(2026,10,20,0,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Current;
    }
    [Fact]
    public async Task ExpiredForgedTokensAndChangedSpecificationAreRejected()
    {
        await using var db=Db(); await Seed(db);
        var clock=new TestClock();
        var svc=new ChemicalFormulaScheduleService(db,new EphemeralDataProtectionProvider(),clock,NullLogger<ChemicalFormulaScheduleService>.Instance);
        await svc.PublishAsync(1,Request(),"editor",default);
        var preview=await svc.PreviewAsync(1,Input(),"editor",default);
        Func<Task> act=()=>svc.PrepareAsync(1,new(Input(),"forged"),"editor",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        clock.Current=clock.Current.AddMinutes(10);
        act=()=>svc.PrepareAsync(1,new(Input(),preview.Token),"editor",default);
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        preview=await svc.PreviewAsync(1,Input(),"editor",default);
        var mapping=await db.PartProcessCharacteristics.SingleAsync(); mapping.USL=1; await db.SaveChangesAsync();
        await act.Should().ThrowAsync<ChemicalPreviewConflictException>();
        (await db.ChemicalCalculationReceipts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task FutureRevisionCanSplitOpenIntervalWithoutChangingIssuedReceiptDate()
    {
        await using var db=Db(); await Seed(db); var svc=Service(db);
        var revision=await svc.PublishAsync(1,new(null,"v1",[new(1,null,"2026-10-01",null)]),"editor",default);
        var input=Input("2026-10-20T00:00:00+08:00");var preview=await svc.PreviewAsync(1,input,"editor",default);
        await svc.PrepareAsync(1,new(input,preview.Token),"editor",default);
        await svc.PublishAsync(1,Request(revision),"editor",default);
        (await svc.PreviewAsync(1,input,"editor",default)).FormulaVersionId.Should().Be(1);
        (await svc.PreviewAsync(1,Input(),"editor",default)).FormulaVersionId.Should().Be(2);
        (await db.ChemicalCalculationReceipts.SingleAsync()).EffectivityId.Should().Be(1);
    }

    [Fact]
    public void WriteEndpointsRequireEditorAndReadsRequireAuthentication()
    {
        var type=typeof(MesSpc.Api.Controllers.ChemicalFormulaScheduleController);
        type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true).Should().NotBeEmpty();
        foreach(var method in new[]{"Publish","Prepare"})
            type.GetMethod(method)!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Roles.Should().Be("Admin,Editor");
    }}
