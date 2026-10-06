# SPC-POINT-FILTER 驗證紀錄

## 2026-10-06 TASK-001 規劃與現況分析

- 狀態：完成。
- 本階段未修改產品程式、未新增 migration、未發布測試站。

### 已確認現況

- `UploadBatches.IsExcluded` 已存在，`SpcController.ToggleExcludeUploadBatch` 會切換整批上傳資料排除狀態。
- `SpcService.GetInteractiveChartAsync` 會依 excluded upload batch 將 `SpcDataPoint/Subgroup/AttributeDataPoint.IsExcluded` 設為 true。
- I-MR、Xbar-R、Xbar-S、Attribute chart 與 normality 目前已有排除 `isExcluded` 的基礎邏輯。
- 管制圖前端已有點位明細按鈕 `剔除此數據/恢復此數據`，但呼叫的是整批 batch exclude，不是單點 exclude。
- 趨勢圖會呈現 `isExcluded` 點位狀態，但沒有右鍵選單、沒有設定/恢復操作。
- 現有系統沒有 `ExcludedHidden` 狀態，也沒有「已排除點」清單供隱藏點恢復。

### 風險

- Xbar-R/Xbar-S 圖上一點可能代表多筆 raw samples；實作前需固定「排除圖上子組點」的識別方式。
- 既有整批排除與未來單點排除可能同時存在；第一版應讓整批排除優先，避免單點恢復誤解除整批排除。
- 隱藏點若沒有清單入口，使用者會無法恢復，因此恢復入口須先於或同時於隱藏功能完成。

### 驗證

- 文件內容檢查：完成。
- 程式建置：不適用，本階段未修改程式。
- 測試站發布：不適用，本階段未修改程式。
