using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace ProductionTestBusinessAlign;
public sealed record BusinessMaster(int Id,string Key,bool Enabled,double? Lsl,double? Usl,double? Target,string Config);
public sealed record BusinessChange(BusinessMaster Before,BusinessMaster After);
public static class BusinessAlignPlanner
{
 public static List<BusinessChange> Build(IEnumerable<BusinessMaster> source,IEnumerable<BusinessMaster> target)
 {
  var a=source.ToDictionary(x=>x.Id);var b=target.ToDictionary(x=>x.Id);
  if(!a.Keys.Order().SequenceEqual(b.Keys.Order()))throw new InvalidOperationException("Mapping sets differ.");
  var changes=new List<BusinessChange>();
  foreach(var id in a.Keys.Order())
  {
   var s=a[id];var t=b[id];
   if(s.Key!=t.Key||Regex.IsMatch(s.Key,@"(?i)(TEST|DEMO|FIXTURE|SYNTHETIC|E2E|測試|驗收)"))throw new InvalidOperationException("Business identity rejected.");
   var sj=JsonNode.Parse(s.Config)?.AsObject()??new();var tj=JsonNode.Parse(t.Config)?.AsObject()??new();
   foreach(var key in sj.Select(x=>x.Key).Union(tj.Select(x=>x.Key)))
   {
    if(key is "version" or "migratedAt" or "migratedFrom" or "sourceSheet" or "sourceRow" or "sourceFormula")continue;
    string Canon(JsonNode? n){if(n is not JsonValue v||!v.TryGetValue<string>(out var str)||!key.Contains("Formula",StringComparison.OrdinalIgnoreCase))return n?.ToJsonString()??"null";bool quoted=false;var chars=new List<char>();foreach(var ch in str){if(ch=='"')quoted=!quoted;if(quoted||!char.IsWhiteSpace(ch))chars.Add(ch);}return new string(chars.ToArray());}
    if(Canon(sj[key])!=Canon(tj[key]))throw new InvalidOperationException($"Unexpected config difference: {id}/{key}");
   }
   bool configChanged=false;
   foreach(var key in new[]{"sourceSheet","sourceRow","sourceFormula"})if(sj.ContainsKey(key)&&!JsonNode.DeepEquals(sj[key],tj[key])){tj[key]=sj[key]?.DeepClone();configChanged=true;}
   if(s.Lsl>s.Usl)throw new InvalidOperationException("Invalid specification range.");
   var after=t with{Enabled=s.Enabled,Lsl=s.Lsl,Usl=s.Usl,Target=s.Target,Config=configChanged?tj.ToJsonString():t.Config};
   if(after!=t)changes.Add(new(t,after));
  }
  return changes;
 }
}
