# SPC-CHEM-OCTOBER-IMPORT-FAILURE-20261014

## 狀態

逐項執行中。`SPC-CHEM-OCT-IMPORT-TASK-001` 已完成並發布 SPC 測試站；其餘小工作待執行。

## 來源

- 使用者指定檔案：`D:\SPC\docs\SPC文件\批次轉換結果_藥液匯入檔_2026-10.xlsx`
- 工作表：`工作表1`
- 資料量：第 1 列為欄位，第 2～677 列為資料，共 676 筆。
- 期間：全部為 `202610`，日期分布為 20261001、20261002、20261005、20261006、20261007、20261008、20261011、20261012、20261013、20261014。

## 初步分析

檔案本體沒有看到明顯必填空白：

- `量測日期`、`量測時間`、`類別`、`線別`、`槽位`、`管制項目`、`量測值`、`SamplingPhase` 皆為 0 筆空白。
- `量測值` 皆有值，未發現空白值。
- 以 `量測日期 + 線別 + 槽位 + 管制項目 + 單位 + SamplingPhase + SamplingStage(GENERAL)` 檢查，未發現檔內重複鍵。
- SamplingPhase 分布：`GENERAL` 527 筆、`OPEN` 81 筆、`CLOSE` 68 筆。
- 線別分布包含 N2、QE2、PT1、DV2、DP、C1、QE1、PT2、DV1、C3、C5、ST2、ST1、N1、CN。

既有後端 Excel 匯入流程會以中文欄名 alias 轉成內部欄位：

- `類別` → `ControlScope`
- `線別` → `MachineCode`
- `槽位` → `TankCode`
- `管制項目` → `CharacteristicCode`
- `量測值` → `MeasuredValue`
- `量測日期` → `MeasuredAt`
- `量測員` → `Operator`
- `單位` → `Unit`
- `規格` / `範圍` → 規格解析輔助欄位

因此本檔較不像是「欄位缺少」造成無法匯入；較可能落在下列類型：

1. 主檔對應失敗：`線別 + 槽位 + 管制項目 + 單位` 在 SPC 主檔找不到唯一啟用的管制項目。
2. 單位不一致：後端已找到線別、槽位、品質特性，但 Excel `單位` 與主檔單位不同，會回 `UNIT_MISMATCH`。
3. 槽位名稱不一致：例如括號、全半形、舊名稱或相似槽位，會回 `TANK_NOT_FOUND` 並提供候選槽位。
4. 檔案重複上傳：同一 Excel MD5 已存在時，API 會回 `409 檔案已重複上傳`。
5. 日報模式與一般檔案模式不一致：若要視為 PortalDaily 日報，需確認上傳端有帶 `portalDaily=true`；否則資料會以一般 File 模式判斷重複鍵與日期時間。
6. 量測時間未併入 `MeasuredAt`：現行 Excel 上傳邏輯只以 `量測日期` 作為 `MeasuredAt`，`量測時間` 未併入。PortalDaily 模式通常可接受，但一般 File 模式會使同日同項目資料都落在 `00:00:00`，容易造成後續重複或查詢口徑混淆。

## 格式建議

若來源檔格式不符，建議調整為可被 SPC 明確解析的統一欄位：

- 保留目前 21 欄欄名，尤其 `類別`、`線別`、`槽位`、`管制項目`、`量測值`、`單位`、`SamplingPhase` 不可改名。
- `量測日期` 建議使用 `yyyyMMdd` 或 `yyyy-MM-dd`；若需要保留時間，匯入程式應將 `量測日期 + 量測時間` 合併成 `yyyy-MM-dd HH:mm:ss`。
- `類別` 固定填 `藥液`。
- `線別` 需對應 SPC 主檔線別代碼或唯一可辨識名稱，例如 `N2`、`QE2`。
- `槽位` 需與 SPC 主檔槽位名稱一致；括號內容需保留，例如 `蝕刻(鎳)` 不可截斷。
- `管制項目` 需對應品質特性或既有管制項目名稱。
- `單位` 建議與 SPC 管制項目主檔完全一致；若 pH 等無單位項目主檔為空白，Excel 也應維持空白。
- `SamplingPhase` 建議只填 `OPEN`、`MIDDLE`、`CLOSE`、`GENERAL`；N1/N2 開/收線若要區分取樣階段，後續需另補 `SamplingStage` 欄位。

## BDD 驗收

### Scenario: 匯入預檢列出 10 月藥液失敗原因

Given 使用者上傳 2026-10 藥液匯入檔  
When SPC 建立匯入預覽  
Then 系統應列出總筆數、可匯入筆數、錯誤筆數  
And 每筆錯誤需包含檔案、工作表、Excel 列號、欄位、錯誤代碼與修正建議。

### Scenario: 主檔對應問題可讀化

Given Excel 的線別、槽位、管制項目或單位與 SPC 主檔不一致  
When 使用者查看匯入預覽錯誤  
Then 系統應顯示是線別不存在、槽位不存在、品質特性不存在、單位不一致、未建立管制項目或對應到多筆設定。

### Scenario: 日期時間合併

Given Excel 同時提供 `量測日期` 與 `量測時間`  
When SPC 解析藥液匯入檔  
Then 系統應可將兩欄合併為完整 `MeasuredAt`，並保留 PortalDaily 日期口徑。

## 小工作排序

### SPC-CHEM-OCT-IMPORT-TASK-001：建立 10 月藥液匯入預檢報告

- 優先：第一順位，阻擋測試匯入與資料正確性。
- 狀態：完成。
- 主系統：SPC。
- 相依系統：TransFiles 產出的 Excel 格式。
- 資料庫影響：讀取 UploadBatches/UploadDetails/UploadErrors 與主檔；不新增 schema。
- 發布影響：後端 API 與前端匯入頁；測試通過後發布 SPC 測試站。
- Rollback：還原匯入預檢 API/UI 變更，不影響既有資料。
- 驗收：上傳本檔後，畫面可看到每筆失敗的檔名、工作表、列號、欄位、錯誤代碼與建議。
- 完成紀錄：後端預覽 API 會回傳 `sourceFile`、`sourceSheet`、`sourceRow` 與 `suggestion`；Excel 上傳解析會將工作表與原始 Excel 列號寫入暫存 payload；前端匯入預覽表格新增來源欄與錯誤代碼/建議。

### SPC-CHEM-OCT-IMPORT-TASK-002：修正藥液 Excel 日期時間解析

- 優先：第二順位，資料時間正確性。
- 主系統：SPC。
- 相依系統：TransFiles。
- 資料庫影響：不新增 schema；僅影響新匯入批次的 `MeasuredAt`。
- 發布影響：後端 API；測試通過後發布 SPC 測試站。
- Rollback：還原解析邏輯；已匯入資料若時間錯誤需依批次刪除重匯。
- 驗收：`量測日期=20261001`、`量測時間=09:10:00` 解析為 `2026-10-01 09:10:00`。

### SPC-CHEM-OCT-IMPORT-TASK-003：補強主檔對應差異建議

- 優先：第三順位，降低人工查錯時間。
- 主系統：SPC。
- 相依系統：無。
- 資料庫影響：唯讀查詢主檔；不新增 schema。
- 發布影響：後端 API 與前端錯誤顯示；測試通過後發布 SPC 測試站。
- Rollback：還原建議訊息；不影響匯入資料。
- 驗收：單位不一致、槽位不存在、管制項目不存在、管制項目重複時，都能顯示下一步修正方式。

### SPC-CHEM-OCT-IMPORT-TASK-004：匯入頁操作直覺化與新資料提示

- 優先：第四順位，使用者高頻操作。
- 主系統：SPC。
- 相依系統：無。
- 資料庫影響：不新增 schema。
- 發布影響：前端匯入/批次頁；測試通過後發布 SPC 測試站。
- Rollback：還原前端顯示；不影響匯入資料。
- 驗收：匯入後在匯入批次頁與 SPC 首頁/管制圖入口顯示「有新資料匯入」摘要，包含批次、來源、筆數與時間。

### SPC-CHEM-OCT-IMPORT-TASK-005：補齊自動化測試與範例資料

- 優先：第五順位，回歸防護。
- 主系統：SPC。
- 相依系統：TransFiles 範例輸出。
- 資料庫影響：測試資料庫或 in-memory 測試；不影響正式資料。
- 發布影響：無直接使用者功能；完成後配合前述任務發布。
- Rollback：移除測試與 fixture。
- 驗收：涵蓋 Happy Path、Boundary Case、Invalid Input、Regression Risk。
