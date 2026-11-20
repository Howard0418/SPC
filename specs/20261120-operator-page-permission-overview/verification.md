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

## 2026-11-20 直接顯示調整

- Playwright 確認無需點擊即可看見「共 1 項」及權限名稱，且沒有「點此查看」。作業人員回歸共 18 項通過，原有三語系「儲存失敗後重送」3 項仍失敗。
- `npm run build:test` 與三語系 2,881 鍵檢查通過。以隔離來源建置，未納入工作區內的趨勢圖改動。
- SPC 測試 Web `0.1.166` → `0.1.167`，API `0.1.156` → `0.1.156`。首頁與主 JS／CSS 均 HTTP 200 且 MIME 正確，測試站主資產含新版號。
- 已發布資產的 Chromium 路由驗證確認同源 `/api`，並用 Chromium 對該實際基底確認 health／version 均 HTTP 200、環境 `test`，未登入 `/api/operators` 回預期的 401。使用者端 `:8901` 也確認版本及環境。
- 待登入目視現行名單。正式站未發布。Web 回復可使用 `release/test/frontend.backup-operator-permissions-direct-20261120` 的 `index.html` 切回既有前版資產，並重驗 Web 與 API。
