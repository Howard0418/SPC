# 技術計畫
- 規格：[spec.md](spec.md)
- Entity/Input/API/列表/表單/匯入 DTO 新增 MeasurementSpecification、Precision、Remarks、CalibrationStandard、AcceptanceCriteria（nullable nvarchar(2000)）。
- 先建 service/parser/backfill 測試，沿用 EF migration 及既有 UI mock；snapshot 先備份、排除非本案差異。
- 專用 EF 工具先 dry-run，比對原 Excel/測試庫後只填空白，記錄 hash 與 before/after；批次交易。
- 驗證：校正測試、API publish、前端 build:test、相關 Playwright、手機/桌面檢視。
- 核對 IIS release/test 與 PMR_SPC_TEST 後備份發布。既有 config、private-data 保留，不改投遞開關，不發通知。
- 回復：還原前後端備份；新增 nullable 欄保留，補值前資料保存稽核，必要時逐筆依稽核回復。正式站不發布。
