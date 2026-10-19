#requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Plan','Apply','Rollback')][string]$Mode,
 [Parameter(Mandatory=$true)][string]$ManifestPath,
 [switch]$ApplicationAuthorized,
 [switch]$DatabaseAuthorized
)
# Backward-compatible production-only entry; no switch here can select Test.
& (Join-Path $PSScriptRoot 'Invoke-ReleaseOnServer.ps1') @PSBoundParameters -Environment Production
