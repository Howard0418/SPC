# 功能規格：TransFiles 日期公式預覽與版本稽核

- 功能 ID：FORMULA-IMPORT-003
- 版本：1
- 狀態：完成
- 主系統：TransFiles
- 相依系統：SPC API（僅既有 preview／confirm 契約；本件不呼叫）
- 授權依據：使用者於 2026-10-31 指示「繼續」。
- 現行需求基準：[日期公式 DB 匯入環境與版本歷史](../20261029-chemical-formula-db-import-environment-history/spec.md)

## 目的與範圍

轉檔工具已提供測試／正式 API 白名單，並拒絕排程直接 confirm 正式環境。本件使每次排程報告清楚列出目標環境、執行／授權身分、預覽狀態與版本事件；來源資料若以量測業務鍵去重，公式、規格、範圍或 F 值變更仍必須以新的版本事件留存，不得因量測不重複規則而遺失稽核記錄。

不在本件執行任何 preview、prepare、confirm 或資料庫寫入；不打包、發布或變更正式站。

## 驗收需求

| 需求 ID | 規則 | 可觀察驗收 |
|---|---|---|
| R-001 | 報告必須標明 mode、環境、執行身分與 confirm 授權來源（如有），且不含權杖或密碼。 | AC-001：dry-run 結果含 `audit` 區段與安全欄位。 |
| R-002 | 公式／規格／F 值版本變更必須保留不可變歷史事件，含來源、工作表、列、時間、前後版本識別與差異欄位。 | AC-002：同一業務鍵的第二筆來源雖標示重複略過，仍新增一筆 `changed` 版本歷史。 |
| R-003 | 相同版本不可產生重複版本歷史；量測資料重複也不得變成可寫入列。 | AC-003：相同快照重跑不增加版本事件，列仍為 `skipped_duplicate`。 |
| R-004 | 正式 confirm 仍維持禁止，測試／預覽／dry-run 均不因本件改變 API 呼叫行為。 | AC-004：既有環境與 confirm 阻擋測試通過。 |

## BDD

```gherkin
Scenario: 重複量測的來源公式變更仍可稽核
  Given 已記錄一筆量測業務鍵與公式版本 v1
  When 讀入相同量測業務鍵但 F 值不同的來源列
  Then 該列不得再次匯入量測資料
  And 報告應標示 skipped_duplicate
  And 狀態檔應追加 changed 的版本歷史事件

Scenario: 預設執行不寫入正式資料
  Given 排程設定為 dry-run 與 test
  When 執行來源掃描
  Then 結果應顯示目標環境及稽核摘要
  And 不呼叫 preview 或 confirm client
```

## 回復

回復僅需還原 `scheduled_import.py` 與其版本稽核狀態檔；本件不含 schema、資料庫或站台發布，無資料回寫需要。
