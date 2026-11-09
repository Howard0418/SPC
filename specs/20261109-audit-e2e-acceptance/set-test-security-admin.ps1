param(
    [Parameter(Mandatory=$true)][string]$SettingsPath,
    [Parameter(Mandatory=$true)][int[]]$OperatorId,
    [switch]$Replace
)
$ErrorActionPreference='Stop'
if($OperatorId.Count -eq 0 -or $OperatorId.Count -gt 64 -or ($OperatorId|Where-Object{$_ -le 0}).Count -gt 0){throw 'OperatorId values must be positive'}
$resolved=(Resolve-Path -LiteralPath $SettingsPath).Path
if($resolved -notlike 'D:\SPC\release\test\backend\appsettings.json'){throw 'Only the SPC test private settings file is allowed'}
$json=Get-Content -LiteralPath $resolved -Raw|ConvertFrom-Json
if($json.AppEnvironment -ne 'test'){throw 'Only the test environment is allowed'}
if($null -eq $json.Audit){throw 'Audit settings are missing'}
$property=$json.Audit.PSObject.Properties['SecurityAdministratorOperatorIds']
$ids=if($null -eq $property){@()}else{@($property.Value|ForEach-Object{[int]$_})}
$candidates=if($Replace){@($OperatorId)}else{@($ids)+@($OperatorId)}
$next=@($candidates|Sort-Object -Unique)
if($null -eq $property){$json.Audit|Add-Member -NotePropertyName SecurityAdministratorOperatorIds -NotePropertyValue $next}
else{$json.Audit.SecurityAdministratorOperatorIds=$next}
$text=$json|ConvertTo-Json -Depth 20
[IO.File]::WriteAllText($resolved,$text,[Text.UTF8Encoding]::new($false))
$check=Get-Content -LiteralPath $resolved -Raw|ConvertFrom-Json
$actual=@($check.Audit.SecurityAdministratorOperatorIds|ForEach-Object{[int]$_}|Sort-Object -Unique)
$difference=Compare-Object $actual $next
if($check.AppEnvironment -ne 'test' -or $null -ne $difference){throw 'Security administrator update verification failed'}
[pscustomobject]@{Environment=$check.AppEnvironment;SecurityEnabled=$check.Audit.SecurityEnabled;AdministratorIds=(@($check.Audit.SecurityAdministratorOperatorIds)-join ',')}
