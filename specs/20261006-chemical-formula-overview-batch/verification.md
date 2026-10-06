# 藥液公式總覽與批次儲存頁驗證紀錄

## 2026-10-06 TASK-001/TASK-002 規格與最小方案

- 狀態：完成。
- 本階段只新增規格與計畫文件，未修改產品程式、未建置、未發布測試站。

### 已確認現況

- `ChemicalAnalysisOverviewView.vue` 已可顯示 CHEM 項目、篩選、匯出 Excel、導向單筆編輯。
- 總覽頁目前不能直接編輯公式或批次儲存。
- 單筆 `PUT /part-process-characteristics/{id}` 已整合 `ChemicalAnalysisFormulaVersionService`。
- 最小方案可先由前端逐筆呼叫既有 PUT，確保批次儲存也會留下版本紀錄。

### 待後續驗證

- 修改 1 筆公式後能儲存並新增版本紀錄。
- 修改多筆公式後只送出異動列，且每筆各自新增版本紀錄。
- 未修改時不送出。
- 前端 build 與測試站 smoke。
