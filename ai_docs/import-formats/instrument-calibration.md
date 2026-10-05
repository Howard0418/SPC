# 儀器校正 Excel 匯入契約

- 2026-09-16：[補齊規格欄位](../../specs/20260916-calibration-details/spec.md)。F=量測規格、G=精度、AB=備註、AC=校驗規範、AD=允收標準；表頭在第 5 列，每欄最多 2000 字。空欄可省略表頭，有資料必須對應正確表頭。公式讀儲存值並警示，錯誤/無儲存值拒絕。重匯仍略過既有編號，不覆寫。

- [SDD 主規格](../../specs/20260911-instrument-calibration-import/spec.md)、[驗證紀錄](../../specs/20260911-instrument-calibration-import/verification.md)
- 本機實作完成；尚未發布。本文件僅說明本次匯入，既有登入採 Portal SSO，沒有另外新增登入方式。

## API
JWT 登入後，啟用的 SPC Editor 且具 `calibration.manage` 才可匯入。未設定自訂頁面權限的 Editor 依既有預設允許；自訂空陣列則拒絕。每次操作以 Username 查目前人員狀態與權限。

| Method / 路由 | 輸入 | 輸出 |
|---|---|---|
| GET /api/v1/instruments/import/access | JWT | success/data.canManage/message |
| POST /api/v1/instruments/import/preview | multipart file (.xlsx ≤10 MiB) | success/data（hash、sheetName、rows）/message |
| POST /api/v1/instruments/import/commit | multipart file + options JSON | success/data（added、skipped、failed、rows）/message |

options 範例（hash 必須取自本次預覽，ID 必須為真實啟用保管人）：
```json
{
  "hash": "<preview.hash>",
  "selectedRows": [6, 8],
  "department": "品保",
  "custodianOperatorId": 1,
  "usageStatus": "Active",
  "includeCustodian": true
}
```
400：檔案/選項/hash/列號格式錯誤。401：未登入。403：無管理權限。409 且 data.failed > 0：選列資料驗證或交易失敗，本次新增已回復；查看 data.rows 原因後重新預覽。

## 來源與解析
工作表順序第一張；固定表頭 A5/B5/H5/I5/J5/Z4/AA4（忽略表頭換行空白），資料自第 6 列。A/B/H/I/J/Z/AA 分別為編號/名稱/放置地點/月週期/校驗方式/上次/下次。空列及已辨識制定日期頁尾不匯入。
直接讀 OOXML，支援共享字串及 1900/1904 日期，公式只讀 v 儲存值。日期支援完整西元年/月/日（-、/、. 分隔）；「未校」「預計:」「-」不轉成日期。上次真正空白可為 null。
週期僅接受「1次/N年」「1次/N月」（N 省略時為 1），移除空白後轉成 1～120 月。未校、預計、-、-- 日期先留空，匯入後由使用者自行建立，不把預計文字轉成到期日。免校／`--` 週期先以 12 月建檔並警示。無下次日期的儀器不列入到期摘要、不寄提醒。

## 交易與稽核
伺服器重新解析並比對 hash，不接受客戶端回傳任意主檔欄位。所選有效新儀器以同一交易寫入 CalibrationInstruments 與 CalibrationAuditLogs。唯一 Code 索引保護競爭寫入，競爭失敗整批回復後可重新預覽再試。
事件：InstrumentImported、InstrumentImportCompleted、InstrumentImportFailed；含檔名、SHA256、列號/結果、actor、UTC 時間。放置地點見 [20260912-calibration-location](../../specs/20260912-calibration-location/spec.md)；校驗方式見 [20260912-calibration-method](../../specs/20260912-calibration-method/spec.md)。無預覽檔持久儲存。不產生歷次校正結果/證書/通知。

## 前端與驗證
InstrumentImportPanel.vue 掛載於 InstrumentCalibrationsView.vue，透過 api/client.js 呼叫，權限不足隱藏入口。補齊欄位套用至勾選列；成功後刷新主檔與摘要。回傳失敗逐列展示，需重新預覽才能再次提交。
測試檔案：CalibrationImportTests.cs、CalibrationImportHttpTests.cs、calibration-import.spec.ts。HTTP 主機只使用隔離 SQLite，不執行產品 Program；介面測試全量 mock API，不能取代測試站端到端。
