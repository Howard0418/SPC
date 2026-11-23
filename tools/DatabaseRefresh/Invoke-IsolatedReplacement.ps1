function Invoke-IsolatedReplacement {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][string]$Snapshot,
        [Parameter(Mandatory)][string[]]$InsertOrder,
        [Parameter(Mandatory)][hashtable]$Columns,
        [string[]]$IdentityTables = @()
    )

    if ($Connection.DataSource -cne 'localhost' -or
        $Connection.Database -cnotmatch '^SPC_Refresh_Rehearsal_Target_([a-f0-9]{32})$' -or
        $Snapshot -cne ('SPC_Refresh_Rehearsal_Snapshot_' + $Matches[1])) {
        throw 'Isolated replacement requires the matching local rehearsal databases.'
    }
    $sql = [System.Collections.Generic.List[string]]::new()
    $sql.Add("IF APPLOCK_MODE('public',N'SPC.DatabaseRefresh','Session') <> N'Exclusive' THROW 51020,'Exclusive refresh lock required',1;")
    $sql.Add("IF COALESCE(CONVERT(nvarchar(20),DATABASEPROPERTYEX(N'$Snapshot','Updateability')),N'') <> N'READ_ONLY' THROW 51021,'Read-only source snapshot required',1;")
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
    for ($index = $InsertOrder.Count - 1; $index -ge 0; $index--) {
        $sql.Add("DELETE FROM dbo.[$($InsertOrder[$index])];")
    }
    foreach ($table in $InsertOrder) {
        $list = ($Columns[$table] | ForEach-Object { "[$_]" }) -join ','
        $from = "[$Snapshot].dbo.[$table]"
        $to = "dbo.[$table]"
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
    $sql.Add('COMMIT; END TRY BEGIN CATCH IF XACT_STATE() <> 0 ROLLBACK; THROW; END CATCH;')
    $command = $Connection.CreateCommand()
    try {
        $command.CommandTimeout = 60
        $command.CommandText = $sql -join [Environment]::NewLine
        $null = $command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}
