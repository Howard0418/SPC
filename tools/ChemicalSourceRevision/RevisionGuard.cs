namespace ChemicalSourceRevision;
public static class RevisionGuard
{
 public static void RequireLegacy(int actualPpc,int expectedPpc,bool receipt,bool trace,bool enabledPolicy,bool hasSourceKey){if(actualPpc!=expectedPpc||receipt||trace||enabledPolicy||hasSourceKey)throw new InvalidOperationException("Only the reviewed legacy row without pinned calculation evidence can be revised by this tool.");}
 public static void RequireValue(double value){if(!double.IsFinite(value))throw new InvalidOperationException("Finite Excel value required.");}
 public static bool Same(double a,double b)=>Math.Abs(a-b)<=1e-8;
}
