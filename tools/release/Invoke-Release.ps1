#requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
 [Parameter(Mandatory=$true)][ValidateSet('SPC','Portal')][string]$System,
 [ValidateSet('Test','Production')][string]$Environment='Test',
 [ValidateSet('Plan','Apply','Rollback')][string]$Mode='Plan',
 [Parameter(Mandatory=$true)][string]$ManifestPath,
 [switch]$ApplicationAuthorized,
 [switch]$DatabaseAuthorized
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'ReleaseRemote.psm1') -Force
try{
 $request=New-ReleaseDispatchRequest $ManifestPath $Environment $System $Mode $PSScriptRoot -ApplicationAuthorized:$ApplicationAuthorized -DatabaseAuthorized:$DatabaseAuthorized
 # WhatIf performs local hash/profile validation only; no connections or incoming directories.
 $action="$Mode $($request.Environment)/$System release $($request.ReleaseId); transfer verified bundle and execute on destination"
 if($Mode -eq 'Plan'){
  if($WhatIfPreference){[void]$PSCmdlet.ShouldProcess($request.HostName,$action);return}
 }elseif(!$PSCmdlet.ShouldProcess($request.HostName,$action)){return}

 $prepare={param($r)
  $ErrorActionPreference='Stop'
  if($env:COMPUTERNAME -ine $r.Machine){throw 'Actual server identity differs from allowlist'}
  $expected='C:\ProgramData\PMR-Release-Incoming\'+$r.Environment+'\'
  $dest=[IO.Path]::GetFullPath($r.Incoming)
  if(!$dest.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase)){throw 'Incoming path rejected'}
  function Assert-StagePath($path){$cursor=[IO.Path]::GetFullPath($path);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Incoming reparse point rejected'}};$parent=Split-Path $cursor -Parent;if($parent -eq $cursor){break};$cursor=$parent}}
  Assert-StagePath $dest
  [void][IO.Directory]::CreateDirectory($dest)
  & icacls.exe $dest '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F'|Out-Null
  if($LASTEXITCODE -ne 0){throw 'Incoming ACL failed'}
  foreach($f in $r.Files){$path=Join-Path $dest $f.Destination;Assert-StagePath $path;[void][IO.Directory]::CreateDirectory((Split-Path $path -Parent))}
 }
 $verify={param($r)
  $ErrorActionPreference='Stop'
  foreach($f in $r.Files){if((Get-FileHash -LiteralPath (Join-Path $r.Incoming $f.Destination) -Algorithm SHA256).Hash -ine $f.Sha256){throw 'Received file hash mismatch'}}
  @{environment=$r.Environment;system=$r.System;releaseId=$r.ReleaseId;mode=$r.Mode;receivedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $r.Incoming 'transfer.json') -Encoding UTF8
 }
 $run={param($r,$manifest)
  $ErrorActionPreference='Stop'
  if($env:COMPUTERNAME -ine $r.Machine){throw 'Server identity changed'}
  $exe=if([Environment]::Is64BitProcess){Join-Path $env:windir 'System32\WindowsPowerShell\v1.0\powershell.exe'}else{Join-Path $env:windir 'Sysnative\WindowsPowerShell\v1.0\powershell.exe'}
  if(!(Test-Path -LiteralPath $exe)){throw '64-bit Windows PowerShell unavailable'}
  $args=@('-NoProfile','-NonInteractive','-File',(Join-Path $r.Incoming 'tools/Start-ReleaseWorker.ps1'),'-Environment',$r.Environment,'-Mode',$r.Mode,'-ManifestPath',$manifest)
  if($r.ApplicationAuthorized){$args+='-ApplicationAuthorized'}
  if($r.DatabaseAuthorized){$args+='-DatabaseAuthorized'}
  & $exe @args
  if($LASTEXITCODE -ne 0){throw 'Destination release failed; inspect destination evidence'}
 }
 $transport=@{
  Open={param($r)
   if($env:COMPUTERNAME -ieq $r.Machine){return @{Local=$true;Session=$null}}
   $s=New-PSSession -ComputerName $r.HostName -Authentication Kerberos -ConfigurationName Microsoft.PowerShell
   try{$identity=Invoke-Command -Session $s -ScriptBlock {$env:COMPUTERNAME};if($identity -ine $r.Machine){throw 'Remote identity mismatch'}}catch{Remove-PSSession $s;throw}
   return @{Local=$false;Session=$s}
  }
  Stage={param($c,$r)
   if($c.Local){& $prepare $r}else{Invoke-Command -Session $c.Session -ScriptBlock $prepare -ArgumentList $r}
   foreach($f in $r.Files){
    $dest=Join-Path $r.Incoming $f.Destination
    if($c.Local){Copy-Item -LiteralPath $f.Source -Destination $dest -Force}else{Copy-Item -LiteralPath $f.Source -Destination $dest -ToSession $c.Session -Force}
   }
   if($c.Local){& $verify $r}else{Invoke-Command -Session $c.Session -ScriptBlock $verify -ArgumentList $r}
   return (Join-Path $r.Incoming 'manifest.json')
  }
  Run={param($c,$r,$manifest)
   if($c.Local){& $run $r $manifest}else{Invoke-Command -Session $c.Session -ScriptBlock $run -ArgumentList $r,$manifest}
  }
  Close={param($c)if(!$c.Local -and $c.Session){Remove-PSSession $c.Session}}
 }
 Invoke-ReleaseDispatch $request $transport
}catch{
 Write-Error ('Release dispatch failed ('+$_.Exception.GetType().Name+$(if($_.Exception -is [Management.Automation.CommandNotFoundException]){': '+$_.Exception.CommandName}else{''})+'). Check selected environment, bundle, destination access and private evidence. No credentials or configuration are printed.') -ErrorAction Continue
 exit 1
}
