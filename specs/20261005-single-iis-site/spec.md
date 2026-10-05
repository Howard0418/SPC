# 功能規格：SPC 單一 IIS Site 部署
- 功能 ID：20261005-single-iis-site
- 版本：1
- 狀態：草稿；完成 TASK-001/TASK-002，待使用者核准後才進入程式修改
- 涉及專案：SPC Web、SPC API、IIS
- 授權依據：2026-10-05 使用者要求將目前專案調整成單一 IIS Site；目前僅核准 TASK-001 分析與 TASK-002 規格/rollback 計畫。
- 現行需求基準：[docs/requirements.md](../../docs/requirements.md)
- 前案或相關規格：多個既有測試站發布規格皆以 `SpcWeb`、`SpcApi` 分站台/分 port 驗證；本規格只定義後續調整邊界。

## 目的、現況與證據
目標是讓使用者只存取單一網址，例如 `https://portal/`；Vue 前端走 `/`，API 走同 origin 的 `/api/*`。

目前觀察：
- Vue 專案位於 `frontend/mes-spc-web`，使用 `createWebHistory()`。
- ASP.NET Core Web API 位於 `backend/MesSpc.Api`。
- API controller route 已包含 `/api` 前綴，例如 `/api/version`、`/api/v1/auth/portal-sso`、`/api/v1/spc`。
- 現行測試站採前後端分開：前端常見 `http://172.16.110.27:8083`，API 常見 `http://172.16.110.27:8081`。
- 前端 `.env.testhost`、`.env.production` 及 `src/api/client.js` 仍含 IP/port API base。
- `Program.cs` 目前已有服務 sibling `frontend` 靜態檔與 `MapFallbackToFile("index.html")` 的能力。

## 範圍與非範圍
範圍：
- 將 SPC Web 呼叫 API 的基準調整為相對路徑 `/api`。
- 保留 API 既有 `/api/*` route，不重寫 controller。
- 優先採單一 IIS Site，由 ASP.NET Core Site 同時提供 Vue 靜態檔與 API。
- 建立測試站部署、smoke test 與 rollback 流程。

非範圍：
- 不修改資料庫 schema。
- 不修改 AD / LDAP / Portal SSO 業務邏輯。
- 不升級套件。
- 不重寫 API。
- 不發布正式站；正式站需另行授權。
- 不將 `D:\Sites\PmrPortal` 作為發布目標。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 使用者只需存取單一 HTTPS 網址。 | AC-001 | `https://目標網址/` 載入 Vue 首頁，不需輸入 API port。 |
| R-002 | Vue API 呼叫使用同 origin 相對路徑。 | AC-002 | build 後前端資產不含 `http://IP:Port/api` 作為 SPC API base；API 請求為 `/api/...`。 |
| R-003 | API route 不可變成 `/api/api/*`。 | AC-003 | `GET /api/version` 成功；前端匯出與一般 API 呼叫不產生 `/api/api/`。 |
| R-004 | Vue history mode 子路由重新整理正常。 | AC-004 | 重新整理 `/spc`、`/calibration-instruments`、`/particle-monitoring` 皆回 Vue app。 |
| R-005 | 既有登入、Portal SSO、JWT、權限不改業務語意。 | AC-005 | 未登入受保護 API 回 401；Portal SSO 後可取得 JWT；Viewer 寫入仍回 403。 |
| R-006 | SQL Server 與既有外部服務設定不被本次切換覆寫。 | AC-006 | `appsettings.json` 既有 connection string、JWT key、Portal SSO key、SMTP、Chameleon 設定保留。 |
| R-007 | HTTPS 下不可產生 Mixed Content。 | AC-007 | 瀏覽器 Network 無 SPC API `http://...` mixed content；API 與前端同 HTTPS origin。 |
| R-008 | 發布與回復可追溯。 | AC-008 | 發布前有 backend/frontend/appsettings/web.config/IIS binding 備份；rollback 可回復到原分站台架構。 |

## 例外與邊界
- 不建議將 ASP.NET Core 設為 IIS `/api` child Application，因現有 API route 已含 `/api`，會有 `/api/api` 風險。
- 若日後必須採 `/api` child Application，需另行設計 PathBase 或重寫 route，屬高風險替代方案。
- `/health` 目前未在 `Program.cs` 找到明確 route；驗證以 `/api/version` 作為核心 API health，另將 `/health` 列為待確認。
- Portal URL 仍由 `VITE_PORTAL_URL` 控制；若入口網址同步異動，需另與 Portal 設定對齊。

## 假設與未決問題
- 假設第一階段只在 SPC 測試站驗證單一 Site。
- 待確認：實際單一 HTTPS 網址、憑證主體/SAN、IIS Site 名稱與 App Pool 帳號。
- 待確認：Portal `/Spc/Launch` 回跳 SPC 的正式 URL 是否也要同步改為新 HTTPS 網址。
- 以上未決不阻擋 TASK-003 程式最小修改，但阻擋實際 IIS 部署。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-05 | 初版；TASK-001 分析後建立單站部署邊界與驗收 |
