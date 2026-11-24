function Invoke-IsolatedReplacement {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][string]$Snapshot,
        [Parameter(Mandatory)][string[]]$InsertOrder,
        [Parameter(Mandatory)][hashtable]$Columns,
        [string[]]$IdentityTables = @(),
        [object[]]$OperatorMapping = @(),
        [hashtable]$MaintenanceTriggers = @{}
    )

    $isLocal = $Connection.DataSource -ceq 'localhost'
    $isRehearsalServer = $Connection.DataSource -cmatch '^(tcp:)?172\.16\.110\.16(,1433)?$'
    $target = $Connection.Database
    if ((!$isLocal -and !$isRehearsalServer) -or
        $target -cnotmatch '^SPC_Refresh_Rehearsal_Target_([a-f0-9]{32})$' -or
        $Snapshot -cne ('SPC_Refresh_Rehearsal_Snapshot_' + $Matches[1])) {
        throw 'Isolated replacement requires matching rehearsal databases on a known SQL endpoint.'
    }
    $sql = [System.Collections.Generic.List[string]]::new()
    $sql.Add("IF DB_NAME() <> N'$target' THROW 51028,'Rehearsal target changed',1;")
    $sql.Add("IF APPLOCK_MODE('public',N'SPC.DatabaseRefresh','Session') <> N'Exclusive' THROW 51020,'Exclusive refresh lock required',1;")
    $sql.Add("IF COALESCE(CONVERT(nvarchar(20),DATABASEPROPERTYEX(N'$Snapshot','Updateability')),N'') <> N'READ_ONLY' THROW 51021,'Read-only source snapshot required',1;")
    if ($isRehearsalServer) {
        $sql.Add("IF (SELECT COUNT(*) FROM sys.databases WHERE user_access_desc=N'RESTRICTED_USER' AND ((name=N'$Snapshot' AND is_read_only=1) OR (name=N'$target' AND is_read_only=0)))<>2 THROW 51029,'Restricted rehearsal pair required',1;")
    }
    $sql.Add('SET XACT_ABORT ON; BEGIN TRY BEGIN TRANSACTION;')
    $ordered = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($table in $InsertOrder) {
        if ($table -cnotmatch '^[A-Za-z_][A-Za-z0-9_]*$' -or !$ordered.Add($table) -or
            !$Columns.ContainsKey($table) -or !$Columns[$table].Count -or
            @($Columns[$table] | Where-Object { $_ -cnotmatch '^[A-Za-z_][A-Za-z0-9_]*$' }).Count) {
            throw 'Invalid rehearsal table or column plan.'
        }
    }
    if ($ordered.Count -ne $Columns.Count) { throw 'The rehearsal plan includes unordered tables.' }
    foreach ($table in $IdentityTables) {
        if (!$ordered.Contains($table)) { throw 'An identity table is outside the rehearsal plan.' }
    }
    $dispatchGuards = @{
        MesSyncMessages = "WHERE SyncStatus = N'Pending'"
        CalibrationNotifications = "WHERE State = N'Pending' OR (State = N'Failed' AND NextAttemptAt IS NOT NULL)"
        CalibrationCertificates = ''
    }
    foreach ($table in $dispatchGuards.Keys | Sort-Object) {
        if ($ordered.Contains($table)) {
            $sql.Add("IF EXISTS(SELECT 1 FROM [$Snapshot].dbo.[$table] $($dispatchGuards[$table])) THROW 51023,'Unisolated source work: $table',1;")
        }
    }
    if ($ordered.Contains('Operators')) {
        if ('PasswordHash' -notin $Columns.Operators -or 'Id' -notin $Columns.Operators) {
            throw 'Operator replacement requires the ID and credential columns.'
        }
        $sql.Add(@"
SELECT TargetId, SourceId, RetainTestOnly INTO #OperatorMapping
FROM OPENJSON(@operatorMapping) WITH (TargetId int, SourceId int, RetainTestOnly bit);
IF EXISTS(SELECT TargetId FROM #OperatorMapping GROUP BY TargetId HAVING COUNT(*)<>1)
   OR EXISTS(SELECT SourceId FROM #OperatorMapping WHERE SourceId IS NOT NULL GROUP BY SourceId HAVING COUNT(*)<>1)
   OR EXISTS(SELECT 1 FROM #OperatorMapping m LEFT JOIN dbo.Operators t ON m.TargetId=t.Id
       LEFT JOIN [$Snapshot].dbo.Operators s ON s.Id=COALESCE(m.SourceId,m.TargetId)
       WHERE t.Id IS NULL OR m.RetainTestOnly IS NULL
          OR (m.RetainTestOnly=1 AND (m.SourceId IS NOT NULL OR s.Id IS NOT NULL))
          OR (m.RetainTestOnly=0 AND (m.SourceId IS NULL OR s.Id IS NULL)))
   OR EXISTS(SELECT 1 FROM dbo.Operators t WHERE NULLIF(LTRIM(RTRIM(t.PasswordHash)),N'') IS NOT NULL
       AND NOT EXISTS(SELECT 1 FROM #OperatorMapping m WHERE m.TargetId=t.Id))
    THROW 51024,'Invalid or incomplete operator mapping',1;
SELECT t.* INTO #TestOperators FROM dbo.Operators t JOIN #OperatorMapping m ON m.TargetId=t.Id;
"@)
        foreach ($settings in 'SpcReportSchedules', 'SpcAlertNotificationSettings') {
            if ($ordered.Contains($settings)) { throw 'Preserved notification settings cannot be replaced.' }
            $sql.Add(@"
IF EXISTS (SELECT 1 FROM dbo.[$settings]
    WHERE COALESCE(ISJSON(RecipientOperatorIdsJson, ARRAY),0) <> 1)
    THROW 51025,'Invalid recipient array in $settings',1;
IF EXISTS (SELECT 1 FROM dbo.[$settings] s CROSS APPLY OPENJSON(s.RecipientOperatorIdsJson) j
    LEFT JOIN #OperatorMapping m ON m.TargetId=TRY_CONVERT(int,j.value)
    WHERE j.type<>2 OR m.TargetId IS NULL)
    THROW 51026,'Invalid or unmapped recipient in $settings',1;
UPDATE s SET RecipientOperatorIdsJson=r.Recipients
FROM dbo.[$settings] s CROSS APPLY (
    SELECT N'['+STRING_AGG(CONVERT(nvarchar(max),COALESCE(m.SourceId,m.TargetId)),N',')
        WITHIN GROUP (ORDER BY CONVERT(int,j.[key]))+N']' AS Recipients,
        MAX(CASE WHEN m.SourceId<>m.TargetId THEN 1 ELSE 0 END) AS Changed
    FROM OPENJSON(s.RecipientOperatorIdsJson) j
    JOIN #OperatorMapping m ON m.TargetId=CONVERT(int,j.value)
) r WHERE r.Changed=1;
"@)
        }
    }
    $restoreTriggers = [System.Collections.Generic.List[string]]::new()
    foreach ($table in $MaintenanceTriggers.Keys | Sort-Object) {
        if (!$ordered.Contains($table)) { throw 'A maintenance trigger table is outside the rehearsal plan.' }
        foreach ($trigger in $MaintenanceTriggers[$table]) {
            if ($trigger -cnotmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Invalid maintenance trigger name.' }
            $sql.Add("IF NOT EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID(N'dbo.[$trigger]') AND parent_id=OBJECT_ID(N'dbo.[$table]') AND is_disabled=0) THROW 51027,'Unexpected trigger state or table',1;")
            $sql.Add("DISABLE TRIGGER dbo.[$trigger] ON dbo.[$table];")
            $restoreTriggers.Add("ENABLE TRIGGER dbo.[$trigger] ON dbo.[$table];")
        }
    }
    for ($index = $InsertOrder.Count - 1; $index -ge 0; $index--) {
        $sql.Add("DELETE FROM dbo.[$($InsertOrder[$index])];")
    }
    foreach ($table in $InsertOrder) {
        $list = ($Columns[$table] | ForEach-Object { "[$_]" }) -join ','
        $from = "[$Snapshot].dbo.[$table]"
        $to = "dbo.[$table]"
        if ($table -eq 'ChemicalStatisticsJobs') {
            $projection = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ChemicalStatisticsHistory.sql') -Raw).Trim().TrimEnd(';')
            $from = '(' + $projection.Replace('{{StatisticsTable}}', $from) + ') AS expected'
        }
        if ($table -eq 'Operators') {
            $operatorColumns = ($Columns.Operators | ForEach-Object {
                if ($_ -eq 'PasswordHash') { 't.[PasswordHash]' } else { "s.[$_]" }
            }) -join ','
            $from = @"
(SELECT $operatorColumns FROM $from s
 LEFT JOIN #OperatorMapping m ON m.SourceId=s.Id AND m.RetainTestOnly=0
 LEFT JOIN #TestOperators t ON t.Id=m.TargetId
 UNION ALL SELECT $list FROM #TestOperators t
 WHERE EXISTS(SELECT 1 FROM #OperatorMapping m WHERE m.TargetId=t.Id AND m.RetainTestOnly=1)) AS expected
"@
        }
        if ($table -in $IdentityTables) {
            $sql.Add(@"
SET IDENTITY_INSERT $to ON;
BEGIN TRY
    INSERT INTO $to ($list) SELECT $list FROM $from;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT $to OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT $to OFF;
"@)
        } else {
            $sql.Add("INSERT INTO $to ($list) SELECT $list FROM $from;")
        }
        $sql.Add(@"
IF (SELECT COUNT_BIG(*) FROM $from) <> (SELECT COUNT_BIG(*) FROM $to)
   OR EXISTS(SELECT $list FROM $from EXCEPT SELECT $list FROM $to)
   OR EXISTS(SELECT $list FROM $to EXCEPT SELECT $list FROM $from)
   THROW 51022,'Rehearsal comparison failed: $table',1;
"@)
    }
    $sql.AddRange($restoreTriggers)
    if ($ordered.Contains('Operators')) { $sql.Add('DROP TABLE #TestOperators, #OperatorMapping;') }
    $sql.Add('COMMIT; END TRY BEGIN CATCH IF XACT_STATE() <> 0 ROLLBACK; THROW; END CATCH;')
    $command = $Connection.CreateCommand()
    try {
        $command.CommandTimeout = 60
        $command.CommandText = $sql -join [Environment]::NewLine
        if ($ordered.Contains('Operators')) {
            $parameter = $command.Parameters.Add('@operatorMapping', [System.Data.SqlDbType]::NVarChar, -1)
            $parameter.Value = ConvertTo-Json -InputObject $OperatorMapping -Compress
        }
        $null = $command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}
