[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceConfigurationPath,
    [Parameter(Mandatory)][string]$TargetConfigurationPath,
    [Parameter(Mandatory)][string]$ScriptDomPath
)

$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $ScriptDomPath

function Get-TriggerTokens([string]$ConfigurationPath, [string]$Database) {
    $config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new(
        ($config.ConnectionStrings.SqlServer -replace '(?i)trust\s+server\s+certificate', 'TrustServerCertificate'))
    if ($builder.InitialCatalog -cne $Database -or
        $builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$') {
        throw 'Unexpected refresh SQL endpoint.'
    }
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $connection.Open()
        if ($connection.Database -cne $Database) { throw 'Unexpected connected database.' }
        $command = $connection.CreateCommand()
        $command.CommandText = @'
IF COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),0)=0
    THROW 51000, 'Trigger catalogue is not completely visible.', 1;
SELECT OBJECT_NAME(t.parent_id), t.name, t.is_disabled, m.uses_quoted_identifier, m.uses_ansi_nulls, m.definition
FROM sys.triggers t
JOIN sys.tables p ON p.object_id=t.parent_id
LEFT JOIN sys.sql_modules m ON m.object_id=t.object_id
WHERE p.schema_id=SCHEMA_ID('dbo') ORDER BY 1,2;
'@
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $name = "$($reader.GetString(0)).$($reader.GetString(1))"
            if ($reader.IsDBNull(5)) { throw "Trigger definition unavailable: $name." }
            $text = [IO.StringReader]::new($reader.GetString(5))
            try {
                $errors = $null
                $parser = [Microsoft.SqlServer.TransactSql.ScriptDom.TSql160Parser]::new($reader.GetBoolean(3))
                $fragment = $parser.Parse($text, [ref]$errors)
                if ($errors.Count) { throw "Trigger parser rejected $name." }
                $tokens = @($fragment.ScriptTokenStream | Where-Object {
                    $_.TokenType -notin 'WhiteSpace','SingleLineComment','MultilineComment','EndOfFile'
                } | ForEach-Object { @([string]$_.TokenType, $_.Text) })
                [pscustomobject]@{
                    Name = $name
                    Disabled = $reader.GetBoolean(2)
                    QuotedIdentifier = $reader.GetBoolean(3)
                    AnsiNulls = $reader.GetBoolean(4)
                    Tokens = ConvertTo-Json -InputObject $tokens -Compress
                }
            }
            finally { $text.Dispose() }
        }
    }
    finally { $connection.Dispose() }
}

$source = @(Get-TriggerTokens $SourceConfigurationPath 'PMR_SPC_2026')
$target = @(Get-TriggerTokens $TargetConfigurationPath 'PMR_SPC_TEST')
if (!$source.Count -or !$target.Count) { throw 'Trigger catalogue is empty.' }
$differences = @(Compare-Object $source $target -CaseSensitive -Property Name, Disabled, QuotedIdentifier, AnsiNulls, Tokens)
[pscustomobject]@{
    SourceTriggers = $source.Count
    TargetTriggers = $target.Count
    DifferentTriggers = @($differences | Select-Object -ExpandProperty Name -Unique)
}
