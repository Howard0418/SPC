$ErrorActionPreference='Stop'
$repo='D:\SPC'; $testRoot=Join-Path $repo 'release\test'; $target=Join-Path $testRoot 'backend'
$source=Join-Path $repo 'release-staging\audit-retention-20261109\backend'; $backup=Join-Path $testRoot 'backend.backup-audit-retention-20261109-r2'
$appcmd=Join-Path $env:windir 'system32\inetsrv\appcmd.exe'; $pool='SpcSingleTest'
$actual=(& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim()
if($LASTEXITCODE -ne 0 -or $actual -ne $target -or (& $appcmd list app 'SpcApiTest/' /text:apppool.name).Trim() -ne $pool){throw 'Unexpected IIS test target'}
$settingsPath=Join-Path $target 'appsettings.json'; $settings=Get-Content $settingsPath -Raw|ConvertFrom-Json
if($settings.AppEnvironment -ne 'test' -or !$settings.Auth.Enabled -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)'){throw 'Unexpected test configuration'}
if(Test-Path $backup){throw 'Backup already exists'}; Copy-Item $target $backup -Recurse
$offline=Join-Path $target 'app_offline.htm'
try{
  Set-Content $offline '<html><body>SPC test deployment in progress.</body></html>' -Encoding utf8
  & $appcmd stop apppool /apppool.name:$pool|Out-Null; Start-Sleep -Seconds 3
  foreach($file in Get-ChildItem $source -Recurse -File){
    $relative=[IO.Path]::GetRelativePath($source,$file.FullName)
    if($file.Name -like 'appsettings*.json' -or $file.Name -eq 'web.config' -or $relative -match '^(uploads|logs|private-data|certificates|App_Data)[\\/]'){continue}
    $destination=[IO.Path]::GetFullPath((Join-Path $target $relative)); if(!$destination.StartsWith($target+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe payload path'}
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force|Out-Null; Copy-Item $file.FullName $destination -Force
    if((Get-FileHash $file.FullName).Hash -ne (Get-FileHash $destination).Hash){throw "Hash mismatch $relative"}
  }
  if(!$settings.Audit){$settings|Add-Member NoteProperty Audit ([pscustomobject]@{})}
  if(!$settings.Audit.Retention){$settings.Audit|Add-Member NoteProperty Retention ([pscustomobject]@{})}
  $settings.Audit.Retention|Add-Member NoteProperty Enabled $false -Force
  $settings.Audit.Retention|Add-Member NoteProperty AllowCurrentConnectionForMaintenance $false -Force
  $settings|ConvertTo-Json -Depth 20|Set-Content $settingsPath -Encoding utf8
  Remove-Item $offline -Force; & $appcmd start apppool /apppool.name:$pool|Out-Null
  $ready=$false; for($i=0;$i -lt 15;$i++){try{$version=Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10;if($version.environment -eq 'test' -and $version.version -eq '0.1.111'){$ready=$true;break}}catch{};Start-Sleep -Seconds 1}
  if(!$ready){throw 'Test API version verification failed'}
  $health=Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 10
  try{Invoke-WebRequest 'https://spctest.pmr.com.tw/api/v1/audit/retention/summary' -TimeoutSec 10|Out-Null;throw 'Anonymous retention endpoint was not protected'}catch{if($_.Exception.Response.StatusCode.value__ -ne 401){throw}}
  [pscustomobject]@{Status='Deployed';Environment=$version.environment;ApiVersion=$version.version;Health=$health.status;RetentionEnabled=$false;BackendBackup=$backup}|ConvertTo-Json|Set-Content (Join-Path $PSScriptRoot 'deployment-result.json') -Encoding utf8
}catch{
  $failure=$_; try{Get-ChildItem $backup -Force|Copy-Item -Destination $target -Recurse -Force}finally{if(Test-Path $offline){Remove-Item $offline -Force};& $appcmd start apppool /apppool.name:$pool|Out-Null};throw $failure
}
