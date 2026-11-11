# 測試站發布備份權限
- 功能 ID：`SPC-RELEASE-BACKUP-PERMISSION-1111-TASK-001`
- 日期：2026-11-11
- 狀態：完成；測試 API 0.1.126 已發布，暫時 `dbcreator` 已撤除
- 授權：使用者明示授予發布維護身分 `BACKUP DATABASE` 權限。

## 範圍與風險
- 只處理測試 SQL Server 的 `PMR_SPC_TEST`，不觸及 `PMR_SPC_2026`。
- 先由發布站台現行連線確認實際 SQL principal；只授予固定資料庫角色 `db_backupoperator`，不授予 `sysadmin`、`db_owner`、資料寫入或其他資料庫權限。
- 權限授予後，以 `HAS_PERMS_BY_NAME('PMR_SPC_TEST','DATABASE','BACKUP DATABASE')` 驗證，再重試既有受控發布工具。
- 回復：若需撤回，於 `PMR_SPC_TEST` 執行 `ALTER ROLE [db_backupoperator] DROP MEMBER [實際principal]`；不得刪除登入或其他角色。

## BDD 驗收
```gherkin
Scenario: 發布維護身分取得測試庫備份權限
  Given 發布工具連線的實際SQL principal已確認
  When 管理員將該principal加入PMR_SPC_TEST的db_backupoperator
  Then BACKUP DATABASE權限檢查為1
  And PMR_SPC_2026及其他角色不變

Scenario: 重新執行受控發布
  Given 測試庫備份權限有效
  When 重新執行既有SPC Test Apply
  Then COPY_ONLY CHECKSUM及RESTORE VERIFYONLY完成後才可套用
  And 失敗時不接續正式發布
```

## 驗證與發布
- Happy Path：測試備份、VERIFYONLY、API126套用及health/version/test標籤通過。
- Boundary：已是角色成員時不重複授權；只驗證測試庫。
- Invalid：principal不存在、無ALTER ROLE權限或目標資料庫不符時停止。
- Regression：確認兩個測試AppPool恢復原狀；正式站與正式資料庫不連線、不變更。

## 執行結果
- 實際發布 principal 為`SpcAuditApiTest003`；授權前不是`db_backupoperator`且`BACKUP DATABASE=0`。
- 使用既有維護身分只連至`PMR_SPC_TEST`，將該principal加入`db_backupoperator`；重新以應用程式身分驗證角色成員與`BACKUP DATABASE`均為1，未連線`PMR_SPC_2026`。
- 原release ID不可重用；以原候選ZIP雜湊驗證後建立R2，Plan通過。R2確實產生近一小時內的測試庫COPY_ONLY備份，但Apply仍於`backup-database`停止、newFiles=0。
- 對該最新備份以應用程式身分單獨執行`RESTORE VERIFYONLY ... WITH CHECKSUM`，SQL錯誤262／class14，證明`db_backupoperator`只足以備份，不足以執行發布器要求的還原驗證。
- 未授予`dbcreator`、`CREATE ANY DATABASE`或`sysadmin`，未套用API126，未執行正式SPC發布。下一步需另行核准暫時較高權限，或由專用維護principal負責備份／VERIFYONLY。
- 使用者後續明示授權：可將`SpcAuditApiTest003`暫時加入SQL Server `dbcreator`，完成測試發布及VERIFYONLY後立即移除；禁止授予`sysadmin`。執行必須使用finally撤權，並以應用程式身分驗證`IS_SRVROLEMEMBER('dbcreator')=0`。
- 依上述授權執行 R3：加入前狀態為`dbcreator=0／sysadmin=0／BACKUP DATABASE=1`；加入期間只取得`dbcreator`，`sysadmin`始終為0。
- R3 已完成`COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY`及測試站套用；發布證據`status=completed／phase=complete／verified=true`，兩個測試 AppPool 皆為`Started`。
- 發布後`https://spctest.pmr.com.tw`首頁、主要 JavaScript／CSS、`/api/health`及`/api/version`均成功；API版本`0.1.126`、環境`test`，資產 MIME 分別為`application/javascript`及`text/css`。
- `finally`已立即移除暫時`dbcreator`；應用程式身分最終狀態`PMR_SPC_TEST|SpcAuditApiTest003|dbcreator=0|sysadmin=0|backup=1`。未連線或異動`PMR_SPC_2026`。
