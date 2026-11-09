# 技術計畫

## 變更範圍

- API：新增受認證的 activity endpoint 與 `NavigationReadAudit` 服務；不新增 migration，沿用 append-only `AuditEvents`。
- Web：新增集中式活動回報工具；Router `afterEach` 只回報成功 canonical route；axios response interceptor 只對成功的受控讀取／匯出回報安全摘要。
- 安全：Viewer 僅可 POST 自己的活動摘要至專用 endpoint；不放寬其他寫入。服務端以現有 `AuditPermissions.CurrentAsync` 驗證有效 JWT 身分。
- 相依：既有 `AuditEventWriter`、`AuditEventContract.Pages`、`AuthenticationAudit` 及 `api/client.js`。

## 實作設計

1. 以固定 DTO 限制 activity 為 `PageEnter`、`Query`、`Filter`、`Export`；只接受 PageKey、導航型別、FilterFields、UTC 日期範圍、ResultCount 或 ExportFormat。
2. 服務端將 DTO 轉換為固定 `AuditEventDraft`，用 `AppendAsync` 留存。頁面事件才帶 client retry GUID；讀取活動不帶 retry GUID。
3. 前端依 route meta／集中對照取得 canonical PageKey；未知 route 或 redirect 不送出。
4. axios 僅觀察成功 GET／blob 回應，排除 health、version、auth、activity endpoint 及非 API URL；只從 params 的 key 形成 FilterFields，絕不傳值。
5. 回報失敗只保留在記憶體一次重試，且不 await／不影響原 API response。

## 測試與發布

- 先建立 API service／controller、DTO 驗證、頁面 mapping、axios 摘要與重試單元測試。
- 建置後發布 SPC 測試 API／Web，驗證首頁、主 JS／CSS MIME、health/version 與瀏覽器實際 API 基底。
- 正式站不更新；回復方式為關閉 `Audit:NavigationReadEnabled` 或回復測試 API／Web 前一版，既有 AuditEvents 不刪除。
