function Get-RefreshConnectionCapabilities {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ConfigurationPath,
        [Parameter(Mandatory)][ValidateSet('PMR_SPC_2026','PMR_SPC_TEST')][string]$ExpectedDatabase
    )
    $config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new(
        ($config.ConnectionStrings.SqlServer -replace '(?i)trust\s+server\s+certificate','TrustServerCertificate'))
    if ($builder.InitialCatalog -cne $ExpectedDatabase -or $builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$') {
        throw 'Unexpected SQL endpoint for refresh capability check.'
    }
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $connection.Open()
        if ($connection.Database -cne $ExpectedDatabase) { throw 'Unexpected live database.' }
        $command = $connection.CreateCommand()
        $command.CommandText = @'
SELECT IS_SRVROLEMEMBER('sysadmin'), IS_ROLEMEMBER('db_owner'),
       HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','BACKUP DATABASE'),
       HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),
       HAS_PERMS_BY_NAME('dbo.Operators','OBJECT','UPDATE'),
       HAS_PERMS_BY_NAME('dbo.AuditEvents','OBJECT','DELETE'),
       HAS_PERMS_BY_NAME('dbo.VariableMeasurements','OBJECT','ALTER');
'@
        $reader = $command.ExecuteReader()
        if (!$reader.Read()) { throw 'Capability probe returned no result.' }
        [pscustomobject]@{
            Database = $ExpectedDatabase
            SystemAdministrator = [int]$reader[0] -eq 1
            DatabaseOwner = [int]$reader[1] -eq 1
            Backup = [int]$reader[2] -eq 1
            ViewDefinition = [int]$reader[3] -eq 1
            OperatorUpdate = [int]$reader[4] -eq 1
            AuditDelete = [int]$reader[5] -eq 1
            MeasurementAlter = [int]$reader[6] -eq 1
        }
    }
    catch { throw "Refresh capability probe failed for $ExpectedDatabase. Connection details withheld." }
    finally { $connection.Dispose() }
}
