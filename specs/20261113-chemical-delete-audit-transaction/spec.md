# 小型變更：藥液量測刪除沿用稽核交易
- 功能 ID：SPC-CHEM-DELETE-TRANSACTION-1113-TASK-001
- 版本：1
- 狀態：測試站完成，待登入實際刪除驗收
- 專案／授權依據／需求基準：SPC API；使用者要求修正刪除錯誤，且不得影響原有功能或再次發生同類錯誤。

## 問題、預期行為與範圍
- 現況：`BusinessDataAuditFilter` 已為刪除請求開啟交易，`ManualMeasurementsV1Controller.DeleteItem` 又嘗試開啟第二個交易，SQL Server 回傳「connection is already in a transaction」。
- 預期：刪除動作在外層稽核交易存在時沿用該交易；沒有外層交易時仍自行建立並提交交易。
- 範圍：只調整單筆 Portal 藥液日報刪除的交易擁有權；保留刪除資格、SPC 結果／警示清理、不可變刪除證據及業務稽核同成同敗。
- 非範圍：不刪除或修改既有資料、不修改公式／F／Receipt／Trace／schema／Portal UI；正式站不發布。
- Rollback：回復控制器、測試與 API 版本至前一提交，重新發布測試 API。

## 需求與驗收
- R-001：刪除端點不得在既有資料庫交易中再次建立交易。
- R-002：端點獨立呼叫時仍須維持原子刪除及提交。
- R-003：既有刪除限制、衍生資料清理與稽核證據不得改變。
- AC-001（Given／When／Then）：Given 業務稽核已開啟關聯式資料庫交易，When 刪除 Portal 藥液量測，Then 回傳成功，量測與既有衍生資料刪除，業務稽核同交易保存，且不發生巢狀交易錯誤。
- AC-002：Given 非 Portal 日報量測，When 呼叫刪除，Then 維持 Conflict 且資料不變。
- AC-003：既有手動量測控制器測試及業務稽核回歸通過。
- AC-004：測試站發布後 Web／資產／API health／version／實際 API 基底驗證通過。

## 計畫與任務
- 受影響檔案：`ManualMeasurementsV1Controller.cs`、關聯式整合測試、API 版本、規格／需求／變更與進度紀錄。
- [x] T-001：記錄根因、範圍與驗收。
- [x] T-002：先建立可重現外層稽核交易的 SQLite 紅燈測試。
- [x] T-003：實作交易擁有權判斷及既有功能回歸。
- [x] T-004：發布並驗證 SPC 測試站。
- [x] T-005：同步需求、變更紀錄、TODO 與原進度表。

## 驗證
- Happy Path：外層稽核交易下刪除成功且稽核同成同敗。
- Boundary：沒有外層交易時由控制器自行交易。
- Invalid Input：非 Portal 日報仍拒絕刪除。
- Regression Risk：量測、SPC 結果、警示、刪除證據及業務稽核的交易原子性。
- 實際結果及證據：詳 `verification.md`；紅燈先重現503，修正後相關36/36通過。
- 發布狀態：SPC測試API 0.1.132已發布；正式未發布。
- 限制或未決問題：未代替使用者刪除量測，登入畫面實際點擊仍待驗收。
