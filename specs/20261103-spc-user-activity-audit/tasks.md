# 任務
- 功能 ID：SPC-AUDIT-20261103
- 規格版本：2
- 狀態：001～006 DONE（測試階段）；007～009未開始；正式未發布
- [規格](spec.md)｜[計畫](plan.md)

| 順序 | 工作代號 | 優先 | 主／相依系統 | 工作內容 | DB／發布／回復 | 狀態 |
|---:|---|---|---|---|---|---|
| 1 | `SPC-AUDIT-1103-TASK-001` | P1 | SPC；Portal／SSO只供身分契約 | 核准事件格式、頁面／操作代碼、敏感排除、可信 actor、關鍵／一般事件失敗策略、查閱權限與保存年限。 | 無 DB；文件型不發布；回復為有紀錄的契約修訂。 | [x] DONE：2026-11-03使用者核准四項政策 |
| 2 | `SPC-AUDIT-1103-TASK-002` | P1 | SPC／SQL Server；依001 | 新增 append-only `AuditEvents`、索引、冪等、共用寫入服務與環境隔離。 | EF migration；測試API98；回復API97並保留新增表。 | [x] DONE：儲存基礎測試完成，正式未發布；[驗證](task002-storage.md) |
| 3 | `SPC-AUDIT-1103-TASK-003` | P1 | SPC SSO；依002 | 記SSO成功／拒絕／失敗、停用本機登入及登出請求與session；Auth啟用／runtime分權，不改Portal帳密驗證。 | 無新增schema；測試API99／Web116；回復舊包／私有設定，保留事件。 | [x] DONE：實站13項及使用者Portal重登通過；[驗證](task003-authentication.md) |
| 4 | `SPC-AUDIT-1103-TASK-004` | P1 | SPC；依003 | 操作者／角色／頁面權限、設定與拒絕事件；明示敏感權限、成功同交易、SMTP意圖與回復。 | 無schema／實際授權變更；測試API106、Web119保持；回復API105與原旗標，保留事件。 | [x] DONE：測試完成／正式未發；[驗證](task004-security.md) |
| 5 | `SPC-AUDIT-1103-TASK-005` | P1 | SPC；依004 | 記量測、匯入確認、公式／F／版本、主檔、刪除／停用等高風險操作；批次採摘要，驗證交易一致。 | 不改業務 schema；測試站 API；舊版 API 回復並保留事件。 | [x] DONE：2026-11-09實際新增／刪除、actor、事件85／86、刪除後留存與原值遮罩通過；正式未發布；[驗證](../20261106-audit-business-operations/verification.md) |
| 6 | `SPC-AUDIT-1103-TASK-006` | P1 | SPC Web／API；依005 | 記成功進頁、查詢／篩選／匯出及允許清單純前端操作；canonical route、去重、有限重試與丟棄監控。 | 無額外 schema；測試站 Web／API；前端／API feature flag 回復。 | [x] DONE：測試站已發布；實際事件95～106、actor、遮罩及冪等查核通過；[驗證](../20261109-audit-navigation-read-activity/verification.md) |
| 7 | `SPC-AUDIT-1103-TASK-007` | P2 | SPC；依006 | 建稽核查詢／匯出 API 與頁面，含游標分頁、時間／人員／頁面／操作／目標／結果篩選及獨立最小權限。 | 無額外 schema；測試站 Web／API；隱藏入口／回復舊包並保留資料。 | [x] 測試完成／正式未發布 |
| 8 | `SPC-AUDIT-1103-TASK-008` | P2 | SPC／SQL Server；依002、007 | 保存／封存、legal hold、清理重跑、容量監控與告警；先以測試站7日流量決定索引／分割。 | 可能新增保留欄位／作業 migration；測試環境先行；停用排程回復，資料不刪。 | [x] 測試完成／正式未發布 |
| 9 | `SPC-AUDIT-1103-TASK-009` | P2 | SPC；依001～008 | 測試站 E2E、遮罩、故障、效能、隔離及截圖證據驗收；整理正式發布包但不自動發正式。 | 不新增 schema；測試站驗收；正式發布另授權。 | [ ] 部分完成：效能已通過；登入畫面／截圖受瀏覽器服務阻擋 |

## 排序與執行限制

- 依序 001→009，每次只執行一件；不自動開始下一件。
- 002 schema migration、003 SSO、004 權限與正式資料相關工作不得與其他同類高風險工作併行。
- 001契約、002儲存、003登入登出、004安全／權限與005高風險資料操作均完成測試階段；Auth／受限runtime已通過實際啟動與身分驗證，後續migration／備份須採獨立管理身分。006相依已解除；006～009維持待執行，不自動開始。
- 每件須先更新自己的 spec／BDD；核心邏輯先建對應測試，完成必要 build/test、測試站驗證、文件與進度表同步後才可 DONE。
- 正式站、正式 migration 或正式資料操作仍須使用者另行授權。

## 各件最低測試面

- Happy Path：正確 actor、事件、頁面／目標、結果、時間與環境。
- Boundary Case：重送、動態路由、多分頁、批次、時區、長度上限、最大查詢區間。
- Invalid Input：偽造 actor、未授權、敏感 payload、錯誤簽章、惡意 JSON／匯出字串。
- Regression Risk：原登入／權限／資料／圖表不變，交易 rollback 一致，頁面事件失敗不白屏，環境不混用。
