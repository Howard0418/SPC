# 作業人員頁面權限總覽驗證

- 日期：2026-11-20
- 工作：`SPC-OPERATOR-PAGE-PERMISSIONS-1120-TASK-001`
- 結果：程式、自動測試及 SPC 測試站發布完成；待使用者登入目視驗收。

## 驗證結果
- Playwright `operator list shows resolved page permission names`：1/1 通過，確認顯示權限項數、展開後顯示目錄名稱，且名稱以純文字呈現。
- 三語系目錄：2,881 鍵 × 3 語系通過。
- `npm run build:test`：通過；使用 testhost mode，主資產不含正式 API 主機或 `:8443`。
- 測試站：首頁、主 JavaScript、CSS 均 HTTP 200，MIME 分別為 `text/html`、`application/javascript`、`text/css`。
- 同源 `/api/health`、`/api/version` 與使用者端非標準埠 `:8901` 均回 API `0.1.156`、environment `test`；前台資產含 Web `0.1.165`。
- 發布前備份：`release/test/backend.backup-operator-page-permissions-20261120`、`release/test/frontend.backup-operator-page-permissions-20261120`。

## 注意事項
- 原 `i18n-settings` 既有「儲存失敗後重送」三語系案例受新增的稽核 activity request 影響仍失敗，與本次列表唯讀欄位無關；本次新增驗收案例及 build 均通過，未將舊案例宣稱通過。
- 正式站未發布；無資料庫或既有權限資料異動。
