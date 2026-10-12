# ARCH-TASK-006-RELEASE-ENVIRONMENT-ISOLATION-20261012

## 目的

改善 SPC 發布流程與環境隔離，將 release 工具中重複的正式環境與 Portal 路徑阻擋規則抽為共用模組，並新增可自動驗證的 PowerShell 測試，避免後續修改其中一支工具時放寬安全邊界。

## 範圍

- 主系統：SPC release 工具。
- 修改檔案：`tools/release/`、`tests/release/`、本規格與同步文件。
- 非範圍：不操作正式站、不操作正式 IIS、不發布測試站或正式站、不刪除資料。
- 資料庫影響：無。
- Rollback：還原本次 release 工具與文件。

## 現況

`ReleaseApplyPackage.ps1` 與 `ReleaseBackupRestore.ps1` 各自內建 `Assert-NonProductionTarget`。兩份邏輯相同但分散，後續若只改其中一份，可能造成正式環境名稱或 `D:\Sites\PmrPortal` 路徑阻擋不一致。

## 需求

- R-001：正式環境名稱阻擋規則須集中為共用 release guard。
- R-002：`D:\Sites\PmrPortal` 及其子路徑須由共用 release guard 阻擋。
- R-003：release apply 與 backup/restore 工具須使用同一份 guard。
- R-004：安全邊界須有自動化測試，不依賴連線正式機或 IIS。

## BDD 驗收

```gherkin
Feature: Release environment isolation

  Scenario: Production environment name is blocked
    Given a release target named Production
    When the release target guard validates it
    Then validation fails before file operations

  Scenario: Portal path is blocked
    Given a release target path under D:\Sites\PmrPortal
    When the release target guard validates it
    Then validation fails before file operations

  Scenario: Sandbox SPC path is allowed
    Given a release target named Sandbox
    And the path is outside D:\Sites\PmrPortal
    When the release target guard validates it
    Then validation succeeds
```

## 驗證規劃

- Happy Path：Sandbox + SPC 非正式路徑通過。
- Boundary Case：Portal 子路徑阻擋、大小寫正式環境名稱阻擋。
- Invalid Input：空白環境名稱或空白路徑拒絕。
- Regression Risk：兩支既有 release 工具仍可載入共用模組。

## 狀態

已完成。Release guard 測試與兩支工具載入驗證通過；本次未發布測試站或正式站。
