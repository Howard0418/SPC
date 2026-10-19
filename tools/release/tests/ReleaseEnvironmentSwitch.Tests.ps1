$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '../ProductionRelease.psm1') -Force
Import-Module (Join-Path $PSScriptRoot '../ReleaseRemote.psm1') -Force
$n=0
function Check($name,[scriptblock]$body){& $body;$script:n++;Write-Output "PASS $name"}
function Assert($value){if(!$value){throw 'Assertion failed'}}
function Reject([scriptblock]$body){$failed=$false;try{& $body|Out-Null}catch{$failed=$true};Assert $failed}
Check 'four allowlisted targets' {
 $s=Get-ReleaseProfile SPC Test;$p=Get-ReleaseProfile Portal Test
 Assert ($s.HostName -eq 'PC-PA16560048.pmr.com.tw');Assert ($s.Database -eq 'PMR_SPC_TEST')
 Assert ($p.Database -eq 'PMR_PORTAL_TEST');Assert ($p.Targets[0].Pool -eq 'PmrPortal')
 Assert ((Get-ReleaseProfile SPC Production).HostName -eq 'PMR-SPC-SRV.pmr.com.tw')
 Assert ((Get-ReleaseProfile Portal Production).HostName -eq 'PMR-WEB.pmr.com.tw')
}
Check 'single-site SPC has two paths and one pool' {
 $p=Get-ReleaseProfile SPC Test
 Assert ($p.Targets[0].Site -eq $p.Targets[1].Site)
 Assert ($p.Targets[0].Path -ne $p.Targets[1].Path)
 Assert ($p.Targets[0].IisPath -eq $p.Targets[1].IisPath)
}
Check 'staging cannot target any test or production website' {
 foreach($envName in @('Test','Production')){foreach($systemName in @('SPC','Portal')){foreach($target in (Get-ReleaseProfile $systemName $envName).Targets){Reject {Assert-ReleaseWorkspacePath (Join-Path $target.Path 'new-stage')}}}}
}
$root=Join-Path ([IO.Path]::GetTempPath()) ('release-switch-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root|Out-Null
try{
$api=Join-Path $root 'api';$web=Join-Path $root 'web';New-Item -ItemType Directory $api,$web|Out-Null
Set-Content (Join-Path $api 'sample.dll') 'binary';Set-Content (Join-Path $web 'index.html') '<html></html>'
'["20261008183026_AddSpcPointRemarks"]'|Set-Content (Join-Path $root 'catalog.json')
foreach($systemName in @('SPC','Portal')){
 $out=Join-Path $root ('bundle-'+$systemName)
 & (Join-Path $PSScriptRoot '../New-ProductionReleaseBundle.ps1') -Environment Test -System $systemName -ReleaseId ('TEST-'+$systemName+'-001') -SourceCommit ('a'*40) -ApiPackage $api -WebPackage $web -ApiVersion 1.0.0 -WebVersion 1.0.0 -PreviousApiVersion 0.9.0 -PreviousWebVersion 0.9.0 -MigrationCatalog (Join-Path $root 'catalog.json') -OutputDirectory $out|Out-Null
 $m=Get-Content (Join-Path $out 'manifest.json') -Raw|ConvertFrom-Json
 Check ($systemName+' test manifest and hostname validation') {
  [void](Test-ReleaseManifest $m $out 'PC-PA16560048' Test)
  Reject {Test-ProductionManifest $m $out 'PC-PA16560048'}
  Reject {Test-ReleaseManifest $m $out 'PMR-WEB' Test}
  Reject {Test-ReleaseManifest $m $out 'PC-PA16560048' Production}
 }
 Check ($systemName+' test smoke hosts') {Assert (@($m.smoke|Where-Object {$_.url -match '172\.16\.119\.|login\.pmr\.com'}).Count -eq 0)}
}
$manifest=Join-Path $root 'bundle-SPC/manifest.json'
Check 'dispatch rejects environment and system mismatch before transport' {
 Reject {New-ReleaseDispatchRequest $manifest Production SPC Plan $PSScriptRoot}
 Reject {New-ReleaseDispatchRequest $manifest Test Portal Plan $PSScriptRoot}
}
Check 'apply needs both authorizations' {
 Reject {New-ReleaseDispatchRequest $manifest Test SPC Apply $PSScriptRoot}
 Reject {New-ReleaseDispatchRequest $manifest Test SPC Apply $PSScriptRoot -ApplicationAuthorized}
}
$request=New-ReleaseDispatchRequest $manifest Test SPC Plan (Split-Path $PSScriptRoot -Parent)
Check 'request targets only expected machine and payload' {
 Assert ($request.HostName -eq 'PC-PA16560048.pmr.com.tw')
 Assert ($request.Environment -eq 'Test');Assert ($request.Files.Count -eq 6)
 Assert (@($request.Files|Where-Object {$_.Destination -match '(appsettings|web.config)'}).Count -eq 0)
}
$script:events=[Collections.Generic.List[string]]::new()
$transport=@{
 Open={param($r)$script:events.Add('open');return @{id='fake'}}
 Stage={param($c,$r)$script:events.Add('stage');return 'C:\safe\manifest.json'}
 Run={param($c,$r,$path)$script:events.Add('run');Assert ($r.Mode -eq 'Plan');Assert ($path -eq 'C:\safe\manifest.json');return 'planned'}
 Close={param($c)$script:events.Add('close')}
}
Check 'CLI WhatIf does not create incoming or connect' {
 Assert (!(Test-Path -LiteralPath $request.Incoming))
 & (Join-Path $PSScriptRoot '../Invoke-Release.ps1') -System SPC -Environment Test -Mode Plan -ManifestPath $manifest -WhatIf
 Assert (!(Test-Path -LiteralPath $request.Incoming))
}
Check 'transport stages, runs and closes in order' {
 $script:events.Clear();$result=Invoke-ReleaseDispatch $request $transport
 Assert ($result -eq 'planned');Assert (($script:events -join ',') -eq 'open,stage,run,close')
}
Check 'transport failure always closes without run' {
 $script:events.Clear();$a=$transport.Clone();$a.Stage={throw 'copy failure'}
 Reject {Invoke-ReleaseDispatch $request $a}
 Assert (($script:events -join ',') -eq 'open,close')
}
Check 'tampered transfer source rejected before connection' {
 Set-Content (Join-Path $root 'bundle-SPC/SpcSingleTest-Api.zip') 'tampered'
 $script:events.Clear();Reject {Invoke-ReleaseDispatch $request $transport};Assert ($script:events.Count -eq 0)
}
Write-Output "TOTAL $n passed"
}finally{if($root.StartsWith([IO.Path]::GetTempPath()) -and (Split-Path $root -Leaf) -like 'release-switch-tests-*'){Remove-Item -LiteralPath $root -Recurse -Force}}
