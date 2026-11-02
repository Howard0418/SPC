using Microsoft.Data.SqlClient;
namespace EtchProduction;
public static class ProductionGuard
{
 public static void RequireDatabase(string? environment,string connection){
  var c=new SqlConnectionStringBuilder(connection);
  if(environment!="production"||c.DataSource!="172.16.110.16"||c.InitialCatalog!="PMR_SPC_2026")throw new InvalidOperationException("Production environment/database guard");
 }
 public static void RequireApplyConfirmation(string mode,string[] args,string hash){
  if(mode=="--apply"&&(!args.Contains("--confirm-database=PMR_SPC_2026")||!args.Contains("--confirm-source-sha256="+hash)))throw new InvalidOperationException("Exact production database/source confirmation required");
 }
}
