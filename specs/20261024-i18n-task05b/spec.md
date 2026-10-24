# 功能規格：其餘基礎主檔三語
- ID：I18N-TASK-05B；版本：1；狀態：完成；主系統SPC。
- 授權：使用者「開始下一項」；基準：[主工作池](../20261022-i18n-task01/tasks.md)、[契約](../../docs/i18n/contract.md)。
## 範圍與現況
四個既有公開頁：ProcessesView（含機台/槽體及唯讀量測明細）、TraceabilityMasterView、ControlChartGroupsView的groupsOnly公開大類頁、SpcRuleGroupsView。翻譯固定標籤、提示/指南、placeholder、前端訊息、確認及顯示數量。復用LocalePicker供模態視窗內切換。
圖表小分類/公式types隱藏分支不在本次，留05C；不新增入口、不開放第九規則、不改WE八規則、機器碼或規則/公式JSON。資料名稱/API錯誤原文保留；API文案契約留TASK08。
## BDD驗收
|需求/驗收|Given / When / Then|
|---|---|
|R1/AC1|Given四頁，When選三語，Then公開固定顯示/標籤/前端提示依語言更新，資料名稱、WE與機器狀態/公式原文不變。|
|R2/AC2|Given已選製程/槽體、篩選與未送表單，When於原頁/視窗切語言，Then路由、DOM、草稿、選取值與查詢結果保留，不自動送出或重新取資料。|
|R3/AC3|Given各語言，When原有新增/編輯/刪除，Then原路徑、DTO/ID/boolean/number/列舉、槽體名稱正規化、規則JSON及必填旗標不變；線別仍唯讀來源，不增加規則。|
|R4/AC4|Given必填空白/非法JSON/409拒絕/刪除取消/HTML字串參數，When操作，Then原阻擋與草稿保留、參數純文字，前端訊息即時隨語言變化。|
|R5/AC5|Given三語及1920/1366/1280/390尺寸，When開模態表單/讀表，Then語言及儲存控制可達、長文字換行、表格可容器捲動；Viewer既有路由規則不變。|
## 非範圍與限制
無DB/API/權限/公式/規則引擎變更，無Portal/KM/TransFiles或正式發布。手機實測發現原sidebar壓縮主內容至無法操作；本批僅這四路徑沿用儀器校正頁既有上下堆疊導覽，不做全站改造。譯文draft，真人審核留TASK20。
