using System.Globalization;
using System.Text.Json;
namespace Pt1CurrentSchedule;
public sealed record Slice(DateTime FromUtc,DateTime? UntilUtc,long FormulaId,long? FId);
public sealed record MergedSchedule(bool Changed,IReadOnlyList<Slice> Periods);
public static class SchedulePlan
{
 public static MergedSchedule Merge(IEnumerable<Slice> old,DateTime start,long formula,long f)
 {
  if(formula<=0||f<=0)throw new InvalidOperationException("Existing formula and complete F history required");
  var rows=old.OrderBy(x=>x.FromUtc).ToList();var desired=new Slice(start,null,formula,f);
  if(rows.LastOrDefault()==desired){Validate(rows);return new(false,rows);}
  if(rows.Any(x=>x.UntilUtc is null||x.UntilUtc>start||x.FromUtc>=start))throw new InvalidOperationException("Cannot overwrite or overlap existing effectiveness");
  rows.Add(desired);Validate(rows);return new(true,rows);
 }
 private static void Validate(IReadOnlyList<Slice> rows)
 {
  for(int i=0;i<rows.Count;i++)if(rows[i].UntilUtc<=rows[i].FromUtc||(i>0&&(rows[i-1].UntilUtc==null||rows[i-1].UntilUtc>rows[i].FromUtc)))throw new InvalidOperationException("Invalid or overlapping periods");
 }
 public static string FSignature(string cells)
 {
  using var doc=JsonDocument.Parse(cells);var normalized=new SortedDictionary<string,decimal>(StringComparer.Ordinal);
  foreach(var c in doc.RootElement.EnumerateArray())
  {
   var address=c.GetProperty("CellAddress").GetString()?.Replace("$","").ToUpperInvariant().Replace("F!","");
   if(string.IsNullOrWhiteSpace(address)||!c.GetProperty("NumericValue").TryGetDecimal(out var value)||!normalized.TryAdd(address,value))throw new InvalidOperationException("Missing or duplicate numeric F cell");
  }
  if(normalized.Count==0)throw new InvalidOperationException("Empty F snapshot");
  return string.Join(";",normalized.Select(x=>x.Key+"="+x.Value.ToString("G29",CultureInfo.InvariantCulture)));
 }
}
