# 功能規格：SPC 測試站 IIS 集區隔離

- 功能 ID：`SPC-TEST-IIS-POOL-ISOLATION-1111-TASK-001`
- 版本：1
- 狀態：執行中
- 主系統：SPC 測試站基礎設施
- 相依系統：`SpcSingleTest`、`SpcApiTest`
- 資料庫影響：無
- 發布影響：僅使既有 SPC Web 測試發布流程可安全執行；正式站不在範圍
- 授權依據：使用者於 2026-11-11 選擇方案 1，將測試站兩個站台分離 IIS 集區。

## 現況與目的

`SpcSingleTest`（前端）與 `SpcApiTest`（後端）目前共用 `SpcSingleTest` application pool。既有發布工具必須停止目標集區並拒絕共用集區，以避免連帶中斷另一站；因此 Web 0.1.128 測試發布被安全阻擋。

本件建立專屬 `SpcApiTest` 集區並只將 `SpcApiTest` 站台改綁定至該集區，讓前端站仍使用 `SpcSingleTest` 集區。新集區沿用原集區的必要非機敏執行設定。

## 範圍與非範圍

- 範圍：測試主機 `PC-PA16560048` 的 `SpcApiTest` 集區建立、站台綁定、連線驗證與回復證據。
- 非範圍：正式 IIS、站台 binding、資料庫、應用程式程式碼、API／Web內容與身分設定。

## 需求與驗收

| 需求 ID | 規則 | 驗收 ID | 可觀察結果 |
|---|---|---|---|
| R-001 | `SpcApiTest` 必須使用獨立集區。 | AC-001 | `SpcApiTest.ApplicationPool=SpcApiTest`；`SpcSingleTest.ApplicationPool=SpcSingleTest`。 |
| R-002 | 新集區須與原集區保有相容執行設定。 | AC-002 | Integrated、ApplicationPoolIdentity、64-bit、OnDemand 等核准設定一致，兩站皆為 Started。 |
| R-003 | 隔離後不得影響兩個測試端點。 | AC-003 | 前端首頁、API health/version 均回 200，環境為 Test。 |
| R-004 | 回復必須可將 `SpcApiTest` 綁回原集區。 | AC-004 | IIS備份存在；回復指令及驗證條件已記錄。 |

## 回復

若新集區啟動或 API 連線失敗，立即將 `SpcApiTest` 綁回 `SpcSingleTest` 集區、啟動原集區及網站，並以 `/api/health`、`/api/version` 回驗。保留 IIS 原生備份，不刪除原集區。
