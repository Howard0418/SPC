using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Controllers;
using MesSpc.Api.SpcEngine.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace MesSpc.Api.Tests;

public class ChemicalHistoryQueryTests
{
    [Fact]
    public async Task MeasurementEndpointAddsJudgmentWithoutChangingOriginalValuesOrPaging()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        var m=await f.Db.PartProcessCharacteristics.SingleAsync();m.USL=1;m.LSL=0;await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1","0")]),"editor",default);
        var response=Assert.IsType<OkObjectResult>(await new ProcessesController(f.Db).GetMeasurements(1,take:1));
        using var json=JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        json.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        var row=json.RootElement.GetProperty("rows")[0];
        row.GetProperty("value").GetDouble().Should().Be(2);row.GetProperty("RecheckValue").GetDouble().Should().Be(0);
        row.GetProperty("effectiveValue").GetDouble().Should().Be(0);row.GetProperty("initialJudgment").GetString().Should().Be("NG");
        row.GetProperty("judgment").GetString().Should().Be("OK");
        row.GetProperty("judgmentReasonCode").ValueKind.Should().Be(JsonValueKind.Null);
        row.GetProperty("repairEligibility").GetString().Should().Be("NotRequired");
        row.GetProperty("repairSuggestion").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SavedThreeDecimalJudgmentSurvivesLaterMasterChanges()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        var mapping=await f.Db.PartProcessCharacteristics.SingleAsync();mapping.USL=1.002;mapping.LSL=0;await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("0.502",null)]),"editor",default);
        mapping.USL=99;await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
        var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value;
        result.EffectiveValue.Should().Be(1.004);result.Judgment.Should().Be("NG");result.InitialJudgment.Should().Be("NG");
        result.JudgmentSource.Should().Be("SavedTrace");result.PrecisionNote.Should().BeNullOrEmpty();
        f.Db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task RecheckZeroIsFinalWhileInitialNgRemainsVisible()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        var m=await f.Db.PartProcessCharacteristics.SingleAsync();m.USL=1;m.LSL=0;await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1","0")]),"editor",default);
        f.Db.ChangeTracker.Clear();var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value;
        result.HasRecheck.Should().BeTrue();result.EffectiveValue.Should().Be(0);result.Judgment.Should().Be("OK");result.InitialJudgment.Should().Be("NG");
    }

    [Fact]
    public async Task InteractiveChartUsesTheSameSavedRecheckEffectiveValueAsHistory()
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var process=new Process{Id=1,ProcessCode="C1"};
        var machine=new Machine{Id=1,MachineCode="P400",ProcessId=1};
        var characteristic=new QualityCharacteristic{Id=1,CharacteristicCode="CHEM",DataCategory="Variable"};
        var chartType=new ControlChartType{Id=1,ChartTypeCode="I_MR",DataCategory="Variable"};
        var mapping=new PartProcessCharacteristic{Id=1,ProcessId=1,MachineId=1,CharacteristicId=1,ChartTypeId=1,ControlScope="CHEM",SampleSize=1,IsEnabled=true};
        var batch=new UploadBatch{UploadBatchId=Guid.NewGuid()};
        db.AddRange(process,machine,characteristic,chartType,mapping,batch,
            new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,ProcessId=1,MachineId=1,CharacteristicId=1,UploadBatchId=batch.UploadBatchId,MeasuredAt=new DateTime(2026,11,9,16,0,0),MeasuredValue=12.18,RecheckValue=12.41});
        await db.SaveChangesAsync();

        var chart=await new SpcService(db,new ChemicalPortalMeasurementTests.FakeEmail(),new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build())
            .GetInteractiveChartAsync(1,null,includeAllPoints:true);

        chart.Should().NotBeNull();
        chart!.RawDataPoints.Should().BeAssignableTo<IEnumerable<SpcDataPoint>>().Which.Should().ContainSingle().Which.Value.Should().Be(12.41);
        (await new ChemicalHistoryQueryService(db).GetAsync(await db.VariableMeasurements.ToListAsync()))[1].EffectiveValue.Should().Be(12.41);
    }

    [Fact]
    public async Task MissingCorruptOrStaleTraceNeverFallsBackToRoundedOk()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1",null)]),"editor",default);
        var row=await f.Db.VariableMeasurements.SingleAsync();
        row.MeasuredValue=99; // Simulate an inconsistent projection input without altering immutable evidence.
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync([row]))[row.Id];
        result.Judgment.Should().Be("Unknown");
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var receiptId=Guid.NewGuid();
        var corrupt=new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,MeasuredValue=2,ChemicalReceiptId=receiptId};
        var missing=new VariableMeasurement{Id=2,PartProcessCharacteristicId=1,MeasuredValue=2,ChemicalReceiptId=Guid.NewGuid()};
        db.AddRange(corrupt,missing,new ChemicalCalculationReceipt{Id=receiptId,PartProcessCharacteristicId=1,EffectivityId=1,PayloadJson="broken"},
            new ChemicalFormulaEffectivity{Id=1,PartProcessCharacteristicId=1,FormulaVersionId=1},
            new ChemicalAnalysisFormulaVersion{Id=1,PartProcessCharacteristicId=1},
            new ChemicalCalculationTrace{VariableMeasurementId=1,Phase="Initial",ReceiptId=receiptId,StoredResultCanonical="2",RawResultCanonical="2"});
        await db.SaveChangesAsync();
        var invalid=await new ChemicalHistoryQueryService(db).GetAsync([corrupt,missing]);
        invalid.Values.Should().OnlyContain(x=>x.Judgment=="Unknown");
    }

    [Fact]
    public async Task LegacyUsesMatchingSavedCalculationSnapshotAndKeepsUnknownHonest()
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var row=new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,MeasuredValue=12,RecheckValue=0};
        db.AddRange(row,new PartProcessCharacteristic{Id=1,USL=-1},new SpcCalculationResult{Id=1,VariableMeasurementId=1,StatisticValue=0,USL=10,LSL=0,IsOutOfSpec=false});
        await db.SaveChangesAsync();db.ChangeTracker.Clear();var service=new ChemicalHistoryQueryService(db);
        var result=(await service.GetAsync([row]))[1];result.Judgment.Should().Be("OK");result.InitialJudgment.Should().Be("NG");result.EffectiveValue.Should().Be(0);
        row.RecheckValue=5;(await service.GetAsync([row]))[1].Judgment.Should().Be("Unknown");
        var none=new VariableMeasurement{Id=2,MeasuredValue=0};(await service.GetAsync([none]))[2].Judgment.Should().Be("Unknown");
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task MissingHistoricalEvidenceReturnsFixedReasonAndActionWithoutUsingCurrentLimits()
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var row=new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,ProcessId=1,MeasuredValue=7};
        db.AddRange(row,new Process{Id=1,ProcessCode="C5",ProcessName="C5"},new PartProcessCharacteristic{Id=1,ProcessId=1,LSL=0,USL=10,ControlScope="CHEM"});
        await db.SaveChangesAsync();

        var result=(await new ChemicalHistoryQueryService(db).GetAsync([row]))[1];

        result.Judgment.Should().Be("Unknown");
        result.JudgmentReasonCode.Should().Be("HISTORICAL_EVIDENCE_MISSING");
        result.RepairEligibility.Should().Be("SourceEvidenceRequired");
        result.RepairSuggestion.Should().Contain("Receipt").And.Contain("公式F").And.Contain("規格");
        result.PrecisionNote.Should().Contain("無法判定");
    }

    [Theory]
    [InlineData(1.002,1.002,false,"OK")]
    [InlineData(1.004,1.002,true,"NG")]
    [InlineData(0,null,false,"Unknown")]
    public async Task SavedPrecisionBoundaryAndMissingSpec(double value,double? usl,bool oos,string expected)
    {
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var row=new VariableMeasurement{Id=1,MeasuredValue=value};
        db.AddRange(row,new SpcCalculationResult{Id=1,VariableMeasurementId=1,StatisticValue=value,USL=usl,IsOutOfSpec=oos});await db.SaveChangesAsync();
        (await new ChemicalHistoryQueryService(db).GetAsync([row]))[1].Judgment.Should().Be(expected);
    }
}
