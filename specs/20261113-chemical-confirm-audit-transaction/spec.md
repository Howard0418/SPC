# 藥液量測確認與稽核交易相容

- 功能 ID：`SPC-CHEM-CONFIRM-AUDIT-TX-1113-TASK-001`
- 主系統：SPC API
- 相依系統：Portal（呼叫既有確認 API）；SPC Web、TransFiles 不變。
- 資料庫影響：無 schema migration；不修改既有量測。
- 發布影響：SPC 測試 API；正式站未授權。
- Rollback：回復本次稽核交易協調程式與測試 API 版本。
- 狀態：執行中。

## 現況與原因

2026-11-13 14:44 測試站 `/api/v1/chemical-portal-measurements/confirm` 由 `BusinessDataAuditFilter` 開啟外層交易後，`ChemicalPortalMeasurementService.ConfirmAsync` 再開啟 Serializable 交易，回傳500且整批未儲存。前一件 PPC 主檔修正不涵蓋此由服務自行管理完整交易及提交後通知的流程。

## Requirement

- R-001：藥液確認仍由原服務擁有交易，稽核不得在外層再建立交易。
- R-002：量測、Receipt、Trace、統計、稽核事件必須在同一交易同成同敗。
- R-003：既有提交後通知只可在交易成功後執行；失敗不得寄送通知。
- R-004：不得改動公式、F、主檔、現有量測、Portal UI或資料庫結構。

## BDD 驗收

- Given 業務稽核已啟用且使用者確認一批藥液量測
- When 確認服務建立並提交其完整交易
- Then 稽核不另開外層交易，量測、Receipt、Trace、統計與稽核事件一起保存，通知只在提交後執行。

- Given 稽核事件無法寫入或量測確認任一步驟失敗
- When 交易回滾
- Then 不留下部分量測、Receipt、Trace或成功稽核事件，也不寄送通知。

## 測試規劃

- Happy Path：SQLite關聯式整合測試經BusinessDataAuditFilter呼叫真實Confirm。
- Boundary：同requestId冪等確認仍保存一次稽核並正常回應。
- Invalid Input：既有預覽過期與批次驗證保持回滾。
- Regression Risk：提交後通知、稽核fail-closed、非藥液寫入交易。
