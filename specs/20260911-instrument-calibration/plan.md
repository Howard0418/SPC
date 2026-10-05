# 技術計畫
- 功能 ID：20260911-instrument-calibration
- 規格版本：4.1
- 規格：[spec.md](spec.md)
- 狀態：本機實作完成；正式 DB／寄信／發布不在本次

## 系統責任與受影響檔案
SPC API／Web 為唯一來源與排程；Portal 代理摘要與首頁卡片。本輪對齊：`CalibrationRules.ShouldScan`／`CountSummary`、排程不因 `DeliveryEnabled` 停止掃描、摘要 `windowDays`、前端台北日與月底週期。

## 實作方式與需求對應
見規格 R-001～R-016。寄信僅在 `Calibration:DeliveryEnabled=true` 時連 SMTP；預設 false。

## 驗證安排
見 [verification.md](verification.md)。本輪已跑校正單元測試與四專案建置。

## 發布、備份及回復
本次不適用正式發布。測試站已於 2026-09-11 發布（備份 `20260911-140728`）。
