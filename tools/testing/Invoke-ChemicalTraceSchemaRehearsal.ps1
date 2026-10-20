param([string]$TestSettingsPath = (Join-Path $PSScriptRoot '..\..\release\test\backend\appsettings.json'))
$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskCfg=Get-Content -LiteralPath $TestSettingsPath -Raw|ConvertFrom-Json
if($taskCfg.AppEnvironment -ne 'test'){throw 'Only the confirmed test source is permitted.'}
$taskBuilder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new([string]$taskCfg.ConnectionStrings.SqlServer)
if($taskBuilder.InitialCatalog -ne 'PMR_SPC_TEST'){throw 'Unexpected source database.'}
& dotnet build (Join-Path $taskRepo 'tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj') --no-restore -p:DefineConstants=CHEMICAL_TRACE_SQL_TESTS -p:UseSharedCompilation=false --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Compile failed; no backup or isolation database was created.'}
$taskNonce=[Guid]::NewGuid().ToString('N').Substring(0,8)
$taskDate=[TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTime]::UtcNow,'Taipei Standard Time').ToString('yyyyMMdd')
$taskName='SPC_FORMULA_SCHEMA_VERIFY_'+$taskDate+'_'+$taskNonce
if($taskName -notmatch '^SPC_FORMULA_SCHEMA_VERIFY_\d{8}_[a-f0-9]{8}$'){throw 'Unsafe isolation name.'}
$taskSource=$taskBuilder.InitialCatalog
$taskBuilder['Initial Catalog']='master'
$taskDb=[System.Data.SqlClient.SqlConnection]::new($taskBuilder.ConnectionString)
function Invoke-TaskSql([string]$Sql,[hashtable]$Parameters=@{},[switch]$Table){
    $taskCmd=$taskDb.CreateCommand();$taskCmd.CommandText=$Sql;$taskCmd.CommandTimeout=180
    foreach($taskKey in $Parameters.Keys){[void]$taskCmd.Parameters.AddWithValue($taskKey,$Parameters[$taskKey])}
    try{if($Table){$taskRows=[System.Data.DataTable]::new();$taskRows.Load($taskCmd.ExecuteReader());return ,$taskRows};[void]$taskCmd.ExecuteNonQuery()}finally{$taskCmd.Dispose()}
}
try{
    $taskDb.Open()
    $taskPaths=Invoke-TaskSql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) Bak,CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')) Data,CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath')) Log" -Table
    $taskExists=Invoke-TaskSql 'SELECT database_id FROM sys.databases WHERE name=@name' @{'@name'=$taskName} -Table
    if($taskExists.Rows.Count){throw 'Isolation database already exists; refusing overwrite.'}
    $taskBak=Join-Path ([string]$taskPaths.Rows[0].Bak) ($taskName+'-source.bak')
    Invoke-TaskSql "BACKUP DATABASE [$taskSource] TO DISK=@bak WITH COPY_ONLY,CHECKSUM" @{'@bak'=$taskBak}
    Invoke-TaskSql 'RESTORE VERIFYONLY FROM DISK=@bak WITH CHECKSUM' @{'@bak'=$taskBak}
    $taskFiles=Invoke-TaskSql 'RESTORE FILELISTONLY FROM DISK=@bak' @{'@bak'=$taskBak} -Table
    $taskMoves=@();$taskIndex=0
    foreach($taskFile in $taskFiles.Rows){
        if($taskFile.Type -notin @('D','L')){throw 'Unexpected backup file type; no restore attempted.'}
        $taskIndex++;$taskParent=if($taskFile.Type -eq 'L'){[string]$taskPaths.Rows[0].Log}else{[string]$taskPaths.Rows[0].Data}
        $taskExtension=if($taskFile.Type -eq 'L'){'.ldf'}else{'.mdf'}
        $taskPhysical=Join-Path $taskParent ($taskName+'_'+$taskIndex+$taskExtension)
        $taskLogical=([string]$taskFile.LogicalName).Replace("'","''");$taskSafePhysical=$taskPhysical.Replace("'","''")
        $taskMoves+="MOVE N'$taskLogical' TO N'$taskSafePhysical'"
    }
    Invoke-TaskSql ("RESTORE DATABASE [$taskName] FROM DISK=@bak WITH CHECKSUM,RECOVERY,"+($taskMoves -join ',')) @{'@bak'=$taskBak}
    Invoke-TaskSql "USE [$taskName]; EXEC sys.sp_addextendedproperty @name=N'SPC.FormulaTrace.Isolation',@value=N'TASK-002';"
    $taskBuilder['Initial Catalog']=$taskName
    $taskPreviousConnection=$env:SPC_FORMULA_SCHEMA_TEST_CONNECTION
    try{
        $env:SPC_FORMULA_SCHEMA_TEST_CONNECTION=$taskBuilder.ConnectionString
        Write-Output ('ISOLATION_DATABASE='+$taskName)
        & dotnet test (Join-Path $taskRepo 'tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj') --no-build --no-restore --filter ChemicalTraceSqlRehearsalTests -p:DefineConstants=CHEMICAL_TRACE_SQL_TESTS --verbosity quiet --logger 'console;verbosity=normal'
        if($LASTEXITCODE -ne 0){throw 'Isolated SQL tests failed; database retained for diagnosis.'}
    }finally{$env:SPC_FORMULA_SCHEMA_TEST_CONNECTION=$taskPreviousConnection}
    $taskFinalBak=Join-Path ([string]$taskPaths.Rows[0].Bak) ($taskName+'-verified.bak')
    Invoke-TaskSql "BACKUP DATABASE [$taskName] TO DISK=@bak WITH COPY_ONLY,CHECKSUM" @{'@bak'=$taskFinalBak}
    Invoke-TaskSql 'RESTORE VERIFYONLY FROM DISK=@bak WITH CHECKSUM' @{'@bak'=$taskFinalBak}
    Write-Output "ISOLATED_SQL_VERIFIED=$taskName; SOURCE_BUSINESS_DATA_UNCHANGED; BACKUP_VERIFYONLY=PASS"
}finally{$taskDb.Dispose()}
