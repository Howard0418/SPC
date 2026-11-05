using Microsoft.Data.SqlClient;
namespace TestMeasurementCleanup;
internal static class GateSqlRehearsal
{
 public static async Task<int> RunAsync(string connection,string original)
 {
  var b=new SqlConnectionStringBuilder(connection);if(b.DataSource!="172.16.110.16"||b.InitialCatalog!="PMR_SPC_TEST")throw new InvalidOperationException("Test maintenance connection required");
  var name="SPC_FORMULA_SCHEMA_VERIFY_CLEANUP_"+Guid.NewGuid().ToString("N");var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master",Pooling=false};
  await using var admin=new SqlConnection(master.ConnectionString);await admin.OpenAsync();
  async Task Exec(SqlConnection c,string sql){await using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.CommandTimeout=60;await cmd.ExecuteNonQueryAsync();}
  await Exec(admin,$"CREATE DATABASE [{name}]");int checks=0;
  try
  {
   await using(var fixture=new SqlConnection(new SqlConnectionStringBuilder(connection){InitialCatalog=name,Pooling=false}.ConnectionString))
   {
    await fixture.OpenAsync();await Exec(fixture,"""
     CREATE TABLE VariableMeasurements(Id int PRIMARY KEY,PartProcessCharacteristicId int,ChemicalReceiptId uniqueidentifier NULL,ChemicalSourceKeyHash nvarchar(64) NULL,MeasuredValue decimal(18,8),RecheckValue decimal(18,8) NULL,MeasuredAt datetime2,IsDeleted bit,UpdatedAt datetime2 NULL,UpdatedBy nvarchar(100) NULL,SourceReference nvarchar(100) NULL,RowVersion rowversion);
     CREATE TABLE ChemicalFormulaPolicies(PartProcessCharacteristicId int,IsEnabled bit);
     CREATE TABLE ChemicalCalculationReceipts(Id uniqueidentifier,PartProcessCharacteristicId int,SourceKeyHash nvarchar(64));
     INSERT ChemicalFormulaPolicies VALUES(1,1);
     INSERT VariableMeasurements(Id,PartProcessCharacteristicId,ChemicalReceiptId,ChemicalSourceKeyHash,MeasuredValue,RecheckValue,MeasuredAt,IsDeleted,UpdatedAt,UpdatedBy,SourceReference) VALUES(1,1,NULL,NULL,5,NULL,'2026-10-01',0,NULL,NULL,'original'),(2,1,NULL,NULL,5,NULL,'2026-10-01',0,NULL,NULL,'original');
     """);
    async Task Reject(string sql){try{await Exec(fixture,sql);throw new InvalidOperationException("Expected gate rejection");}catch(SqlException e)when(e.Number==51110){checks++;}}
    await Exec(fixture,original);await Reject("UPDATE VariableMeasurements SET IsDeleted=1 WHERE Id=1");
    var changed=LegacyArchiveGate.Build(original,["Id","PartProcessCharacteristicId","ChemicalReceiptId","ChemicalSourceKeyHash","MeasuredValue","RecheckValue","MeasuredAt","IsDeleted","UpdatedAt","UpdatedBy","SourceReference"]);await Exec(fixture,changed);
    await Reject("UPDATE VariableMeasurements SET IsDeleted=1,MeasuredValue=99 WHERE Id=1");
    await Reject("UPDATE VariableMeasurements SET IsDeleted=1,SourceReference='changed' WHERE Id=1");
    await Reject("UPDATE VariableMeasurements SET MeasuredValue=99 WHERE Id=1");
    await Reject("INSERT VariableMeasurements(Id,PartProcessCharacteristicId,IsDeleted)VALUES(3,1,0)");
    await Reject("INSERT VariableMeasurements(Id,PartProcessCharacteristicId,IsDeleted)VALUES(3,1,1)");
    await Exec(fixture,"UPDATE VariableMeasurements SET IsDeleted=1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy='authorized cleanup'");checks++;
    await Reject("UPDATE VariableMeasurements SET IsDeleted=0 WHERE Id=1");
    await Reject("UPDATE VariableMeasurements SET UpdatedBy='later edit' WHERE Id=1");
    await using var count=fixture.CreateCommand();count.CommandText="SELECT COUNT(*) FROM VariableMeasurements WHERE IsDeleted=1 AND MeasuredValue=5 AND SourceReference='original'";if(Convert.ToInt32(await count.ExecuteScalarAsync())!=2)throw new InvalidOperationException("Fixture data changed");checks++;
   }
  }
  finally{if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"\ASPC_FORMULA_SCHEMA_VERIFY_CLEANUP_[a-f0-9]{32}\z"))throw new InvalidOperationException("Unexpected isolated database name");await Exec(admin,$"DROP DATABASE [{name}]");}
  return checks;
 }
}
