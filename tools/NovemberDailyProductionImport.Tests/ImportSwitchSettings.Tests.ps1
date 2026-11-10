$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '..\NovemberDailyProductionImport\ImportSwitchSettings.psm1') -Force
$root=Join-Path ([IO.Path]::GetTempPath()) ('import-switch-'+[guid]::NewGuid())
try {
  $path=Join-Path $root 'settings.json'
  $default=Get-ImportSwitchSettings -Path $path
  if(-not $default.MasterEnabled -or -not $default.EnableChemical -or -not $default.EnableEtching){throw 'default failed'}
  try{Save-ImportSwitchSettings -Path $path -MasterEnabled $true -EnableChemical $false -EnableEtching $false;throw 'zero selection accepted'}catch{if($_.Exception.Message -notlike '*至少選擇*'){throw}}
  Save-ImportSwitchSettings -Path $path -MasterEnabled $true -EnableChemical $true -EnableEtching $false
  $saved=Get-ImportSwitchSettings -Path $path;if(-not $saved.EnableChemical -or $saved.EnableEtching){throw 'save failed'}
  $items=@([pscustomobject]@{dataType='chemical'},[pscustomobject]@{dataType='etching'},[pscustomobject]@{dataType='schedule'})
  $filtered=@(Select-EnabledImportItems -Items $items -Settings $saved)
  if($filtered.Count-ne 2 -or 'etching' -in $filtered.dataType){throw 'filter failed'}
  'ImportSwitchSettings tests passed: 4'
} finally {Remove-Item $root -Recurse -Force -ErrorAction SilentlyContinue}
