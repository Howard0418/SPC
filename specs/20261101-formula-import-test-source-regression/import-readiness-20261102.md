# 藥液資料／公式實際匯入入口核對

- 日期：2026-11-02；既有 FORMULA-IMPORT-005 補充證據，不新增工作或變更既有進度狀態。
- 授權：使用者要求可正常匯入才執行測試匯入，否則說明原因；正式區及既有正常功能保持。
- 範圍：TransFiles 原始碼、既有測試及設定來源的代表檔唯讀檢查。沒有產品修正、API 呼叫、資料庫寫入、EXE 打包或網站發布。

## 驗收與方式

- Given 實際矩陣日報，When 使用日期公式 DB 入口的 load_rows，Then 必須可讀取並通過契約才能預覽／寫入；失敗不得改走僅濃度的舊匯入。
- Happy Path：執行既有 test_chemical_direct_writer、test_chemical_daily_parser。
- Boundary／Invalid：既有測試覆蓋 0、空值、無效日期、NaN、時區及錯環境。
- Regression：來源 SHA-256 前後一致；不改公式、F、排程、主檔或任何量測。

## 本次實際結果

1. 兩個測試模組共 17 項通過；不等同實際端到端匯入成功。
2. 設定 chemical_source 的 2026-10-27.xlsx，現行 chemical_daily_parser 解析 73 筆，全部 ready_for_preview；此標記僅是解析成功。與前次111筆紀錄不同，本次以目前程式／目前來源觀察為準，不回寫歷史結果。
3. 真正入口 chemical_direct_writer.load_rows 在 `with openpyxl.load_workbook(...)` 發生 `TypeError: 'Workbook' object does not support the context manager protocol`，在預覽／寫入前失敗。既有17測試未涵蓋該實檔讀取。
4. 另以顯式 close 的唯讀方式模擬同一讀取迴圈，直接 schema 合格0列、錯誤232列，首個錯誤CN第2列缺PpcId。232包含表頭／輔助內容，不是232筆量測錯誤。
5. 矩陣日報與直接匯入要求的首列表頭 PpcId、MeasuredAt、Primary、SamplingPhase、SamplingStage 不同；原始日期00:00無時區也不能冒充實際量測時間。
6. preview_rows／confirm_rows 僅呼叫既有公式排程 preview／prepare 及受控 SQL；未包含將來源 Excel 公式／F 建立成可用版本與排程的寫入步驟。本機版本事件記錄不代表伺服器公式已匯入。
7. 代表列CN第7列保留原始2.4、公式 `=IF(H7="","",24.5*H7*F!$B$3)`、F=0.99、結果約58.212；讀取前後來源SHA-256一致。

## 結論與格式建議

現有工具尚不能直接把來源日報的資料、公式與F完整匯入；本次沒有確認匯入。應先修復實檔讀取並補整合測試，將矩陣parser接到受控匯入契約，保留來源對照及初複驗，再核定測試專用公式／F／適用日期並建立排程。不可只新增表頭就宣稱公式已可匯入，也不猜班別、時間、F或業務生效日。

若人工準備原始值格式，至少需第一列含PpcId、MeasuredAt（含+08:00）、Primary、SamplingPhase、SamplingStage；必要時加Secondary、RecheckPrimary、RecheckSecondary。此格式仍依賴測試項目已有有效公式排程。

本次為既有工作唯讀能力核對補充文件，不變更工作狀態／日期／里程碑；進度表更新不適用。網站建置／發布不適用；實際測試匯入及圖表查回未執行。不得將本紀錄標為匯入完成。
