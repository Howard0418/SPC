# 技術計畫
- 功能 ID：SPC-VERSION-HISTORY-DATE-FILTER-20261019；版本1；[規格](spec.md)。

## 系統責任與實作順序
1. 前案公式共用歷史完成後，先核對規格界線歷史實際入口、來源與修改時間欄位。記錄ai_docs/10_change_log.md目的，核心邏輯先建測試；未確定稽核來源前不實作規格查詢。
2. 藥液公式版本Controller／F表SpcController及ChemicalFTableService，與實際規格歷史API共用日期語意。規劃可選fromDate／toDate（ISO日期），保留既有呼叫相容性；先WHERE後ORDER／分頁，回應格式依既有契約維持或版本化擴充。
3. PartProcessCharacteristicsView、ChemicalAnalysisOverviewView、ChemicalFTableView與核實後規格歷史介面補日期／查詢／清除／空與錯誤狀態，兩公式入口共用查詢契約。

## 驗證
Happy Path：三類各區間查詢、清除及兩公式入口一致（AC-001／005）。Boundary：單日、單邊、空白、月年界、時區日界與多於20筆（AC-002／004）。Invalid Input：反向區間、無效ISO日、錯項目與無權限（AC-003／005）。Regression：原無參數查詢、分頁、修改／回復、計算與歷史資料不變。
執行日期篩選服務／Controller自動測試、前端行為測試、受影響backend/frontend build及三類人工驗收；未執行不得記通過。

## DB、發布及回復
預期既有版本欄位篩選無schema變更；規格稽核來源未核實，migration不視為已授權。主系統SPC，其他系統不動，不與DB／IIS高風險工作並行。
本次只有規劃，build／發布不適用。未來必要驗證通過後發布release/test、核對真實測試路徑及備份，禁止D:/Sites/PmrPortal；正式另行授權。失敗回復同批前後端產物，唯讀篩選不需整庫還原；若需schema先另補計畫與回復策略。
