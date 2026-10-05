# 小型變更：隱藏校正管理列表保管人欄位
- 功能 ID：20260921-calibration-hide-custodian-column
- 狀態：已完成；測試站已發布
- 涉及專案：SPC Web

## 目的
從儀器校正到期管理列表移除「保管人」顯示欄位；保留主檔編輯、保管人收件通知與後端契約。

## 驗收
- AC-001：列表表頭不顯示「保管人」。
- AC-002：列表資料列不顯示保管人值，其他欄位順序與內容維持。
- AC-003：載入中／無資料列的 `colspan` 與欄位數一致。

## 驗證結果
- AC-001～AC-003：Vue 模板已調整，`npm run build:test` 通過。
- 發布：2026-10-05 已發布至 SPC 測試站前端 `D:\SPC\release\test\frontend`；備份 `release-staging/calibration-hide-custodian-20261005-075314/frontend-backup`。`http://172.16.110.27:8083/calibration-instruments` 載入新資產 `index-Cpses5Qc.js` 且資產 HTTP 200；API `8081/api/version` 回 `0.1.59/test`、`8081/health` HTTP 200。正式站未發布。
