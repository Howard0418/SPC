# ARCH-TASK-006 驗證紀錄

## 自動化驗證

- `pwsh -NoProfile -File tests\release\ReleaseTargetGuard.Tests.ps1`
  - 結果：通過，輸出 `ReleaseTargetGuard.Tests passed.`
  - 覆蓋：Production/正式名稱拒絕、`D:\Sites\PmrPortal` 子路徑拒絕、Portal 兄弟路徑不誤擋、空白 EnvironmentName/SitePath 拒絕、Sandbox + SPC 測試路徑允許。

## 工具載入驗證

- `pwsh -NoProfile -File tools\release\ReleaseApplyPackage.ps1 -EnvironmentName Production -SitePath D:\SPC\release\test\backend -PackagePath D:\SPC\release\test\backend -WhatIf`
  - 結果：如預期失敗，錯誤為 `This prototype refuses Production targets...`；未進入檔案操作。
- `pwsh -NoProfile -File tools\release\ReleaseBackupRestore.ps1 -Mode Backup -EnvironmentName Sandbox -SitePath D:\Sites\PmrPortal -BackupRoot D:\SPC\release-staging -WhatIf`
  - 結果：如預期失敗，錯誤為 `Blocked target path: D:\Sites\PmrPortal...`；未進入檔案操作。
- `pwsh -NoProfile -File tools\release\ReleaseApplyPackage.ps1 -EnvironmentName Sandbox -SitePath D:\SPC\release\test\backend -PackagePath D:\SPC\release\test\backend -WhatIf`
  - 結果：通過，進入 WhatIf `Stage package and preserved files`。
- `pwsh -NoProfile -File tools\release\ReleaseBackupRestore.ps1 -Mode Backup -EnvironmentName Sandbox -SitePath D:\SPC\release\test\backend -BackupRoot D:\SPC\release-staging -WhatIf`
  - 結果：通過，進入 WhatIf `Create backup directory and copy site files`。

## 發布

- 本次為 release 工具與文件小工作，未發布 SPC 測試站或正式站。
