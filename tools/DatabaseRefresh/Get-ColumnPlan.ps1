function Get-RefreshColumnPlan {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$Tables,
        [Parameter(Mandatory)][object[]]$Columns
    )

    $byTable = $Columns | Group-Object Table -AsHashTable -AsString
    foreach ($table in $Tables) {
        $all = @($byTable[$table] | Where-Object { $null -ne $_ })
        $write = @($all | Where-Object Writable)
        $key = @($all | Where-Object { $_.KeyOrdinal -gt 0 } | Sort-Object KeyOrdinal)
        if (!$write.Count -or !$key.Count -or @($key | Where-Object { !$_.Writable }).Count) {
            throw "No writable primary key for replacement table $table."
        }
        if (@($write | Where-Object { !$_.Comparable }).Count) {
            throw "Unsupported data comparison type in replacement table $table."
        }
        [pscustomobject]@{
            Table = $table
            WriteColumns = @($write.Name)
            PrimaryKey = @($key.Name)
            IdentityColumns = @($write | Where-Object Identity | Select-Object -ExpandProperty Name)
            GeneratedColumns = @($all | Where-Object { !$_.Writable } | Select-Object -ExpandProperty Name)
        }
    }
}
