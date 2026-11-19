# 驗證紀錄

- Portal 回歸：`ChemicalMeasurementExclusionContractTests` 4/4 通過；API／Web Release build 通過。
- SPC API：`ChemicalPortalExclusionTests` 7/7 通過，確認固定備註只保存、不建立排除，並可從日報 payload 依 PPC 讀取。
- SPC Web：備註相關 Playwright 4/4 通過；繁中畫面另留本機驗證截圖 `test-remark-separated.png`（不納入版本庫）。
- i18n：三語 2,876 keys 檢查通過；Web test build 通過。
- 測試區：Portal API `1.0.284`／Web `1.0.323`；SPC API `0.1.152`／Web `0.1.158`。
- 連線：Portal 與 SPC 首頁、主要 JavaScript／CSS、health／version、瀏覽器實際 API 基底均成功，環境標籤為 `test`。
- 資料契約：固定下拉備註存 `SpcPointExclusion.Reason`；圖點自由備註存 `SpcPointRemark.Remark`，兩者互不覆蓋。
- 統計契約：固定備註點正常列入統計與繪圖；只有 SPC 人工排除影響統計。
- 資料驗證：固定原因啟用排除 0 筆、日報固定備註 10 筆可讀取、人工排除 1 筆仍啟用。
- 無 schema migration；正式區未發布。
- 依使用者既定規則，本件不更新進度 Excel。
