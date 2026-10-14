# TransFiles 直接匯入 SPC 契約

版本：2026-10-14

## 目的

SPC 網站不再作為 Excel 人工上傳匯入入口。TransFiles 轉檔工具負責解析來源檔、預檢、直接匯入資料庫；SPC API/Web 負責批次查詢、錯誤追蹤、新資料提示與製圖確認。

## 寫入批次

TransFiles 每次匯入需建立或等效寫入 `UploadBatches`：

- `UploadType`：`Variable`、`Attribute` 或後續約定值。
- `SourceType`：建議使用 `TransFiles`，若為日報資料可使用 `PortalDaily` 但需在錯誤報告保留來源檔案。
- `ImportStatus`：預檢完成未寫入為 `PreviewReady`；成功寫入為 `Imported`；失敗或部分失敗需保留 `ErrorRows`。
- `OriginalFileName`：來源檔名或完整可識別名稱。
- `FileHash`：來源檔雜湊，避免重複匯入。
- `TotalRows`、`ValidRows`、`ErrorRows`：必須可對帳。
- `ConfirmedAt`：完成寫入時間。

## 錯誤報告

格式或主檔對應錯誤不可寫入該筆資料，並需建立 `UploadDetails`/`UploadErrors` 或等效 API 可查結果。每筆錯誤至少包含：

- 檔案完整路徑或檔名。
- 工作表名稱。
- Excel 列號。
- 欄位名稱。
- 原始值。
- 錯誤代碼。
- 錯誤訊息。
- 修正建議。

建議錯誤代碼：

- `INVALID_FORMAT`：欄位格式錯誤。
- `INVALID_MEASURED_AT`：日期時間無法解析。
- `MACHINE_NOT_FOUND`：線別不存在。
- `TANK_NOT_FOUND`：槽位不存在或不屬於該線別。
- `CHAR_NOT_FOUND`：管制項目/品質特性不存在。
- `UNIT_MISMATCH`：Excel 單位與主檔單位不一致。
- `MAPPING_NOT_FOUND`：未建立線別、槽位、管制項目與單位的 SPC 管制項目。
- `MAPPING_AMBIGUOUS`：對應到多筆啟用設定。
- `DUPLICATE_DATA`：重複資料。

## 藥液資料

- `類別` 固定為 `藥液`。
- `線別` 對應 SPC 主檔線別代碼或唯一名稱。
- `槽位` 對應該線別底下槽位，括號內容需保留。
- `管制項目` 對應品質特性或管制項目。
- `單位` 與 SPC 管制項目主檔一致；無單位項目維持空白。
- `量測日期` 與 `量測時間` 合併成完整 `MeasuredAt`。
- `SamplingPhase` 僅允許 `OPEN`、`MIDDLE`、`CLOSE`、`GENERAL`。

## 咬蝕量與落塵

咬蝕量與落塵監控若資料無法匯入，錯誤報告必須指出來源檔、工作表與未匯入的列/點位。咬蝕量 `X-` 線別維持既有規則：不產生 SPC 匯入資料，並列入排除清單。

## SPC 網站確認方式

1. 到「匯入批次查詢」確認批次狀態、總筆數、成功筆數與錯誤筆數。
2. 點進批次預覽確認錯誤列的來源檔、工作表、列號、欄位、錯誤代碼與修正建議。
3. 成功匯入後從首頁新資料提示或批次頁進入 SPC 管制圖。
4. 依線別、槽位、管制項目、日期區間確認圖表點數與來源檔筆數一致。

## 回復

- 未寫入資料：修正來源檔或主檔後重新由 TransFiles 匯入。
- 已寫入錯誤批次：依 `UploadBatchId` 查出相關量測資料，經授權後刪除該批次量測、SPC 計算與警報，再重匯。
- 正式資料回復需另案授權，不可由 SPC 網頁或 TransFiles 自動刪除正式資料。
