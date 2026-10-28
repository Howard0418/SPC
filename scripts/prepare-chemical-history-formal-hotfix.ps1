param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$output=[IO.Path]::GetFullPath($OutputRoot)
if(!$output.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Candidate must remain inside this workspace'}
if(Test-Path -LiteralPath $output){throw 'Choose a new candidate directory; existing files are never replaced'}
$baseline='a376290fd3da244397a6b04ce9270342eed80798'
New-Item -ItemType Directory -Path $output | Out-Null
$zip=Join-Path $output 'baseline.zip';$source=Join-Path $output 'source'
& git -C $repo archive --format=zip "--output=$zip" $baseline backend/MesSpc.Api tests/MesSpc.Api.Tests tests/MesSpc.Chemical.Tests tools/EtchTrialImport
if($LASTEXITCODE){throw 'Baseline archive failed'}
Expand-Archive -LiteralPath $zip -DestinationPath $source
$backend=Join-Path $source 'backend/MesSpc.Api'
$formalSource=Join-Path $repo 'tmp_production_config_plan/ppc-optional-machine/production-source/backend/MesSpc.Api'
$formalPackage=Join-Path $repo 'tmp_production_config_plan/ppc-optional-machine/production-package/MesSpc.Api.dll'
if((Get-FileHash -LiteralPath $formalPackage).Hash -ne '423204FFC1B0CF685C7E71B46F4BEB6B71EC561690FCD126E2102C6142058F18'){throw 'Previous formal package does not match the verified deployment'}
foreach($relative in @('Controllers/MasterDataV2Controller.cs','MesSpc.Api.csproj')){
    $expected=if($relative -like 'Controllers/*'){'86EA95A236D3C490BBAF63C0AA7B35362B06E0A16CA7617756237239F31F94F6'}else{'90262F0B20E9AACB48C917A9E382677997B80ACCFAD6CD4A49D459BAA70A30F8'}
    if((Get-FileHash -LiteralPath (Join-Path $formalSource $relative)).Hash -ne $expected){throw 'Verified formal source changed; review again'}
    Copy-Item -LiteralPath (Join-Path $formalSource $relative) -Destination (Join-Path $backend $relative)
}
$original=@(Get-ChildItem -LiteralPath $backend -File -Recurse | ForEach-Object {@{Path=$_.FullName.Substring($backend.Length+1);Hash=(Get-FileHash $_.FullName).Hash}})
$controller='Controllers/MasterDataV2Controller.cs'
$old=[IO.File]::ReadAllText((Join-Path $backend $controller));$current=[IO.File]::ReadAllText((Join-Path $repo "backend/MesSpc.Api/$controller"))
$start='        var variableQuery =';$end='    [HttpDelete("{id:int}")]'
$a=$old.IndexOf($start);$b=$old.IndexOf($end,$a);$ca=$current.IndexOf($start);$cb=$current.IndexOf($end,$ca)
if($a -lt 0 -or $b -lt 0 -or $ca -lt 0 -or $cb -lt 0){throw 'Measurement block changed; review before preparing'}
[IO.File]::WriteAllText((Join-Path $backend $controller),$old.Substring(0,$a)+$current.Substring($ca,$cb-$ca)+$old.Substring($b))
$trace=Join-Path $backend 'Services/ChemicalTraceQueryService.cs';$text=[IO.File]::ReadAllText($trace)
if(!$text.Contains('private static ChemicalTraceCalculation Read(')){throw 'Trace reader baseline changed'}
[IO.File]::WriteAllText($trace,$text.Replace('private static ChemicalTraceCalculation Read(','internal static ChemicalTraceCalculation Read('))
Copy-Item -LiteralPath (Join-Path $repo 'backend/MesSpc.Api/Services/ChemicalHistoryQueryService.cs') -Destination (Join-Path $backend 'Services/ChemicalHistoryQueryService.cs')
$project=Join-Path $backend 'MesSpc.Api.csproj';$text=[IO.File]::ReadAllText($project)
if(!$text.Contains('<Version>0.1.71</Version>')){throw 'Unexpected baseline version'}
[IO.File]::WriteAllText($project,$text.Replace('<Version>0.1.71</Version>','<Version>0.1.90</Version>'))
Copy-Item -LiteralPath (Join-Path $repo 'tests/MesSpc.Api.Tests/ChemicalHistoryQueryTests.cs') -Destination (Join-Path $source 'tests/MesSpc.Api.Tests/ChemicalHistoryQueryTests.cs')
Copy-Item -LiteralPath (Join-Path $repo 'tmp_production_config_plan/ppc-optional-machine/production-tests/PpcOptionalMachineEditTests.cs') -Destination (Join-Path $source 'tests/MesSpc.Api.Tests/PpcOptionalMachineEditTests.cs')
$allowed=@('Controllers\MasterDataV2Controller.cs','Services\ChemicalTraceQueryService.cs','MesSpc.Api.csproj')
$changed=@($original | Where-Object {(Get-FileHash -LiteralPath (Join-Path $backend $_.Path)).Hash -ne $_.Hash})
if($changed.Count -ne 3 -or @($changed | Where-Object {$_.Path -notin $allowed}).Count){throw 'Unexpected source changes'}
[pscustomobject]@{Baseline=$baseline;PriorFormalRelease='SPC-PPC-OPTIONAL-MACHINE-20261022-R2';Version='0.1.90';ChangedBaselineFiles=$changed.Path;AddedService='Services/ChemicalHistoryQueryService.cs';ProgramModelsMigrationsPackagesUnchanged=$true;Deployed=$false} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'source-proof.json') -Encoding UTF8
Write-Output "Prepared candidate source only: $source"
