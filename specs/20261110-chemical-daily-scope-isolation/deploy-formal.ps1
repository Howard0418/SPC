$ErrorActionPreference='Stop'
$releaseId='SPC-CHEM-DAILY-SCOPE-1110-20261110-R2'
$candidateRoot='D:/SPC/.chemical-daily-formal-20261110'
$apiFiles=@('MesSpc.Api.dll','MesSpc.Api.exe','MesSpc.Api.pdb','MesSpc.Api.deps.json','MesSpc.Api.runtimeconfig.json')
$portalFiles=@('PmrPortal.Web.dll','PmrPortal.Web.exe','PmrPortal.Web.pdb','PmrPortal.Web.deps.json','PmrPortal.Web.runtimeconfig.json')
if((Get-Item "$candidateRoot/spc-api/MesSpc.Api.dll").VersionInfo.FileVersion -ne '0.1.104.0'){throw 'SPC candidate version mismatch.'}
if((Get-Item "$candidateRoot/portal-web/PmrPortal.Web.dll").VersionInfo.FileVersion -ne '1.0.249.0'){throw 'Portal candidate version mismatch.'}

$spcSession=New-PSSession -ComputerName 'PMR-SPC-SRV' -Authentication Kerberos
$portalSession=New-PSSession -ComputerName 'PMR-WEB' -Authentication Kerberos
$prepared=$false
try {
  $spcBefore=Invoke-Command -Session $spcSession -ArgumentList $releaseId -ScriptBlock {
    param($releaseId); $ErrorActionPreference='Stop'; Import-Module WebAdministration
    $root='C:/inetpub/wwwroot/production/backend'; $backup="C:/ProgramData/PMR-Releases/Production/$releaseId"
    $site=Get-Website 'SpcApi'; if($site.physicalPath -ine $root.Replace('/','\') -or $site.applicationPool -ne 'SpcApi'){throw 'SPC formal IIS guard failed.'}
    $cfg=Get-Content "$root/appsettings.json" -Raw|ConvertFrom-Json
    $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cfg.ConnectionStrings.SqlServer.Replace('Trust Server Certificate=','TrustServerCertificate='))
    if($cfg.AppEnvironment -ne 'production' -or $builder.DataSource -ne '172.16.110.16' -or $builder.InitialCatalog -ne 'PMR_SPC_2026'){throw 'SPC formal environment guard failed.'}
    if((Get-Item "$root/MesSpc.Api.dll").VersionInfo.FileVersion -ne '0.1.103.0'){throw 'SPC formal baseline changed.'}
    if(Test-Path $backup){throw 'SPC release backup already exists.'}; New-Item -ItemType Directory $backup|Out-Null
    Copy-Item $root "$backup/api-before" -Recurse
    & C:/Windows/System32/inetsrv/appcmd.exe add backup $releaseId | Out-Null; if($LASTEXITCODE){throw 'SPC IIS backup failed.'}
    $sql=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString);$sql.Open()
    try{$cmd=$sql.CreateCommand();$cmd.CommandText="SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";$backupDir=[string]$cmd.ExecuteScalar();$bak=Join-Path $backupDir ($releaseId+'.bak');$escaped=$bak.Replace("'","''");$cmd=$sql.CreateCommand();$cmd.CommandTimeout=1800;$cmd.CommandText="BACKUP DATABASE [PMR_SPC_2026] TO DISK=N'$escaped' WITH COPY_ONLY,CHECKSUM,INIT,NAME=N'$releaseId'";$null=$cmd.ExecuteNonQuery();$cmd=$sql.CreateCommand();$cmd.CommandTimeout=1800;$cmd.CommandText="RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM";$null=$cmd.ExecuteNonQuery();$fp=@{};foreach($t in @('VariableMeasurements','PartProcessCharacteristics','__EFMigrationsHistory')){$cmd=$sql.CreateCommand();$cmd.CommandText="SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM [$t]";$rd=$cmd.ExecuteReader();$null=$rd.Read();$fp[$t]="$($rd.GetValue(0)):$($rd.GetValue(1))";$rd.Close()}}finally{$sql.Dispose()}
    [pscustomobject]@{Root=$root;Backup=$backup;ConfigHash=(Get-FileHash "$root/appsettings.json").Hash;WebConfigHash=(Get-FileHash "$root/web.config").Hash;Fingerprints=$fp;SqlBackup=$bak;SqlVerifyOnly=$true}
  }
  $portalBefore=Invoke-Command -Session $portalSession -ArgumentList $releaseId -ScriptBlock {
    param($releaseId);$ErrorActionPreference='Stop';Import-Module WebAdministration
    $root='C:/inetpub/wwwroot/publish/portal-web';$backup="C:/ProgramData/PMR-Releases/Production/$releaseId"
    $site=Get-Website 'PmrPortalWeb';if($site.physicalPath -ine $root.Replace('/','\') -or $site.applicationPool -ne 'PmrPortalWeb'){throw 'Portal formal IIS guard failed.'}
    $cfg=Get-Content "$root/appsettings.json" -Raw|ConvertFrom-Json;if($cfg.PortalApi.BaseUrl -ne 'https://login.pmr.com.tw:8443/'){throw 'Portal formal API base guard failed.'}
    if((Get-Item "$root/PmrPortal.Web.dll").VersionInfo.FileVersion -ne '1.0.248.0'){throw 'Portal formal baseline changed.'}
    if(Test-Path $backup){throw 'Portal release backup already exists.'};New-Item -ItemType Directory $backup|Out-Null;Copy-Item $root "$backup/web-before" -Recurse
    & C:/Windows/System32/inetsrv/appcmd.exe add backup $releaseId | Out-Null;if($LASTEXITCODE){throw 'Portal IIS backup failed.'}
    [pscustomobject]@{Root=$root;Backup=$backup;ConfigHash=(Get-FileHash "$root/appsettings.json").Hash;WebConfigHash=(Get-FileHash "$root/web.config").Hash}
  }
  $prepared=$true
  Invoke-Command -Session $spcSession {Import-Module WebAdministration;for($i=0;$i -lt 60;$i++){try{Stop-WebAppPool 'SpcApi' -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState 'SpcApi').Value -eq 'Stopped'){return}};throw 'SpcApi pool did not stop.'}
  Invoke-Command -Session $portalSession {Import-Module WebAdministration;for($i=0;$i -lt 60;$i++){try{Stop-WebAppPool 'PmrPortalWeb' -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState 'PmrPortalWeb').Value -eq 'Stopped'){return}};throw 'PmrPortalWeb pool did not stop.'}
  foreach($f in $apiFiles){Copy-Item "$candidateRoot/spc-api/$f" -Destination "C:/inetpub/wwwroot/production/backend/$f" -ToSession $spcSession -Force}
  foreach($f in $portalFiles){Copy-Item "$candidateRoot/portal-web/$f" -Destination "C:/inetpub/wwwroot/publish/portal-web/$f" -ToSession $portalSession -Force}
  $spcAfter=Invoke-Command -Session $spcSession -ArgumentList $spcBefore -ScriptBlock {
    param($before);$ErrorActionPreference='Stop';Import-Module WebAdministration;$root=$before.Root
    if((Get-Item "$root/MesSpc.Api.dll").VersionInfo.FileVersion -ne '0.1.104.0'){throw 'SPC deployed version mismatch.'}
    if((Get-FileHash "$root/appsettings.json").Hash -ne $before.ConfigHash -or (Get-FileHash "$root/web.config").Hash -ne $before.WebConfigHash){throw 'SPC protected config changed.'}
    for($i=0;$i -lt 40;$i++){try{Start-WebAppPool 'SpcApi' -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState 'SpcApi').Value -eq 'Started'){break}};Start-Sleep 3;$health=Invoke-RestMethod 'https://pmr-spc-srv.pmr.com.tw:8443/api/health' -TimeoutSec 30;$version=Invoke-RestMethod 'https://pmr-spc-srv.pmr.com.tw:8443/api/version' -TimeoutSec 30
    if($health.status -ne 'ok' -or $version.version -ne '0.1.104' -or $version.environment -ne 'production'){throw 'SPC health/version failed.'}
    [pscustomobject]@{Version=$version.version;Environment=$version.environment;Health=$health.status;Pool=(Get-WebAppPoolState 'SpcApi').Value}
  }
  $portalAfter=Invoke-Command -Session $portalSession -ArgumentList $portalBefore -ScriptBlock {
    param($before);$ErrorActionPreference='Stop';Import-Module WebAdministration;$root=$before.Root
    if((Get-Item "$root/PmrPortal.Web.dll").VersionInfo.FileVersion -ne '1.0.249.0'){throw 'Portal deployed version mismatch.'}
    if((Get-FileHash "$root/appsettings.json").Hash -ne $before.ConfigHash -or (Get-FileHash "$root/web.config").Hash -ne $before.WebConfigHash){throw 'Portal protected config changed.'}
    for($i=0;$i -lt 40;$i++){try{Start-WebAppPool 'PmrPortalWeb' -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState 'PmrPortalWeb').Value -eq 'Started'){break}};Start-Sleep 3;$portalResponse=Invoke-WebRequest 'https://login.pmr.com.tw/' -UseBasicParsing -TimeoutSec 30
    if($portalResponse.StatusCode -ne 200 -or $portalResponse.Headers.'Content-Type' -notmatch 'text/html'){throw 'Portal home failed.'}
    [pscustomobject]@{Version=(Get-Item "$root/PmrPortal.Web.dll").VersionInfo.FileVersion;Status=$portalResponse.StatusCode;Mime=[string]$portalResponse.Headers.'Content-Type';Pool=(Get-WebAppPoolState 'PmrPortalWeb').Value}
  }
  $cfg=Invoke-Command -Session $spcSession -ArgumentList $spcBefore -ScriptBlock {param($before);$cfg=Get-Content "$($before.Root)/appsettings.json" -Raw|ConvertFrom-Json;$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cfg.ConnectionStrings.SqlServer.Replace('Trust Server Certificate=','TrustServerCertificate='));$sql=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString);$sql.Open();try{$fp=@{};foreach($t in @('VariableMeasurements','PartProcessCharacteristics','__EFMigrationsHistory')){$cmd=$sql.CreateCommand();$cmd.CommandText="SELECT COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM [$t]";$rd=$cmd.ExecuteReader();$null=$rd.Read();$fp[$t]="$($rd.GetValue(0)):$($rd.GetValue(1))";$rd.Close()};$fp}finally{$sql.Dispose()}}
  foreach($p in $spcBefore.Fingerprints.PSObject.Properties){if($cfg[$p.Name] -ne [string]$p.Value){throw "Formal data fingerprint changed: $($p.Name)"}}
  [pscustomobject]@{ReleaseId=$releaseId;Spc=$spcAfter;Portal=$portalAfter;SqlVerifyOnly=$spcBefore.SqlVerifyOnly;FingerprintsUnchanged=$true}|ConvertTo-Json -Depth 6|Set-Content "$candidateRoot/deployment-result.json" -Encoding utf8
} catch {
  $original=$_
  if($prepared){
    Invoke-Command -Session $spcSession -ArgumentList $spcBefore,$apiFiles -ScriptBlock {param($b,$files);Import-Module WebAdministration;Stop-WebAppPool 'SpcApi' -ErrorAction SilentlyContinue;Start-Sleep 2;foreach($f in $files){Copy-Item "$($b.Backup)/api-before/$f" "$($b.Root)/$f" -Force};Start-WebAppPool 'SpcApi'} -ErrorAction Continue
    Invoke-Command -Session $portalSession -ArgumentList $portalBefore,$portalFiles -ScriptBlock {param($b,$files);Import-Module WebAdministration;Stop-WebAppPool 'PmrPortalWeb' -ErrorAction SilentlyContinue;Start-Sleep 2;foreach($f in $files){Copy-Item "$($b.Backup)/web-before/$f" "$($b.Root)/$f" -Force};Start-WebAppPool 'PmrPortalWeb'} -ErrorAction Continue
  }
  throw $original
} finally {Remove-PSSession $spcSession,$portalSession}
