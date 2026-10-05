# 驗證紀錄（2026-09-17）
## 核心
- [IdentityTests.cs](../../tests/MesSpc.Identity.Tests/IdentityTests.cs) 先建立再實作。修正前 7 fail/5 pass；修正後發現軟刪除 global filter 邊界，改為配對時包含停用/刪除記錄並拒絕登入；最後 12/12 通過。
- 指令：`dotnet test tests/MesSpc.Identity.Tests --no-restore --nologo -v quiet`，exit 0。SQLite 記憶體 DB 自動釋放。
- AC-001：預建工號 Username 空白/工號、AD/網域/UPN/工號交替、合法首次建檔、缺工號不重設既有 code；ID/角色/頁面權限保留。
- AC-002：停用/軟刪除不能重建，AD 與工號分屬不同資料列不自動合併，不同 canonical AD 衝突拒絕。
- AC-003：失敗後 legacy 重試不能新建，未簽章工號/部門不採用；已存在同 AD 的 legacy 可登入。
- AC-004：nonce 重放/簽章錯誤拒絕，併發首次登入只有一筆。SQL Server 用 transaction-owned sp_getapplock，程序內 SemaphoreSlim 防止同程序並發競態。
## 實際 SQL Server/HTTP
- 測試目標 PMR_SPC_TEST / 8081；建立唯一臨時工號、Username NULL、Viewer 與單一頁面權限。
- 3 次交替（domain AD、工號、UPN）及 2 個同時發出的 AD/工號 POST /api/v1/auth/portal-sso 全部 200，回同一原 ID/canonical AD、Viewer 與原頁面權限；SQL 僅一筆。
- finally 依本次 Id+唯一工號+CreatedBy 清理 1 筆；測試前後 Operators 都是 23 筆，未改真實使用者。HMAC/JWT 不输出保存、沒有使用真人密碼。
- 第一次 smoke 指令發生 PowerShell 引號解析錯誤，尚未執行資料寫入；改用 SQL 參數後上述 smoke 通過。
## 發布
- API `dotnet publish -c Release --no-restore` 成功，git diff --check 通過。
- 核對 IIS SpcApi=D:/SPC/release/test/backend，AppEnvironment=test、PMR_SPC_TEST。備份 `backend.backup-sso-single-operator-20260917-105440`。
- 首次檔案複製遇鎖定，重設 app_offline 並等待釋放後完成同次發布。最終 DLL 與 stage hash 相同、appsettings*.json/web.config 與備份 hash 相同，offline 已移除；version 200/test。
- 無 migration；Portal、前端及正式站未發布。測試通過後無無關重跑。
## 限制與待確認
- 真人 AD/工號密碼登入未執行；驗證完整已簽 SSO 請求至真實測試 API。
- 已存在兩筆不同 AD/工號時仍需確認同人與保留資料，不能猜測合併。ID1027 Daniel 與 ID1062 daniel_teng 已詢問使用者，尚未收到答覆；歷史資料合併未執行，不標示整體歷史去重完成。
- 只讀查看 Portal 的簽章/legacy 重試流程，未修改 Portal。舊版客戶端首次建檔會被拒絕，現有完整格式可正常建立。
- SDD/基準/索引/變更同步及本次 Markdown 相對連結檢查通過；原工作區其他變更保留，未提交 Git。
## 同人確認後唯讀盤點
- 使用者已確認 Daniel/daniel_teng 同人，取代上方「尚未收到答覆」狀態。
- 確認兩筆角色一致、頁面授權不同；權限選擇待回覆，尚未合併。
- 相關結構另有 CalibrationInstruments.CustodianOperatorId，以及 CalibrationInstruments、SpcReportSchedules、SpcAlertNotificationSettings 的 RecipientOperatorIdsJson；實際引用筆數待資料整理規格階段核對。
