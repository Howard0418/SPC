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
