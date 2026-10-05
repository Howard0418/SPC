# 9/1 四線試匯工具
本工具只允許2026-09-01的PT1/PT2/QE1/QE2；只允許172.16.110.16的PMR_SPC_TEST、PMR_PORTAL_TEST，不接受其他日期/正式庫。

1. `python -m unittest discover -s tools/EtchTrialImport -p test_extract.py`
2. `python tools/EtchTrialImport/extract.py docs/PD-3-581-06B-咬蝕量_2026.xlsx release-staging/etch-trial-20260917/input.json`
3. `dotnet run --project tools/EtchTrialImport -- release-staging/etch-trial-20260917/input.json`：只讀預覽。
4. 核准後加`--apply`：先寫目標備份與來源manifest，再受控补主檔，Portal完整日報＋SPC完整子組。相同內容略過，不同內容拒絕。

每個run目錄保存before.json、input.json、result.json、masters-after.json。原Excel不改。兩系統不是分散式交易，若SPC失敗Portal保留Pending、可嚴格重跑恢復。既有衝突不可直接覆蓋。程序異常時先讀備份與批次證據，不自動整庫回復。

日報沒有原量測人員時標示「歷史匯入（原量測人員未記錄）」，UpdatedBy/UploadBatch.CreatedBy標示匯入工具，不能當作實際量測者。

## 2026-09-18 月份模式
明確加入--month才允許2026年9月；extract.py與dotnet工具皆支援，預設仍限制9/1。負值及未完整日報隔離，空白略過。日期＋線別作為既有日報比對鍵。只允許既定兩個測試庫。
本次預覽48份；月份apply尚待第3批明確授權，詳見specs/20260918-etch-month-import。

第3批已於2026-09-18明確核准並完成；48份（新增44）重跑皆略過。結果見specs/20260918-etch-month-import/verification.md。

## 2026-09-22 正式環境 dry-run
正式 Portal 已確認為 `PMR_PORTAL_UAT`，SPC 為 `PMR_SPC_2026`。目前工具只開放正式唯讀 dry-run：

```powershell
dotnet run --project tools/EtchTrialImport --no-build -- `
  release-staging/etch-month-20260918/input.json `
  --month `
  --environment=production
```

dry-run 會驗證固定來源 SHA256、48 份／3,650 點、正式資料庫連線、既有鍵與主檔差異，並輸出：

`release-staging/etch-month-20260918/master-plan-production.json`

正式備份腳本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  tools/EtchTrialImport/BackupProductionDatabases.ps1 `
  -Execute `
  -ConfirmPortal PMR_PORTAL_UAT `
  -ConfirmSpc PMR_SPC_2026
```

正式 `--apply` 必須提供兩個精確資料庫確認值，以及 24 小時內、兩庫皆通過 COPY_ONLY／CHECKSUM／VERIFYONLY 的 `--backup-proof`。即使證據有效，仍須先取得使用者明確 apply 授權；不可因 dry-run 或備份完成而自動匯入。
