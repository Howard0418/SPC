# 驗證紀錄
- 功能 ID：20260922-etch-production-import
- 規格版本：1
- 日期／環境：2026-09-22；正式目標只讀盤點
- 實作狀態：完成
- 驗證狀態：49 個 Chemical/.NET 測試、7 個 Excel 解析測試、正式 dry-run、雙庫備份、匯入、DB／圖表對帳及重跑均通過
- 發布狀態：無應用程式發布；正式資料匯入完成

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 來源與 manifest 驗證 | 固定 SHA256、48 份、3,650 點 | production dry-run 驗證通過 | 通過 |
| AC-002 | 正式連線盤點／守衛測試 | Portal=`PMR_PORTAL_UAT`、SPC=`PMR_SPC_2026` | 守衛測試 9 項通過；錯誤伺服器／庫名與缺 confirm 均拒絕 | 通過 |
| AC-003 | 48 鍵正式差異 | 正式無衝突 | dry-run：Portal existing=0、SPC existing=0 | 通過 |
| AC-004 | 兩正式庫備份＋VERIFYONLY | 寫入前均通過 | Portal backup set 5096、SPC 5097；兩者 COPY_ONLY／CHECKSUM／VERIFYONLY 通過 | 通過 |
| AC-005 | 主檔差異與補齊 | 四線 16 PPC 完整且圖型／樣本數正確 | 15 項計畫套用後 16 個 mapping／16 個正式圖表 API 成功 | 通過 |
| AC-006 | 正式匯入筆數 | Portal 48／3,650；SPC 3,746 | Portal 48／3,650；SPC 3,746 | 通過 |
| AC-007 | 跨系統失敗／重跑 | Pending 可安全續跑 | 48 份皆 SUCCESS；重跑安全 | 通過 |
| AC-008 | 逐點與圖表對帳 | 來源值、平均、S、速率、線速一致 | 3,650 點及 16 圖逐組平均／樣本 S／速率／線速一致 | 通過 |
| AC-009 | 第二次重跑 | 48 份 Unchanged、ID 不變 | 48 份全部 Unchanged；ReportId／BatchId 與首次相同 | 通過 |
| AC-010 | 隔離查詢 | 8 個隔離鍵無本批資料 | quarantinedRows=0 | 通過 |

## 執行結果、限制及後續
- 使用者於 2026-09-22 11:09（UTC+8）明確授權正式 apply。
- dry-run 命令：`dotnet run --project tools/EtchTrialImport --no-build -- release-staging/etch-month-20260918/input.json --month --environment=production`
- dry-run 輸出：`release-staging/etch-month-20260918/master-plan-production.json`，15 項主檔變更。
- 工具建置 0 警告／0 錯誤；Chemical 測試 49 通過；Python 解析測試 7 通過。
- 備份證據：`evidence/backup-proof.json`；工具以 `--validate-backup-proof-only` 驗證通過。
- 首次正式 run：`release-staging/etch-month-20260918/run-20260922-110958/`。
- 第二次重跑：`release-staging/etch-month-20260918/run-20260922-111221/`。
- 完整證據：`evidence/reconciliation.json`、`release-staging/etch-production-20260922/database-after.json`、`charts.json`。
- 首次重跑時發現 preflight 預期鍵缺少 `ETCH:` 前綴，守衛在寫入前中止；補回歸測試並修正後，49 項測試通過，再重跑 48 份皆 Unchanged。
- 本次沒有發布正式 API／Web；資料可由既有正式 API 圖表查詢，16 項圖表 API 均已驗證。

## 基準與變更紀錄更新位置
- [需求索引](../../docs/requirements.md)
- [客製變更紀錄](../../CHANGELOG_CUSTOM.md)
- [開發目的紀錄](../../ai_docs/10_change_log.md)
