# 技術計畫
- 功能 ID：20260922-etch-production-import
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- `tools/EtchTrialImport/`：沿用解析與嚴格匯入邏輯，但新增明確正式模式；測試模式守衛不得弱化。
- `release-staging/etch-month-20260918/input.json`：固定 48 份 manifest，只讀。
- `release-staging/etch-production-20260922/`：保存 dry-run、備份驗證、before/result/after/reconciliation 證據。
- Portal 正式庫 `PMR_PORTAL_UAT`：日報表頭與原始點。
- SPC 正式庫 `PMR_SPC_2026`：咬蝕主檔、匯入批次、量測。
- 應用程式 API／Web：預設不修改、不發布。

## 實作方式與需求對應
1. 將現有工具的環境選擇改為明確列舉：
   - 預設／`--environment=test`：只允許現有測試庫。
   - `--environment=production`：只允許 `PMR_PORTAL_UAT` 與 `PMR_SPC_2026`。
   - 正式 `--apply` 另要求 `--confirm-portal=PMR_PORTAL_UAT` 與 `--confirm-spc=PMR_SPC_2026`。
2. dry-run 在任何寫入前完成來源雜湊、48 鍵、點數、資料庫、既有資料及主檔差異驗證（R-001～R-003）。
3. 正式主檔計畫先輸出，不直接套用；只允許建立缺少的 `XBAR_S`、`ETCH_LINE_SPEED` 與 16 個每線 PPC，或將既有同一 PPC 調整成已驗證設定。若既有值不一致，停止並要求審查（R-005）。
4. apply 前由獨立腳本建立兩個正式庫 COPY_ONLY 備份，保存檔名、SHA256／SQL 驗證結果與 `RESTORE VERIFYONLY` 證據（R-004）。
5. Portal 每份日報使用 Serializable＋UPDLOCK/HOLDLOCK；同鍵存在時逐欄／逐點比對，不同即停止。
6. Portal 新增後狀態 Pending，再呼叫 SPC `EtchReportSyncService`；成功才更新 SUCCESS。既有服務以應用鎖與 SourceReference `ETCH:yyyy-MM-dd:line` 防重複（R-006、R-007）。
7. 每處理一份即寫入 result.json，避免程序中斷後無法判定進度。
8. 匯入後執行既有 reconciliation 邏輯，並加上正式庫非本批資料前後指紋比較（R-008～R-010）。

## API、檔案格式及資料模型
- 不新增外部 API。
- manifest 沿用：
  - `source`
  - `sha256`
  - `reports[]`：reportDate、lineCode、lineSpeed、operatorName、points[]
- Portal 業務鍵：ReportDate＋LineCode。
- SPC 業務鍵：SourceReference=`ETCH:{yyyy-MM-dd}:{LineCode}`＋PPC＋SampleNo。
- 稽核操作者使用新的正式匯入識別，例如 `EtchProductionImport:20260922`，不可冒用原量測人員。

## 相容性與風險
- 正式 Portal production 連線已確認為 `PMR_PORTAL_UAT`；名稱含 UAT 但本案視為正式，工具仍需雙重確認參數。
- 測試 SPC 現況缺少 48 筆 LINE_SPEED，不可用測試庫筆數作正式寫入來源；正式驗收以 Excel／manifest 與 3,746 筆為準。
- 正式 A/B 既有 PPC 多為 I-MR，與完整 25／50 點子組不相容；不得直接沿用錯誤圖型。
- Portal 與 SPC 不是分散式交易；以 Pending／嚴格重跑控制跨系統失敗。
- 正式站應用程式若未包含現行完整點同步程式，離線工具仍可直接使用現行程式碼，但正式 UI 查詢／圖表驗收前須確認 API 相容性。

## 驗證安排
- AC-001：SHA256、manifest 48 份、3,650 點、PT1/2/QE1/2=10/12/13/13。
- AC-002：連線守衛單元測試，錯誤庫名／伺服器／缺 confirm 都拒絕。
- AC-003：正式 dry-run；48 鍵均為新增，無單邊或異值衝突。
- AC-004：備份命令輸出與 VERIFYONLY 結果。
- AC-005：主檔 before/plan/after 差異；16 PPC 與圖型／樣本數。
- AC-006～AC-008：Portal／SPC 筆數、逐點、速率、線速、圖表及非本批指紋。
- AC-009：第二次執行 48 份 Unchanged。
- AC-010：隔離 8 鍵查詢為 0。

## 發布、備份及回復
- 規格／工具開發階段：不發布正式站。
- 正式資料執行前：
  - `PMR_PORTAL_UAT` COPY_ONLY＋CHECKSUM＋VERIFYONLY。
  - `PMR_SPC_2026` COPY_ONLY＋CHECKSUM＋VERIFYONLY。
- 回復優先使用 run 目錄的 before/result 與本批業務鍵定向清除新增資料及回復主檔；只有定向回復不可行時才評估整庫 restore。
- 若正式使用者已在匯入後修改相關日報，不得自動回復；先審查稽核與引用。
