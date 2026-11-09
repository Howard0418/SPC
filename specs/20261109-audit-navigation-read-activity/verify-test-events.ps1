param([string]$SettingsPath = 'D:\SPC\release\test\backend\appsettings.json')

$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Refusing non-test database settings.'
}
$connectionString = $settings.ConnectionStrings.SqlServer -replace '(?i)Trust Server Certificate', 'TrustServerCertificate'
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @'
SELECT Id, ClientEventId, Environment, EventCode, Outcome, ActorOperatorId,
       ActorAccountSnapshot, ActorDisplayNameSnapshot, ActorRoleSnapshot, SessionIdHash,
       PageKey, RouteTemplate, HttpMethod, HttpStatus, Source, DetailsJson, CorrelationId
FROM dbo.AuditEvents
WHERE Id BETWEEN 95 AND 106
ORDER BY Id;
'@
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    if ($table.Rows.Count -ne 12) { throw "Expected 12 acceptance events; found $($table.Rows.Count)." }

    $codes = @($table.Rows | Group-Object EventCode | ForEach-Object { [pscustomobject]@{ Code = $_.Name; Count = $_.Count } })
    foreach ($required in 'navigation.page.enter','read.query.execute','read.filter.apply','read.export.generate') {
        if (-not ($codes | Where-Object Code -eq $required)) { throw "Missing $required." }
    }
    foreach ($row in $table.Rows) {
        if ($row.Environment -ne 'Test' -or $row.Outcome -ne 'Succeeded' -or $row.ActorOperatorId -ne 27 -or
            $row.ActorAccountSnapshot -ne 'ihao_ting' -or [string]::IsNullOrWhiteSpace($row.ActorDisplayNameSnapshot) -or
            $row.ActorRoleSnapshot -ne 'Editor' -or [string]::IsNullOrWhiteSpace($row.SessionIdHash) -or
            $row.PageKey -ne 'analysis.spc.overall' -or $row.RouteTemplate -ne '/spc' -or
            $row.HttpMethod -ne 'POST' -or $row.HttpStatus -ne 204 -or [string]::IsNullOrWhiteSpace($row.CorrelationId)) {
            throw "Invalid trusted fields at AuditEvents.Id=$($row.Id)."
        }
        $details = $row.DetailsJson | ConvertFrom-Json
        $keys = @($details.PSObject.Properties.Name)
        $allowed = switch ($row.EventCode) {
            'navigation.page.enter' { @('NavigationKind','SchemaVersion') }
            'read.query.execute' { @('FilterFields','ResultCount','DateFromUtc','DateToUtc','SchemaVersion') }
            'read.filter.apply' { @('FilterFields','ResultCount','DateFromUtc','DateToUtc','SchemaVersion') }
            'read.export.generate' { @('ExportFormat','ResultCount','DateFromUtc','DateToUtc','SchemaVersion') }
        }
        if (@($keys | Where-Object { $_ -notin $allowed }).Count -gt 0) { throw "Unexpected detail key at Id=$($row.Id)." }
        if ($row.DetailsJson -match '(?i)authorization|bearer|password|token|secret|ihao_ting') { throw "Sensitive detail found at Id=$($row.Id)." }
        if ($row.EventCode -eq 'navigation.page.enter') {
            if ($row.ClientEventId -is [DBNull]) { throw "Missing ClientEventId at Id=$($row.Id)." }
        }
        elseif ($row.ClientEventId -isnot [DBNull]) { throw "Unexpected ClientEventId at Id=$($row.Id)." }
    }
    $duplicateClientIds = @($table.Rows | Where-Object { $_.ClientEventId -isnot [DBNull] } | Group-Object ClientEventId | Where-Object Count -gt 1).Count
    if ($duplicateClientIds -ne 0) { throw 'Duplicate navigation ClientEventId persisted.' }

    [pscustomobject]@{
        AcceptanceEventIds = '95-106'
        Total = $table.Rows.Count
        Navigation = ($codes | Where-Object Code -eq 'navigation.page.enter').Count
        Query = ($codes | Where-Object Code -eq 'read.query.execute').Count
        Filter = ($codes | Where-Object Code -eq 'read.filter.apply').Count
        Export = ($codes | Where-Object Code -eq 'read.export.generate').Count
        TrustedActorRows = $table.Rows.Count
        UnexpectedDetailKeys = 0
        SensitiveDetailLeaks = 0
        DuplicateClientEventIds = 0
        Environment = 'Test'
        Database = 'PMR_SPC_TEST'
    } | ConvertTo-Json
}
finally {
    $connection.Dispose()
}
