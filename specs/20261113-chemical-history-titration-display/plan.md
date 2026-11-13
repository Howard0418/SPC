# 計畫

- SPC：擴充 `api/processes/{id}/measurements`，以 Receipt 優先、UploadDetail 回退回傳初驗／複驗滴定值。
- 日期一致化：Portal 日報資料的查詢、總筆數與顯示日期一律採 `PortalDailyDate`；非日報舊資料才回退 `MeasuredAt`，避免 UTC 時刻把 11/10 資料列到 11/09。
- Portal：查詢表格顯示初驗／複驗滴定值，保留班別／階段篩選與分析值三位顯示。
- 排序：共用新增頁 `getBatchMappings()` 的槽體 sequence、項目 sequence、名稱與 ID 穩定排序鍵；歷史日期仍由新到舊，不以量測 ID 決定同日項目順序。
- 驗證：SPC controller 回歸、Portal UI 回歸、Release build、測試站 health/version、測試庫唯讀對帳。
- 發布：只發布 SPC／Portal 測試站；正式站另行授權。回復可用前一測試 DLL／Portal publish 備份。
- 測試 API 最終重發使用 `deploy-test.ps1`，先核對 `test／PMR_SPC_TEST`、備份完整目錄，再離線替換非設定檔；失敗時由該備份回復。
