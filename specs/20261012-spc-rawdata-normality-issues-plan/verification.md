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

- `SPC-RAWDATA-1012-TASK-003`：此 Task 執行時尚未處理；已於下節補驗完成。

## SPC-RAWDATA-1012-TASK-003：Raw data 補上規格資訊

日期：2026-10-13

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-003 | 通過 | Raw data CSV 已新增 `SpecLSL`、`SpecTarget`、`SpecUSL`、`Unit` 欄位，值來自下載目標 `PartProcessCharacteristic` 的 `LSL`、`TargetValue`、`USL`、`Unit`，與圖表 limits 使用同一 mapping 來源。 |

### 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter RawDataCsvExporterTests --no-restore -p:UseSharedCompilation=false`：3 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。

### 測試站發布與 smoke

- Publish：`dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -p:UseSharedCompilation=false -o release-staging\rawdata-spec-task003\backend` 通過。
- 發布範圍：SPC 測試站 backend。
- 備份：`release/test/backend.backup-rawdata-spec-20261013-074946`。
- 發布目標：`release/test/backend`。
- Playwright/Chromium HTTPS smoke：
  - `https://172.16.110.27:8901/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/api/version`：200，`{"version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/`：200，`text/html`

### 未執行／後續

- Raw Data 三個小工作已完成；常態檢定、直方圖與咬蝕 X- 仍依 `TODO.md` 後續排序執行。

## SPC-NORMALITY-1012-TASK-001：常態檢定與 Minitab 差異確認

日期：2026-10-13

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-004 | 通過 | 後端 `NormalityTestResult` 新增 `MethodNote`，明確揭露 SPC 採 Adjusted Jarque-Bera 偏態/峰態檢定，Minitab 常用 Anderson-Darling，兩者 p-value 與判定不可直接等同；管制圖與趨勢圖直方圖 tooltip/卡片皆顯示方法說明。 |

### 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter NormalityTest --no-restore -p:UseSharedCompilation=false`：5 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。首次與測試平行執行遇 `obj` 檔案鎖，單獨重跑後通過。
- `npm run build`：通過，僅既有 Vite chunk size warning。

### 測試站發布與 smoke

- Publish：`dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -p:UseSharedCompilation=false -o release-staging\normality-task001\backend` 通過。
- 發布範圍：SPC 測試站 backend/frontend。
- 備份：
  - `release/test/backend.backup-normality-task001-20261013-080810`
  - `release/test/frontend.backup-normality-task001-20261013-080810`
- Playwright/Chromium HTTPS smoke：
  - `https://172.16.110.27:8901/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/api/version`：200，`{"version":"0.1.59","environment":"test"}`
  - `https://172.16.110.27:8901/`：200，`text/html`

### 未執行／後續

- 本 Task 不把 SPC p-value 調整為 Minitab Anderson-Darling；差異以方法揭露處理。若未來要求完全對齊 Minitab，需另開統計方法替換或新增檢定 Task。
- `SPC-HISTOGRAM-1012-TASK-001` 仍待執行。
