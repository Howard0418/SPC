# ARCH-TASK-005-DATABASE-STARTUP-FLOW-20261011

## 目的

改善 SPC API 資料庫 migration 與啟動流程的可讀性與可測試性，先將 provider 判斷與啟動動作決策抽離，降低日後整理相容性 SQL、測試站啟動與正式站發布時誤判 provider 的風險。

## 範圍

- 主系統：SPC backend。
- 相依系統：無。
- 資料庫影響：不新增 migration、不執行正式資料修補、不改現有相容性 SQL 內容。
- 發布影響：後端程式變更，測試通過後發布 SPC 測試站 backend；正式站不發布。
- Rollback：還原本次後端程式與測試站 backend 備份。

## 現況

`Program.cs` 啟動流程直接以 `DatabaseProvider` 字串判斷 sqlite/sqlserver，並在同一段中混合 `EnsureCreated`、`Migrate`、SQL Server baseline 與多段相容性 SQL。這使 provider 決策缺少單元測試，也讓日後拆分相容性 SQL 時風險偏高。

## 需求

- R-001：資料庫啟動 provider 正規化需可單元測試，空白或未知值沿用 SQL Server。
- R-002：SQLite 啟動應明確使用 `EnsureCreated`，不跑 EF migration 與 SQL Server baseline。
- R-003：SQL Server 啟動應明確先跑 baseline 再跑 EF migration。
- R-004：本次不變更相容性 SQL 內容、不新增 migration、不操作正式資料庫。

## BDD 驗收

```gherkin
Feature: Database startup flow decision

  Scenario: SQLite startup uses model creation
    Given DatabaseProvider is sqlite
    When the API builds its database startup plan
    Then the startup action is EnsureCreated
    And SQL Server migration baseline is not required

  Scenario: SQL Server startup uses migration
    Given DatabaseProvider is empty
    When the API builds its database startup plan
    Then the startup action is Migrate
    And SQL Server migration baseline is required
```

## 驗證規劃

- Happy Path：sqlite 與 sqlserver provider 正規化與動作正確。
- Boundary Case：空白、null、大小寫混用、未知 provider。
- Invalid Input：未知 provider 不走 sqlite 分支，預設 SQL Server。
- Regression Risk：後端 build 通過；測試站 smoke 通過。

## 狀態

已完成。啟動決策測試與後端 build 通過；已發布 SPC 測試站 backend，正式站未發布。
