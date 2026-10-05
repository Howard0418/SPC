$ErrorActionPreference='Stop'
$cfg=Get-Content D:/SPC/release/production/backend/appsettings.json -Raw | ConvertFrom-Json
$builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.SqlServer)
if ($builder.DataSource -ne '172.16.110.16' -or $builder.InitialCatalog -ne 'PMR_SPC_2026') { throw 'Unexpected target' }
$con=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$con.Open()
try {
    $cmd=$con.CreateCommand()
    $cmd.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))"
    $backupDir=[string]$cmd.ExecuteScalar()
    if (!$backupDir) {throw 'No server backup directory'}
    $backupFile=$backupDir.TrimEnd('\')+'\PMR_SPC_2026_pre_chemical_transfer_'+[DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss')+'.bak'
    $cmd=$con.CreateCommand(); $cmd.CommandTimeout=900
    $cmd.CommandText='BACKUP DATABASE @database TO DISK=@path WITH COPY_ONLY, CHECKSUM, COMPRESSION'
    [void]$cmd.Parameters.AddWithValue('@database','PMR_SPC_2026')
    [void]$cmd.Parameters.AddWithValue('@path',$backupFile)
    [void]$cmd.ExecuteNonQuery()
    Write-Output 'COPY_ONLY CHECKSUM backup completed; verifying.'
    $cmd=$con.CreateCommand();$cmd.CommandTimeout=900
    $cmd.CommandText='RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM'
    [void]$cmd.Parameters.AddWithValue('@path',$backupFile)
    [void]$cmd.ExecuteNonQuery()
    $record=[ordered]@{Database='PMR_SPC_2026';Server='PMR-MESSQL';Path=$backupFile;Verified=$true;VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');CopyOnly=$true;Checksum=$true}
    $json=$record | ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'backup.json'),$json)
    Write-Output $json
} finally {$con.Dispose()}
