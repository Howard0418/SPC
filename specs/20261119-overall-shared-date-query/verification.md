# 驗證紀錄

- 2026-11-19 `npx playwright test tests/overall.spec.ts --reporter=line`：3 項通過（1366px、390px、日期邊界與錯誤處理）。
- 2026-11-19 `npm run build:test`：通過；i18n 2864 × 3 通過。
- 2026-11-19 `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：通過，0 警告、0 錯誤。
- 測試區已發布：API `0.1.141`、Web `0.1.147`，更新前為 API `0.1.140`、Web `0.1.146`。
- `https://spctest.pmr.com.tw/`、實際主 JavaScript 與 CSS 均為 HTTP 200，MIME 分別為 `application/javascript`、`text/css`；前台資產可見 Web `0.1.147`，未包含正式 API 位址。
- 瀏覽器實際使用的同源 `/api/health`、`/api/version` 均回應 API `0.1.141`／`test`；匿名受保護端點回 401，確認測試 Web／API 環境隔離正常。
- 正式區未授權、未發布。
