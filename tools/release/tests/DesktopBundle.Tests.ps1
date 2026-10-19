$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('bundle-bridge-tests-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory $root|Out-Null
$count=0
function Check($name,[scriptblock]$body){& $body;$script:count++;Write-Output "PASS $name"}
function Invoke-Rejected($request){$encoded=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($request|ConvertTo-Json -Depth 8)));$prior=$ErrorActionPreference;$ErrorActionPreference='Continue';try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Invoke-DesktopBundle.ps1') -RequestBase64 $encoded 2>&1|Out-Null;if($LASTEXITCODE -eq 0){throw 'Expected failure'}}finally{$ErrorActionPreference=$prior}}
try{
Copy-Item (Join-Path $PSScriptRoot '../desktop/Invoke-DesktopBundle.ps1') $root
Copy-Item (Join-Path $PSScriptRoot '../ProductionRelease.psm1') $root
$r=@{Operation='CreateBundle';Environment='Test';System='SPC';ReportPath=(Join-Path $root 'result.json');ReleaseId='BUNDLE-001';SourceCommit=('a'*40);OutputParent=$root}
Check 'unsupported operation cannot deploy' {$bad=$r.Clone();$bad.Operation='Apply';Invoke-Rejected $bad}
Check 'unsupported environment refused' {$bad=$r.Clone();$bad.Environment='Other';Invoke-Rejected $bad}
Check 'report cannot escape private cache' {$bad=$r.Clone();$bad.ReportPath=Join-Path (Split-Path $root -Parent) 'outside-bundle-test.json';Invoke-Rejected $bad}
Check 'missing source identity refused before output' {$bad=$r.Clone();$bad.SourceCommit='';Invoke-Rejected $bad;if(Test-Path (Join-Path $root 'BUNDLE-001')){throw 'Unexpected output'}}
Check 'existing bundle never overwritten' {$out=Join-Path $root 'BUNDLE-001';New-Item -ItemType Directory $out|Out-Null;Set-Content (Join-Path $out 'keep.txt') 'original';Invoke-Rejected $r;if((Get-Content (Join-Path $out 'keep.txt')) -ne 'original'){throw 'Overwrote existing output'}}
Write-Output "TOTAL $count passed"
}finally{if($root.StartsWith([IO.Path]::GetTempPath()) -and (Split-Path $root -Leaf) -like 'bundle-bridge-tests-*'){Remove-Item -LiteralPath $root -Recurse -Force}}
