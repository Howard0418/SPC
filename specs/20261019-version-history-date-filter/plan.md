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

## v2擴充實作計畫（待使用者確認）
1. 先建核心測試：PPC規格變更、分段新增／修改／刪除、批次EF匯入、純管制線變更不誤建規格版、未變不建版、唯一版號與交易失敗回復；再實作統一EF記錄邊界，避免只補單一畫面而漏記。
2. 新增SpecificationLimitVersions實體、DbSet、索引與EF Core migration（同步snapshot），記錄來源與不可變前後快照；伺服器取得actor，不信任客戶端填入操作者／時間。
3. 先確認migration diff僅新增歷史表與索引；在獨立測試DB演練Up、資料保留／交易測試及Down回復，不手動改表。測試庫實際連線目標核對後先做可驗證COPY_ONLY備份；無備份權限時使用授權維運流程，不提高應用帳號權限。
4. 完成規格只讀歷史API／UI與三類一致日期查詢，覆蓋AC-001～007、必要build／自動化／隔離瀏覽器驗收；成功後才套用測試庫migration並發布release/test。
5. 回復：先回復同批程式，新增歷史表保留；不自動刪歷史或整庫回復。演練Down只在隔離空間；若正式日後需要更新，再提供獨立正式DB備份與發布方案取得授權。
本次只完成具體提案；尚未新增實體、migration、API或執行DDL。正式環境不在此同意範圍內。

## 實際執行與差異（2026-10-20）
- 使用者已同意v2。本案完成：AppDbContext統一捕捉同步／非同步EF規格寫入，業務資料與快照同交易，伺服器actor、SQL同PPC更新鎖、過期規格拒絕；保留既有同步SaveChanges入口，不以同步等待包裝async。沒有使用SaveChanges(false)的規格路徑，該路徑明確拒絕以維持二階段快照原子性。
- 真實SQL查詢發現舊F表歷史migration缺少識別標記、測試庫缺表；補正註冊並使用原EF migration SQL，不手動建表。隔離演練兩類Up／Down／重跑，原規格與F值不變。
- 測試庫更新前再次COPY_ONLY/CHECKSUM＋VERIFYONLY與IIS／檔案備份，兩筆DDL在同一交易套用，逐檔／設定hash及三類HTTP驗證通過。實際IIS SpcSingleTest／release/test，API0.1.62、Web0.1.65。
- 正式仍需另授權並核對F歷史實表與舊migration紀錄；不能直接把測試包拿到正式啟動Migrate。備份、已知基準失敗與回復細節見[驗證](verification.md)。
