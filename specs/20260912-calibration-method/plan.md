# 計畫：儀器主檔顯示校驗方式
- 功能 ID：20260912-calibration-method
- 對應規格：v1

## 影響範圍
`CalibrationInstrument.CalibrationMethod`、migration `20260912083000_AddCalibrationInstrumentCalibrationMethod`、匯入解析 J 欄、列表／表單／匯入預覽。

## 作法
可選 nvarchar(100)。匯入讀 J5=校驗方式。只當文字保存，不改週期或免校判定。測試站啟動時 `Migrate()` 套用測試庫；正式庫與正式站不發布。

## 驗證
校正單元測試與匯入測試；API 建置；前端 Playwright mock；測試站 smoke。