# PPC 公式儲存沿用稽核交易

- 功能 ID：`SPC-PPC-AUDIT-TX-1113-TASK-001`
- 主系統：SPC API
- 相依系統：SPC Web；Portal、TransFiles 不變。
- 資料庫影響：無 schema migration；僅修正既有更新交易的擁有權。
- 發布影響：測試站 API；正式站未授權。
- Rollback：回復本次控制器程式與測試站 API 版本。
- 狀態：測試站已發布；待使用者重新儲存並確認畫面，正式站未授權。

## 現況與原因

啟用 `BusinessDataAuditFilter` 時，篩選器已先建立 Serializable 交易；`PartProcessCharacteristicsController.Update` 又建立第二個交易，造成 `The connection is already in a transaction`，公式與版本歷史均未儲存。

## Requirement

- R-001：PPC 更新若已有外層交易，必須沿用該交易；只有沒有交易時才自行建立及提交。
- R-002：公式設定、公式版本歷史與稽核事件仍須同成同敗。
- R-003：不得改動公式內容、正式資料、權限或資料庫結構。

## BDD 驗收

- Given 稽核已為 PPC 更新建立交易
- When 編輯者儲存藥液分析公式
- Then 更新不得建立巢狀交易，PPC 公式、版本歷史與稽核事件在同一交易成功儲存。

- Given 更新 API 沒有外層稽核交易
- When 儲存 PPC
- Then API 仍自行建立交易並維持原有原子性。

## 測試規劃

- Happy Path：SQLite 關聯式測試在稽核交易內更新藥液公式。
- Boundary：無外層交易的既有維護測試保持通過。
- Invalid Input：既有驗證與錯誤處理不變。
- Regression Risk：公式版本歷史、稽核事件及非藥液 PPC 更新。
