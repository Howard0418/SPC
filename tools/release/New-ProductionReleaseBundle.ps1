#requires -Version 5.1
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('SPC','Portal')][string]$System,
 [ValidateSet('Test','Production')][string]$Environment='Production',
 [Parameter(Mandatory=$true)][string]$ReleaseId,
 [Parameter(Mandatory=$true)][string]$SourceCommit,
 [Parameter(Mandatory=$true)][string]$ApiPackage,
 [Parameter(Mandatory=$true)][string]$WebPackage,
 [Parameter(Mandatory=$true)][string]$ApiVersion,
 [Parameter(Mandatory=$true)][string]$WebVersion,
 [Parameter(Mandatory=$true)][string]$PreviousApiVersion,
 [Parameter(Mandatory=$true)][string]$PreviousWebVersion,
 [Parameter(Mandatory=$true)][string]$OutputDirectory,
 [Parameter(Mandatory=$true)][string]$MigrationCatalog,
 [string]$MigrationSql,
 [string[]]$MigrationId=@()
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1') -Force
if($ReleaseId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{2,79}$' -or $SourceCommit -notmatch '^[a-fA-F0-9]{40}$'){throw 'Invalid release identity'}
if(($MigrationSql -and !$MigrationId.Count) -or (!$MigrationSql -and $MigrationId.Count)){throw 'Supply both reviewed EF SQL and migration IDs'}
foreach($v in @($ApiVersion,$WebVersion,$PreviousApiVersion,$PreviousWebVersion)){if($v -notmatch '^\d+\.\d+\.\d+(\.\d+)?$'){throw 'Invalid version'}}
$profile=Get-ReleaseProfile $System $Environment
$out=[IO.Path]::GetFullPath($OutputDirectory)
Assert-ReleaseWorkspacePath $out
if(Test-Path -LiteralPath $out){throw 'OutputDirectory already exists; use a unique empty destination'}
$inputs=@((Resolve-Path -LiteralPath $ApiPackage).Path,(Resolve-Path -LiteralPath $WebPackage).Path)
foreach($inputPath in $inputs){
 Assert-NoReleaseReparse $inputPath
 if($out -ieq $inputPath -or $out.StartsWith($inputPath.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be outside published inputs'}
}
foreach($t in $profile.Targets){if($out -ieq $t.Path -or $out.StartsWith($t.Path+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must not be a website'}}
[void][IO.Directory]::CreateDirectory($out)
Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
$packages=@();$versions=@($ApiVersion,$WebVersion)
for($i=0;$i -lt 2;$i++){
 $inputPath=$inputs[$i];$target=$profile.Targets[$i];$archive=$target.Site+'-'+$target.Role+'.zip';$zip=[IO.Compression.ZipFile]::Open((Join-Path $out $archive),[IO.Compression.ZipArchiveMode]::Create)
 $files=@()
 try{
  foreach($f in Get-ChildItem -LiteralPath $inputPath -File -Recurse -Force){
   Assert-NoReleaseReparse $f.FullName
   $rel=$f.FullName.Substring($inputPath.TrimEnd('\','/').Length+1).Replace('\','/')
   Assert-ReleaseRelativePath $rel
   try{Assert-ReleasePayloadPath $rel}catch{continue} # Explicitly excluded configuration/private payload, never copied.
   [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$f.FullName,$rel)
   $files+=@{path=$rel;sha256=(Get-ReleaseHash $f.FullName)}
  }
 }finally{$zip.Dispose()}
 $packages+=@{site=$target.Site;pool=$target.Pool;path=$target.Path;version=$versions[$i];archive=$archive;sha256=(Get-ReleaseHash (Join-Path $out $archive));files=$files}
}
function New-SmokeChecks($apiVersion,$webVersion){
 if($System -eq 'SPC'){
  return @(
   @{url=($profile.ApiBase+'/api/version');method='GET';status=200;bodyPattern=('"version"\s*:\s*"'+[regex]::Escape($apiVersion)+'"')},
   @{url=($profile.ApiBase+'/api/health');method='GET';status=200;bodyPattern='"status"\s*:\s*"ok"'},
   @{url=($profile.WebBase+'/');method='GET';status=200;bodyPattern='<html'},
   @{url=($profile.ApiBase+'/api/v1/etch-import-test/preview');method='POST';status=401;bodyPattern=''})
 }
 return @(
  @{url=($profile.ApiBase+'/health');method='GET';status=200;bodyPattern='"status"\s*:\s*"ok"'},
  @{url=($profile.WebBase+'/app-version');method='GET';status=200;bodyPattern=('"version"\s*:\s*"'+[regex]::Escape($webVersion)+'(\.0)?"')},
  @{url=($profile.WebBase+'/Login');method='GET';status=200;bodyPattern='<html'},
  @{url=($profile.ApiBase+'/api/etch-import-test/preview');method='POST';status=401;bodyPattern=''})
}
$catalog=Get-Content -LiteralPath $MigrationCatalog -Raw -Encoding UTF8|ConvertFrom-Json
$catalog=@($catalog)
$db=@{name=$profile.Database;knownMigrationIds=$catalog;migrationFile='';migrationSha256='';migrationIds=@($MigrationId)}
if($MigrationSql){$db.migrationFile='reviewed-ef-migration.sql';Copy-Item -LiteralPath $MigrationSql -Destination (Join-Path $out $db.migrationFile);$db.migrationSha256=Get-ReleaseHash (Join-Path $out $db.migrationFile)}
$manifest=@{schemaVersion=1;releaseId=$ReleaseId;sourceCommit=$SourceCommit;system=$System;host=$profile.HostName;environment=$profile.Environment;database=$db;packages=$packages;smoke=@(New-SmokeChecks $ApiVersion $WebVersion);rollbackSmoke=@(New-SmokeChecks $PreviousApiVersion $PreviousWebVersion)}
if($System -eq 'SPC'){foreach($file in $packages[1].files){if($file.path -match '\.(js|css)$'){
 $type=if($file.path.EndsWith('.css')){'text/css'}else{'application/javascript'}
 $manifest.smoke+=@{url=(($profile.WebBase+'/')+$file.path);method='GET';status=200;bodyPattern='';contentType=$type}
}}}
Write-ReleaseJson $manifest (Join-Path $out 'manifest.json')
# Offline validation uses the allowlisted host identity; actual CLI still validates the real machine and IIS.
$parsed=Get-Content (Join-Path $out 'manifest.json') -Raw -Encoding UTF8|ConvertFrom-Json
[void](Test-ReleaseManifest $parsed $out $profile.HostName $profile.Environment)
Write-Output ('Bundle created and verified: '+(Join-Path $out 'manifest.json'))
