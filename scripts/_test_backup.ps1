$f = [System.IO.Path]::GetTempFileName() + '.sql'
$q = "BACKUP DATABASE [PMR_SPC_2026] TO DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_2026.bak' WITH FORMAT, NAME = N'Full', STATS = 10;"
[System.IO.File]::WriteAllText($f, $q, [System.Text.Encoding]::UTF8)
Write-Host "SQL file: $f"
sqlcmd -S 172.16.110.16 -U sa -P "a@t123" -i $f
Write-Host "ExitCode=$LASTEXITCODE"
