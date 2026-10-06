# SPC-POINT-FILTER 任務

## 階段 1：現況分析與規格

- [x] TASK-001：盤點現有管制圖/趨勢圖點位來源與排除機制。
- [x] TASK-002：將「已排除點清單與恢復入口」納入規格。
- [x] TASK-003：更新 TODO 小工作排序。

## 階段 2：後端基礎

- [x] TASK-004：建立單一圖點排除資料模型與 migration。
- [x] TASK-005：建立查詢/設定/恢復 API，含權限與稽核欄位。
- [x] TASK-006：管制圖計算套用單點排除狀態。
- [x] TASK-007：趨勢圖、直方圖、常態檢定套用單點排除狀態。

## 階段 3：前端互動

- [x] TASK-008：管制圖點位右鍵選單與 ExcludedVisible 樣式。
- [x] TASK-009：管制圖「已排除點」清單與隱藏點恢復。
- [x] TASK-010：趨勢圖點位右鍵選單與 ExcludedVisible 樣式。
- [x] TASK-011：趨勢圖「已排除點」清單與隱藏點恢復。

## 階段 4：驗證與發布

- [x] TASK-012：後端測試，涵蓋製程、藥液、Xbar 子組與趨勢圖 normality。
- [ ] TASK-013：前端 build / UI 測試。
- [ ] TASK-014：發布 SPC 測試站並 smoke test。
- [ ] TASK-015：同步 CHANGELOG_CUSTOM.md 與 TODO 完成紀錄。
