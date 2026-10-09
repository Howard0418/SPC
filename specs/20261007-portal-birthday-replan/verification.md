# Portal 生日通知調整與團保取消規劃驗證

功能 ID：PORTAL-BIRTHDAY-REPLAN-20261007  
日期：2026-10-07  
狀態：TASK-002 已實作並完成本機驗證

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
- 2026-10-09 TASK-001：Portal 權限管理已補回出生年月日欄位；規格與驗證紀錄位於 `D:\PmrPortal\specs\20261009-birthday-permissions-replan\`。
- 2026-10-09 驗證：Portal `AdminUsersControllerTests|AuthControllerTests` 11 passed；Portal API/Web build 0 warnings / 0 errors。
- 2026-10-09 發布：已發布 Portal 測試站 API/Web；Smoke：API health 200、權限管理頁未登入 401；正式站未發布。
- 2026-10-09 TASK-002：Portal 人事專區已新增生日通知設定頁；規格與驗證紀錄位於 `D:\PmrPortal\specs\20261009-birthday-notification-settings\`。
- 2026-10-09 TASK-002 驗證：Portal `HumanResourcesBirthdayNotificationSettingsControllerTests|HumanResourcesUsersControllerTests|AdminUsersControllerTests` 7 passed；Portal API/Web build 0 warnings / 0 errors。
- 2026-10-09 TASK-002 發布：已發布 Portal 測試站 API/Web；Smoke：API `/health` 200/test、新設定頁未登入 401、設定 API 未登入 401；正式站未發布。

