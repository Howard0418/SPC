# 驗證紀錄
- 功能 ID：SPC-CHEM-PRECISION-1113-TASK-001
- 狀態：已完成（正式站未發布）

| 驗收 ID | 驗證 | 狀態 |
|---|---|---|
| AC-001 | `PreviewReturnsConfiguredDecimalPlaces`、`node --test tests/chemical-effective-ui.cjs` | 通過：4 位公式回傳與 10 項 Portal 回歸 |
| AC-002 | Portal 批次與單筆 payload 依公式回傳／項目設定小數位格式化 | 通過：不再固定 `toFixed(3)`，4 位不遺失 |
| AC-003 | Portal 載入歷史初驗／複驗與最終值 | 通過：既有 2 筆載入回歸以 3 位顯示，保存原始證據不改寫 |

## 建置與測試站

- SPC API：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -v:q` 及聚焦 xUnit 1/1 通過。
- Portal Web：`dotnet build src/PmrPortal.Web/PmrPortal.Web.csproj --no-restore -v:q` 通過；Node UI 10/10 通過。
- 測試站：SPC backend 已發布至 `D:\SPC\release\test\backend`；Portal API 1.0.234、Web 1.0.275 已發布。Portal `/Login` 與 CSS／JS 均回 200 且 MIME 正確；Portal 實際 API `/health`、SPC `/api/health`／`/api/version` 均為 test 成功。預覽端點匿名回 401，符合權限保護。
- 正式站：未發布；無 schema migration、無歷史量測資料修改。
