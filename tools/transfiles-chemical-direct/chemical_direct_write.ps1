$ErrorActionPreference='Stop'
[Console]::InputEncoding=[System.Text.Encoding]::UTF8
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$taskConnection=$null
try {
 $taskInput=[Console]::In.ReadToEnd()|ConvertFrom-Json
 $taskBuilder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:SPC_CHEMICAL_DB_CONNECTION)
 if($taskBuilder['Initial Catalog'] -ne 'PMR_SPC_TEST' -or -not $taskBuilder['Integrated Security']){throw 'Only integrated test DB is permitted'}
 $taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskBuilder.ConnectionString);$taskConnection.Open()
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.CommandType=[System.Data.CommandType]::StoredProcedure;$taskCommand.CommandTimeout=60
 if($taskInput.action -eq 'status'){
  $taskCommand.CommandText='dbo.GetChemicalDirectImportStatus'
  [void]$taskCommand.Parameters.Add('@ReceiptId',[System.Data.SqlDbType]::UniqueIdentifier);$taskCommand.Parameters['@ReceiptId'].Value=[Guid]$taskInput.receiptId
 }else{
  $taskCommand.CommandText='dbo.CommitChemicalMeasurementWithTrace'
  [void]$taskCommand.Parameters.Add('@InitialReceiptId',[System.Data.SqlDbType]::UniqueIdentifier);$taskCommand.Parameters['@InitialReceiptId'].Value=[Guid]$taskInput.initialReceiptId
  [void]$taskCommand.Parameters.Add('@RecheckReceiptId',[System.Data.SqlDbType]::UniqueIdentifier);$taskCommand.Parameters['@RecheckReceiptId'].Value=if($taskInput.recheckReceiptId){[Guid]$taskInput.recheckReceiptId}else{[DBNull]::Value}
 }
 [void]$taskCommand.Parameters.Add('@Actor',[System.Data.SqlDbType]::NVarChar,100);$taskCommand.Parameters['@Actor'].Value=[string]$taskInput.actor
 $taskReader=$taskCommand.ExecuteReader();$taskResult=@{}
 if($taskReader.Read()){for($taskIndex=0;$taskIndex -lt $taskReader.FieldCount;$taskIndex++){if(-not $taskReader.IsDBNull($taskIndex)){$taskResult[$taskReader.GetName($taskIndex)]=$taskReader.GetValue($taskIndex)}}}
 $taskReader.Dispose();[Console]::Write(($taskResult|ConvertTo-Json -Compress))
}catch{[Console]::Error.Write('受控測試資料庫操作失敗；請核對環境、權限及版本／來源衝突。');exit 1}
finally{if($taskConnection){$taskConnection.Dispose()}}
