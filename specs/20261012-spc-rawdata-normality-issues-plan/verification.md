# SPC Raw Data 與常態檢定異常驗證紀錄

## SPC-RAWDATA-1012-TASK-001：All 總表後 raw data 下載上下文修正

日期：2026-10-12

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-001 | 通過 | 新增 `resolveActiveRawDataPpcId`，下載 raw data 時依序解析 `ppcId`、總表列 `partProcessCharacteristicId`、舊欄位 `ppcId`、選單 mapping id；保護 All 總表 drilldown 後仍可取得單一管制項目 ID。 |

### 自動化驗證

- `node --test frontend/mes-spc-web/tests/spc-raw-data-download.test.mjs`：5 passed。
- `npm run build`：通過；僅保留既有 Vite chunk size warning。

### 測試站發布與 smoke

- 發布範圍：SPC 測試站 frontend。
- 備份：`release/test/frontend.backup-rawdata-context-20261012-172446`。
- 發布目標：`release/test/frontend`。
- 資產檢查：active JS `/assets/index-VGtDwk3O.js` 不含 `172.16.110.27:8081/api`、`localhost:`、`/api/api`。
- Playwright/Chromium HTTPS smoke：
  - `https://172.16.110.27:8901/api/version`：200，`application/json; charset=utf-8`
  - `https://172.16.110.27:8901/`：200，`text/html`
  - `https://172.16.110.27:8901/assets/index-VGtDwk3O.js`：200，`text/javascript`

### 未執行／後續

- `SPC-RAWDATA-1012-TASK-002`：Raw data `Value` 日期誤判與項目資料正確性尚未執行。
- `SPC-RAWDATA-1012-TASK-003`：Raw data 補上規格資訊尚未執行。
- Windows PowerShell/curl 對測試 HTTPS 憑證仍遇 `SEC_E_NO_CREDENTIALS`，本次沿用 Playwright `ignoreHTTPSErrors` 做 HTTPS smoke。

## SPC-RAWDATA-1012-TASK-002：Raw data Value 日期誤判與項目資料正確性

日期：2026-10-13

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-002 | 通過 | 後端 CSV 組裝抽出 `RawDataCsvExporter`，日期欄位 `MeasuredAt`、`PortalDailyDate` 才套用日期格式化；`Value` 欄保留原始 invariant 字串，不再被 `DateTime.TryParse` 轉成日期。 |
| AC-002 補充 | 通過 | `RawDataCsvExporter` 只輸出目標 `PartProcessCharacteristicId` 或未帶 ppcId 的列，避免其他管制項目資料混入下載結果。 |

### 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter RawDataCsvExporterTests --no-restore -p:UseSharedCompilation=false`：2 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 第一次測試曾與 build 平行執行撞到 `obj\Debug\net10.0\MesSpc.Api.MvcApplicationPartsAssemblyInfo.cs` 檔案鎖；已改為單獨重跑並通過。

### 測試站發布與 smoke

- Publish：`dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -p:UseSharedCompilation=false -o release-staging\rawdata-value-task002\backend` 通過。
- 發布範圍：SPC 測試站 backend。
- 備份：`release/test/backend.backup-rawdata-value-20261013-074103`。
- 發布目標：`release/test/backend`。
- Playwright/Chromium HTTPS smoke：
  - `https://172.16.110.27:8901/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/api/version`：200，`{"version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/`：200，`text/html`

### 未執行／後續

- `SPC-RAWDATA-1012-TASK-003`：Raw data 補上規格資訊尚未執行。
