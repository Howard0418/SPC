# SPC-ADJUST-1028-TASK-003 驗證紀錄

- 2026-10-29：更新測試環境入口根網址與登入回跳預設值為 `https://portaltest.pmr.com.tw/`，保留 `/Spc/Launch`。
- `npm run build -- --mode testhost` 通過；i18n 檢查通過，Vite 建置完成。
- 已套用 `release/test/frontend`，備份 `release/test/frontend.backup-entry-url-20261029-123607`。
- Playwright 受既有 `localhost:5173` 工作階段占用，未能啟動獨立 runner；以來源檢查、建置及 bundle 產物驗證。
- 正式環境未修改。
