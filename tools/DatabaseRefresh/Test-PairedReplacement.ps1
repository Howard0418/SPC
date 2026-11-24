[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^PMR_SPC_2026-REFRESH-SOURCE-1124-[a-f0-9]{32}\.bak$')][string]$SourceBackup,
    [Parameter(Mandatory)][ValidatePattern('^PMR_SPC_TEST-REFRESH-IDENTITY-1124-[a-f0-9]{32}\.bak$')][string]$TargetBackup
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-Catalog.ps1')
. (Join-Path $PSScriptRoot 'Compare-Catalog.ps1')
. (Join-Path $PSScriptRoot 'Get-ColumnPlan.ps1')
. (Join-Path $PSScriptRoot 'Get-TransferOrder.ps1')
$source = Get-RefreshCatalog 'D:/SPC/release/production/backend/appsettings.json' PMR_SPC_2026
$target = Get-RefreshCatalog 'D:/SPC/release/test/backend/appsettings.json' PMR_SPC_TEST
$policies = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'Table-Policies.psd1')
$null = Compare-RefreshCatalog $source $target $policies
$order = Get-RefreshTransferOrder $policies $target.ForeignKeys
if ($order.BlockedByCycle.Count -or $order.RetainedReferences.Count -or $order.InsertOrder.Count -ne 69) {
    throw 'Unexpected foreign-key layout.'
}
$columns = @(Get-RefreshColumnPlan $order.InsertOrder $target.Columns)
$projection = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ChemicalStatisticsHistory.sql') -Raw
$engine = @{
    Replacement = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-IsolatedReplacement.ps1') -Raw
    Lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Refresh-Lock.ps1') -Raw
    Mapping = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Operator-Mapping.ps1') -Raw
    Projection = $projection
    Order = $order.InsertOrder
    Columns = $columns
    Triggers = @($target.Triggers | Where-Object { !$_.Disabled -and $_.Table -in $order.InsertOrder })
    Retained = @(foreach ($table in ($policies.Keys | Sort-Object)) {
        if ($policies[$table] -notin 'KeepTestSettings','KeepMigrationHistory') { continue }
        $metadata = @($target.Columns | Where-Object Table -eq $table)
        $keys = @($metadata | Where-Object KeyOrdinal -gt 0 | Sort-Object KeyOrdinal)
        if (!$keys.Count) { throw "Retained table lacks a primary key: $table." }
        $names = @($metadata | Where-Object {
            $table -notin 'SpcReportSchedules','SpcAlertNotificationSettings' -or
                ($_.Writable -and $_.Name -ne 'RecipientOperatorIdsJson')
        } | Select-Object -ExpandProperty Name)
        [pscustomobject]@{Table=$table; Names=$names; Keys=@($keys.Name)}
    })
    SourceBackup = $SourceBackup
    TargetBackup = $TargetBackup
}

Invoke-Command -ComputerName PMR-SPC-SRV.pmr.com.tw -Authentication Kerberos -ArgumentList $engine -ScriptBlock {
    param($engine)
    $ErrorActionPreference = 'Stop'
    if ($env:COMPUTERNAME -ne 'PMR-SPC-SRV') { throw 'Wrong rehearsal host.' }
    $work = Join-Path 'C:/ProgramData/PMR-Releases/Production' ('refresh-paired-rehearsal-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -Path $work -ItemType Directory
    foreach ($item in @{ 'Invoke-IsolatedReplacement.ps1' = $engine.Replacement; 'ChemicalStatisticsHistory.sql' = $engine.Projection }.GetEnumerator()) {
        [IO.File]::WriteAllText((Join-Path $work $item.Key), $item.Value, [Text.UTF8Encoding]::new($true))
    }
    . (Join-Path $work 'Invoke-IsolatedReplacement.ps1')
    . ([scriptblock]::Create($engine.Lock))
    . ([scriptblock]::Create($engine.Mapping))
    $config = Get-Content 'C:/ProgramData/PMR-Releases/Production/SPC-PROD-1123-AUDIT-WEB147/appsettings.original.json' -Raw | ConvertFrom-Json
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.SqlServer)
    if ($builder.DataSource -ne '172.16.110.16' -or $builder.InitialCatalog -ne 'PMR_SPC_2026') { throw 'Wrong SQL endpoint.' }
    $builder['Initial Catalog'] = 'master'
    $builder['Pooling'] = $false
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    $suffix = [guid]::NewGuid().ToString('N')
    $source = "SPC_Refresh_Rehearsal_Snapshot_$suffix"
    $target = "SPC_Refresh_Rehearsal_Target_$suffix"
    $created = [Collections.Generic.List[string]]::new()

    function Read-Rows([string]$Sql) {
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $Sql
            $command.CommandTimeout = 120
            $data = [System.Data.DataTable]::new()
            $data.Load($command.ExecuteReader())
            $data.Rows
        }
        finally { $command.Dispose() }
    }

    try {
        $connection.Open()
        if ($connection.Database -ne 'master') { throw 'Wrong maintenance database.' }
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 600
        $paths = Read-Rows "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) AS BackupRoot,CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')) AS DataRoot,CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath')) AS LogRoot;"
        $available = Read-Rows "SELECT MIN(v.available_bytes) AS FreeBytes FROM sys.master_files f CROSS APPLY sys.dm_os_volume_stats(f.database_id,f.file_id) v WHERE f.database_id IN(DB_ID(N'PMR_SPC_2026'),DB_ID(N'PMR_SPC_TEST'));"
        [decimal]$required = 1GB
        $restores = foreach ($item in @(
            @{ Db='PMR_SPC_2026'; Scratch=$source; File=$engine.SourceBackup; ReadOnly=$true },
            @{ Db='PMR_SPC_TEST'; Scratch=$target; File=$engine.TargetBackup; ReadOnly=$false }
        )) {
            $path = Join-Path $paths.BackupRoot $item.File
            $command.Parameters.Clear()
            $null=$command.Parameters.AddWithValue('@path',$path)
            $null=$command.Parameters.AddWithValue('@db',$item.Db)
            $command.CommandText=@'
IF NOT EXISTS(SELECT 1 FROM msdb.dbo.backupset s JOIN msdb.dbo.backupmediafamily f ON f.media_set_id=s.media_set_id
    WHERE s.database_name=@db AND s.is_copy_only=1 AND s.has_backup_checksums=1 AND f.physical_device_name=@path)
    THROW 51003,'Backup provenance mismatch',1;
RESTORE FILELISTONLY FROM DISK=@path;
'@
            $reader = $command.ExecuteReader()
            $files = @(while ($reader.Read()) {
                [pscustomobject]@{ Name=[string]$reader['LogicalName']; Type=[string]$reader['Type']; Size=[decimal]$reader['Size'] }
            })
            $reader.Dispose()
            if ($files.Count -ne 2 -or @($files | Where-Object Type -eq 'D').Count -ne 1 -or @($files | Where-Object Type -eq 'L').Count -ne 1) {
                throw 'Unexpected backup file layout.'
            }
            $moves = foreach ($file in $files) {
                $required += $file.Size
                $dest = if ($file.Type -eq 'D') { Join-Path $paths.DataRoot ($item.Scratch+'.mdf') } else { Join-Path $paths.LogRoot ($item.Scratch+'.ldf') }
                "MOVE N'$($file.Name.Replace("'","''"))' TO N'$($dest.Replace("'","''"))'"
            }
            [pscustomobject]@{ Path=$path; Name=$item.Scratch; ReadOnly=$item.ReadOnly; Moves=($moves -join ',') }
        }
        if ($available.FreeBytes -eq [DBNull]::Value -or [decimal]$available.FreeBytes -lt $required) { throw 'Insufficient verified SQL capacity.' }
        foreach ($item in $restores) {
            $name = $item.Name
            $command.Parameters.Clear()
            $command.CommandText = "IF DB_ID(N'$name') IS NOT NULL THROW 51004,'Scratch already exists',1;"
            $null = $command.ExecuteNonQuery()
            $created.Add($name)
            $null = $command.Parameters.AddWithValue('@path',$item.Path)
            $command.CommandText = "RESTORE DATABASE [$name] FROM DISK=@path WITH CHECKSUM, RESTRICTED_USER, $($item.Moves);"
            if ($item.ReadOnly) { $command.CommandText += " ALTER DATABASE [$name] SET READ_ONLY;" }
            $null = $command.ExecuteNonQuery()
        }
        $command.Parameters.Clear()
        $connection.ChangeDatabase($target)
        $sourcePeople = @(Read-Rows "SELECT Id,OperatorCode,Username FROM [$source].dbo.Operators;")
        $targetPeople = @(Read-Rows 'SELECT Id,OperatorCode,Username FROM dbo.Operators;')
        $requiredIds = @(Read-Rows @'
SELECT Id FROM dbo.Operators WHERE NULLIF(LTRIM(RTRIM(PasswordHash)), N'') IS NOT NULL
UNION SELECT CONVERT(int,j.value) FROM dbo.SpcReportSchedules CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j
UNION SELECT CONVERT(int,j.value) FROM dbo.SpcAlertNotificationSettings CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j
UNION SELECT 18;
'@ | ForEach-Object { [int]$_.Id })
        $mapping = @(Get-RefreshOperatorMapping $sourcePeople $targetPeople $requiredIds -TestOnlyOperators @{18='P22077'})
        $columns = @{}
        $identity = foreach ($plan in $engine.Columns) {
            $columns[$plan.Table] = @($plan.WriteColumns)
            if ($plan.IdentityColumns.Count) { $plan.Table }
        }
        $triggers = @{}
        foreach ($trigger in $engine.Triggers) { $triggers[$trigger.Table] = @($triggers[$trigger.Table] | Where-Object { $_ }) + @($trigger.Name) }
        Invoke-WithRefreshOperationLock -Connection $connection -Operation {
            $fingerprints = foreach ($item in $engine.Retained) {
                $names = ($item.Names | ForEach-Object { "[$_]" }) -join ','
                $keys = ($item.Keys | ForEach-Object { "[$_]" }) -join ','
                "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT $names FROM dbo.[$($item.Table)] ORDER BY $keys FOR JSON PATH,INCLUDE_NULL_VALUES)),2) AS Signature"
            }
            $fingerprints += @(
                "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT name,is_disabled,OBJECT_DEFINITION(object_id) AS body FROM sys.triggers WHERE parent_class=1 ORDER BY name FOR JSON PATH,INCLUDE_NULL_VALUES)),2) AS Signature"
            )
            $check = $fingerprints -join ' UNION ALL '
            $before = @(Read-Rows $check | Select-Object -ExpandProperty Signature)
            Invoke-IsolatedReplacement $connection $source $engine.Order $columns -IdentityTables @($identity) -OperatorMapping $mapping -MaintenanceTriggers $triggers
            $after = @(Read-Rows $check | Select-Object -ExpandProperty Signature)
            if ($before.Count -ne $fingerprints.Count -or ($before -join '|') -cne ($after -join '|')) {
                throw 'Retained settings or trigger fingerprint mismatch.'
            }
        }
        $proof = Read-Rows @'
SELECT (SELECT COUNT(*) FROM sys.triggers WHERE parent_class=1 AND is_disabled=0) AS EnabledTriggers,
       (SELECT COUNT(*) FROM dbo.Operators WHERE Id=18 AND Username=N'P22077' AND PasswordHash IS NOT NULL) AS RetainedOperator,
       (SELECT COUNT(*) FROM dbo.ChemicalStatisticsJobs WHERE Status<>N'Completed' AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc<=SYSUTCDATETIME())) AS EligibleStatistics;
'@
        if ($proof.EnabledTriggers -ne $engine.Triggers.Count -or $proof.RetainedOperator -ne 1 -or $proof.EligibleStatistics -ne 0) {
            throw 'Post-replacement evidence mismatch.'
        }
        "Replacement and row verification completed for $($engine.Order.Count) tables; retained settings and trigger signatures match."
    }
    finally {
        try {
            if ($connection.State -eq [System.Data.ConnectionState]::Open) {
                $connection.ChangeDatabase('master')
                foreach ($name in $created) {
                    if ($name -cnotmatch '^SPC_Refresh_Rehearsal_(Snapshot|Target)_[a-f0-9]{32}$') { throw 'Unsafe cleanup target.' }
                    $cleanup = $connection.CreateCommand()
                    $cleanup.CommandText = "IF DB_ID(N'$name') IS NOT NULL DROP DATABASE [$name]; IF DB_ID(N'$name') IS NOT NULL THROW 51008,'Scratch cleanup failed',1;"
                    $null = $cleanup.ExecuteNonQuery()
                    $cleanup.Dispose()
                }
                if ($created.Count) { "Rehearsal databases cleaned up: $($created.Count)" }
            }
        }
        finally { $connection.Dispose() }
    }
}
