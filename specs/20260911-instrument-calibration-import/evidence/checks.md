# 本機檢查結果（2026-09-12）

- 校正後端回歸：72/72 passed，failed=0，notExecuted=0，見 [TRX](calibration-import.trx)。
- 隔離瀏覽器：3/3 passed、exit 0，見 [Playwright 最後狀態](playwright-last-run.json)。桌面/手機畫面已人工視覺檢查；mobile 表格使用水平捲動，補齊欄位及確認操作可見。
- API：dotnet build backend/MesSpc.Api --no-restore --verbosity minimal → 0 警告、0 錯誤。
- Web：npm run build:test → 成功，2395 modules，輸出 index-BeD6UJF2.js / index-D61I7qhV.css；既有大 bundle 警示保留。
- 文件：主規格四份、匯入技術契約、需求索引及基準的 25 個本地連結全數存在；新 evidence 檔案另已確認存在。R-001～R-010 與 AC-001～AC-010 已對應 tasks/verification。
- 工作區：本次只新增/修改匯入相關檔案及 SDD/需求/技術索引；既有 migration 備份刪除、其他程式變更保留，不執行 git reset/checkout/clean。
- 原始 Excel SHA256：604CA8F6E6CC0D12ED64B45E2EEF6A13F09357E0C206405B2E790F74212E5B81；讀取前後 bytes 相同。
- 2026-09-12 已發布 SPC 測試站，備份 `20260912-145908`；正式庫／真信未操作。實際測試站登入及 SQL Server 端到端尚未執行。
