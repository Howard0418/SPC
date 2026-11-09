param(
    [Parameter(Mandatory=$true)][string]$SettingsPath,
    [Parameter(Mandatory=$true)][int]$OperatorId
)
$ErrorActionPreference='Stop'
if($OperatorId -le 0){throw 'OperatorId must be positive'}
$resolved=(Resolve-Path -LiteralPath $SettingsPath).Path
if($resolved -notlike 'D:\SPC\release\test\backend\appsettings.json'){throw 'Only the SPC test private settings file is allowed'}
$json=Get-Content -LiteralPath $resolved -Raw|ConvertFrom-Json
if($json.AppEnvironment -ne 'test'){throw 'Only the test environment is allowed'}
if($null -eq $json.Audit){throw 'Audit settings are missing'}
$property=$json.Audit.PSObject.Properties['SecurityAdministratorOperatorIds']
$ids=if($null -eq $property){@()}else{@($property.Value|ForEach-Object{[int]$_})}
$next=@($ids+$OperatorId|Sort-Object -Unique)
if($null -eq $property){$json.Audit|Add-Member -NotePropertyName SecurityAdministratorOperatorIds -NotePropertyValue $next}
else{$json.Audit.SecurityAdministratorOperatorIds=$next}
$text=$json|ConvertTo-Json -Depth 20
[IO.File]::WriteAllText($resolved,$text,[Text.UTF8Encoding]::new($false))
$check=Get-Content -LiteralPath $resolved -Raw|ConvertFrom-Json
if($check.AppEnvironment -ne 'test' -or @($check.Audit.SecurityAdministratorOperatorIds) -notcontains $OperatorId){throw 'Security administrator update verification failed'}
[pscustomobject]@{Environment=$check.AppEnvironment;SecurityEnabled=$check.Audit.SecurityEnabled;AdministratorIds=(@($check.Audit.SecurityAdministratorOperatorIds)-join ',')}
