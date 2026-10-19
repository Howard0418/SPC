$ErrorActionPreference='Stop'
$count=0
function Check($name,[scriptblock]$body){& $body;$script:count++;Write-Output "PASS $name"}
function Assert($value){if(!$value){throw 'Expected nonzero failure exit code'}}
$missing=Join-Path ([IO.Path]::GetTempPath()) ('missing-release-'+[guid]::NewGuid().ToString('N')+'.json')
Check 'server worker propagates validation failure' {
 $prior=$ErrorActionPreference;$ErrorActionPreference='Continue'
 try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '../Start-ReleaseWorker.ps1') -Environment Test -Mode Plan -ManifestPath $missing 2>&1|Out-Null;Assert ($LASTEXITCODE -ne 0)}finally{$ErrorActionPreference=$prior}
}
Check 'production wrapper propagates validation failure' {
 $prior=$ErrorActionPreference;$ErrorActionPreference='Continue'
 try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '../Invoke-ProductionRelease.ps1') -Mode Plan -ManifestPath $missing 2>&1|Out-Null;Assert ($LASTEXITCODE -ne 0)}finally{$ErrorActionPreference=$prior}
}
$root=Join-Path ([IO.Path]::GetTempPath()) ('desktop-bridge-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root|Out-Null
try{
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../desktop/Invoke-DesktopRelease.ps1') -Destination $root
 "exit 7"|Set-Content (Join-Path $root 'Invoke-Release.ps1') -Encoding UTF8
 $request=@{Environment='Test';System='SPC';Mode='Plan';ManifestPath=$missing;ApplicationAuthorized=$false;DatabaseAuthorized=$false}|ConvertTo-Json
 $base64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($request))
 Check 'desktop bridge propagates child failure' {
  $prior=$ErrorActionPreference;$ErrorActionPreference='Continue'
  try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Invoke-DesktopRelease.ps1') -RequestBase64 $base64 2>&1|Out-Null;Assert ($LASTEXITCODE -ne 0)}finally{$ErrorActionPreference=$prior}
 }
 Check 'desktop bridge rejects unsupported mode' {
  $request=@{Environment='Test';System='SPC';Mode='Delete';ManifestPath=$missing;ApplicationAuthorized=$true;DatabaseAuthorized=$true}|ConvertTo-Json
  $bad=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($request))
  $prior=$ErrorActionPreference;$ErrorActionPreference='Continue'
  try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Invoke-DesktopRelease.ps1') -RequestBase64 $bad 2>&1|Out-Null;Assert ($LASTEXITCODE -ne 0)}finally{$ErrorActionPreference=$prior}
 }
 Write-Output "TOTAL $count passed"
}finally{if($root.StartsWith([IO.Path]::GetTempPath()) -and (Split-Path $root -Leaf) -like 'desktop-bridge-tests-*'){Remove-Item -LiteralPath $root -Recurse -Force}}
