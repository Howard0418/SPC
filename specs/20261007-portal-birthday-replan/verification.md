# Portal 生日通知調整與團保取消規劃驗證

功能 ID：PORTAL-BIRTHDAY-REPLAN-20261007  
日期：2026-10-07  
狀態：規劃完成

## 驗證
- Happy Path：已將新 Portal 生日調整拆成可逐一執行的小工作。
- Boundary Case：團保專區已標示取消，不再加入未完成工作池。
- Invalid Input：本次未讀取或異動 `D:\PmrPortal\GroupInsurance`，未修改 Portal 程式或資料庫。
- Regression Risk：保留舊完成紀錄於 `CHANGELOG_CUSTOM.md`，新需求以新規格覆蓋後續工作池，不改寫歷史紀錄。

## 結果
- AC-001：`TODO.md` 不再列團保專區新工作。
- AC-002：`TODO.md` 新增 `PORTAL-BIRTHDAY-REPLAN-TASK-001`。
- AC-003：`TODO.md` 新增 `PORTAL-BIRTHDAY-REPLAN-TASK-002`。
- AC-004：`TODO.md` 新增 `PORTAL-BIRTHDAY-REPLAN-TASK-003`。
- AC-005：本次僅文件與工作池更新，未發布。

