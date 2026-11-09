# 小型變更：稽核紀錄顯示目標與安全異動摘要
- 功能 ID：SPC-AUDIT-1103-TASK-009-SUMMARY
- 版本：1
- 狀態：測試站API0.1.116與效能驗收完成，待登入畫面驗收；正式未發布
- 依據：使用者 2026-11-09 實際驗收指出無法辨識誰輸入什麼、誰調整哪位使用者的權限。

## 問題、預期行為與範圍
查詢 API 目前省略已保存的 allowlist `DetailsJson`，目標只顯示型別／ID。新增安全摘要 DTO，僅交付已存在且通過契約的欄位名稱、權限代碼、狀態、版本、筆數及日期，不交付原始 JSON、密碼、權杖、完整表單、搜尋值或敏感原值；Operator 目標補目前可辨識姓名／代碼並保留 ID。

## BDD 驗收
- Given 權限事件保存 `PermissionCodes` 與目標 Operator ID，When 有 `admin.audit.read` 的使用者查詢，Then 顯示操作者、目標姓名／代碼／ID及「權限設為」清單。
- Given 一般資料異動只保存 `ChangedFields`／`RecordCount`，When 查詢，Then 顯示異動欄位與筆數，不虛構原值。
- Given Detail 含非 allowlist 或格式錯誤，When 查詢，Then 不回傳該內容且查詢仍安全完成。
- 查詢／匯出權限、分頁、篩選、原稽核資料及正式站均不變。

## 任務與驗證
- [x] 先建立安全摘要、目標辨識、原始 JSON 不外洩測試。
- [x] 實作 API DTO、Xlsx 與前端「異動摘要」欄。
- [x] 後端6項、前端5項、三語系2,822鍵及build通過；測試API0.1.112／Web0.1.123已發布，首頁、JS／CSS MIME、health/version及test環境通過。正式另授權。
- [x] 同步文件、TODO、變更紀錄及原進度表。
- [ ] 登入後查詢／匯出、異動摘要與遮罩截圖仍待畫面驗收。

## 發布與限制
- 備份：`release/test/backend.backup-audit-summary-20261109-144928`、`frontend.backup-audit-summary-20261109-144928`；私有設定以系統管理權限補備份且雜湊一致。
- 回復：用上述備份回復測試backend/frontend；不需資料回復或migration。
- 現有歷史事件只能顯示「權限設為」完整清單，因原事件未保存新增／移除差異；資料輸入只顯示既有安全欄位／筆數，不能補造原始值。
