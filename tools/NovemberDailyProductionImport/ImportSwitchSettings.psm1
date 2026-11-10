function Get-ImportSwitchSettings {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{ MasterEnabled=$true; EnableChemical=$true; EnableEtching=$true }
    }
    try {
        $value=Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        return [pscustomobject]@{
            MasterEnabled=if($null -eq $value.MasterEnabled){$true}else{[bool]$value.MasterEnabled}
            EnableChemical=if($null -eq $value.EnableChemical){$true}else{[bool]$value.EnableChemical}
            EnableEtching=if($null -eq $value.EnableEtching){$true}else{[bool]$value.EnableEtching}
        }
    } catch { throw "匯入開關設定損壞，請重新儲存：$($_.Exception.Message)" }
}
function Save-ImportSwitchSettings {
    param([Parameter(Mandatory)][string]$Path,[bool]$MasterEnabled,[bool]$EnableChemical,[bool]$EnableEtching)
    if($MasterEnabled -and -not($EnableChemical -or $EnableEtching)){throw '啟用排程時至少選擇藥液或咬蝕量。'}
    $parent=Split-Path $Path -Parent;New-Item -ItemType Directory -Path $parent -Force|Out-Null
    [ordered]@{MasterEnabled=$MasterEnabled;EnableChemical=$EnableChemical;EnableEtching=$EnableEtching;UpdatedAt=(Get-Date).ToString('o');UpdatedBy=[Security.Principal.WindowsIdentity]::GetCurrent().Name}|ConvertTo-Json|Set-Content -LiteralPath $Path -Encoding utf8
}
function Select-EnabledImportItems {
    param([Parameter(Mandatory)]$Items,[Parameter(Mandatory)]$Settings)
    $Items|Where-Object{($_.dataType -eq 'chemical' -and $Settings.EnableChemical)-or($_.dataType -eq 'etching' -and $Settings.EnableEtching)-or($_.dataType -notin @('chemical','etching'))}
}
Export-ModuleMember -Function Get-ImportSwitchSettings,Save-ImportSwitchSettings,Select-EnabledImportItems
