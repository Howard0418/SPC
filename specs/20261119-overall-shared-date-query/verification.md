# 驗證紀錄

- 2026-11-19 `npx playwright test tests/overall.spec.ts --reporter=line`：3 項通過（1366px、390px、日期邊界與錯誤處理）。
- 2026-11-19 `npm run build:test`：通過；i18n 2864 × 3 通過。
- 2026-11-19 `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：通過，0 警告、0 錯誤。
- 測試區已發布：API `0.1.141`、Web `0.1.147`，更新前為 API `0.1.140`、Web `0.1.146`。
- `https://spctest.pmr.com.tw/`、實際主 JavaScript 與 CSS 均為 HTTP 200，MIME 分別為 `application/javascript`、`text/css`；前台資產可見 Web `0.1.147`，未包含正式 API 位址。
- 瀏覽器實際使用的同源 `/api/health`、`/api/version` 均回應 API `0.1.141`／`test`；匿名受保護端點回 401，確認測試 Web／API 環境隔離正常。
- 2026-11-19 經使用者授權發布正式區：API `0.1.134` → `0.1.135`、Web `0.1.140` → `0.1.141`。
- 正式 Web 首頁、主 JavaScript／CSS 均回 HTTP 200 且 MIME 正確；正式 API `/api/health`、`/api/version` 回應 `0.1.135`／`production`。
- 正式部署備份與回復點：`C:\ProgramData\PMR-Releases\Production\SPC-FOUR-20261119-111729`；無 DB migration 或正式資料異動。
