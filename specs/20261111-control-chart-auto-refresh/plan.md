# 技術計畫

- 功能 ID：`SPC-CONTROL-CHART-AUTO-REFRESH-1111-TASK-001`
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- 前端共用工具：新增同源 PPC 圖表刷新通知與訂閱器。
- `PartProcessCharacteristicsView.vue`：僅在 PUT／POST 成功後通知。
- `SpcChartView.vue`、`TrendChartView.vue`：僅在目前 PPC 相符時重載既有圖表資料。

## 實作方式與需求對應

1. 將 PPC ID 標準化為正整數，使用 CustomEvent 處理同頁、BroadcastChannel 處理同源分頁，storage 處理通道不支援情境。
2. 儲存成功後發布，失敗時不發布。
3. 圖表頁掛載時訂閱、卸載時解除；事件 PPC 與現況不相符則不查詢。
4. 沿用 `loadInteractiveChart`／`loadUploadBatchChart`，不新增 API 或改寫計算。

## API、檔案格式及資料模型

不變更 API、資料庫或資料格式。

## 相容性與風險

- 通知僅同源本機瀏覽器，不傳送量測或公式內容，只含 PPC ID。
- 通知支援失敗時安全降級，不阻斷保存。

## 驗證安排

- AC-001～003：共用工具測試、前端靜態／行為測試、production build。
- 測試站發布後：實際儲存一項公式，確認既開圖表的管制線與 API 請求更新。

## 發布、備份及回復

測試通過後發布 SPC 測試站。正式發布仍須使用者另行授權；回復為前一版前端資產，不涉及資料庫或資料回復。
