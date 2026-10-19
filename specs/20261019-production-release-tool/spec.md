# 正式版更新工具
ID：RELEASE-PRODUCTION-TOOL-20261019；狀態：實作及隔離驗證完成；本次正式機執行不適用。
主系統 SPC；相依 Portal；僅實作工具，不部署正式站、不修改 DB schema。
既有非正式工具保留 Production 阻擋，新增獨立正式入口；不碰 D:\Sites\PmrPortal。
## 需求與 BDD
- R1/AC1 Given 已建立交付 manifest When Plan Then 核對實際主機、IIS、固定目標、版本/來源 commit、SHA256、檔案安全及 smoke 定義，唯讀不停止服務。
- R2/AC2 Given 明確正式發布授權與資料庫備份權限 When Apply Then 先停寫、網站/IIS/SQL COPY_ONLY CHECKSUM 備份及 VERIFYONLY，全部成功才套用；migration 僅執行經 hash 與授權核對的 EF SQL。
- R3/AC3 Given 套件含環境設定、私有目錄、路徑穿越或 hash 不符 When 驗證 Then 在停止服務前拒絕。
- R4/AC4 Given 更新或 smoke 失敗 When 回復 Then 回復此次覆寫的應用檔與原池狀態，新增檔列入保留、不刪業務資料；DB 不自動 Down/還原，回復失敗保持維護且明確回報。
- R5/AC5 Given 正式授權或 DB 授權缺失 When Apply Then 拒絕；使用固定環境設定且保護 secrets 不輸出。
- R6/AC6 Given Rollback When 指定同批備份 Then 檢查備份 hash、主機/目標一致；禁止使用不同 manifest、重複 release ID 或覆寫備份。
## 驗證規劃
Happy：隔離檔案套用/回復與適配器測試；Boundary：空 migration/多站、原池停止；Invalid：缺欄位/假環境/惡意 ZIP/備份失敗；Regression：原型正式阻擋、設定保留、401 不誤判、失敗恢復。
本次只用測試替身與暫存資料驗證 IIS/SQL 邏輯，正式機演練不屬本次授權；不得宣稱現場演練完成。

## 介面與回復限制
交付工具由 Export-ProductionMigrationReview、New-ProductionReleaseBundle、Invoke-ProductionRelease 與 ProductionRelease 模組組成；產生 manifest 作為範本，不提交實際交付 ZIP／SQL／catalogue。完整來源 migration catalogue 與正式 pending 必須恰等於已授權列表；API／Portal Web 額外驗證 DLL 版本。備份目錄限 Administrators／SYSTEM，原池狀態恢復，前後版本 smoke 分開。新增應用檔保留並列證據、不自動清除；migration 必須先確認向後相容，工具不自動 Down 或整庫還原。
