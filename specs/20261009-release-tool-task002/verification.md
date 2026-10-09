# RELEASE-TOOL-TASK-002 驗證紀錄

日期：2026-10-09

## 檢查項目

- 已新增 `tools/release/ReleaseBackupRestore.ps1`。
- 工具拒絕 Production/Formal/正式環境名稱。
- 工具拒絕 `D:\Sites\PmrPortal` 目標路徑。
- Restore 需要 `-AllowNonProductionRestore`。
- Restore 不刪除目標多餘檔案。
- 未連線正式機、未操作正式 IIS、未讀取機敏設定、未刪除資料。

## 結果

- 文件驗證：通過。
- PowerShell 語法：通過，`[scriptblock]::Create((Get-Content -Raw tools\release\ReleaseBackupRestore.ps1))` 回 `syntax-ok`。
- Production 阻擋：通過，`-EnvironmentName Production` 會停止並回報需要正式授權。
- Portal 路徑阻擋：通過，`-SitePath D:\Sites\PmrPortal` 會停止並回報不可作為 SPC release tool 目標。
- 非正式資料夾實際備份/回復：未執行；未建立或刪除任何 sandbox 檔案。
- 2026-10-09 RELEASE-TOOL-TASK-004 演練補充：首次 sandbox 備份發現 `Copy-Item -LiteralPath ... *` 不展開 wildcard，已改用 `-Path`，並以 `sandbox-rollback-002` 完成備份與回復驗證。
- 發布：不適用；未發布測試站或正式站。
