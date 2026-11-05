using Microsoft.Data.SqlClient;
namespace EtchProduction;
public static class ProductionGuard
{
 public static void RequireReportScope(IEnumerable<DateTime> dates,bool annual,DateTime asOf){var days=dates.ToArray();if(days.Length==0||days.Any(d=>d.Year!=2026||d.Date>asOf.Date||(!annual&&d.Month is not(9 or 10))))throw new InvalidOperationException("Report date is outside the explicitly reviewed 2026 scope.");}
 public static void RequireDatabase(string? environment,string connection){
  var c=new SqlConnectionStringBuilder(connection);
  if(environment!="production"||c.DataSource!="172.16.110.16"||c.InitialCatalog!="PMR_SPC_2026")throw new InvalidOperationException("Production environment/database guard");
 }
 public static void RequireApplyConfirmation(string mode,string[] args,string hash){
  if(mode=="--apply"&&(!args.Contains("--confirm-database=PMR_SPC_2026")||!args.Contains("--confirm-source-sha256="+hash)))throw new InvalidOperationException("Exact production database/source confirmation required");
 }
}
