# RELEASE-TOOL-TASK-005 正式機使用手冊與授權檢核

日期：2026-10-10
狀態：DONE
主系統：SPC
相依系統：RELEASE-TOOL-TASK-001～004、IIS、正式站主機、發布來源包
資料庫影響：無
發布影響：文件型 Task；未操作正式站、未發布
Rollback：還原本規格、手冊、TODO、需求索引與變更紀錄

## Requirement

整理正式機更新工具的使用手冊、參數範本、前置檢查、回復步驟、證據格式與核准清單，讓未來正式站發布必須先取得明確授權、核對目標環境與回復路徑，並保留可追溯證據。

本 Task 僅建立文件，不連線正式機、不查詢或修改正式 IIS、不執行備份、不套用交付包、不刪除任何資料。

## 產出

- `docs/release-tools/production-release-runbook.md`
- `specs/20261010-release-tool-task005/spec.md`

## 高風險邊界

- 正式站備份、套用、回復與 app_offline 移除仍需使用者另行明確授權。
- 不可發布到 `D:\Sites\PmrPortal`。
- 不記錄帳密、憑證、token、連線字串或正式主機機敏資訊。
- 文件中的正式環境值使用 placeholder；實際值需於授權執行時由操作者在安全通道提供。
- 正式執行前必須核對 manifest、來源包 hash、目標環境、IIS site/app pool、實體路徑、備份路徑、保留檔案、smoke test URL 與 rollback 步驟。
- 任何刪除動作需另列待刪除清單並取得確認；本手冊不授權自動刪除。

## BDD 驗收

### Scenario: 建立正式機操作手冊

Given RELEASE-TOOL-TASK-002～004 已完成非正式工具與演練
When 本 Task 建立操作手冊
Then 手冊包含前置檢查、參數範本、備份、套用、驗證、回復與證據格式
And 手冊不包含帳密、憑證、token 或連線字串

### Scenario: 正式站需另行授權

Given 使用者尚未授權正式站操作
When 本 Task 完成
Then 不連線正式機
And 不操作正式 IIS
And 不發布正式站

### Scenario: 阻擋錯誤目標

Given 操作者準備正式發布
When 前置檢查發現目標路徑為 `D:\Sites\PmrPortal` 或 manifest 目標不是 SPC
Then 手冊要求停止執行
And 不得套用交付包

### Scenario: 回復步驟可追溯

Given 正式發布後 smoke test 失敗或授權者要求回復
When 操作者依手冊執行 rollback
Then 必須使用同一 releaseId 對應備份
And 保留回復 evidence、smoke test 結果與待確認刪除項目

## 測試規劃

- Happy Path：檢查手冊涵蓋授權、manifest、備份、套用、smoke test、rollback 與證據格式。
- Boundary Case：正式值未知時以 placeholder 呈現；app_offline 移除、暫存清理與舊備份清理不自動執行。
- Invalid Input：目標為 Portal 路徑、manifest 缺 hash、目標環境不一致、未取得正式授權時，手冊要求停止。
- Regression Risk：不得放寬正式站授權、不得暗示可直接操作正式機、不得把非正式工具的 Production 阻擋視為已授權正式執行。

## 驗證

- 方式、環境：文件內容與連結檢查。
- 實際結果及證據：手冊已建立；`TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md` 已同步；未執行程式建置或發布。
- 發布狀態：文件型 Task，不發布測試站或正式站。
- 限制或未決問題：正式站實際執行仍需使用者另行授權；正式環境參數與機敏值需由授權執行時安全提供。
