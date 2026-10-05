# 計畫
1. 依 [規格](spec.md) 確認人員同步邊界，建立明確資料表清單，不自動擴大範圍。
2. 保存兩端版本、筆數及測試保留資料驗證值；建立測試庫 COPY_ONLY 完整備份並驗證可讀。
3. 暫停測試 API 寫入；在交易中同步批准的共同欄位，保留測試新增 SamplingStage 欄位規則（既有正式資料為 GENERAL），不改 schema。
4. 檢查資料筆數／內容、外鍵完整性、保留資料不變；失敗 rollback，必要時還原測試備份。
5. 恢復測試服務並檢查 API；同步 SDD 與需求索引、變更紀錄。無程式變更不建置發布。
## 待盤點
實際資料表同步清單、快照一致性、通知隔離、備份檔名與可用空間，執行前確認。

## 執行方式定案
使用 tools/RefreshTestBusinessData.ps1 產生明確共同資料表清單。排除 Operators、__EFMigrationsHistory、SpcReportSchedules、SpcAlertNotificationSettings、EquipmentPointMappings、ChameleonSourceSettings，測試獨有表全部保留。先 COPY_ONLY 備份並 VERIFYONLY；交易內來源共享鎖取得一致資料，所有目標共同欄位做雙向 EXCEPT 與筆數核對，保留表做 JSON SHA256 比對，外鍵重新 WITH CHECK 驗證。僅測試庫短暫 app_offline；任何檢查失敗交易 rollback。備份路徑與實際結果記錄於 release-staging/data-refresh-20260917。
