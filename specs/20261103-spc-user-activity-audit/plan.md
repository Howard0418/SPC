# 技術計畫
- 功能 ID：SPC-AUDIT-20261103
- 規格版本：1（TASK-001 候選契約）
- 規格：[spec.md](spec.md)
- 狀態：TASK-001 技術契約草案完成／待政策核准；產品實作未開始
- 事件契約：[event-contract-v1.md](event-contract-v1.md)

## 系統責任與受影響範圍

- SPC 後端：身分可信來源、稽核事件寫入、交易一致性、查詢／匯出授權、保存／監控。
- SPC 前端：成功路由與允許清單中的純前端事件；所有 API 經既有 `api/client.js`。
- Portal／SSO：只作現有身分來源相依；本案不修改 Portal 帳密驗證或 Portal schema。
- SQL Server：預計新增 `AuditEvents` 與必要索引；只能透過 EF Core Migration。
- 發布：各實作工作完成必要測試後才發布 SPC 測試站；正式站仍須另行授權。此規劃本身不建置、不發布。

預計受影響檔案類型，不代表本次已修改：

- `backend/MesSpc.Api/Controllers/AuthController.cs`
- `backend/MesSpc.Api/Services/Security/` 與預計新增的 `Services/Auditing/`
- `backend/MesSpc.Api/Infrastructure/Data/` 與 `Migrations/`
- 高風險寫入 Controller／Service
- `frontend/mes-spc-web/src/router/index.js`、`api/client.js`、稽核查詢 View
- 對應後端、前端與 E2E 測試

## 實作順序與需求對應

一次只執行一件，上一件驗證完成後才開始下一件：

1. 核准事件契約、代碼、遮罩、權限、保存與失敗策略（R-001）。技術候選版已完成；四項政策尚待核准。
2. 建立 append-only schema、索引、事件服務及冪等（R-002、R-003、R-011）。
3. SPC SSO／登出事件（R-004）。
4. 安全、操作者、角色、頁面權限、設定與拒絕存取事件（R-005、R-008）。
5. 量測、匯入、公式／F／版本、主檔、停用／刪除事件（R-006）。
6. 成功進頁、查詢／篩選／匯出與允許清單純前端事件（R-007、R-010）。
7. 稽核查詢／匯出 API 與頁面、最小權限（R-008）。
8. 保存、封存、legal hold、容量與告警（R-009）。
9. 測試站端到端、效能、故障、隔離與截圖證據（R-010～R-012）。

schema migration、角色權限、SSO 與正式資料均屬高風險，不與其他同類工作併行。

## API 與資料模型提案

`AuditEvents` 應依[event-contract-v1.md](event-contract-v1.md)使用伺服器 EventId、可空 ClientEventId、UTC 時間、環境、結構化 actor、session hash、事件分類／代碼、頁面／目標、結果、HTTP／Trace、來源及白名單 details JSON。v1 不收集 IP／原始 User-Agent，也不實作無金鑰治理的普通 IntegrityHash。索引至少包含：

- `(OccurredAtUtc, Id)`
- `(ActorOperatorId, OccurredAtUtc)`
- `(EventCode, OccurredAtUtc)`
- `(TargetType, TargetId, OccurredAtUtc)`
- `CorrelationId`

寫入面：

- 登入與安全事件由伺服器直接寫入。
- 關鍵資料寫入與稽核同交易；不得資料成功但稽核遺失。
- 頁面事件由前端送 EventId、PageKey 與 route key，後端重建 actor、environment、received time 並驗證 allowlist。
- 一般頁面事件失敗採有限重試與監控，不阻斷原頁；關鍵事件依 TASK-001 核准策略 fail closed 或可靠 outbox，不可靜默丟失。

讀取面：

- 權限分成 `admin.audit.read`、`admin.audit.export`、`admin.audit.evidence`，不沿用 Editor 預設全權。
- 預設 7 日、最多 31 日、游標分頁；時間、人員、頁面、事件、目標與結果可篩選。
- 匯出欄位固定、時間標示 UTC／台北時區、文字做 Excel 公式注入防護；查詢／匯出本身再產生稽核事件。

## 相容性與風險

- 既有 `CalibrationAuditLogs`、`UploadDetails` 及點位追溯先保留，不在首批改寫；新表是全站事件權威，後續是否整併另立工作。
- actor 不得採 request body／query 的 `operatorName`；由後端解析，避免偽造。
- 新敏感權限需避開 Editor 目前「目錄全授予」規則；先完成權限模型測試。
- 詳細 payload、完整 URL、Header、設定與例外若直接序列化會洩密；採欄位 allowlist、長度上限及負面測試。
- SPA 進頁需在 router 成功後回報；Viewer 寫入防護若擋住頁面事件 POST，需對受限端點做明確例外，不能放寬其他 Viewer 寫入。
- `Auth:Enabled=false` 時無法可信回答「誰」；測試與正式實作前須核對前後端驗證均啟用，不以 request body 補假身分。
- IP 若經反向代理，未核准 forwarded headers 前不可把代理位址當真實來源。
- 高流量頁面事件先在測試站量測 7 日；依 `活躍人數 × 登入次數 + 進頁次數 + 稽核操作次數` 估容量，再決定分割／封存。
- 截圖不放資料庫；正式附件功能未核准前不實作。

## 驗證安排

- Happy Path：SSO 成功、頁面成功、資料新增／修改／停用、查詢／匯出及正確 actor／target／result／trace。
- Boundary Case：動態路由、alias、重新整理、多分頁、重送 EventId、過長 user-agent／details、批次摘要、時區日界線。
- Invalid Input：錯誤簽章、重播、停用／未知帳號、偽造 actor、未授權查閱、惡意 JSON／Excel 公式字首、假 MIME 截圖。
- Regression Risk：登入 token／權限不變、業務交易結果不變、rollback 一致、頁面日誌故障不白屏、測試／正式隔離、既有校驗／匯入追溯保持。
- Migration：隔離 DB Up／Down／Up、索引與約束、既有表筆數／指紋不變；正式有稽核資料後不可用 Down 刪除證據。
- 效能：一般讀取／進頁 p95 增幅 ≤5%，關鍵寫入 ≤10%；尖峰 2～5 倍及清理並行驗證。
- 截圖：只在測試驗收時遮罩帳號、量測機敏值與權杖，標註環境、日期、事件 ID；證據不提交 Git。

## 發布、備份及回復

- 本次：補齊 TASK-001 候選契約、BDD、工作池與進度狀態；build／測試站／正式站發布不適用。政策未核准，TASK-001 不標完成且不開始 TASK-002。
- 未來各工作：測試通過後依規則直接發布測試站，並驗首頁、主 JS／CSS MIME、API health/version 及瀏覽器實際 API 基底。
- 正式發布：另行授權並核對實際環境；不因本規格自動發布。
- 回復：前端／API 以 feature flag 與前一版套件回復；新增表屬加法 migration。正式已有稽核後保留表與資料，以停用收集／查詢功能回復，不能直接 Down 刪表。
