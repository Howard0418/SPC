#requires -Version 5.1
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RequestBase64)
$ErrorActionPreference='Stop'
try{
 [Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
 $request=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($RequestBase64))|ConvertFrom-Json
 if($request.Environment -cnotin @('Test','Production') -or $request.System -cnotin @('SPC','Portal') -or $request.Mode -cnotin @('Plan','Apply','Rollback')){throw 'Unsupported desktop request'}
 if($request.ApplicationAuthorized -isnot [bool] -or $request.DatabaseAuthorized -isnot [bool]){throw 'Authorization must be Boolean'}
 $invoke=@{System=$request.System;Environment=$request.Environment;Mode=$request.Mode;ManifestPath=[string]$request.ManifestPath;ApplicationAuthorized=[bool]$request.ApplicationAuthorized;DatabaseAuthorized=[bool]$request.DatabaseAuthorized;Confirm=$false}
 $global:LASTEXITCODE=0
 & (Join-Path $PSScriptRoot 'Invoke-Release.ps1') @invoke 2>&1 | ForEach-Object { [Console]::WriteLine($_.ToString()) }
 if(!$? -or $LASTEXITCODE -ne 0){exit 1}
 exit 0
}catch{[Console]::Error.WriteLine('桌面更新失敗（'+$_.Exception.GetType().Name+'），請檢查交付包與執行結果。');exit 1}
