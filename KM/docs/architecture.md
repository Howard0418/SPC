# KM Architecture

## Current Structure

- `KM/testing/`：既有測試規劃與進度文件。
- `KM/specs/`：新 SDD Task 規格入口。
- `KM/features/`：BDD Given / When / Then 驗收情境。
- `KM/docs/`：架構與進度紀錄。
- `KM/tests/`：後續 KM 自動化測試或測試說明。

## Development Mode

KM 後續採 SDD + BDD + AI Coding：

1. Requirement：確認單一 Task 與範圍。
2. Spec：在 `KM/specs/tasks/` 建立或更新規格。
3. BDD：以 Given / When / Then 定義重要驗收行為。
4. Implementation：只修改目前 Task 授權範圍。
5. Test：覆蓋 Happy Path、Boundary Case、Invalid Input、Regression Risk。
6. Verify：記錄 build、測試與人工驗證結果。
7. Progress：更新 `KM/docs/progress.md`。

## Boundaries

- KM 框架文件不改 SPC 業務邏輯。
- 跨 SPC、Portal、TransFiles 的需求需另建對應 Spec，並標明主系統、相依系統、資料庫影響、發布影響與 rollback。
- 大規模架構調整須使用者確認後才執行。
