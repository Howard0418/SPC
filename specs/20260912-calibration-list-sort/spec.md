# 小型變更：儀器列表依編號排序
- 功能 ID：20260912-calibration-list-sort
- 版本：1
- 狀態：已完成
- 專案：SPC
- 授權依據：2026-09-12 使用者「以儀器設備編號為排序」
- 需求基準：[需求索引](../../docs/requirements.md)、[基準 3.6](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)

## 問題、預期行為與範圍
儀器校正管理列表原本先依下次校正日再依編號。已改為依儀器設備編號（Code）升冪排序。到期摘要、測試寄信選樣、匯入預覽列序不改。

## 需求與驗收
- R-001：GET /api/v1/instruments 依 Code 升冪排序。
- AC-001：到期日較早但編號較後的儀器排在編號較前的儀器之後。

## 計畫與任務
- 受影響檔案：InstrumentCalibrationsController.cs、CalibrationServiceTests.cs
- [x] T-001：先寫列表排序測試再改 OrderBy。
- [x] T-002：跑校正測試並發布測試站。
- [x] T-003：同步需求與變更紀錄。

## 驗證
- 方式、環境：InstrumentListIsOrderedByCodeNotDueDate；測試站 `app_offline` 確認 IIS 後發布 API
- 實際結果及證據：校正測試 82 通過；SPC API 建置 0 警告／0 錯誤；`8081/api/version` 200（test）；儀器列表未登入 401
- 發布狀態：已發布 SPC 測試站 API（備份 `20260912-162830`）；正式庫／正式站未發布
- 限制或未決問題：登入後畫面端到端待操作