#requires -Version 5.1
[CmdletBinding()]
param([string]$OutputDirectory=(Join-Path $PSScriptRoot '../../../release-staging/ReleaseUpdater-1.0.0'))
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '../ProductionRelease.psm1') -Force
$out=[IO.Path]::GetFullPath($OutputDirectory);Assert-ReleaseWorkspacePath $out
[void][IO.Directory]::CreateDirectory($out)
$build=Join-Path $out '_build';[void][IO.Directory]::CreateDirectory($build)
$csc=Join-Path $env:windir 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path -LiteralPath $csc)){throw '.NET Framework 4.x x64 compiler not found'}
$profiles=@();foreach($environmentName in @('Test','Production')){foreach($systemName in @('SPC','Portal')){
 $p=Get-ReleaseProfile $systemName $environmentName
 $profiles+=[pscustomobject]@{Environment=$p.Environment;System=$systemName;HostName=$p.HostName;Database=$p.Database;Targets=$p.Targets}
}}
$profiles|ConvertTo-Json -Depth 12|Set-Content (Join-Path $build 'profiles.json') -Encoding UTF8
$resources=@{};foreach($name in @('Invoke-Release.ps1','ReleaseRemote.psm1','ProductionRelease.psm1','Invoke-ReleaseOnServer.ps1','Start-ReleaseWorker.ps1')){$resources[$name]=Join-Path $PSScriptRoot ('../'+$name)}
$resources['Invoke-DesktopRelease.ps1']=Join-Path $PSScriptRoot 'Invoke-DesktopRelease.ps1'
$resources['profiles.json']=Join-Path $build 'profiles.json'
$hashes=@{};foreach($name in $resources.Keys){$hashes[$name]=Get-ReleaseHash $resources[$name]}
$hashes|ConvertTo-Json|Set-Content (Join-Path $build 'asset-hashes.json') -Encoding UTF8
$resources['asset-hashes.json']=Join-Path $build 'asset-hashes.json'
$reference=@('/nologo','/platform:x64','/optimize+','/utf8output','/langversion:5','/r:System.dll','/r:System.Core.dll','/r:System.Xml.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll')
foreach($name in $resources.Keys){$reference+=('/resource:'+$resources[$name]+',Updater.'+$name)}
$core=@((Join-Path $PSScriptRoot 'ReleaseCore.cs'),(Join-Path $PSScriptRoot 'EmbeddedTools.cs'))
$exe=Join-Path $out 'PMR.ReleaseUpdater.exe'
& $csc @reference '/target:winexe' ('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')) ('/out:'+$exe) @core (Join-Path $PSScriptRoot 'UpdaterForm.cs')
if($LASTEXITCODE -ne 0){throw 'Updater compilation failed'}
$tests=Join-Path $build 'CoreTests.exe'
& $csc @reference '/target:exe' ('/out:'+$tests) @core (Join-Path $PSScriptRoot 'CoreTests.cs')
if($LASTEXITCODE -ne 0){throw 'Core test compilation failed'}
& $tests
if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '使用說明.txt') -Destination (Join-Path $out '使用說明.txt') -Force
(Get-ReleaseHash $exe)+'  PMR.ReleaseUpdater.exe'|Set-Content (Join-Path $out 'SHA256SUMS.txt') -Encoding ASCII
Write-Output ('EXE_READY='+$exe)
