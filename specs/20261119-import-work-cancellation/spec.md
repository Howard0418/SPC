# 匯入資料相關小工作取消

- 日期：2026-11-19
- 範圍：SPC／Portal／TransFiles 狀態同步
- 程式、資料庫、網站發布：不適用

## 使用者決定

使用者確認下列9項匯入資料工作不再執行；保留既有程式、測試、發布、稽核與歷史資料，不刪除、不回復、不啟用排程，也不進行後續正式匯入或人工驗收：

1. `TRANSFILES-CHEM-OPERATOR-FALLBACK-1118-TASK-001`
2. `TRANSFILES-PROD-1028-TASK-003`
3. `TRANSFILES-PROD-1028-TASK-004`
4. `TRANSFILES-PROD-1028-TASK-005`
5. `TRANSFILES-PROD-1028-TASK-006`
6. `TRANSFILES-PROD-1028-TASK-007`
7. `TRANSFILES-PROD-1028-TASK-008`
8. `TRANSFILES-ETCH-1103-TASK-001`
9. `SPC-CHEM-HISTORY-JUDGMENT-1104-TASK-001` 尚未執行的 T-005／T-006

## BDD

- Given 上述工作已有部分完成紀錄，When 標記取消，Then 保留完成證據但不再列入未完成工作。
- Given 正式匯入、排程或資料補正尚未執行，When 取消，Then 不執行正式資料寫入、排程啟用或網站發布。
- Given 未來需要恢復，Then 必須由使用者重新明確授權並建立新的工作／風險檢查，不自動恢復舊工作。
