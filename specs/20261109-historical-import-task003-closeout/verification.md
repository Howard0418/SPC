# 驗證：SPC-HISTORICAL-IMPORT-1102-TASK-003

日期：2026-11-09

| 驗收項目 | 權威證據 | 判定 |
|---|---|---|
| 正式目標與環境 | SQL `172.16.110.16／PMR_SPC_2026`、正式API `production` health/version guard | 通過 |
| 來源SHA與日期／線別 | 咬蝕134份及後續166份清冊、824份藥液逐檔SHA；日期／線別／原值可追溯 | 通過 |
| 主檔相容性 | 原65份「缺主檔」更正為合法空機台對應；完整專屬優先、唯一空機台組才允許，其他拒絕 | 通過 |
| 防重與衝突 | 已存在同內容略過；同鍵不同內容、軟刪除來源鍵、缺時間／班別及來源異常停止 | 通過 |
| 備份與還原驗證 | 多次`COPY_ONLY,CHECKSUM`及`RESTORE VERIFYONLY`成功；失敗交易回復 | 通過 |
| 回復門檻 | 依本批batch／ID或before證據受控還原；禁止直接整庫覆蓋後續寫入 | 通過 |
| 修正清單 | 藥液待補／已存在／來源異常、咬蝕異常／X排除／主檔差異均有清單與建議 | 通過 |

主要證據：

- `specs/20261102-production-import-insert-only/verification.md`
- `specs/20261103-etch-missing-line-reconciliation/verification.md`
- `specs/20261105-production-align-test-excel/verification.md`

## 結論

TASK-003 的 preflight、備份及回復門檻已完整執行，可標完成。不合格資料被拒絕是驗收通過，不是要求繞過阻擋；缺時間藥液、來源異常及TASK-004仍為待辦。

本核銷未執行程式、資料庫或網站變更；build、發布及資料rollback不適用。中央進度表已將本件「目前待辦」改為否；核心未完成39降為38、SPC相關16降為15、完成200增為201，並新增2026-11-09原生核銷里程碑。更新前備份與原檔雜湊一致；候選ZIP可讀、OpenXML SDK 0錯誤、artifact-tool重新開啟／渲染及公式錯誤搜尋通過，取代後雜湊與候選一致。Microsoft Excel人工開啟未執行。
