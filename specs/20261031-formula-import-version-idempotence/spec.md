# 功能規格：日期公式版本語意差異與冪等
- 功能 ID：FORMULA-IMPORT-002
- 版本：1
- 狀態：完成
- 涉及專案：SPC API
- 授權依據：使用者於 2026-10-31 指示「繼續」。
- 現行需求基準：[日期公式 DB 匯入環境與版本歷史](../20261029-chemical-formula-db-import-environment-history/spec.md)
- 前案：[FORMULA-IMPORT-001](../20261031-formula-import-contract-verification/verification.md)

## 目的、現況與證據

既有 `ChemicalAnalysisFormulaVersionService` 已保存公式版本，`ChemicalFormulaScheduleService` 已按量測日期選取排程期間並保護已發行計算憑證。程式觀察發現公式設定比較僅去除整段字串首尾空白；相同 JSON 若屬性順序或縮排不同，仍會建立無意義版本。

## 範圍與非範圍

- 範圍：將公式設定正規化為遞迴排序的 JSON，供版本差異比較與歷史儲存使用；補足等價重送、不等價設定、既有日期排程與已發行收據不可回溯的測試。
- 非範圍：不新增 DB migration、不建立或替換任何公式排程、不呼叫匯入 API、不寫入量測資料、不改 Portal／TransFiles／IIS，亦不發布正式站。

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 同一公式設定即使 JSON 屬性順序、縮排不同，也視為相同。 | AC-001 | 重送等價 JSON 後 `Created=false`，版本筆數與 VersionNo 不增加。 |
| R-002 | 公式設定、F 值引用或精度等 JSON 值有差異時，才新增一筆遞增版本與前版快照。 | AC-002 | 差異 JSON 產生下一個 VersionNo，保留原 ConfigJson 作 PreviousConfigJson。 |
| R-003 | 日期效期、排程選版及已發行收據不可回溯規則維持。 | AC-003 | 原有跨日期選版及收據保護測試通過。 |

## 例外與邊界

- 非 JSON 的既有設定仍採去除首尾空白後比較，避免既有無效資料造成例外。
- JSON 陣列順序視為資料意義的一部分，不排序陣列。
- JSON 字串值（例如公式字串）不任意改寫，避免改變計算語意。

## 假設與未決問題

無阻擋事項。實際 Excel 匯入對應的版本建立與批次收據，留待 FORMULA-IMPORT-003／005 的端到端工作。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-31 | 初版。 |
