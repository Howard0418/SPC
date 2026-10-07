# 驗證紀錄
- 功能 ID：20261005-single-iis-site
- 規格版本：1
- 日期／環境：2026-10-05／D:\SPC 工作區
- 實作狀態：尚未修改產品程式；已完成 TASK-001/TASK-002 文件
- 驗證狀態：待執行
- 發布狀態：未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 開啟 `https://目標網址/` | 載入 Vue 首頁 | 2026-10-07：新增 IIS 測試單站 `SpcSingleTest`，binding `http/172.16.110.27:8084:`，PhysicalPath `D:\SPC\release\test\backend`，AppPool `SpcSingleTest`；`GET http://172.16.110.27:8084/` 回 200 且引用 production 前端資產。HTTPS 目標網址待提供 binding/cert 後驗證。 | 部分通過 |
| AC-002 | 搜尋 build 後資產 | 不含 SPC API `http://IP:Port/api` | 2026-10-07：production build 通過並發布至 `release/test/frontend`；檢查該目錄無 `172.16.119.140:8081/api`、`172.16.110.27:8081/api`、`172.16.110.27:8082/api`、`localhost:5243`、`/api/api`。 | 通過 |
| AC-003 | `GET /api/version`、檢查匯出 URL | 不產生 `/api/api` | 2026-10-07：匯出 URL 改為以 API root 加 `/v1/reports/cpk-summary`，避免 baseURL `/api` 時產生 `/api/api`；`npm run build`、`npm run build:test` 通過。 | 通過 |
| AC-004 | 重新整理 Vue 子頁 | 仍載入 Vue app | 2026-10-07：`SpcSingleTest` 實站驗證 `http://172.16.110.27:8084/spc`、`/calibration-instruments`、`/particle-monitoring` 均回 200 且載入同一組 Vue 資產。 | 通過 |
| AC-005 | 登入、JWT、401、403 | 行為與既有一致 | 尚未部署 | 未執行 |
| AC-006 | 檢查部署前後設定 hash | SQL/JWT/SSO/SMTP/外部服務設定保留 | 尚未部署 | 未執行 |
| AC-007 | HTTPS 瀏覽器 Network | 無 mixed content | 2026-10-07：目前僅建立 HTTP 測試單站 `http://172.16.110.27:8084/`；尚未有 HTTPS binding/cert，無法驗證瀏覽器 mixed content。 | 待確認 |
| AC-008 | 備份與 rollback 演練 | 可回復原分站台 | 2026-10-07：新增前已建立 IIS 備份 `20261007T131619`；測試前端備份 `release/test/frontend.backup-single-iis-t006-20261007-131757`。Rollback：刪除 `SpcSingleTest` site/app pool，或還原 IIS 備份；前端可還原該 backup 目錄。既有 `SpcApi:8081`、`SpcWeb:8083` 未改動，仍指向 `release/production`。 | 部分通過 |

## 未執行項目、限制及後續
- T-001/T-002 僅完成分析與計畫，未改程式、未建置、未發布。
- 2026-10-07：T-003 已將前端 API base 預設改為相對 `/api`；T-004 已修正 CPK 匯出 URL 不再重複加 `/api`。
- 2026-10-07：T-005 已完成前端 production build、後端 Release publish 與單一站台 staging package 檢查；本機 staging `GET /api/version` 200/test、`GET /` 200、三個 Vue 子路由 200、`GET /assets/index-Bdeb9Yzx.js` 200 `text/javascript`。實際 IIS Site 設定與使用者登入 smoke 留待 T-006/T-007。
- 2026-10-07：T-006 已新增獨立 HTTP 測試單站 `SpcSingleTest` 於 `http://172.16.110.27:8084/`，並將 `release/test/frontend` 更新為 production build 使用同 origin `/api`。未改 `SpcApi:8081`、`SpcWeb:8083`，未發布正式站。
- 2026-10-07：T-006 尚未完成 HTTPS mixed content 驗證；需確認 HTTPS 目標網址、憑證與 binding 後，才能將 AC-007 與 T-006 標示 DONE。
- 2026-10-07：測試站 smoke 首頁 200、新 JS 200、`8081/api/version` 200；但 `8081/api/version` 回 `environment=production`，列為 IIS/API 環境設定剩餘風險，非本次 T-003 修改範圍。

## 基準與變更紀錄更新位置
- 本規格：`specs/20261005-single-iis-site/`
- 變更目的已記錄於 `ai_docs/10_change_log.md`。
