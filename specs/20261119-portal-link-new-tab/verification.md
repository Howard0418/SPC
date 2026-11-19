# 驗證紀錄

- 2026-11-19 `node --test tests/shell-auth-controls.test.mjs`：1 項通過；入口網站連結具 `target="_blank"`、`rel="noopener noreferrer"`，且不再呼叫原頁導向函式。
- `npm run build:test`：通過；i18n 2864 × 3 通過。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：通過，0 警告、0 錯誤。
- 測試區已發布：API `0.1.142`、Web `0.1.148`，更新前為 API `0.1.141`、Web `0.1.147`。
- 測試首頁、實際 JavaScript／CSS 均為 HTTP 200 且 MIME 正確；資產可見 Web `0.1.148` 與測試 Portal 網址，未包含正式 API 位址。
- 同源 `/api/health`、`/api/version` 回應 API `0.1.142`／`test`；匿名受保護端點回 401。
- 正式區未授權、未發布。
