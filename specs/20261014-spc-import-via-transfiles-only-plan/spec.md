# SPC-IMPORT-VIA-TRANSFILES-ONLY-20261014

## 狀態

已完成。SPC 已改為支援 TransFiles 直接匯入後的批次查詢、錯誤追蹤、新資料提示與製圖確認；SPC 網站不再引導 Excel 人工上傳匯入。

## 需求

使用者確認 SPC 網站後續不從網頁進行資料匯入，資料匯入改由 TransFiles 轉檔工具直接匯入資料庫。SPC 網站保留資料查詢、匯入批次檢視、錯誤追蹤、主檔維護與製圖確認能力。

## 新匯入責任分工

- TransFiles：負責讀取來源 Excel、依指定格式解析、預檢、主檔對應檢查、錯誤報告、直接寫入測試/正式資料庫，以及產生匯入批次紀錄。
- SPC API：負責提供主檔查詢/驗證所需 API、匯入批次/錯誤查詢 API、SPC 計算與圖表資料。不再以 SPC 網頁作為人工上傳 Excel 的主要入口。
- SPC Web：負責查看匯入狀態、新資料提示、錯誤報告、製圖與資料確認；隱藏或停用「從網頁上傳匯入」操作。

## 格式不符時的建議

TransFiles 需要在匯入前就輸出錯誤報告，至少包含：

- 檔案完整路徑。
- 工作表名稱。
- Excel 列號。
- 欄位名稱。
- 原始值。
- 錯誤代碼。
- 錯誤訊息。
- 修正建議。
- 是否已寫入資料庫：格式錯誤或主檔對應錯誤時不可寫入該筆資料。

藥液資料建議格式：

- `類別` 固定為 `藥液`。
- `線別` 必須對應 SPC 主檔線別代碼或唯一名稱。
- `槽位` 必須對應該線別底下的 SPC 槽位，括號內容需保留。
- `管制項目` 必須對應 SPC 品質特性或管制項目。
- `單位` 必須與 SPC 管制項目主檔一致；無單位項目請維持空白。
- `量測日期` 與 `量測時間` 必須合併成完整量測時間。
- `SamplingPhase` 僅允許 `OPEN`、`MIDDLE`、`CLOSE`、`GENERAL`。

咬蝕量與落塵監控需沿用已確認要求：若資料無法匯入，錯誤報告必須指出哪個檔案、哪個工作表、哪一筆資料沒有匯入。

## BDD 驗收

### Scenario: SPC 網站不再提供 Excel 人工匯入

Given 使用者進入 SPC 資料匯入頁  
When 網站已切換為 TransFiles 匯入模式  
Then 使用者不應看到可從 SPC 網頁上傳 Excel 並確認匯入的主要操作  
And 仍可查看 TransFiles 匯入批次、成功/失敗筆數與錯誤明細。

### Scenario: TransFiles 匯入失敗可回 SPC 網站查詢

Given TransFiles 匯入藥液、咬蝕量或落塵資料時發現格式或主檔對應錯誤  
When 使用者在 SPC 網站查看匯入批次  
Then 系統應顯示來源檔案、工作表、列號、欄位、原始值、錯誤代碼與修正建議。

### Scenario: 新資料匯入後可確認製圖

Given TransFiles 成功直接寫入 SPC 測試資料庫  
When 使用者進入 SPC 管制圖或首頁  
Then 系統應提示有新資料匯入  
And 使用者可依批次或管制項目進入圖表確認。

## 舊小工作調整

原 `SPC-CHEM-OCT-IMPORT-TASK-002`～`TASK-005` 不照原樣執行，改由本規格的新小工作承接：

- 日期時間解析改由 TransFiles 直接匯入資料前完成。
- 主檔對應差異建議改為 TransFiles 預檢與 SPC 批次錯誤查詢共同呈現。
- 匯入頁操作直覺化改為「匯入批次/錯誤查詢與新資料提示」。
- 自動化測試改為涵蓋 TransFiles 直接匯入、SPC 查詢與製圖確認。

## 小工作排序

### SPC-TRANSFILES-IMPORT-TASK-001：定義 TransFiles 直接匯入資料契約與錯誤報告

- 優先：第一順位，資料正確性與可回復。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：先盤點既有 UploadBatches/UploadDetails/UploadErrors 與量測表欄位是否足夠，不先新增 schema。
- 發布影響：文件與可能的 API 契約；本 Task 先不發布。
- Rollback：保留舊網頁匯入入口，契約文件可回退。
- 驗收：文件明確定義 TransFiles 寫入欄位、批次狀態、錯誤欄位、日期時間、主檔對應與重複資料規則。
- 狀態：完成。契約文件：`docs/imports/transfiles-direct-import-contract.md`。

### SPC-TRANSFILES-IMPORT-TASK-002：SPC API 提供批次/錯誤查詢與主檔驗證支援

- 優先：第二順位，阻擋 TransFiles 匯入錯誤可視化。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：優先使用既有資料表；若不足，另開 migration 小工作，不與本 Task 併行。
- 發布影響：後端 API；測試通過後發布 SPC 測試站。
- Rollback：還原新增 API，不刪資料。
- 驗收：可依批次查詢成功/失敗筆數與每筆錯誤的檔案、工作表、列號、欄位、原始值、錯誤代碼與建議。
- 狀態：完成。新增 `GET /api/uploads` 與 `GET /api/uploads/recent-imports`，既有預覽錯誤報告補 `rawValue`。

### SPC-TRANSFILES-IMPORT-TASK-003：SPC Web 隱藏網頁上傳入口，改為批次查詢/錯誤追蹤

- 優先：第三順位，使用者操作方向改變。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：無。
- 發布影響：前端；測試通過後發布 SPC 測試站。
- Rollback：還原資料匯入頁原入口。
- 驗收：SPC 網頁不再引導使用者上傳 Excel 匯入，改顯示 TransFiles 匯入狀態、錯誤報告與修正建議。
- 狀態：完成。`/uploads` 改為匯入批次與錯誤追蹤頁。

### SPC-TRANSFILES-IMPORT-TASK-004：新資料匯入提示與製圖確認入口

- 優先：第四順位，製圖確認與高頻操作。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：不新增 schema；以批次與量測資料查詢為主。
- 發布影響：後端/前端；測試通過後發布 SPC 測試站。
- Rollback：還原提示 UI 與查詢端點。
- 驗收：TransFiles 匯入成功後，SPC 首頁/管制圖入口能提示新資料，並可依批次進入圖表確認。
- 狀態：完成。首頁讀取近期已匯入批次並顯示新資料提示。

### SPC-TRANSFILES-IMPORT-TASK-005：端到端測試與操作說明

- 優先：第五順位，回歸防護與上線使用。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：測試資料庫或隔離批次，不碰正式資料。
- 發布影響：文件；必要時跟隨前述 Task 發布。
- Rollback：移除測試 fixture 或恢復文件。
- 驗收：完成藥液、咬蝕量、落塵至少各一個 Happy Path 與 Invalid Input 測試；文件說明如何確認資料已匯入與製圖是否正確。
- 狀態：完成。自動化涵蓋批次清單、近期已匯入與錯誤 raw value；操作說明納入契約文件。
