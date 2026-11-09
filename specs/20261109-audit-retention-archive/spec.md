# SPC-AUDIT-1103-TASK-008 稽核保存、封存與 legal hold

## 範圍

- 主系統 SPC；相依 SQL Server。只在 `PMR_SPC_TEST` 建置與驗證，正式另行授權。
- 關鍵事件線上 24 個月、封存再保存 36 個月；頁面進入與一般查詢線上 180 日、封存至滿 1 年。
- 建立可驗證的資料庫封存批次、不可變封存事件、legal hold、dry-run／受控執行、容量摘要與失敗告警。
- 排程預設停用；本件不建立截圖附件、不收集新個資、不直接啟用正式清理。

## BDD

### 到期封存

- Given 線上事件已超過所屬類別的線上期限且未超過總保存期限
- When 維護者先 dry-run 再執行
- Then 系統以同一交易建立不可變封存副本、批次筆數與 SHA-256，驗證逐筆 hash／抽樣還原後才刪除線上事件。

### legal hold 優先

- Given 有效 hold 的 UTC 範圍及可選事件／頁面／目標條件涵蓋到期事件
- When 執行封存或最終清理
- Then 該事件保留，解除 hold 前不得刪除。

### 失敗與重跑

- Given 封存 hash、抽樣還原或資料庫交易任一步驟失敗
- When 執行清理
- Then 線上事件保持、批次標示失敗並告警；同一範圍重跑不產生重複封存。

### 權限與環境

- Given 非安全管理者、沒有 `admin.audit.evidence` 或非 Test 環境
- When 要求建立／解除 hold 或執行維護
- Then 回 403；正式環境執行仍需明示發布與維護授權。

### 容量

- Given 稽核事件與封存資料持續增加
- When 查詢容量摘要
- Then 回傳事件數、最早／最晚 UTC、封存數、有效 hold、待封存與逾期筆數；不回傳 Details 或人員資料。

## 非功能與回復

- runtime 保持 `AuditEvents` 只 SELECT／INSERT，封存／刪除使用分離維護身分。
- migration 只新增表／索引／受控 trigger；Down 遇任何封存或 hold 資料即拒絕。
- 回復 API／Web 時保留新增表與封存資料；停用維護開關與排程即可停止寫入。
