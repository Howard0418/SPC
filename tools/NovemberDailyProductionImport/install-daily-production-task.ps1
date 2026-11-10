[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ExpectedHost = 'PMR-SPC-SRV',
    [string]$TaskName = 'PMR TransFiles 每日前一日正式匯入'
)
$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -ne $ExpectedHost) { throw "排程只能安裝於 $ExpectedHost；目前為 $env:COMPUTERNAME。" }
$pwsh = (Get-Command pwsh.exe -ErrorAction Stop).Source
$runner = (Resolve-Path (Join-Path $PSScriptRoot 'daily-production-import.ps1')).Path
$action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$runner`"" -WorkingDirectory (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$trigger = New-ScheduledTaskTrigger -Daily -At '09:00'
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)
$principal = New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Highest
if ($PSCmdlet.ShouldProcess($env:COMPUTERNAME,"建立並啟用 $TaskName")) {
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description '每日 09:00 掃描並以防重複規則匯入前一日藥液與咬蝕量；錯誤或衝突整日阻擋。' -Force | Out-Null
    $task = Get-ScheduledTask -TaskName $TaskName
    if ($task.State -eq 'Disabled') { Enable-ScheduledTask -TaskName $TaskName | Out-Null }
    Get-ScheduledTask -TaskName $TaskName
}
