using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Controllers;
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
    }

    [Fact]
    public async Task SavedRawJudgmentSurvivesDisplayRoundingAndLaterMasterChanges()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        var mapping=await f.Db.PartProcessCharacteristics.SingleAsync();mapping.USL=1.002;mapping.LSL=0;await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("0.502",null)]),"editor",default);
        mapping.USL=99;await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
        var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value;
        result.EffectiveValue.Should().Be(1);result.Judgment.Should().Be("NG");result.InitialJudgment.Should().Be("NG");
        result.JudgmentSource.Should().Be("SavedTrace");result.PrecisionNote.Should().NotBeEmpty();
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
