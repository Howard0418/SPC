$connString = "Server=172.16.110.16;Database=PMR_SPC_2026;User Id=sa;Password=a@t123;Encrypt=True;TrustServerCertificate=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connString)
try {
    $connection.Open()
    
    Write-Host "=== Machines ==="
    $cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT Id, MachineCode, MachineName, ProcessId, IsEnabled FROM Machines", $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $adapter.Fill($dt) | Out-Null
    $dt | Format-Table -AutoSize

    Write-Host "=== ProductionLines ==="
    $cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT Id, LineCode, LineName, IsActive FROM ProductionLines", $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $adapter.Fill($dt) | Out-Null
    $dt | Format-Table -AutoSize

    Write-Host "=== Tanks ==="
    $cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT Id, LineId, TankCode, TankName, IsActive FROM Tanks", $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $adapter.Fill($dt) | Out-Null
    $dt | Format-Table -AutoSize
    
    Write-Host "=== PartProcessCharacteristics ==="
    $cmd = New-Object System.Data.SqlClient.SqlCommand("SELECT Id, ControlScope, ProcessId, MachineId, TankId, CharacteristicId FROM PartProcessCharacteristics WHERE ControlScope = 'CHEMICAL'", $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $adapter.Fill($dt) | Out-Null
    $dt | Format-Table -AutoSize

} catch {
    Write-Error $_.Exception.Message
} finally {
    if ($connection.State -eq "Open") {
        $connection.Close()
    }
}
