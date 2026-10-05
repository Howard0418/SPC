# 驗證紀錄
- 功能 ID：20260912-calibration-location
- 實作狀態：本機完成
- 驗證狀態：單元／介面通過；測試站發布後再看實際匯入畫面
- 發布狀態：待發布測試站

| 驗收 | 方式 | 結果 | 狀態 |
|---|---|---|---|
| AC-001 | SavesAndClearsPlacementLocation、OverlongLocation | 可存可清空；超長標錯 | 通過 |
| AC-002 | 列表／表單欄位 | 本機畫面已加欄 | 通過（本機） |
| AC-003 | ReadsPlacementLocation、SelectedOnlyAndReplay | H 值寫入且部門仍為指定「品保」 | 通過 |

- `dotnet test tests/MesSpc.Calibration.Tests` 79 通過。
- Playwright 4 通過。SPC API 0 警告／0 錯誤。
