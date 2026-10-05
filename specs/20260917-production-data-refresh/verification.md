# 驗證
- 唯讀 SQL 確認來源 PMR_SPC_2026：42 migrations、2,424 筆 VariableMeasurements、17 筆 Operators。
- 目標 PMR_SPC_TEST：50 migrations、1,838 筆 VariableMeasurements、23 筆 Operators、60 筆 CalibrationInstruments。
- 校正保管人 ID 為 27/28；多個其他人員 ID 在兩端代表不同人。
- 共同表新增欄位為 VariableMeasurements.SamplingStage；兩端 SpcReportSchedules.IsEnabled 皆 false。
- 尚未備份／寫入／測試／發布。此文件為盤點證據，不宣稱同步完成。
## 執行結果（取代前述未執行狀態）
- 使用者已核准保留測試人員、權限、通知及設備設定；同步 41 張共同業務表，18 張表保持原資料。清單：D:/SPC/release-staging/data-refresh-20260917/plan.json。
- 備份在 SQL Server 172.16.110.16：C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_TEST_before_refresh_20260917-110711.bak；COPY_ONLY/CHECKSUM 及 RESTORE VERIFYONLY WITH CHECKSUM 成功。未實際演練還原。
- 執行 tools/RefreshTestBusinessData.ps1 -Apply exit 0。先 app_offline，再備份；交易中來源共用表鎖維持一致性，只有測試庫資料被修改。
- 每張同步表共同欄位双向 EXCEPT 及筆數比對通過；所有相關外鍵／CHECK constraints WITH CHECK 重新啟用成功。
- 18 張保留表 JSON SHA256 比對不變，包含人員角色／權限、校正稽核／通知／設定及設備設定；校正保管人關聯無缺漏。
- 最終 VariableMeasurements=2424（由1838更新）；CalibrationInstruments=60、Operators=23、__EFMigrationsHistory=50 不變。正式資料無 SamplingStage，測試新欄位預設 GENERAL，驗證全部符合。
- API /api/version=200、environment=test、version=0.1.57；/health=200，測試前端 :8083=200。維護檔已移除。
- 先前規劃執行被本機 PowerShell 執行原則阻擋，未寫 DB；僅腳本程序使用 ExecutionPolicy Bypass，未變更系統原則。
- 無應用程式變更，因此未重跑先前單元測試、未建置或發布；未寄測試通知、未登入真人帳密。正式應用及 Portal 未修改。
- 回復：停止測試寫入，由上述完整備份僅還原 PMR_SPC_TEST 至原測試檔案位置；不可還原覆蓋 PMR_SPC_2026。此次無需回復。
