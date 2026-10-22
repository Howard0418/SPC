# 計畫
- 先建立Controller實際Update回歸測試，重現線別被清空而409，再移除誤清空MachineId的單行，保留RequiresMachine必要性檢查。
- 不變更業務識別、唯一索引、schema、其他欄位規則或正式資料。
- Happy：修改LSL保留Machine；Boundary：原null/必填null；Invalid：真重複409；Regression：其他Controller測試。
- API測試/Release build後只發布SpcSingleTest測試backend，保護環境設定及資料；正式僅唯讀診斷。
- rollback：測試站備份回復原backend，保留appsettings/web.config與資料；無DB還原。
