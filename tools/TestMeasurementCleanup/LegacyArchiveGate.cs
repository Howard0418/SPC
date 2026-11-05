using System.Text.RegularExpressions;
namespace TestMeasurementCleanup;
public static class LegacyArchiveGate
{
 public const string Marker="SPC-TEST-DATA-CLEANUP-20261105 pure legacy archive";
 public static string Normalize(string sql)=>Regex.Replace(sql.Trim(),@"\A(?:CREATE(?:\s+OR\s+ALTER)?|ALTER)\s+TRIGGER", "CREATE TRIGGER",RegexOptions.IgnoreCase).Replace("\r\n","\n");
 public static string Build(string original,IReadOnlyList<string> columns)
 {
  const string clause="WHERE p.IsEnabled=1 AND (i.ChemicalReceiptId IS NULL OR i.ChemicalSourceKeyHash IS NULL)";
  if(!original.Contains("TR_ChemicalMeasurementGate")||original.Split(clause).Length!=2||original.Contains(Marker))throw new InvalidOperationException("Unrecognized gate definition; manual review required");
  if(new[]{"Id","IsDeleted","UpdatedAt","UpdatedBy"}.Any(x=>!columns.Contains(x)))throw new InvalidOperationException("Unrecognized measurement schema");
  var business=columns.Where(x=>x is not("IsDeleted" or "UpdatedAt" or "UpdatedBy")).ToArray();
  string Select(string alias)=>string.Join(",",business.Select(x=>alias+".["+x.Replace("]","]]")+"]"));
  var exception=$"NOT EXISTS(SELECT 1 FROM deleted d WHERE d.Id=i.Id AND d.IsDeleted=0 AND i.IsDeleted=1 AND NOT EXISTS(SELECT {Select("i")} EXCEPT SELECT {Select("d")}))";
  var result=original.Replace(clause,$"WHERE p.IsEnabled=1 AND {exception} AND (i.ChemicalReceiptId IS NULL OR i.ChemicalSourceKeyHash IS NULL)");
  result=Regex.Replace(result,@"\A\s*CREATE(?:\s+OR\s+ALTER)?\s+TRIGGER", "ALTER TRIGGER",RegexOptions.IgnoreCase);
  if(!result.StartsWith("ALTER TRIGGER",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsupported trigger header");
  return result+"\n-- "+Marker;
 }
}
