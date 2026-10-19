$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '../ProductionRelease.psm1') -Force
$count=0
function Check($name,[scriptblock]$body){& $body;$script:count++;Write-Output "PASS $name"}
function Assert($condition,$message='Assertion failed'){if(!$condition){throw $message}}
function Reject([scriptblock]$body){$rejected=$false;try{& $body|Out-Null}catch{$rejected=$true};Assert $rejected 'Expected rejection'}
$root=Join-Path ([IO.Path]::GetTempPath()) ('release-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root|Out-Null
try{
$profile=Get-ProductionProfile SPC
$bundle=Join-Path $root 'bundle';New-Item -ItemType Directory $bundle|Out-Null
$source=Join-Path $root 'source';New-Item -ItemType Directory $source|Out-Null
Set-Content (Join-Path $source 'app.dll') 'new'
Compress-Archive (Join-Path $source 'app.dll') (Join-Path $bundle 'api.zip')
$fileHash=(Get-FileHash (Join-Path $source 'app.dll')).Hash
$m=[pscustomobject]@{schemaVersion=1;releaseId='TEST-001';sourceCommit=('a'*40);system='SPC';host=$profile.HostName;environment='Production';database=[pscustomobject]@{name=$profile.Database; migrationFile='';migrationSha256='';migrationIds=@();knownMigrationIds=@('20261008183026_AddSpcPointRemarks')};packages=@();smoke=@([pscustomobject]@{url='http://172.16.119.140:8081/api/version';status=200;method='GET';bodyPattern='0.1.60'},[pscustomobject]@{url='http://172.16.119.140/';status=200;method='GET';bodyPattern=''})}
foreach($p in $profile.Targets){$m.packages+= [pscustomobject]@{site=$p.Site;pool=$p.Pool;path=$p.Path;archive='api.zip';sha256=(Get-FileHash (Join-Path $bundle 'api.zip')).Hash;version='1.0.0';files=@([pscustomobject]@{path='app.dll';sha256=$fileHash})}}
$m | Add-Member -NotePropertyName rollbackSmoke -NotePropertyValue $m.smoke
Check 'valid production plan' {Test-ProductionManifest $m $bundle $profile.HostName|Out-Null}
Check 'wrong host blocked' {Reject {Test-ProductionManifest $m $bundle 'OTHER'}}
Check 'forbidden target blocked' {$old=$m.packages[0].path;$m.packages[0].path='D:\Sites\PmrPortal';try{Reject {Test-ProductionManifest $m $bundle $profile.HostName}}finally{$m.packages[0].path=$old}}
Check 'tampered archive blocked' {$old=$m.packages[0].sha256;$m.packages[0].sha256='0'*64;try{Reject {Test-ProductionManifest $m $bundle $profile.HostName}}finally{$m.packages[0].sha256=$old}}
Check 'forbidden output root blocked' {Reject {Assert-ReleaseWorkspacePath 'D:\Sites\PmrPortal\release'}}
Check 'traversal blocked' {foreach($s in @('../x','C:/x','a/../x','a:stream','/root','a\..\x','x.','x ')){Reject {Assert-ReleaseRelativePath $s}}}
Check 'settings/private files blocked' {foreach($s in @('appsettings.json','x/appsettings.Production.json','web.config','uploads/a','keys/key','private-data/x','persistent-data/a','logs/x','app_offline.htm','.env','cert.pfx')){Reject {Assert-ReleasePayloadPath $s}}}
Check 'unlisted zip file blocked' {$m.packages[0].files=@();try{Reject {Test-ProductionManifest $m $bundle $profile.HostName}}finally{$m.packages[0].files=@([pscustomobject]@{path='app.dll';sha256=$fileHash})}}
Check 'missing database authorization blocks before stop' {
 $a=@{Inspect={};Stop={throw 'Must not stop'}}
 Reject {Invoke-ProductionTransaction $m $bundle (Join-Path $root 'denied') $a -ApplicationAuthorized}
 Assert (!(Test-Path (Join-Path $root 'denied')))
}
# Real filesystem transaction; replace only IIS/SQL/HTTP through explicit adapter.
function New-Scenario($name){
 $site=Join-Path $root $name;New-Item -ItemType Directory $site|Out-Null
 Set-Content (Join-Path $site 'app.dll') 'old';Set-Content (Join-Path $site 'appsettings.json') 'secret';
 $plan=[pscustomobject]@{releaseId=$name;system='SPC';host='test';database=$m.database;packages=@([pscustomobject]@{site='fake';pool='fake';path=$site;archive='api.zip';files=$m.packages[0].files});smoke=@()}
 return $plan
}
$script:events=[Collections.Generic.List[string]]::new()
$adapter=@{
 Secure={param($r)}
 Inspect={param($p)@{fake='Started'}}
 Stop={param($p)$script:events.Add('stop')}
 RestorePools={param($p,$state)$script:events.Add('restore-pools')}
 Start={param($p)$script:events.Add('start')}
 BackupIis={param($p,$r)$script:events.Add('iis')}
 BackupDatabase={param($p,$r)$script:events.Add('db-backup')}
 Migrate={param($p,$b)$script:events.Add('migrate')}
 Smoke={param($p)$script:events.Add('smoke')}
 RollbackSmoke={param($p)$script:events.Add('rollback-smoke')}
}
Check 'apply backups before migrations, preserves configuration' {
 $p=New-Scenario 'happy';$script:happyPlan=$p;$script:events.Clear()
 Invoke-ProductionTransaction $p $bundle (Join-Path $root 'happy-backup') $adapter -ApplicationAuthorized -DatabaseAuthorized|Out-Null
 Assert ((Get-Content (Join-Path $p.packages[0].path 'app.dll')) -eq 'new')
 Assert ((Get-Content (Join-Path $p.packages[0].path 'appsettings.json')) -eq 'secret')
 Assert (($script:events -join ',') -eq 'stop,iis,db-backup,migrate,start,smoke,restore-pools')
}
Check 'backup failure restores pool and never migrates' {
 $p=New-Scenario 'backup-fail';$script:events.Clear();$a=$adapter.Clone();$a.BackupDatabase={throw 'injected database backup failure'}
 Reject {Invoke-ProductionTransaction $p $bundle (Join-Path $root 'fail-backup') $a -ApplicationAuthorized -DatabaseAuthorized}
 Assert ((Get-Content (Join-Path $p.packages[0].path 'app.dll')) -eq 'old')
 Assert (!$script:events.Contains('migrate'));Assert ($script:events.Contains('restore-pools'))
}
Check 'smoke failure rolls back overwritten files' {
 $p=New-Scenario 'smoke-fail';$a=$adapter.Clone();$a.Smoke={throw 'injected smoke failure'}
 Reject {Invoke-ProductionTransaction $p $bundle (Join-Path $root 'smoke-backup') $a -ApplicationAuthorized -DatabaseAuthorized}
 Assert ((Get-Content (Join-Path $p.packages[0].path 'app.dll')) -eq 'old')
 $e=Get-Content (Join-Path $root 'smoke-backup/evidence.json') -Raw|ConvertFrom-Json;Assert ($e.status -eq 'rolled-back')
}
Check 'reused release directory blocked' {Reject {Invoke-ProductionTransaction (New-Scenario 'reuse') $bundle (Join-Path $root 'happy-backup') $adapter -ApplicationAuthorized -DatabaseAuthorized}}
Check 'manual rollback validates backup hash' {
 Set-Content (Join-Path $root 'happy-backup/files/0/app.dll') 'corrupt'
 Reject {Restore-ProductionFiles (Join-Path $root 'happy-backup') $happyPlan}
}
Check 'malicious ZIP entry rejected before extract' {
 $zipPath=Join-Path $bundle 'evil.zip';$zip=[IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
 try{[void]$zip.CreateEntry('../escape.dll')}finally{$zip.Dispose()}
 $oldArchive=$m.packages[0].archive;$oldHash=$m.packages[0].sha256
 $m.packages[0].archive='evil.zip';$m.packages[0].sha256=(Get-FileHash $zipPath).Hash
 try{Reject {Test-ProductionManifest $m $bundle $profile.HostName}}finally{$m.packages[0].archive=$oldArchive;$m.packages[0].sha256=$oldHash}
}
Check 'migration failure leaves original files and restores pools' {
 $p=New-Scenario 'migration-fail';$script:events.Clear();$a=$adapter.Clone();$a.Migrate={throw 'injected migration failure'}
 Reject {Invoke-ProductionTransaction $p $bundle (Join-Path $root 'migration-backup') $a -ApplicationAuthorized -DatabaseAuthorized}
 Assert ((Get-Content (Join-Path $p.packages[0].path 'app.dll')) -eq 'old')
 Assert ($script:events.Contains('restore-pools'))
}
Check 'rollback smoke failure explicitly keeps maintenance' {
 $p=New-Scenario 'recovery-fail';$script:events.Clear();$a=$adapter.Clone();$a.Smoke={throw 'new version fails'};$a.RollbackSmoke={throw 'old version fails'}
 Reject {Invoke-ProductionTransaction $p $bundle (Join-Path $root 'recovery-backup') $a -ApplicationAuthorized -DatabaseAuthorized}
 $e=Get-Content (Join-Path $root 'recovery-backup/evidence.json') -Raw|ConvertFrom-Json
 Assert ($e.status -eq 'recovery-failed-maintenance');Assert ($script:events[$script:events.Count-1] -eq 'stop')
}
Check 'original stopped pool state is restored' {
 $p=New-Scenario 'original-stopped';$a=$adapter.Clone();$a.Inspect={param($p)@{fake='Stopped'}};$a.RestorePools={param($p,$state)Assert ($state.fake -eq 'Stopped')}
 Invoke-ProductionTransaction $p $bundle (Join-Path $root 'stopped-backup') $a -ApplicationAuthorized -DatabaseAuthorized|Out-Null
}
Check 'bundle generator excludes settings and private payload' {
 $api=Join-Path $root 'api-input';$web=Join-Path $root 'web-input';New-Item -ItemType Directory $api,$web|Out-Null
 Set-Content (Join-Path $api 'sample.dll') 'sample';Set-Content (Join-Path $api 'appsettings.json') 'do not ship';Set-Content (Join-Path $web 'index.html') '<html></html>';Set-Content (Join-Path $web 'app.js') 'console.log(1)'
 '["20261008183026_AddSpcPointRemarks"]'|Set-Content (Join-Path $root 'catalog.json')
 & (Join-Path $PSScriptRoot '../New-ProductionReleaseBundle.ps1') -MigrationCatalog (Join-Path $root 'catalog.json') -System SPC -ReleaseId GENERATED-001 -SourceCommit ('a'*40) -ApiPackage $api -WebPackage $web -ApiVersion 1.0.0 -WebVersion 1.0.0 -PreviousApiVersion 0.9.0 -PreviousWebVersion 0.9.0 -OutputDirectory (Join-Path $root 'generated')|Out-Null
 $g=Get-Content (Join-Path $root 'generated/manifest.json') -Raw|ConvertFrom-Json
 Assert ($g.packages[0].files.Count -eq 1);Assert ($g.packages[0].files[0].path -eq 'sample.dll');Assert (@($g.smoke|Where-Object {$_.url -eq 'http://172.16.119.140/app.js' -and $_.contentType -eq 'application/javascript'}).Count -eq 1)
}
Check 'pending migration authorization must match exactly' {
 Assert-ProductionPendingMigrations @('one','two') @('two','one')
 Reject {Assert-ProductionPendingMigrations @('one','two') @('one')}
 Reject {Assert-ProductionPendingMigrations @() @('one')}
}
Check 'legacy production guard retained' {
 Import-Module (Join-Path $PSScriptRoot '../ReleaseTargetGuard.psm1') -Force
 Reject {Assert-SpcReleaseTarget Production 'C:\inetpub\wwwroot\production\backend'}
}
Write-Output "TOTAL $count passed"
}finally{
# All paths originate from the unique test root, never a supplied production path.
if($root.StartsWith([IO.Path]::GetTempPath()) -and (Split-Path $root -Leaf) -like 'release-tests-*'){Remove-Item -LiteralPath $root -Recurse -Force}
}
