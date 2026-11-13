# 驗證紀錄

- SPC `HistoryTests`：5/5 通過；新增 Receipt `Primary=8.5` 回傳、同階段多版本採最新 VersionNo，以及 `PortalDailyDate=11/09` 不受 `MeasuredAt=11/10` 影響的日期回歸。
- Portal `chemical-effective-ui.cjs`：12/12 通過，新增查詢列顯示 `titrationValue=9` 測試。
- SPC Release build：0 警告、0 錯誤；Portal Web build：0 警告、0 錯誤。
- 正確來源證據：`2026-11-09.xlsx`、`N1 (開)`、第7列為滴定值`8.5`、公式結果`19.040`，業務鍵`2587|2026-11-09|OPEN|OPEN`；ID92273的`9／20.160`在既有對帳證據中屬報表日11/10，不得列入11/09。
- SPC 測試 API 最終 DLL：2026-11-13 13:38:04；備份 `backend.backup-history-titration-20261113-133805`；`/api/health`、`/api/version` 均成功且為 `0.1.127／test`。
- Portal 測試 API 1.0.241／Web 1.0.282 已發布；正式站未發布。
- 2026-11-13 收尾重驗：SPC 首頁、主 JS／CSS 均 HTTP 200 且 MIME 正確；`/api/health`、`/api/version` 回 `0.1.127／test`。Portal Login、主 JS／CSS 均 HTTP 200 且 MIME 正確；瀏覽器同站代理連線正常，內部 API `/health` 回資料庫環境 `test`，Web `/app-version` 回 `1.0.282.0`。
- 限制：電腦操作原生管道不可用；匿名查詢端點依設計回 401，登入後實際畫面由使用者重新整理驗收。
- 進度表：原檔已備份為`進度表-藥液查詢日期更正前-20261113-1340.xlsx`後更新；OpenXML驗證0錯誤、重複Print_Area為0，渲染檢查通過，原路徑重讀雜湊`47C4931EB6E56BB87EB97271ACB55904E44D5DE49502426585F7B2E3FFC08CB6`。核心未完成39件、SPC相關15件不變。
