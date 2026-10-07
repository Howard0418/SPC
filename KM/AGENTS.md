# KM 開發入口

- 回覆繁體中文，簡述 Task、狀態、變更、驗證與風險。
- 每次只處理一個 Task；完成後 STOP，等待使用者確認。
- 開工先讀本檔、`KM/specs/README.md`、`KM/docs/progress.md`，以及目前 Task 對應 Spec。
- Token-Saving Mode：只讀目前 Task 必要檔案；不要重複掃描整個 repository，不輸出完整 diff 或長篇程式說明。
- 不修改與目前 Task 無關的程式，不順手重構，不未經確認做大規模架構調整。

## SDD + BDD + AI Coding 流程

每個 Task 必須依序執行：

Requirement → Spec → BDD → Implementation → Test → Verify → `progress.md`

## Spec 必填欄位

每個開發 Task 必須有對應 Spec，至少包含：

- Goal
- Scope
- Out of Scope
- Current Behavior
- Expected Behavior
- Business Rules
- Technical Impact
- API Impact
- Database Impact
- UI Impact
- Acceptance Criteria
- Risks

## BDD 規則

- 重要功能需在 `KM/features/` 或對應 Spec 中記錄 Given / When / Then。
- 驗收條件必須可對應到測試或人工驗證紀錄。

## 測試規則

至少評估並記錄：

- Happy Path
- Boundary Case
- Invalid Input
- Regression Risk

適合自動化測試的功能需建立自動化測試。

## DONE 條件

只有以下全部成立才可標示 DONE：

- Spec 完成
- BDD Acceptance Criteria 通過
- Build 成功，或文件型 Task 明確標示不適用
- 相關 Test 通過，或文件型 Task 明確標示不適用
- 沒有發現明顯 Regression
- `KM/docs/progress.md` 已更新

## STOP RULE

完成目前 Task 後只輸出簡短摘要：

- Task
- Status
- Changed Files
- Tests
- Result
- Risks / Remaining Issues

然後停止，不自動開始下一個 Task。
