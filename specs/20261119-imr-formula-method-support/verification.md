# 驗證紀錄

- API 單元測試：`ImrChartCalculatorTests`、`XbarRChartCalculatorTests`、`XbarSChartCalculatorTests` 共 8 項通過。
- Web E2E：I-MR 適用公式、管制圖新分頁、IIS Updating 重試共 3 項通過。
- Web 測試建置與三語系 2,867 鍵檢查通過。
- 測試站 #2437（21 點）實測：
  - 樣本標準差法：UCL 100.42、CL 93.85、LCL 87.27。
  - 平均移動全距法：UCL 98.40、CL 93.85、LCL 89.30。
  - 測試後已回復原設定「樣本標準差法」。
- I-MR 無子組全距，因此介面不再顯示不適用的「標準全距法」；Xbar-R／Xbar-S 選項不變。
- 測試區版本：API `0.1.148`、Web `0.1.156`；首頁、主 JS、API health/version、同源 API 與 `test` 環境標籤通過。
- 正式區未授權，未發布。
- 依目前規則，本次不更新進度 Excel。

## 畫面

- `../20261119-control-limit-formula-verification/actual-2437-moving-range-after-fix.png`
- `../20261119-control-limit-formula-verification/actual-2437-chart-after-fix.png`
- `../20261119-control-limit-formula-verification/actual-2437-imr-options-after-fix.png`
