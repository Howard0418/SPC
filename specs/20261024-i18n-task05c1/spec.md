# 功能規格：I18N-TASK-05C1 圖表小分類與公式介面
- 版本：1
- 狀態：完成（保留分支；公開入口維持原狀）
- 日期：2026-10-24
- 授權依據：使用者「開始下一項」；依工作池單件執行。
- 主系統：SPC；相依系統：無；資料庫：無 schema/資料調整；發布：僅測試前端。
- 基準：[國際化契約](../../docs/i18n/contract.md)、[主任務](../20261022-i18n-task01/tasks.md)、05B 保留的小分類分支。

## 目的與範圍
05C 原包含管制項目、圖表小分類、線別分析與 F 表，拆成 05C1 圖表小分類/公式設定、05C2 線別分析/F表/公式歷程、05C3 管制項目/規格與排程；本次只執行 C1。
程式核對修正：公開 /control-chart-groups 固定 groupsOnly=true，小分類分支無公開入口，舊 /control-chart-types 轉往管制項目。本次為保留分支補繁中、簡中、英文，以不發布的隔離元件入口驗證；不恢復入口。翻譯固定標籤、提示、驗證及確認；資料名稱、代碼、公式 JSON 原文、enum、API DTO、固定 WE 八大規則維持基準。
不改公式引擎、權限、路由入口、資料存取；缺少大類別的顯示可翻譯，但原搜尋語意不可隨語言改變。

## BDD 與驗收
| ID | Given / When / Then |
|---|---|
| AC1 | Given 小分類清單及編輯器，When 切換三語，Then 固定文案更新、原名稱與 JSON 不變。 |
| AC2 | Given 未存表單與篩選，When 切語言，Then DOM/草稿/URL/篩選保留且無額外 API 寫入或重載。 |
| AC3 | Given 新增/編輯/刪除，When 儲存或確認，Then 原 enum、ID、抽樣數、JSON 與 DTO 完整維持；規則不能改寫。 |
| AC4 | Given 空白必填、非法 JSON 或 API409，When 儲存，Then 阻擋無效輸入，固定錯誤可切語言，API 原訊息保留。 |
| AC5 | Given 390/1280/1366/1920 寬度，When 三語編輯，Then 表單可操作並可捲至儲存。 |
| AC6 | Given 公開路由/Viewer、元件空清單、缺父類別，When 開頁或切語言，Then 既有權限/搜尋保留，空態/缺值顯示正確。 |

## 測試與回復
Happy Path：三語 CRUD DTO；Boundary：抽樣1/50、空態/長原名；Invalid Input：必填/非法JSON/API失敗；Regression：WE唯讀、搜尋與既有大類別分支、權限。
自動化：Node 字典/翻譯稽核與模板、Playwright 介面及 request interception、測試 build、發布站靜態 payload 與匿名 health/version。
瀏覽器寫入以 mock 攔截，不代表真 SSO/真資料庫驗收。翻譯為 draft，真人審核留 TASK20。
Rollback：發布前備份測試 frontend 並驗 hash，回復備份 index 與靜態檔；保留環境設定、不重啟後端。無正式發布。

## 未決問題
無阻擋問題。

- 2026-10-24 規格v1補充：首輪路由瀏覽器定位證實 types 隱藏，改用獨立建置元件測試；發布站只驗公開分支/權限及既有導向，不能宣稱公開可操作小分類。
