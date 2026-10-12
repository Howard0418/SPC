[CmdletBinding()]
param(
    [string[]]$Path = @('backend/MesSpc.Api', 'frontend/mes-spc-web/src'),

    [int]$Threshold = 700,

    [int]$Top = 20,

    [switch]$IncludeGenerated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$extensions = @('.cs', '.vue', '.ts', '.js')
$files = @()

foreach ($relativePath in $Path) {
    $root = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Path does not exist or is not a directory: $relativePath"
    }

    $files += Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object {
            $extensions -contains $_.Extension -and
            $_.FullName -notmatch '\\(bin|obj|dist|node_modules)\\' -and
            ($IncludeGenerated -or (
                $_.FullName -notmatch '\\Migrations[^\\]*\\' -and
                $_.Name -notlike '*.Designer.cs' -and
                $_.Name -ne 'AppDbContextModelSnapshot.cs'
            ))
        }
}

$rows = $files |
    ForEach-Object {
        $lineCount = (Get-Content -LiteralPath $_.FullName | Measure-Object -Line).Lines
        [pscustomobject]@{
            Lines = $lineCount
            RelativePath = [System.IO.Path]::GetRelativePath($repoRoot, $_.FullName)
            OverThreshold = $lineCount -gt $Threshold
        }
    } |
    Sort-Object -Property Lines -Descending

$rows | Select-Object -First $Top | Format-Table -AutoSize

$overThreshold = @($rows | Where-Object { $_.OverThreshold })
if ($overThreshold.Count -gt 0) {
    Write-Error "$($overThreshold.Count) module(s) exceed threshold $Threshold lines."
}
