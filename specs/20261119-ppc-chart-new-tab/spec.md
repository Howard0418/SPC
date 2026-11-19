# SPC-PPC-CHART-NEW-TAB-1119-TASK-001

## 需求
「SPC 管制項目設定與規格維護」清單的橘色管制圖按鍵，須在新分頁開啟對應管制圖或趨勢圖，原設定頁保留以便對照。

## BDD 驗收
- Given 使用者位於管制項目設定清單，When 點擊一般管制圖按鍵，Then 新分頁開啟 `/spc?ppcId={id}`，原頁不導頁。
- Given 項目為趨勢圖，When 點擊同一按鍵，Then 新分頁開啟 `/trend-chart?ppcId={id}`。
- 公式、編輯、刪除及警示等相鄰操作不變。

## 驗證範圍
- Happy Path：一般管制圖及趨勢圖各自開啟正確新分頁。
- Boundary：缺少或無法辨識圖別設定時沿用 `/spc` fallback。
- Regression：原清單 URL 不變，既有操作按鍵不受影響。
