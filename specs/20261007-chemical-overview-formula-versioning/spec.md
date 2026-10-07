# 線別分析項目總覽公式版本記錄補強

功能 ID：SPC-CHEM-OVERVIEW-FORMULA-VERSION-20261007  
狀態：規劃完成，待執行  
涉及專案：SPC  
授權依據：使用者要求「線別分析項目總覽的公式修改也要加入版本記錄，跟 SPC 管制項目設定頁面的藥液分析公式的版本記錄是同一個功能」。

## Goal

線別分析項目總覽若可直接修改藥液分析公式，必須共用既有「SPC 管制項目設定頁」的 `ChemicalAnalysisFormulaVersion` 版本記錄與回復功能，避免同一公式因不同入口修改而失去追溯。

## Scope

- 補強 `ChemicalAnalysisOverviewView.vue` 公式修改/儲存流程。
- 確認總覽頁儲存時每一筆公式變更都走既有 `PUT /api/v1/part-process-characteristics/{id}` 或同一個 `ChemicalAnalysisFormulaVersionService`。
- 總覽頁需提供或導向同一份版本記錄查詢/回復功能。
- 更新自動化測試，證明總覽頁修改與管制項目設定頁修改會寫入同一張版本表。

## Out of Scope

- 不新增第二套公式版本表。
- 不改藥液公式 JSON 格式、公式語法與計算引擎。
- 不修改 F 表版本記錄。
- 不處理 Excel 匯入公式回存。
- 不發布正式站。

## Current Behavior

- 藥液分析公式版本功能已在 `ChemicalAnalysisFormulaVersion` 與 `ChemicalAnalysisFormulaVersionService` 完成。
- 管制項目設定頁修改 `ChemicalAnalysisConfigJson` 時，已能建立版本記錄與回復。
- 線別分析項目總覽/批次儲存規格已要求「逐筆建立版本紀錄」，但尚未作為獨立小工作鎖定總覽頁入口與驗收。

## Expected Behavior

- 從線別分析項目總覽修改公式後，可在同一筆管制項目的版本記錄看到新版本。
- 從總覽頁與管制項目設定頁修改同一筆公式時，版本號連續、來源一致、可用同一個回復 API 回復。
- 若公式內容無變更，不新增重複版本。
- 若其中一筆儲存失敗，成功項目的版本記錄仍可追溯，失敗項目不得產生假版本。

## Business Rules

- 藥液公式只有一個權威版本記錄來源：`ChemicalAnalysisFormulaVersions`。
- 總覽頁不可繞過既有版本服務直接寫入 `ChemicalAnalysisConfigJson`。
- 批次儲存以每一筆管制項目為版本單位，不建立跨項目的合併版本。
- 權限沿用 SPC 主檔/管制項目編輯權限。

## Technical Impact

- 可能影響前端 `ChemicalAnalysisOverviewView.vue` 的儲存流程與版本紀錄入口。
- 若現行總覽頁已逐筆呼叫既有 PUT，需補測試與 UI 版本入口；若未走既有 PUT，需改為共用版本服務路徑。
- 後端優先沿用既有 API；只有現有流程無法支援時，才新增薄型批次 API，且仍呼叫同一個版本 service。

## API Impact

- 優先沿用：
  - `PUT /api/v1/part-process-characteristics/{id}`
  - 既有藥液公式版本查詢/回復 API
- 若新增批次 API，必須在內部逐筆呼叫同一版本記錄邏輯，且回傳每筆成功/失敗結果。

## Database Impact

- 不新增新表。
- 不改 `ChemicalAnalysisFormulaVersions` schema，除非實作時發現缺少必要來源欄位；若需新增來源欄位需另行拆小工作。

## UI Impact

- 線別分析項目總覽頁需讓使用者能從修改後的項目查看版本記錄，或清楚導向同一筆管制項目的版本記錄。
- 儲存摘要需提示「將建立公式版本記錄」。
- 儲存結果需顯示每筆成功/失敗，避免使用者誤以為全部都有版本。

## Acceptance Criteria

- AC-001：總覽頁修改一筆藥液公式並儲存後，該 PPC 的 `ChemicalAnalysisFormulaVersions` 新增一筆版本。
- AC-002：總覽頁與管制項目設定頁修改同一 PPC 時，版本記錄可在同一份清單連續查到。
- AC-003：總覽頁公式未變更時儲存，不新增重複版本。
- AC-004：批次儲存兩筆公式時，兩筆各自建立版本；其中一筆失敗時只成功者建版。
- AC-005：未登入或無權限使用者不可從總覽頁儲存公式，也不可回復版本。
- AC-006：完成後更新 `TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md` 與驗證紀錄，測試站發布狀態明確。

## BDD

### Scenario: 總覽頁修改公式建立同一份版本記錄

Given 使用者在線別分析項目總覽看到某一筆藥液分析項目  
When 使用者修改濃度公式並儲存  
Then 系統必須使用既有藥液公式版本功能建立該 PPC 的新版本記錄

### Scenario: 兩個入口共用版本清單

Given 使用者已在管制項目設定頁修改過同一筆藥液公式  
When 使用者再從線別分析項目總覽修改同一筆公式  
Then 版本記錄清單必須同時包含兩次修改，且版本號連續

### Scenario: 批次儲存部分失敗

Given 使用者在線別分析項目總覽修改兩筆公式  
When 儲存時其中一筆通過、另一筆因驗證失敗被拒絕  
Then 成功項目必須建立版本記錄，失敗項目不得建立版本記錄

## Test Plan

- Happy Path：總覽頁修改單筆公式後產生版本。
- Boundary Case：公式無變更不新增版本；批次多筆各自建版。
- Invalid Input：無效公式 JSON、非 CHEM 項目、未授權儲存/回復。
- Regression Risk：管制項目設定頁既有版本記錄與回復不可壞；F 表版本記錄不可受影響。

## Risks

- 若總覽頁未來改用新批次 API，可能繞過既有單筆 PUT 的版本服務。
- 批次部分成功會造成使用者誤解，需要清楚顯示每筆結果。
- 若沒有自動化測試鎖住同一版本表，後續可能又產生兩套記錄邏輯。
