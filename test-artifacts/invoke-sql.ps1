param(
  [Parameter(Mandatory = $true)]
  [string]$SqlFile
)

$ErrorActionPreference = "Stop"
$assembly = "D:\SPC\backend\MesSpc.Api\bin\Debug\net10.0\Microsoft.Data.SqlClient.dll"
Add-Type -Path $assembly

$connectionString = "Server=172.16.110.16;Database=PMR_SPC_2026;User Id=sa;Password=a@t123;Encrypt=True;TrustServerCertificate=True;"
$sql = Get-Content -LiteralPath $SqlFile -Raw

$connection = [Microsoft.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
try {
  $command = $connection.CreateCommand()
  $command.CommandText = $sql
  $command.CommandTimeout = 300
  $reader = $command.ExecuteReader()
  try {
    do {
      $rows = @()
      while ($reader.Read()) {
        $row = [ordered]@{}
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
          $row[$reader.GetName($i)] = if ($reader.IsDBNull($i)) { $null } else { $reader.GetValue($i) }
        }
        $rows += [pscustomobject]$row
      }
      if ($rows.Count -gt 0) {
        $rows | ConvertTo-Json -Depth 5
      }
    } while ($reader.NextResult())
  }
  finally {
    $reader.Dispose()
    $command.Dispose()
  }
}
finally {
  $connection.Dispose()
}
