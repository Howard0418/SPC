using ProductionTestBusinessAlign;

namespace MesSpc.Api.Tests;
public class ProductionTestBusinessAlignTests
{
 [Fact] public void Plan_AlignsApprovedMasterValues_AndIsIdempotent()
 {
  var target=new BusinessMaster(1,"C5|清潔|硫酸",true,48,72,60,"{\"concentrationFormula\":\"Primary*8.7*F!B3\"}");
  var source=target with { Lsl=80,Usl=120,Target=100 };
  var p=Assert.Single(BusinessAlignPlanner.Build([source],[target]));
  Assert.Equal(80,p.After.Lsl);Assert.Equal(120,p.After.Usl);Assert.Equal(100,p.After.Target);
  Assert.Empty(BusinessAlignPlanner.Build([source],[p.After]));
 }
 [Fact] public void Plan_RejectsBusinessIdentityMismatch()=>Assert.Throws<InvalidOperationException>(()=>BusinessAlignPlanner.Build([Row(1,"A")],[Row(1,"B")]));
 [Fact] public void Plan_RejectsTestIdentity()=>Assert.Throws<InvalidOperationException>(()=>BusinessAlignPlanner.Build([Row(1,"TEST-FAKE")],[Row(1,"TEST-FAKE")]));
 [Fact] public void Plan_RejectsUnexpectedFormulaChange()=>Assert.Throws<InvalidOperationException>(()=>BusinessAlignPlanner.Build([Row(1,"A") with{Config="{\"concentrationFormula\":\"Primary*9\"}"}],[Row(1,"A")]));
 [Fact] public void Plan_CopiesSourceLocationButPreservesOtherMetadata()
 {
  var target=Row(1,"A") with{Config="{\"concentrationFormula\":\"Primary\",\"version\":\"formal\"}"};
  var source=target with{Config="{\"concentrationFormula\":\"Primary\",\"version\":\"test\",\"sourceRow\":12}"};
  var p=Assert.Single(BusinessAlignPlanner.Build([source],[target]));
  Assert.Contains("formal",p.After.Config);Assert.Contains("sourceRow",p.After.Config);
 }
 private static BusinessMaster Row(int id,string key)=>new(id,key,true,null,null,null,"{\"concentrationFormula\":\"Primary\"}");
 [Fact] public void Plan_RejectsReversedLimits()=>Assert.Throws<InvalidOperationException>(()=>BusinessAlignPlanner.Build([Row(1,"A") with{Lsl=10,Usl=2}],[Row(1,"A")]));
 [Fact] public void Plan_RejectsMissingTarget()=>Assert.Throws<InvalidOperationException>(()=>BusinessAlignPlanner.Build([Row(1,"A")],[]));
 [Fact] public void Plan_DoesNotCopyMissingSourceProvenance()
 {
  var target=Row(1,"A") with{Config="{\"concentrationFormula\":\"Primary\",\"sourceRow\":12}"};
  Assert.Empty(BusinessAlignPlanner.Build([Row(1,"A")],[target]));
 }
 [Fact] public void Plan_DisablesDuplicateWithoutChangingFormula()
 {
  var target=Row(1,"A");var change=Assert.Single(BusinessAlignPlanner.Build([target with{Enabled=false}],[target]));
  Assert.False(change.After.Enabled);Assert.Equal(target.Config,change.After.Config);
 }
}
