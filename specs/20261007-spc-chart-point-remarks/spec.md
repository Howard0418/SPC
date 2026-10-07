# SPC 管制圖量測點備註

功能 ID：SPC-CHART-POINT-REMARKS-20261007  
狀態：規劃完成，待執行  
涉及專案：SPC  
授權依據：使用者要求「SPC 管制圖，可以在每個點點右鍵時出現選單，可增加備註，對每個量測點希望都可以增加備註」。

## Goal

讓使用者在 SPC 管制圖的每個量測點右鍵新增或修改備註，保留點位說明、異常觀察或現場判斷依據，且不影響原始量測值與 SPC 計算。

## Scope

- SPC 管制圖點位右鍵選單新增「新增/編輯備註」。
- 每個量測點可保存一筆目前備註與異動資訊。
- 點位 tooltip、點位明細或清單需可查看備註。
- 備註需支援 VariableMeasurement、AttributeMeasurement 與 Xbar 子組點位的穩定定位策略。
- 建立 API 與必要資料模型保存備註。

## Out of Scope

- 不改量測值本身。
- 不改 CL/UCL/LCL、OOC/OOS、Cpk 或八大規則計算。
- 不取代既有點位排除/隱藏功能。
- 不做附件上傳或圖片備註。
- 不發布正式站。

## Current Behavior

- 管制圖與趨勢圖已有點位右鍵排除/隱藏功能，使用 `SpcPointExclusion` 記錄排除狀態與可選備註。
- 目前若使用者只想對量測點增加說明但不排除該點，沒有獨立點位備註功能。
- 原始量測資料可能有批次或量測列備註，但圖上單點沒有一致的備註入口。

## Expected Behavior

- 使用者在 SPC 管制圖點位右鍵時，可選擇新增或編輯備註。
- 備註保存後，重新查詢同一管制圖仍可看到該點備註。
- 備註不改變該點是否列入計算。
- 備註異動需記錄修改人與修改時間。
- 對同一點再次修改備註時，至少保留最新備註；是否保留歷史需在實作前確認，建議第一版保留異動時間與操作者即可。

## Business Rules

- 點位備註是資料註解，不是 SPC 排除狀態。
- 備註不得讓點位從計算中消失，也不得改變 OOC/OOS 判定。
- 一般使用者是否可新增備註需沿用 SPC 圖表/主檔編輯權限；若現行角色不足，先以「有 SPC 編輯權限者」為第一版。
- 備註文字需限制長度，建議 500 字以內，保留換行。

## Technical Impact

- 後端可能新增 `SpcPointRemarks` entity/API，或擴充既有點位狀態服務但不可與排除狀態混淆。
- `SpcService.GetInteractiveChartAsync` 需把點位備註帶回 chart point DTO。
- 前端管制圖右鍵選單需新增備註操作與 modal/inline editor。
- 若趨勢圖也共用點位 DTO，可列入後續延伸；本 Task 先以 SPC 管制圖為主。

## API Impact

- 可能新增：
  - `GET /api/v1/spc/point-remarks`
  - `PUT /api/v1/spc/point-remarks`
  - `DELETE /api/v1/spc/point-remarks/{id}` 或清空備註
- API 需接受穩定點位識別：VariableMeasurementId、AttributeMeasurementId 或 Subgroup PointKey。
- 未授權需回 401/403；找不到點位需回 404 或 400。

## Database Impact

- 可能新增 `SpcPointRemarks` 表。
- 建議欄位：PointScope、VariableMeasurementId、AttributeMeasurementId、PartProcessCharacteristicId、PointKey、Remark、UpdatedBy、UpdatedAt、CreatedBy、CreatedAt、IsDeleted。
- 需建立同一 active point 唯一索引，避免同一點多筆目前備註造成畫面混亂。

## UI Impact

- SPC 管制圖點位右鍵選單新增「新增備註」或「編輯備註」。
- 已有備註的點位需在 tooltip 或點位明細顯示備註。
- 備註儲存成功後不重新計算也可更新畫面；若重新查詢，備註仍存在。
- 不應與既有「顯示但不列入計算」、「隱藏且不列入計算」、「恢復列入計算」混淆。

## Acceptance Criteria

- AC-001：在 SPC 管制圖任一可定位量測點右鍵，可新增備註。
- AC-002：新增備註後重新查詢同一圖，該點仍顯示備註。
- AC-003：編輯或清空備註後，畫面與 API 回傳一致。
- AC-004：新增/編輯備註不改變 SPC 計算、OOC/OOS、Cpk 與點位排除狀態。
- AC-005：未登入或無權限者不可新增/修改/刪除備註。
- AC-006：VariableMeasurement、AttributeMeasurement、Xbar 子組點位至少要有明確支援範圍與測試；若第一版不支援某類點位，右鍵選單需禁用並顯示原因。
- AC-007：完成後更新 `TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md` 與驗證紀錄，測試站發布狀態明確。

## BDD

### Scenario: 對量測點新增備註

Given 使用者在 SPC 管制圖看到某一個量測點  
When 使用者右鍵該點並輸入備註  
Then 系統必須保存備註，且重新查詢同一圖時可看到該備註

### Scenario: 備註不影響 SPC 計算

Given 管制圖已有 CL/UCL/LCL、OOC/OOS 與 Cpk 結果  
When 使用者只對某量測點新增備註  
Then 系統不得因備註改變管制界線、異常判定或能力指標

### Scenario: 未授權不可修改備註

Given 使用者未登入或沒有 SPC 編輯權限  
When 使用者呼叫點位備註 API  
Then 系統必須拒絕並不寫入備註

## Test Plan

- Happy Path：右鍵新增備註、重新查詢仍顯示。
- Boundary Case：備註清空、長文字、換行、多種點位 scope。
- Invalid Input：找不到點位、非本 PPC 點位、未授權、超過長度。
- Regression Risk：點位排除/隱藏功能不可壞；SPC 計算結果不可因備註改變。

## Risks

- Xbar 子組點位不是單一 raw measurement，需使用穩定 PointKey 才能正確掛備註。
- 點位備註若與排除備註混在同一 UI，使用者可能誤以為新增備註會排除資料。
- 備註若未帶回 chart DTO，畫面可能保存成功但查圖看不到。
