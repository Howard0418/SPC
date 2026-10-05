# 驗證紀錄
- 功能 ID：20260912-calibration-method
- 實作狀態：已完成
- 驗證狀態：校正測試與介面 mock 通過；測試站 smoke 通過；登入後畫面端到端待操作
- 發布狀態：已發布 SPC 測試站（備份 `20260912-161409`）；正式庫／正式站未發布

| 驗收 | 方式 | 結果 | 狀態 |
|---|---|---|---|
| AC-001 | SavesAndClearsCalibrationMethod、ReadsCalibrationMethodAndRejectsOverlongValue | 可存可清空；超長拒絕 | 通過 |
| AC-002 | 列表／表單／匯入預覽欄位；Playwright 預覽可見校驗方式 | 畫面已加欄；Playwright 4 通過 | 通過（程式） |
| AC-003 | ReadsFirstByRelationship、SelectedOnlyAndReplay | J 欄寫入外校；重匯略過不覆寫；週期仍讀 I 欄 | 通過 |

- `dotnet test tests/MesSpc.Calibration.Tests` 81 通過。
- Playwright 4 通過。SPC API 建置 0 警告／0 錯誤。
- 測試站：`app_offline.htm` 確認 IIS 指向 `D:\SPC\release\test`（8081 回 503）；`/api/version` 200（0.1.57／test）；儀器／匯入／摘要未登入 401；Web `8083/calibration-instruments` 與新資產 200；測試庫已有 `CalibrationMethod` 欄與 migration `20260912083000_AddCalibrationInstrumentCalibrationMethod`。