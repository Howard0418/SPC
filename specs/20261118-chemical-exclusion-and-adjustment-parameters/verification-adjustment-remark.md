# PORTAL-CHEM-ADJUST-REMARK-1118-TASK-001 驗證

- SPC 核心：3/3 通過，覆蓋去除空白、保存重載、超過 500 字拒絕且原值不變。
- Portal API：288/288 通過；Portal Web：47/47 通過。
- SPC API、Portal API、Portal Web Release 建置均 0 warning / 0 error。
- 發布：Portal 測試 API `1.0.263`／Web `1.0.301`、SPC 測試 API `0.1.136`／Web `0.1.142`。
- 連線：Portal health 為 `database=ok`、`dbEnvironment=test`，SPC health/version 為 `0.1.136`、`test`；兩站首頁、JS、CSS 均 HTTP 200 且 MIME 正確，Portal 同源匿名 API 401 符合權限保護。
- 回復：SPC 備份 `D:\SPC\release\backup-adjust-remark-test-20261118-200701`。
- 人工驗收：待確認新增、修改、清空及重載備註；正式未授權、未發布。

## 2026-11-20 測試站複核

- 維護入口已移至 SPC 測試站 `/chemical-adjustment-parameters`。頁面及主 JS／CSS 均回 HTTP 200，資產 MIME 正確；同源 API `/api/health` 與 `/api/version` 均回 HTTP 200，版本 `0.1.156`、環境 `test`。
- 瀏覽器操作服務及 Windows 原生操作服務均不可用。登入後新增、修改、清空、重載及回復測試資料原值未執行，保留待人工驗收。本次未寫入資料或重新發布。
