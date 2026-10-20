using System.Text.Json;
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Tests;
public class ChemicalTraceQueryTests
{
 [Fact] public async Task SavedInitialRecheckAndLegacyAreReadWithoutRecalculation()
 {
  await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
  await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1.2345","2.3456")]),"editor",default);
  var vm=await f.Db.VariableMeasurements.SingleAsync();
  f.Db.Add(new VariableMeasurement{PartProcessCharacteristicId=1,MeasuredAt=new(2026,10,19),MeasuredValue=99});
  var segment=new ControlLimitSegment{PartProcessCharacteristicId=1,StartDate=new(2026,10,18),EndDate=new(2026,10,20,23,59,59)};f.Db.Add(segment);
  var mapping=await f.Db.PartProcessCharacteristics.SingleAsync();mapping.ChemicalAnalysisConfigJson="{\"concentrationFormula\":\"Primary*999\"}";
  await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
  var svc=new ChemicalTraceQueryService(f.Db);
  var page=await svc.GetAsync(1,segment.Id,null,0,0,default);
  page!.TotalMeasurements.Should().Be(2);page.UnknownMeasurements.Should().Be(1);
  var saved=page.Rows.Single(x=>x.MeasurementId==vm.Id);saved.Status.Should().Be("Recorded");
  saved.Calculations.Select(x=>x.Phase).Should().BeEquivalentTo("Initial","Recheck");
  saved.Calculations.Single(x=>x.Phase=="Initial").RawResult.Should().Be("2.469");
  saved.Calculations.Single(x=>x.Phase=="Initial").FormulaConfigJson.Should().Contain("Primary*2").And.NotContain("999");
  page.Rows.Single(x=>x.MeasurementId!=vm.Id).Status.Should().Be("Unknown");
  (await svc.GetAsync(1,null,null,0,0,default))!.Rows.Should().BeEmpty();
  (await svc.GetAsync(2,segment.Id,null,0,0,default)).Should().BeNull();
  (await svc.GetAsync(1,null,vm.Id,0,0,default))!.Rows.Should().ContainSingle();
  f.Db.ChangeTracker.Entries().Should().BeEmpty();
  (await f.Db.ChemicalCalculationTraces.CountAsync()).Should().Be(2);
 }
 [Fact] public async Task TraceHistoryPagesKeepPhaseVersionsAndMissingEvidenceIsHonest()
 {
  await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
  for(var i=1;i<=12;i++)await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row(i.ToString(),(i+1).ToString())]),"editor",default);
  var vm=await f.Db.VariableMeasurements.SingleAsync();f.Db.ChangeTracker.Clear();
  var svc=new ChemicalTraceQueryService(f.Db);var first=(await svc.GetAsync(1,null,vm.Id,0,0,default))!;
  first.Rows.Single().Calculations.Should().HaveCount(20);first.HasMoreTrace.Should().BeTrue();
  first.Rows.Single().Calculations.Count(x=>x.IsCurrent).Should().Be(2);
  var second=(await svc.GetAsync(1,null,vm.Id,0,20,default))!;
  second.Rows.Single().Calculations.Should().HaveCount(4);second.HasMoreTrace.Should().BeFalse();
  second.Rows.Single().Calculations.Should().OnlyContain(x=>!x.IsCurrent);
  (await svc.GetAsync(1,null,99999,0,0,default)).Should().BeNull();
  Func<Task> invalid=()=>svc.GetAsync(1,null,null,-1,0,default);await invalid.Should().ThrowAsync<ArgumentException>();
 }
 [Fact] public async Task CorruptReceiptDoesNotBecomeLatestFormulaOrCrash()
 {
  await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
  db.Add(new PartProcessCharacteristic{Id=1,ControlScope="CHEM"});
  var id=Guid.NewGuid();db.Add(new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,ChemicalReceiptId=id});
  db.Add(new ChemicalCalculationReceipt{Id=id,PartProcessCharacteristicId=1,PayloadJson="broken"});
  db.Add(new ChemicalCalculationTrace{VariableMeasurementId=1,ReceiptId=id,Phase="Initial",VersionNo=1});await db.SaveChangesAsync();db.ChangeTracker.Clear();
  var page=(await new ChemicalTraceQueryService(db).GetAsync(1,null,1,0,0,default))!;
  page.Rows.Single().Status.Should().Be("Incomplete");page.Rows.Single().Calculations.Single().EvidenceStatus.Should().Be("Unavailable");
  db.ChangeTracker.Entries().Should().BeEmpty();
 }

 [Fact] public async Task MultipleVersionsAndFReferencesUseSavedSnapshotsOnly()
 {
  await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
  db.Add(new PartProcessCharacteristic{Id=1,ControlScope="CHEM",ChemicalAnalysisConfigJson="{\"concentrationFormula\":\"Primary*999\"}"});
  for(var i=1;i<=2;i++){
   var config="{\"concentrationFormula\":\"Primary*F!B3\"}";
   var f=new ChemicalAnalysisFormulaVersion{Id=i,PartProcessCharacteristicId=1,VersionNo=i,ConfigJson=config,Reason="SAVED-DOC"};
   var e=new ChemicalFormulaEffectivity{Id=i,PartProcessCharacteristicId=1,FormulaVersionId=i,FTableHistoryId=77,ScheduleRevisionId=8,EffectiveFromUtc=new(2026,10,1)};
   var id=Guid.NewGuid();var r=new ChemicalCalculationReceipt{Id=id,PartProcessCharacteristicId=1,EffectivityId=i,
    PayloadJson=JsonSerializer.Serialize(new{ppcId=1,effectivityId=i,formulaVersionId=i,formulaVersionNo=i,formulaConfigJson=config,
     fCellsJson="[{\"CellAddress\":\"B3\",\"NumericValue\":2}]",originalInput=new{Primary="1.2345"},result=new{Raw="2.469",Stored="2.47",Judgment="OK"}})};
   db.AddRange(f,e,r,new VariableMeasurement{Id=i,PartProcessCharacteristicId=1,MeasuredAt=new(2026,10,i),MeasuredValue=2.47,ChemicalReceiptId=id},
    new ChemicalCalculationTrace{VariableMeasurementId=i,ReceiptId=id,RawResultCanonical="2.469",StoredResultCanonical="2.47"});
  }
  await db.SaveChangesAsync();db.ChangeTracker.Clear();
  var page=(await new ChemicalTraceQueryService(db).GetAsync(1,null,null,0,0,default))!;
  page.MultipleFormulaVersions.Should().BeTrue();page.Rows.SelectMany(x=>x.Calculations).Should().OnlyContain(x=>x.FormulaConfigJson!.Contains("F!B3") && x.Reason=="SAVED-DOC");
  page.Rows[0].Calculations[0].FCells!.Single().NumericValue.Should().Be("2");
  db.ChangeTracker.Entries().Should().BeEmpty();
 }
 [Fact] public async Task ReadControllerRequiresAuthenticationAndNonChemicalIsNotApplicable()
 {
  await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
  db.Add(new PartProcessCharacteristic{Id=1,ControlScope="PRODUCT"});await db.SaveChangesAsync();
  (await new ChemicalTraceQueryService(db).GetAsync(1,null,null,0,0,default))!.Applicable.Should().BeFalse();
  typeof(MesSpc.Api.Controllers.ChemicalTraceQueryController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true).Should().NotBeEmpty();
 }
}
