# 計畫：儀器主檔顯示放置地點
- 功能 ID：20260912-calibration-location
- 規格版本：1

## 受影響
`CalibrationInstrument.Location`、migration `20260912080000_AddCalibrationInstrumentLocation`、匯入解析 H 欄、儀器列表／表單／預覽。

## 實作
可選 nvarchar(100)。匯入讀 H5=放置地點。部門仍由使用者指定。測試站發布時由既有 `Migrate()` 套用測試庫；不發正式站。

## 驗證
校正單元／匯入測試；API 建置；前端 Playwright mock；測試站 smoke。
