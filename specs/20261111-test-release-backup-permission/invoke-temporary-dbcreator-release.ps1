param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [Parameter(Mandatory = $true)][string]$ErrorPath
)

$ErrorActionPreference = 'Stop'
$principal = 'SpcAuditApiTest003'
$testDatabase = 'PMR_SPC_TEST'
$adminSettings = 'D:\SPC\release\production\backend\appsettings.json'
$appSettings = 'D:\SPC\release\test\backend\appsettings.json'
$releaseScript = 'D:\SPC\tools\release\Invoke-Release.ps1'

function New-TestConnection([string]$settingsPath) {
    $cfg = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $raw = ([string]$cfg.ConnectionStrings.SqlServer).Replace('Trust Server Certificate', 'TrustServerCertificate')
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($raw)
    $builder['Initial Catalog'] = $testDatabase
    if ($builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$' -or $builder.InitialCatalog -cne $testDatabase) {
        throw 'Refusing non-test SQL target.'
    }
    return [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
}

function Invoke-AdminSql([string]$sql) {
    $connection = New-TestConnection $adminSettings
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

function Read-AppRoleState {
    $connection = New-TestConnection $appSettings
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT CONCAT(DB_NAME(), N'|', ORIGINAL_LOGIN(), N'|', COALESCE(CONVERT(nvarchar(10),IS_SRVROLEMEMBER(N'dbcreator')),N'NULL'), N'|', COALESCE(CONVERT(nvarchar(10),IS_SRVROLEMEMBER(N'sysadmin')),N'NULL'), N'|', COALESCE(CONVERT(nvarchar(10),HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','BACKUP DATABASE')),N'NULL'))"
        return [string]$command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

$before = Read-AppRoleState
if ($before -ne 'PMR_SPC_TEST|SpcAuditApiTest003|0|0|1') {
    throw "Unexpected pre-grant role state: $before"
}

$releaseExitCode = $null
$revokeVerified = $false
try {
    $null = Invoke-AdminSql "IF DB_NAME()<>N'PMR_SPC_TEST' THROW 51000,'Wrong database',1; IF IS_SRVROLEMEMBER(N'dbcreator',N'SpcAuditApiTest003')<>0 THROW 51001,'Preexisting dbcreator membership',1; ALTER SERVER ROLE [dbcreator] ADD MEMBER [SpcAuditApiTest003]; SELECT IS_SRVROLEMEMBER(N'dbcreator',N'SpcAuditApiTest003');"
    $during = Read-AppRoleState
    if ($during -ne 'PMR_SPC_TEST|SpcAuditApiTest003|1|0|1') {
        throw "Temporary role verification failed: $during"
    }

    $commandText = @"
`$ErrorActionPreference = 'Stop'
& '$releaseScript' -System SPC -Environment Test -Mode Apply -ManifestPath '$ManifestPath' -ApplicationAuthorized -DatabaseAuthorized -Confirm:`$false
exit `$LASTEXITCODE
"@
    $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($commandText))
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encodedCommand)
    $pwsh = (Get-Command pwsh.exe -ErrorAction Stop).Source
    $process = Start-Process -FilePath $pwsh -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $OutputPath -RedirectStandardError $ErrorPath
    $releaseExitCode = $process.ExitCode
}
finally {
    $null = Invoke-AdminSql "IF IS_SRVROLEMEMBER(N'dbcreator',N'SpcAuditApiTest003')=1 ALTER SERVER ROLE [dbcreator] DROP MEMBER [SpcAuditApiTest003]; SELECT IS_SRVROLEMEMBER(N'dbcreator',N'SpcAuditApiTest003');"
    $after = Read-AppRoleState
    $revokeVerified = $after -eq 'PMR_SPC_TEST|SpcAuditApiTest003|0|0|1'
    if (-not $revokeVerified) {
        throw "Temporary dbcreator role was not revoked: $after"
    }
}

[pscustomobject]@{
    Database = $testDatabase
    Principal = $principal
    TemporaryRole = 'dbcreator'
    SysadminGranted = $false
    ReleaseExitCode = $releaseExitCode
    RevokeVerified = $revokeVerified
    FinalRoleState = Read-AppRoleState
} | ConvertTo-Json

if ($releaseExitCode -ne 0) {
    exit $releaseExitCode
}
