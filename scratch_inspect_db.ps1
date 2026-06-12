$connString = "Server=172.16.110.16;Database=PMR_SPC_2026;User Id=sa;Password=a@t123;Encrypt=True;TrustServerCertificate=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connString)
try {
    $connection.Open()
    $query = "SELECT t.name AS TableName, i.rows AS RowCounts FROM sys.tables t INNER JOIN sys.sysindexes i ON t.object_id = i.id WHERE i.indid < 2 ORDER BY t.name"
    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset) | Out-Null
    $dataset.Tables[0] | Format-Table -AutoSize
} catch {
    Write-Error $_.Exception.Message
} finally {
    if ($connection.State -eq "Open") {
        $connection.Close()
    }
}
