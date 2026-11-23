function Get-RefreshTransferOrder {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Policies,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$ForeignKeys
    )

    $dependencies = @{}
    foreach ($table in $Policies.Keys) {
        if ($Policies[$table] -notin 'KeepTestSettings', 'KeepMigrationHistory') {
            $dependencies[$table] = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        }
    }
    $retainedLinks = [System.Collections.Generic.List[object]]::new()
    $selfLinks = [System.Collections.Generic.List[object]]::new()
    foreach ($key in $ForeignKeys) {
        $child = [string]$key.Child
        $parent = [string]$key.Parent
        if (!$Policies.ContainsKey($child) -or !$Policies.ContainsKey($parent)) {
            throw "Foreign key $($key.Name) refers outside the refresh catalogue."
        }
        $replaceChild = $dependencies.ContainsKey($child)
        $replaceParent = $dependencies.ContainsKey($parent)
        if ($replaceChild -ne $replaceParent) { $retainedLinks.Add($key) }
        if (!$replaceChild -or !$replaceParent) { continue }
        if ($child -eq $parent) {
            $selfLinks.Add($key)
        } else {
            $null = $dependencies[$child].Add($parent)
        }
    }
    $insert = [System.Collections.Generic.List[string]]::new()
    while ($dependencies.Count) {
        $ready = @($dependencies.Keys | Where-Object { $dependencies[$_].Count -eq 0 } | Sort-Object)
        if (!$ready.Count) { break }
        foreach ($table in $ready) {
            $insert.Add($table)
            $dependencies.Remove($table)
            foreach ($pending in $dependencies.Values) { $null = $pending.Remove($table) }
        }
    }
    $delete = $insert.ToArray()
    [array]::Reverse($delete)
    [pscustomobject]@{
        InsertOrder = $insert.ToArray()
        DeleteOrder = $delete
        BlockedByCycle = @($dependencies.Keys | Sort-Object)
        SelfReferences = $selfLinks.ToArray()
        RetainedReferences = $retainedLinks.ToArray()
    }
}
