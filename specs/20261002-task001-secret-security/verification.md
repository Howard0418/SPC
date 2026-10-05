# TASK-001 驗證紀錄

## 結果

- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter FullyQualifiedName~SettingsControllerTests --no-restore`：2 通過。
- `npm run build`：通過；Vite 僅提示既有 bundle size warning。
- 範例設定檔 JSON：有效，JWT、SSO、SMTP、SQL 欄位均為 `__SET_VIA_ENV__`。

## 未完成的運維作業

- 現有測試站與正式站的實際 JWT、SSO、SMTP、資料庫密碼尚未輪替，需由部署管理者依環境注入並驗證。
