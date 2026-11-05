using ChemicalSourceRevision;
public class ChemicalSourceRevisionGuardTests
{
 [Fact] public void LegacyUniqueRowCanBeRevised()=>RevisionGuard.RequireLegacy(1,1,false,false,false,false);
 [Theory][InlineData(true,false,false,false)][InlineData(false,true,false,false)][InlineData(false,false,true,false)][InlineData(false,false,false,true)]
 public void ExistingEvidenceOrPolicyCannotBeBypassed(bool receipt,bool trace,bool policy,bool key)=>Assert.Throws<InvalidOperationException>(()=>RevisionGuard.RequireLegacy(1,1,receipt,trace,policy,key));
 [Fact]public void DifferentMappingRejected()=>Assert.Throws<InvalidOperationException>(()=>RevisionGuard.RequireLegacy(1,2,false,false,false,false));
 [Fact]public void NonfiniteResultRejected()=>Assert.Throws<InvalidOperationException>(()=>RevisionGuard.RequireValue(double.NaN));
 [Fact]public void SameSourceRetryIsUnchanged(){Assert.True(RevisionGuard.Same(1.092,1.092));Assert.False(RevisionGuard.Same(1.094,1.092));}
}
