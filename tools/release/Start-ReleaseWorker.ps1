#requires -Version 5.1
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Test','Production')][string]$Environment,
 [Parameter(Mandatory=$true)][ValidateSet('Plan','Apply','Rollback')][string]$Mode,
 [Parameter(Mandatory=$true)][string]$ManifestPath,
 [switch]$ApplicationAuthorized,
 [switch]$DatabaseAuthorized
)
# The dispatcher has already handled confirmation. Pass a real Boolean switch value inside PowerShell,
# rather than a string through powershell.exe -File (which cannot bind -Confirm:$false on Windows PS5).
& (Join-Path $PSScriptRoot 'Invoke-ReleaseOnServer.ps1') @PSBoundParameters -Confirm:$false
