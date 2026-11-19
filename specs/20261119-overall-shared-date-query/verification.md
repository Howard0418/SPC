# 驗證紀錄

- 2026-11-19 `npx playwright test tests/overall.spec.ts --reporter=line`：3 項通過（1366px、390px、日期邊界與錯誤處理）。
- 2026-11-19 `npm run build:test`：通過；i18n 2864 × 3 通過。
- 2026-11-19 `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：通過，0 警告、0 錯誤。
- 測試區發布與連線驗證待完成；正式區未授權、未發布。
