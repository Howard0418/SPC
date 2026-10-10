# ARCH-TASK-002-DIAGNOSTICS-MES-SYNC-RELIABILITY-20261010

## 目的

改善 MES Sync 背景處理的資料可靠性，避免尚未實作處理邏輯或 Payload 無效時仍被標示為 `Processed`，造成資料漏同步卻無法追查。

## 範圍

- 主系統：SPC backend。
- 相依系統：MES 訊息來源只以 `MesSyncMessages` 既有表格為邊界；本次不修改 MES、Portal 或 TransFiles。
- 資料庫影響：不新增 migration、不變更 schema。
- 發布影響：後端變更，測試通過後發布 SPC 測試站 backend；正式站不發布。
- Rollback：還原本次後端程式與測試站 backend 備份即可。

## 現況

`MesSyncProcessorService` 目前直接撈取 `Pending` 訊息，尚未實作 `MessageType` 實際處理邏輯，卻會將每筆訊息標示為 `Processed`。這會讓未處理訊息從待辦佇列消失，影響 MES 來源資料的追溯與補救。

## 需求

- R-001：MES Sync 不得將未支援或尚無處理器的 `MessageType` 標示為 `Processed`。
- R-002：`PayloadJson` 不是有效 JSON 時，訊息應標示為 `Failed` 並留下可追查錯誤。
- R-003：背景排程與批次處理邏輯需可單元測試，避免未來新增處理器時只能靠長輪詢驗證。
- R-004：本次不做 schema migration，不改既有 MES Sync 表格欄位。

## BDD 驗收

```gherkin
Feature: MES Sync message reliability

  Scenario: Unsupported MES message type is not marked as processed
    Given a pending MES sync message with a valid JSON payload
    And no registered handler supports its message type
    When the MES sync batch processor runs
    Then the message status is Failed
    And the message has an error message explaining the unsupported type
    And the message is not marked Processed

  Scenario: Invalid MES payload is rejected
    Given a pending MES sync message with invalid JSON
    When the MES sync batch processor runs
    Then the message status is Failed
    And the message has an invalid JSON error
    And the message is not marked Processed
```

## 驗證規劃

- Happy Path：註冊測試 handler 時，支援的有效 JSON 訊息可標示 `Processed`。
- Boundary Case：批次只處理 `Pending` 且最多 100 筆，排序依 `CreatedAt`。
- Invalid Input：無效 JSON 與未支援 MessageType 標示 `Failed`。
- Regression Risk：確認後端 build 通過，背景服務仍註冊並可呼叫批次處理器。

## 狀態

已完成。後端測試與 build 通過，已發布 SPC 測試站 backend；正式站未發布。
