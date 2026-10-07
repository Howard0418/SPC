# 驗證紀錄
- 功能 ID：20261005-single-iis-site
- 規格版本：1
- 日期／環境：2026-10-05／D:\SPC 工作區
- 實作狀態：尚未修改產品程式；已完成 TASK-001/TASK-002 文件
- 驗證狀態：待執行
- 發布狀態：未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 開啟 `https://目標網址/` | 載入 Vue 首頁 | 尚未設定單一 IIS Site | 未執行 |
| AC-002 | 搜尋 build 後資產 | 不含 SPC API `http://IP:Port/api` | 2026-10-07：production build 通過；檢查 `frontend/mes-spc-web/dist` 無 `172.16.119.140:8081/api`、`172.16.110.27:8082/api`；source 預設為 `/api`。現行測試站仍為 Web/API 分離，testhost 發布暫保留 `172.16.110.27:8081/api` 至 T-006。 | 通過 |
| AC-003 | `GET /api/version`、檢查匯出 URL | 不產生 `/api/api` | 尚未修改與部署 | 未執行 |
| AC-004 | 重新整理 Vue 子頁 | 仍載入 Vue app | 尚未部署 | 未執行 |
| AC-005 | 登入、JWT、401、403 | 行為與既有一致 | 尚未部署 | 未執行 |
| AC-006 | 檢查部署前後設定 hash | SQL/JWT/SSO/SMTP/外部服務設定保留 | 尚未部署 | 未執行 |
| AC-007 | HTTPS 瀏覽器 Network | 無 mixed content | 尚未部署 | 未執行 |
| AC-008 | 備份與 rollback 演練 | 可回復原分站台 | 尚未部署 | 未執行 |

## 未執行項目、限制及後續
- 本次只完成分析與計畫，未改程式、未建置、未發布。
- 2026-10-07：T-003 已將前端 API base 預設改為相對 `/api`；T-004 手動組 URL 仍保留未執行。
- 2026-10-07：測試站現況仍為 Web `8083`、API `8081` 分離，`8083/api/version` 回 SPA 首頁；因此測試站發布用 `testhost` env 暫保留絕對 API base，待 T-006 單一 IIS Site 設定後再切成同 origin `/api`。
- 2026-10-07：測試站 smoke 首頁 200、新 JS 200、`8081/api/version` 200；但 `8081/api/version` 回 `environment=production`，列為 IIS/API 環境設定剩餘風險，非本次 T-003 修改範圍。

## 基準與變更紀錄更新位置
- 本規格：`specs/20261005-single-iis-site/`
- 變更目的已記錄於 `ai_docs/10_change_log.md`。
