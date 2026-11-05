using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MesSpc.Api.Infrastructure.Data;
namespace TestMeasurementCleanup;
public sealed record TableState(string Table,long Total,long? Active,int? Checksum,string Sha256);
public static class DatabaseInventory
{
 public static async Task<List<TableState>> ReadAsync(AppDbContext db)
 {
  if(db.Database.GetDbConnection().State!=System.Data.ConnectionState.Open)await db.Database.OpenConnectionAsync();
  var tables=new List<(string Name,bool Deleted)>();
  await using(var c=db.Database.GetDbConnection().CreateCommand()){c.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();c.CommandText="SELECT t.name,CASE WHEN EXISTS(SELECT 1 FROM sys.columns c WHERE c.object_id=t.object_id AND c.name='IsDeleted') THEN 1 ELSE 0 END FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' ORDER BY t.name";await using var r=await c.ExecuteReaderAsync();while(await r.ReadAsync())tables.Add((r.GetString(0),r.GetInt32(1)==1));}
  var result=new List<TableState>();foreach(var t in tables){await using var c=db.Database.GetDbConnection().CreateCommand();c.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();var table=t.Name.Replace("]","]]");
   c.CommandText="SELECT STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_primary_key=1 AND i.object_id=OBJECT_ID(@table)";var p=c.CreateParameter();p.ParameterName="@table";p.Value="dbo."+t.Name;c.Parameters.Add(p);var order=Convert.ToString(await c.ExecuteScalarAsync());if(string.IsNullOrWhiteSpace(order))throw new InvalidOperationException("Stable primary key required: "+t.Name);
   c.Parameters.Clear();c.CommandText=$"SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)),{(t.Deleted?"SUM(CASE WHEN IsDeleted=0 THEN CONVERT(bigint,1) ELSE CONVERT(bigint,0) END)":"CAST(NULL AS bigint)")},CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE((SELECT * FROM dbo.[{table}] ORDER BY {order} FOR JSON PATH,INCLUDE_NULL_VALUES),N'[]')),2) FROM dbo.[{table}]";c.CommandTimeout=120;
   await using var r=await c.ExecuteReaderAsync();await r.ReadAsync();result.Add(new(t.Name,r.GetInt64(0),t.Deleted?(r.IsDBNull(2)?0:r.GetInt64(2)):null,r.IsDBNull(1)?null:r.GetInt32(1),r.GetString(3)));}return result;
 }
}
