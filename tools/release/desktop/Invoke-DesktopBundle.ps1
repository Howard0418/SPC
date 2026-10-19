#requires -Version 5.1
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RequestBase64)
$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue'
try{
 [Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
 Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1') -Force
 $r=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($RequestBase64))|ConvertFrom-Json
 if($r.System -cnotin @('SPC','Portal') -or $r.Environment -cnotin @('Test','Production') -or $r.Operation -cnotin @('InspectSource','ReadTargetVersions','CreateBundle')){throw 'Unsupported package operation'}
 $report=[IO.Path]::GetFullPath([string]$r.ReportPath)
 if(!$report.StartsWith([IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($report) -ne '.json'){throw 'Result path must be in private tool cache'}
 Assert-NoReleaseReparse $report
 function Inspect-Source($request,$dest){
  $dllName=if($request.System -eq 'SPC'){'MesSpc.Api.dll'}else{'PmrPortal.Api.dll'}
  $dll=Join-Path $request.ApiDirectory $dllName;Assert-NoReleaseReparse $dll
  $before=$env:DOTNET_BUNDLE_EXTRACT_BASE_DIR
  try{
   $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR=Join-Path $PSScriptRoot 'runtime'
   & (Join-Path $PSScriptRoot 'MetadataInspector.exe') $request.System $dll $dest
   if($LASTEXITCODE -ne 0){throw 'Static API inspection failed'}
  }finally{if($null -eq $before){Remove-Item Env:DOTNET_BUNDLE_EXTRACT_BASE_DIR -ErrorAction SilentlyContinue}else{$env:DOTNET_BUNDLE_EXTRACT_BASE_DIR=$before}}
 }
 if($r.Operation -eq 'InspectSource'){Inspect-Source $r $report;exit 0}
 if($r.Operation -eq 'ReadTargetVersions'){
  $p=Get-ReleaseProfile $r.System $r.Environment
  $read={param($profile,$systemName)
   if($env:COMPUTERNAME -ine $profile.Machine){throw 'Unexpected target computer'}
   function VersionOf($file){$v=[version][Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion;if($v.Revision -eq 0){return $v.ToString(3)};return $v.ToString()}
   $apiName=if($systemName -eq 'SPC'){'MesSpc.Api.dll'}else{'PmrPortal.Api.dll'}
   $api=@($profile.Targets|Where-Object {$_.Role -eq 'Api'})[0].Path
   $web=@($profile.Targets|Where-Object {$_.Role -eq 'Web'})[0].Path
   $wv=if($systemName -eq 'Portal'){VersionOf (Join-Path $web 'PmrPortal.Web.dll')}else{''}
   [pscustomobject]@{ApiVersion=(VersionOf (Join-Path $api $apiName));WebVersion=$wv;HostName=$profile.HostName}
  }
  $v=if($env:COMPUTERNAME -ieq $p.Machine){& $read $p $r.System}else{Invoke-Command -ComputerName $p.HostName -Authentication Kerberos -ScriptBlock $read -ArgumentList $p,$r.System}
  Write-ReleaseJson $v $report;exit 0
 }
 # Validate every local path and identity before producing anything.
 if($r.ReleaseId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{2,79}$' -or $r.SourceCommit -notmatch '^[a-fA-F0-9]{40}$'){throw 'Invalid release identity'}
 $parent=[IO.Path]::GetFullPath([string]$r.OutputParent)
 if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Output parent missing'}
 $final=[IO.Path]::GetFullPath((Join-Path $parent $r.ReleaseId));Assert-ReleaseWorkspacePath $final
 if(Test-Path -LiteralPath $final){throw 'Output already exists; use a new release ID'}
 $temp=[IO.Path]::GetFullPath((Join-Path $parent ($r.ReleaseId+'.incomplete-'+[guid]::NewGuid().ToString('N'))));Assert-ReleaseWorkspacePath $temp
 if(!$temp.StartsWith($parent.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or !$final.StartsWith($parent.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output escaped parent'}
 $metadata=Join-Path $PSScriptRoot ('source-'+[guid]::NewGuid().ToString('N')+'.json');Inspect-Source $r $metadata
 $meta=Get-Content $metadata -Raw -Encoding UTF8|ConvertFrom-Json
 if($r.ApiVersion -ne $meta.ApiVersion -or ($r.ApiSha256 -and $r.ApiSha256 -ine $meta.ApiSha256)){throw 'API changed; inspect source again'}
 $catalog=Join-Path $PSScriptRoot ('catalog-'+[guid]::NewGuid().ToString('N')+'.json')
 ConvertTo-Json -InputObject @($meta.MigrationIds)|Set-Content $catalog -Encoding UTF8
 $ids=@();if($r.MigrationIds){$ids=@(([string]$r.MigrationIds -split '[,;\s]+')|Where-Object {$_})}
 $args=@{System=$r.System;Environment=$r.Environment;ReleaseId=$r.ReleaseId;SourceCommit=$r.SourceCommit;ApiPackage=$r.ApiDirectory;WebPackage=$r.WebDirectory;ApiVersion=$r.ApiVersion;WebVersion=$r.WebVersion;PreviousApiVersion=$r.PreviousApiVersion;PreviousWebVersion=$r.PreviousWebVersion;OutputDirectory=$temp;MigrationCatalog=$catalog}
 if($r.MigrationSql){$args.MigrationSql=$r.MigrationSql;$args.MigrationId=$ids}elseif($ids.Count){throw 'SQL and migration IDs must be paired'}
 & (Join-Path $PSScriptRoot 'New-ProductionReleaseBundle.ps1') @args 2>&1|ForEach-Object {[Console]::WriteLine($_.ToString())}
 if(!$?){throw 'Bundle generator failed'}
 $manifest=Get-Content (Join-Path $temp 'manifest.json') -Raw -Encoding UTF8|ConvertFrom-Json
 $p=Get-ReleaseProfile $r.System $r.Environment
 $apiPath=@($p.Targets|Where-Object {$_.Role -eq 'Api'})[0].Path
 $package=@($manifest.packages|Where-Object {$_.path -ieq $apiPath})[0]
 $apiName=if($r.System -eq 'SPC'){'MesSpc.Api.dll'}else{'PmrPortal.Api.dll'}
 if(@($package.files|Where-Object {$_.path -eq $apiName -and $_.sha256 -ieq $meta.ApiSha256}).Count -ne 1){throw 'Packaged API differs from metadata inspected'}
 # This directory move only renames a validated sibling of final, never an IIS/source directory.
 Assert-ReleaseWorkspacePath $temp;Assert-ReleaseWorkspacePath $final
 if(Test-Path -LiteralPath $final){throw 'Output created by another process'}
 Move-Item -LiteralPath $temp -Destination $final
 Write-ReleaseJson @{ManifestPath=(Join-Path $final 'manifest.json')} $report
 [Console]::WriteLine('交付包完成：'+$final);exit 0
}catch{[Console]::Error.WriteLine('桌面產包失敗（'+$_.Exception.GetType().Name+'），請核對來源、版本、SQL與輸出位置。');exit 1}
