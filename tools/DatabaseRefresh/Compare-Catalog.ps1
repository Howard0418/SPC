function Compare-RefreshCatalog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Source,
        [Parameter(Mandatory)]$Target,
        [Parameter(Mandatory)][hashtable]$TablePolicies
    )

    if ($Source.Database -cne 'PMR_SPC_2026' -or $Target.Database -cne 'PMR_SPC_TEST') {
        throw 'Unexpected refresh direction. Only PMR_SPC_2026 to PMR_SPC_TEST is supported.'
    }
    if (!$Source.Server -or $Source.Server -ine $Target.Server) {
        throw 'The SQL server identity does not match.'
    }
    if (!$Source.Collation -or $Source.Collation -cne $Target.Collation) {
        throw 'The database collation does not match.'
    }
    $sourceMigrations = @($Source.Migrations | Sort-Object)
    $targetMigrations = @($Target.Migrations | Sort-Object)
    if (!$sourceMigrations.Count -or !$targetMigrations.Count -or
        ($sourceMigrations -join '|') -cne ($targetMigrations -join '|')) {
        throw 'The migration catalogue does not match.'
    }

    $sourceTables = @{}
    $targetTables = @{}
    foreach ($pair in @(@{Catalog = $Source; Index = $sourceTables},
                        @{Catalog = $Target; Index = $targetTables})) {
        foreach ($table in $pair.Catalog.Tables) {
            $name = [string]$table.Name
            if (!$name -or !$table.Signature -or $null -eq $table.Rows -or [long]$table.Rows -lt 0) {
                throw 'A table catalogue entry is incomplete.'
            }
            if ($pair.Index.ContainsKey($name)) { throw "The catalogue contains a duplicate table: $name." }
            $pair.Index.Add($name, $table)
        }
    }
    if (!$sourceTables.Count -or $sourceTables.Count -ne $targetTables.Count) {
        throw 'The table catalogues differ.'
    }
    $allowedPolicies = @('ReplaceBusinessData', 'KeepTestSettings', 'PreserveTestCredentials',
                         'CopyHistoricalAudit', 'CopyWithDispatchIsolation', 'KeepMigrationHistory')
    $tables = foreach ($name in ($sourceTables.Keys | Sort-Object)) {
        if (!$targetTables.ContainsKey($name) -or $sourceTables[$name].Signature -cne $targetTables[$name].Signature) {
            throw "The table definition differs: $name."
        }
        if (!$TablePolicies.ContainsKey($name) -or $TablePolicies[$name] -cnotin $allowedPolicies) {
            throw "Missing or invalid table classification: $name."
        }
        [pscustomobject]@{
            Name = $name
            Policy = $TablePolicies[$name]
            SourceRows = [long]$sourceTables[$name].Rows
            TargetRows = [long]$targetTables[$name].Rows
        }
    }
    if ($TablePolicies.Count -ne $sourceTables.Count) {
        throw 'The table classification contains entries absent from the catalogue.'
    }

    [pscustomobject]@{
        SourceDatabase = $Source.Database
        TargetDatabase = $Target.Database
        MigrationCount = $sourceMigrations.Count
        Tables = @($tables)
    }
}
