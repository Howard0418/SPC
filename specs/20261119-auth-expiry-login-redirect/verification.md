# 驗證紀錄

- 日期：2026-11-19
- 自動化：過期 Token 導登入與短暫 401 後成功回應取消導向，共 2 項 E2E 通過。
- Build：三語系 2,864 鍵檢查及 Vite test build 通過。
- 測試發布：SPC API `0.1.140`、Web `0.1.146`，Git `d32a6e0d`。
- 實站：Web 首頁 200；主 JS `application/javascript`、CSS `text/css`；API `/api/health`、`/api/version` 200 且為 test；未登入 Overall API 回 401，符合本次導向觸發條件。
- 備份：`D:/SPC/release/backup-auth-expiry-test-20261119-20261119-092948`。
- 正式狀態：未發布，待人工驗收及另行授權。
- 回復：以備份目錄回復 `release/test` API／Web，重新啟動 `SpcApiTest` 集區。
