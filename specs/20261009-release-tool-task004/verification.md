# RELEASE-TOOL-TASK-004 驗證紀錄

日期：2026-10-09

## 執行環境

- 目標：本機非正式 sandbox。
- Root：`D:\SPC\release-staging\release-tool-task004\run-20261009-201622`
- Site：`D:\SPC\release-staging\release-tool-task004\run-20261009-201622\site`
- Package：`D:\SPC\release-staging\release-tool-task004\run-20261009-201622\package`
- Backup：`D:\SPC\release-staging\release-tool-task004\run-20261009-201622\backups\sandbox-rollback-002`

## 執行結果

- 首次備份演練發現 `Copy-Item -LiteralPath ... *` 不展開 wildcard，已修正 `ReleaseBackupRestore.ps1` 與 `ReleaseApplyPackage.ps1` 改用 `-Path` 處理 wildcard。
- 備份重跑成功：產生 `backup-manifest.json` 與 `evidence/site-hashes.json`。
- 套用成功：產生 `_release-evidence-20261009-201703.json`。
- 回復成功：產生 `restore-evidence.json`。
- 回復後 `index.html` 為 `sandbox v1`。
- 回復後 `appsettings.json` 為測試環境設定：`{"Environment":"Sandbox","Secret":"TEST_VALUE_ONLY"}`。
- `app_offline.htm` 仍存在，符合不自動刪除規則；移除需另行確認。

## 安全檢查

- 未連線正式機。
- 未操作正式 IIS。
- 未讀取或寫入正式設定。
- 未刪除任何資料或檔案。
- Sandbox 證據輸出未納入提交。

## 發布

- 不適用；未發布測試站或正式站。
