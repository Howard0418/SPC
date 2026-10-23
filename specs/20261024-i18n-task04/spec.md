# 功能規格：SPC 語言切換試點
- 功能 ID：I18N-TASK-04；版本：1；狀態：完成
- 主系統：SPC；相依：TASK-02 共用契約、TASK-03 本機偏好限制。
- 授權：2026-10-24 使用者「開始下一項」。基準：[任務表](../20261022-i18n-task01/tasks.md)。
## 目的、現況與範圍
App.vue 共用導覽目前為固定文字，尚無 Vue I18n。本件加入共用 header、側欄、環境/角色顯示三語及本機選單；頁面內文由 TASK-05～07 分批處理。譯文為初稿，真人審查留 TASK-20，不宣稱全站完成。
不改 API、DB、登入/權限、公式、圖表計算、機器列舉、路由或 Portal/KM；不連接尚未實作的 Portal 偏好 API。
## 需求與 BDD 驗收
|需求|驗收|Given / When / Then|
|---|---|---|
|R-001|AC-001|Given 共用介面，When 切換 zh-TW/zh-CN/en-US，Then 標籤立即更新、html lang 更新；預設及缺 key 回繁中。|
|R-002|AC-002|Given 已開表單/查詢，When 切換，Then 路由、未送資料、頁面元件實例、查詢條件與圖表實例保留；焦點可停留語言控制，不強制重載。|
|R-003|AC-003|Given 本機已選語言，When 重載同站同環境同 SPC subject，Then 恢復；不同使用者/匿名/環境互不沿用。未知 locale 回繁中。|
|R-004|AC-004|Given storage 讀寫失敗，When 選語言，Then 本頁仍切換並顯示無法保存提示，不清表單。|
|R-005|AC-005|Given Viewer/Editor，When 切語言，Then 原機器 role、選單可見性及路由不變；參數以純文字插值。|
|R-006|AC-006|Given 1920/1366/1280/390 視窗及三語，When 操作 header，Then 語言控制可見可鍵盤操作，header 不溢出，選單文字可換行。|
## 邊界、風險與假設
本機 subject 使用現有登入回傳 Operators.Id；此 key 僅隔離顯示偏好，不是授權證據。已登入但缺合法 ID 僅本頁套用，不讀寫匿名偏好；未登入使用獨立 anonymous。不憑帳號猜 Portal 使用者。無跨分頁推播與雲端同步。詞彙為草稿，只發布測試。
