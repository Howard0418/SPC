#requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Plan','Apply','Rollback')][string]$Mode,
 [Parameter(Mandatory=$true)][string]$ManifestPath,
 [switch]$ApplicationAuthorized,
 [switch]$DatabaseAuthorized
)
# Backward-compatible production-only entry; no switch here can select Test.
$ProgressPreference='SilentlyContinue'
$global:LASTEXITCODE=0
& (Join-Path $PSScriptRoot 'Invoke-ReleaseOnServer.ps1') @PSBoundParameters -Environment Production

if(!$? -or $LASTEXITCODE -ne 0){exit 1}
exit 0
