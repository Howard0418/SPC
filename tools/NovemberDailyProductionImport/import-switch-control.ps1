[CmdletBinding()]param([string]$RunRoot='C:\ProgramData\PMR\TransFilesDaily',[string]$TaskName='PMR TransFiles 每日前一日正式匯入')
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Windows.Forms;Add-Type -AssemblyName System.Drawing
Import-Module (Join-Path $PSScriptRoot 'ImportSwitchSettings.psm1') -Force
$path=Join-Path $RunRoot 'settings.json';$settings=Get-ImportSwitchSettings -Path $path
$form=[Windows.Forms.Form]@{Text='PMR 每日匯入開關';Size=[Drawing.Size]::new(430,300);StartPosition='CenterScreen';FormBorderStyle='FixedDialog';MaximizeBox=$false}
$title=[Windows.Forms.Label]@{Text='每日 09:00 正式資料匯入';Location=[Drawing.Point]::new(24,20);AutoSize=$true;Font=[Drawing.Font]::new('Microsoft JhengHei UI',14,[Drawing.FontStyle]::Bold)}
$master=[Windows.Forms.CheckBox]@{Text='啟用每日排程';Location=[Drawing.Point]::new(28,70);AutoSize=$true;Checked=$settings.MasterEnabled}
$chemical=[Windows.Forms.CheckBox]@{Text='匯入藥液資料';Location=[Drawing.Point]::new(52,110);AutoSize=$true;Checked=$settings.EnableChemical}
$etch=[Windows.Forms.CheckBox]@{Text='匯入咬蝕量資料';Location=[Drawing.Point]::new(52,145);AutoSize=$true;Checked=$settings.EnableEtching}
$hint=[Windows.Forms.Label]@{Text='關閉總開關會停用排程；啟用時至少選擇一項。';Location=[Drawing.Point]::new(28,185);AutoSize=$true;ForeColor=[Drawing.Color]::DimGray}
$save=[Windows.Forms.Button]@{Text='儲存並套用';Location=[Drawing.Point]::new(205,220);Size=[Drawing.Size]::new(105,32)}
$cancel=[Windows.Forms.Button]@{Text='取消';Location=[Drawing.Point]::new(320,220);Size=[Drawing.Size]::new(75,32)}
$save.Add_Click({try{Save-ImportSwitchSettings -Path $path -MasterEnabled $master.Checked -EnableChemical $chemical.Checked -EnableEtching $etch.Checked;if($master.Checked){Enable-ScheduledTask -TaskName $TaskName|Out-Null}else{Disable-ScheduledTask -TaskName $TaskName|Out-Null};[Windows.Forms.MessageBox]::Show('設定已套用。','完成','OK','Information')|Out-Null;$form.Close()}catch{[Windows.Forms.MessageBox]::Show($_.Exception.Message,'無法儲存','OK','Error')|Out-Null}})
$cancel.Add_Click({$form.Close()});$form.Controls.AddRange(@($title,$master,$chemical,$etch,$hint,$save,$cancel));[void]$form.ShowDialog()
