# 小型變更：校正提醒天數簡化設定
- 功能 ID：20260915-calibration-reminder-picker
- 版本：1
- 狀態：測試站已發布
- 專案／授權：SPC；使用者要求提醒天數換簡單方式。
- 基準：[校正通知](../20260911-instrument-calibration/spec.md)。

## 問題、預期行為與範圍
將逗號文字改成常用天數勾選（30、14、7、3、1 天及到期當天），可新增自訂整數天數、取消勾選及恢復預設。保留既有自訂值，API 與通知業務規則不變。
## 需求與驗收
- R-001／AC-001：載入原設定、勾選與自訂天數後，儲存正確數字陣列；保留非預設天數。
- R-002／AC-002：拒絕空選取與無效自訂值；預設仍為 30、7、0。
- R-003／AC-003：手機可捲動操作，原測試寄信功能維持。
## 計畫與任務
- [x] T-001：修改 InstrumentCalibrationsView.vue。
- [x] T-002：Playwright 驗證與 build:test。
- [x] T-003：更新需求紀錄，核對 IIS 後發布測試站前端。
## 驗證
- AC-001：Playwright 驗證載入 45 天、加入 60 天、勾選 14 天，PUT 數字陣列正確。
- AC-002：366 拒絕、空選取拒絕、恢復 30/7/0 通過。
- AC-003：390px 手機截圖檢視通過，原測試 Email 與匯入回歸通過；6 項案例皆回報 ok。API 使用 mock，未寄真信。
- build:test 成功；既有 bundle 大小警告。
- 發布：IIS SpcWeb 指向 D:/SPC/release/test/frontend；備份 release-staging/reminder-picker-20260915-110645/frontend。頁面及新 JS HTTP 200，首頁引用新資產已確認。正式站未發布。
