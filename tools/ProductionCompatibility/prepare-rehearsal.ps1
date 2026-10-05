param([ValidateSet('SPC','Portal')][string]$Application)
$ErrorActionPreference='Stop'
# Run in Windows PowerShell: installed SQL SMO assemblies target .NET Framework.
$smoDir='C:\Program Files\Microsoft SQL Server\170\Shared\MDS5xSMO'
Add-Type @'
using System;using System.IO;using System.Reflection;
public static class SmoDependencies {
 public static void Register(string path){AppDomain.CurrentDomain.AssemblyResolve+=(sender,args)=>{
  var name=new AssemblyName(args.Name).Name;
  foreach(var a in AppDomain.CurrentDomain.GetAssemblies())if(a.GetName().Name==name)return a;
  var file=Path.Combine(path,name+".dll");return File.Exists(file)?Assembly.LoadFrom(file):null;
 };}
}
'@
[SmoDependencies]::Register($smoDir)
foreach($file in @('System.Runtime.CompilerServices.Unsafe.dll','System.Numerics.Vectors.dll','System.Buffers.dll','System.Memory.dll','System.Threading.Tasks.Extensions.dll','System.Diagnostics.DiagnosticSource.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $smoDir $file))|Out-Null}
foreach($file in @('Microsoft.Data.SqlClient.dll','Microsoft.SqlServer.ConnectionInfo.dll','Microsoft.SqlServer.Management.Sdk.Sfc.dll','Microsoft.SqlServer.Smo.dll','Microsoft.SqlServer.SmoExtended.dll')){
 [Reflection.Assembly]::LoadFrom((Join-Path $smoDir $file))|Out-Null
}
$out='D:\SPC\release-staging\production-compatibility-20260918\rehearsal'
New-Item -ItemType Directory -Path $out -Force|Out-Null
if($Application -eq 'SPC'){
 $cfg=[IO.File]::ReadAllText('D:\SPC\release\production\backend\appsettings.json')|ConvertFrom-Json
 $sourceConnection=$cfg.ConnectionStrings.SqlServer;$expected='PMR_SPC_2026';$target='PMR_COMPAT_SPC_20260918'
}else{
 $sourceConnection=Invoke-Command -ComputerName PMR-WEB.pmr.com.tw -ScriptBlock {
  try { $cfg=[IO.File]::ReadAllText('C:\inetpub\wwwroot\publish\portal-api\appsettings.json')|ConvertFrom-Json -ErrorAction Stop; $cfg.ConnectionStrings.Production }
  catch {throw 'Production config read failed; details withheld'}
 };$expected='PMR_PORTAL_UAT';$target='PMR_COMPAT_PORTAL_20260918'
}
$b=[Microsoft.Data.SqlClient.SqlConnectionStringBuilder]::new($sourceConnection)
if($b.DataSource -ne '172.16.110.16' -or $b.InitialCatalog -ne $expected){throw 'Unexpected source database'}
$sourceSql=[Microsoft.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
$source=[Microsoft.SqlServer.Management.Smo.Server]::new([Microsoft.SqlServer.Management.Common.ServerConnection]::new($sourceSql))
$source.Databases.Refresh()
$database=$source.Databases[$expected]
if($null -eq $database){throw "Source database lookup returned null ($expected); server $($source.Name); visible count $($source.Databases.Count)"}
$transfer=[Microsoft.SqlServer.Management.Smo.Transfer]::new($database)
$transfer.CopyAllTables=$true;$transfer.CopyAllViews=$true;$transfer.CopyAllUserDefinedDataTypes=$true
$transfer.CopyAllUsers=$false;$transfer.CopyAllLogins=$false;$transfer.CopyAllRoles=$false
$transfer.Options.ScriptSchema=$true;$transfer.Options.ScriptData=$false
$transfer.Options.DriAll=$true;$transfer.Options.Indexes=$true;$transfer.Options.Triggers=$false
$transfer.Options.IncludeDatabaseContext=$false;$transfer.Options.ScriptBatchTerminator=$false
$scripts=@($transfer.ScriptTransfer() | Where-Object {$_ -notmatch '(?im)^\s*CREATE\s+(USER|LOGIN)\b'}) # Do not clone production principals.
if($scripts.Count -eq 0){throw 'Empty schema'}
$scripts -join "`r`nGO`r`n"|Set-Content -LiteralPath "$out\$Application-baseline-schema.sql" -Encoding UTF8
$local=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=master;Integrated Security=True;Encrypt=False')
$local.Open()
try{
 $cmd=$local.CreateCommand();$cmd.CommandText='SELECT DB_ID(@name)';$null=$cmd.Parameters.AddWithValue('@name',$target)
 $exists=$cmd.ExecuteScalar() -isnot [DBNull]
 # Target is selected from two constants above, never source configuration.
 $cmd.Parameters.Clear()
 if(!$exists){$cmd.CommandText="CREATE DATABASE [$target]";$cmd.ExecuteNonQuery()|Out-Null}
 $local.ChangeDatabase($target)
 $needsSchema=!$exists
 if($exists){
  $cmd.CommandText='SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0';$tableCount=[int]$cmd.ExecuteScalar()
  $needsSchema=$tableCount -eq 0
  if(!$needsSchema){
  $cmd.CommandText='SELECT COUNT(*) FROM dbo.__EFMigrationsHistory';if([int]$cmd.ExecuteScalar() -ne 0){throw 'Existing rehearsal contains history; refusing overwrite'}
  $cmd.CommandText='SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0';if([int]$cmd.ExecuteScalar() -ne $database.Tables.Count){throw 'Incomplete rehearsal schema; refusing reuse'}
  }
 }
 if($needsSchema){foreach($script in $scripts){
  if($script -match '(?im)^\s*USE\s|CREATE\s+(LOGIN|USER)|ALTER\s+DATABASE'){throw 'Unexpected server/database context command in schema'}
  $cmd.CommandText=$script;$cmd.CommandTimeout=120;$cmd.ExecuteNonQuery()|Out-Null
 }}
 if($sourceSql.State -ne 'Open'){$sourceSql.Open()}
 try{
  $read=$sourceSql.CreateCommand();$read.CommandText='SELECT MigrationId,ProductVersion FROM dbo.__EFMigrationsHistory'
  $reader=$read.ExecuteReader();$bulk=[System.Data.SqlClient.SqlBulkCopy]::new($local);$bulk.DestinationTableName='dbo.__EFMigrationsHistory';$bulk.WriteToServer($reader);$reader.Close();$bulk.Dispose()
 }finally{$sourceSql.Close()}
 @{Application=$Application;Source=$expected;Target=$target;SchemaBatches=$scripts.Count;CopiedData='EF history only';Server='localhost';Prepared=$true}|ConvertTo-Json|Set-Content "$out\$Application-baseline.json" -Encoding UTF8
 Write-Output "$Application schema prepared on localhost/$target; $($scripts.Count) batches; no business data copied."
}finally{$local.Dispose();$source.ConnectionContext.Disconnect()}
