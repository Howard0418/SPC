# 計畫

- SPC：擴充 `api/processes/{id}/measurements`，以 Receipt 優先、UploadDetail 回退回傳初驗／複驗滴定值。
- Portal：查詢表格顯示初驗／複驗滴定值，保留班別／階段篩選與分析值三位顯示。
- 驗證：SPC controller 回歸、Portal UI 回歸、Release build、測試站 health/version、測試庫唯讀對帳。
- 發布：只發布 SPC／Portal 測試站；正式站另行授權。回復可用前一測試 DLL／Portal publish 備份。
- 測試 API 最終重發使用 `deploy-test.ps1`，先核對 `test／PMR_SPC_TEST`、備份完整目錄，再離線替換非設定檔；失敗時由該備份回復。
