# 小型變更：移除查詢列常態檢定方法區塊
- 功能 ID：SPC-UI-1103-NORMALITY-FIELD
- 版本：1
- 狀態：完成
- 專案／授權：SPC前端；使用者明確指定測試站與正式站均更新。

## 問題、預期行為與範圍
- 使用者截圖以黃色框標示SPC即時互動管制圖查詢列的「常態檢定方法／Anderson-Darling」區塊，要求移除；量測值趨勢圖同樣移除。
- 僅刪除兩頁查詢列的靜態顯示區塊。API仍送`normalityMethod=AD`，圖表、常態性結果、P-value、查詢條件及其他功能保持。
- 不改後端、資料庫、公式、F值、權限、Portal或IIS設定。

## 需求與驗收
- R-001：`/spc`查詢列不顯示`normality-method`區塊。
- R-002：`/trend-chart`查詢列不顯示`normality-method`區塊。
- R-003：兩頁圖表請求仍固定傳送`normalityMethod=AD`，結果常態性資訊仍可見。
- AC-001 Given 兩頁已載入，Then 查詢列無該區塊，其他篩選欄與查詢按鈕存在。
- AC-002 Given 執行圖表查詢，Then API參數仍為AD，圖表及常態判定正常。
- AC-003 測試與正式實站首頁／JS／CSS、health/version、瀏覽器實際API基底及兩頁畫面驗證通過。
- Boundary：桌面與390px畫面；Invalid Input：既有無足夠資料／無變異訊息保持；Regression：多語切換不重查或重建圖表。

## 計畫與任務
- [x] 先更新兩頁UI回歸測試並確認舊版失敗。
- [x] 最小刪除兩個模板區塊，版本升至0.1.115。
- [x] 執行相關Playwright、i18n檢查及production/test build。
- [x] 發布測試與正式前端，逐站驗證並同步文件與原進度表。
- 回復：以各站發布前備份還原前端靜態檔；不涉及資料回復。

## 完成摘要
- 2026-11-03：兩頁查詢列靜態區塊已移除；`normalityMethod=AD`、常態結果與圖表行為保持。
- 26項Playwright回歸通過，測試與正式隔離建置通過；兩站首頁、主JS／CSS、health/version與瀏覽器兩頁驗收通過，詳[驗證紀錄](verification.md)。
- 原進度表已備份後取代：新增`SPC-UI-1103-TASK-001`完成紀錄與11/03原生里程碑；核心未完成35件、SPC直接13件不變。
