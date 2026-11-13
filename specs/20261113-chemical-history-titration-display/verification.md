# 驗證紀錄

- SPC `HistoryTests`：4/4 通過，新增 Receipt `Primary=8.5` 回傳測試，並確認同階段多版本採最新 VersionNo。
- Portal `chemical-effective-ui.cjs`：12/12 通過，新增查詢列顯示 `titrationValue=9` 測試。
- SPC Release build：0 警告、0 錯誤；Portal Web build：0 警告、0 錯誤。
- 測試資料庫唯讀核對：11/09 16:00 N1 七筆 Receipt Primary 為 `9、12.7、6.7、0.205、5.5、14.6、0.1`；P200 `12.7 → 151.638`。
- SPC 測試 API 最終 DLL：2026-11-13 13:30:28；備份 `backend.backup-history-titration-20261113-133115`；`/api/health`、`/api/version` 均成功且 environment=`test`。
- Portal 測試 API 1.0.240／Web 1.0.281 已含查詢欄位；正式站未發布。
- 2026-11-13 收尾重驗：SPC 首頁、主 JS／CSS 均 HTTP 200 且 MIME 正確；`/api/health`、`/api/version` 回 `0.1.126／test`。Portal Login、主 JS／CSS 均 HTTP 200 且 MIME 正確；瀏覽器同站代理連線正常，內部 API `/health` 回資料庫環境 `test`，Web `/app-version` 回 `1.0.281.0`。
- 限制：匿名查詢端點依設計回 401；登入後實際畫面由使用者重新整理驗收。
