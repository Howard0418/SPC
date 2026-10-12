# 驗證紀錄
- 功能 ID：20261005-single-iis-site
- 規格版本：1
- 日期／環境：2026-10-05／D:\SPC 工作區
- 實作狀態：T-001～T-006 已完成；T-007 已補驗 HTTP/HTTPS 單站 smoke、未登入 401、短效測試 JWT 與 Viewer 403，真人登入/Portal SSO 仍待測試帳號；T-008 文件同步完成
- 驗證狀態：部分通過；HTTPS binding、mixed content、JWT、401、Viewer 403 已通過，真人登入/Portal SSO 仍待外部條件
- 發布狀態：已發布 SPC 測試站 frontend/backend 檔案並建立 HTTP/HTTPS 測試單站；2026-10-12 已刷新測試單站 frontend production build；正式站未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 開啟 `https://目標網址/` | 載入 Vue 首頁 | 2026-10-12：`SpcSingleTest` 新增 `https/172.16.110.27:8901:` binding，PhysicalPath 仍為 `D:\SPC\release\test\backend`，AppPool `SpcSingleTest`；Playwright/Chromium 開啟 `https://172.16.110.27:8901/` 回 200，因 auth 啟用會導向 HTTPS Portal `https://172.16.110.27:8900/Login?ReturnUrl=%2FSpc%2FLaunch`。 | 通過 |
| AC-002 | 搜尋 build 後資產 | 不含 SPC API `http://IP:Port/api` | 2026-10-12：發現測試單站 active JS 仍為舊 testhost bundle 並含 `172.16.110.27:8081/api`；已重跑 production `npm run build`，備份 `release/test/frontend.backup-single-iis-t007-http-smoke-20261012-130624`，清除舊 bundle 後刷新 `release/test/frontend`。HTTPS 補驗時將 production `VITE_PORTAL_URL` 改為 `https://172.16.110.27:8900/Login`，再次備份 `release/test/frontend.backup-single-iis-t006-https-20261012-132549` 並刷新測試前端。`rg -l "http://login\.pmr\.com\.tw|http://172\.16\.|localhost:|/api/api" release\test\frontend\assets frontend\mes-spc-web\dist\assets` 無輸出。 | 通過 |
| AC-003 | `GET /api/version`、檢查匯出 URL | 不產生 `/api/api` | 2026-10-07：匯出 URL 改為以 API root 加 `/v1/reports/cpk-summary`，避免 baseURL `/api` 時產生 `/api/api`；`npm run build`、`npm run build:test` 通過。 | 通過 |
| AC-004 | 重新整理 Vue 子頁 | 仍載入 Vue app | 2026-10-12：`SpcSingleTest` 實站驗證 `http://172.16.110.27:8084/`、`/spc`、`/calibration-instruments`、`/particle-monitoring` 均回 200；`index.html` 指向 `/assets/index-DghC2Ifj.js`。 | 通過 |
| AC-005 | 登入、JWT、401、403 | 行為與既有一致 | 2026-10-12：`GET http://172.16.110.27:8084/api/version` 回 `{"version":"0.1.59","environment":"test"}`；未登入 `GET /api/v1/operators/directory-comparison` 回 401 Unauthorized。2026-10-12 補驗 HTTPS 單站短效測試 JWT：未登入 `PUT /api/v1/spc/point-remarks` 回 401；Viewer JWT `PUT /api/v1/spc/point-remarks` 回 403；Editor JWT `PUT /api/v1/spc/point-remarks` 回 400 validation error，代表已通過授權並進入 controller 驗證，且未寫入資料。`GET /api/v1/operators/directory-comparison` 使用 Editor JWT 回 502，訊息為 Portal 人員比對服務 401，代表 SPC JWT 已通過但 Portal directory 外部服務授權未通過。真人登入與 Portal SSO 仍待可用測試帳號。 | 部分通過 |
| AC-006 | 檢查部署前後設定 hash | SQL/JWT/SSO/SMTP/外部服務設定保留 | 2026-10-09：本輪未改 `release/test/backend`、IIS、appsettings、JWT/SSO/SMTP/SQL 設定或正式站；僅更新文件。設定 hash 未重算。 | 部分通過 |
| AC-007 | HTTPS 瀏覽器 Network | 無 mixed content | 2026-10-12：使用者提供 Portal HTTPS `https://172.16.110.27:8900/Login`；確認該 binding 屬 `PmrPortalWeb`，`SpcSingleTest` 另新增 `https://172.16.110.27:8901` 並沿用憑證 hash `6a24ea17c559069d29139c38c79fe246bdfdc6a2`。Playwright/Chromium 以 `ignoreHTTPSErrors` 開啟 `https://172.16.110.27:8901/`、`/spc`、`/calibration-instruments`、`/particle-monitoring` 皆回 200 並導向 HTTPS Portal，Network 中 `httpRequests` 皆為空；`/api/version` 回 200/test。PowerShell/curl 受 Windows Schannel `SEC_E_NO_CREDENTIALS` 限制無法作為此憑證的 HTTP 驗證工具。 | 通過 |
| AC-008 | 備份與 rollback 演練 | 可回復原分站台 | 2026-10-12：新增 HTTPS binding 前建立 IIS 備份 `SPC-single-iis-t006-https-20261012-132421`；測試前端備份 `release/test/frontend.backup-single-iis-t006-https-20261012-132549`。Rollback：移除 `SpcSingleTest` 的 `https/172.16.110.27:8901:` binding、執行 `netsh http delete sslcert ipport=172.16.110.27:8901`，或還原 IIS 備份；前端可還原該 backup 目錄。既有 `SpcApi:8081`、`SpcWeb:8083/8089`、`PmrPortalWeb:80/8900` 未移除。 | 通過 |

## 未執行項目、限制及後續
- T-001/T-002 僅完成分析與計畫，未改程式、未建置、未發布。
- 2026-10-07：T-003 已將前端 API base 預設改為相對 `/api`；T-004 已修正 CPK 匯出 URL 不再重複加 `/api`。
- 2026-10-07：T-005 已完成前端 production build、後端 Release publish 與單一站台 staging package 檢查；本機 staging `GET /api/version` 200/test、`GET /` 200、三個 Vue 子路由 200、`GET /assets/index-Bdeb9Yzx.js` 200 `text/javascript`。實際 IIS Site 設定與使用者登入 smoke 留待 T-006/T-007。
- 2026-10-07：T-006 已新增獨立 HTTP 測試單站 `SpcSingleTest` 於 `http://172.16.110.27:8084/`，並將 `release/test/frontend` 更新為 production build 使用同 origin `/api`。未改 `SpcApi:8081`、`SpcWeb:8083`，未發布正式站。
- 2026-10-12：T-006 已補上 HTTPS binding/cert 與 mixed content 驗證：`SpcSingleTest` 提供 `https://172.16.110.27:8901`，Portal 登入導向改為 HTTPS `https://172.16.110.27:8900/Login`。未操作正式站。
- 2026-10-07：測試站 smoke 首頁 200、新 JS 200、`8081/api/version` 200；但 `8081/api/version` 回 `environment=production`，列為 IIS/API 環境設定剩餘風險，非本次 T-003 修改範圍。
- 2026-10-09：T-007 可離線驗證項目已執行；HTTP 實站 smoke 因本機 helper 建立程序失敗未重跑；HTTPS mixed content 隨 T-006 外部環境阻擋。
- 2026-10-12：補驗發現 `release/test/frontend` active JS 為舊 testhost bundle，仍含 `172.16.110.27:8081/api`；已用 production build 刷新測試單站前端並清除舊 bundle。HTTP 實站 smoke 通過：首頁、`/spc`、`/calibration-instruments`、`/particle-monitoring` 皆 200，新 JS `text/javascript`，`/api/version` 200/test，未登入受保護 API 401。
- 2026-10-12：T-007 已補驗短效測試 JWT、401 與 Viewer 403；`demo/demo123` 於 Portal 測試登入頁回「帳號或密碼錯誤」，不再猜測帳密。T-007 不標示完成；待測試帳號後補驗真人登入與 Portal SSO。

## 基準與變更紀錄更新位置
- 本規格：`specs/20261005-single-iis-site/`
- 變更目的已記錄於 `ai_docs/10_change_log.md`。
- 2026-10-10 文件同步：`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`。
