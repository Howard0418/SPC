# I18N-TASK-06A：分段版本與已保存計算證據三語
- 版本：1；日期：2026-10-26；狀態：DONE，已發布測試站；其餘06B～D未開始。
- 授權：使用者「開始下一項」；依[工作池](../20261022-i18n-task01/tasks.md)、[國際化契約](../../docs/i18n/contract.md)、[版本明細基準](../20261020-chart-segment-version-detail/spec.md)。

## 需求與範圍
- 主系統 SPC；只翻譯 ChartSegmentDetails.vue、ChemicalCalculationDetails.vue 共用唯讀介面的固定文字與 aria、參數化提示、公式標籤及統計狀態顯示。
- TASK-06 依規模拆 A 共用版本／證據、B 管制圖主頁、C 趨勢圖及統計顯示、D 警示顯示；本次只做 A，不將未翻譯主頁標示完成。
- API URL/query、分頁大小20、request generation 防舊回應、角色、原始備註／原因／人員／enum／JSON／公式／單位、數字與 Asia/Taipei 日期保持。查閱只讀已保存資料，不重算、不推定舊版本，不新增獨立版本或 DB schema。
- 保留舊量測無證據、多版本、子組／無量測ID等明示；固定AD、偏態／峰態已移除基準不變。本次不改任何計算引擎或圖表數列。
- Portal／TransFiles／後端／正式站不變。譯文 draft／uncertain，真人審稿留 TASK-20。
- 手機驗證發現原側欄將明細壓到約74px，固定內容溢出；僅 `/spc`、`/trend-chart` 沿用 App 既有上下堆疊導覽，兩頁其餘固定文字仍留後續批次。

## BDD 與測試矩陣
- AC1 Happy Path：Given 管制圖或趨勢圖有分段／歷史／證據，When 查閱並切三語，Then 固定介面及aria即時更新，既有分段／展開／分頁／量測選擇／DOM／URL保持且不額外查詢或寫入。
- AC2 Boundary：Given 0、null、空歷史／無迄日、非藥液、子組或無ID點，When 查閱，Then 原數值與語意保持，不推定版本或以第一筆代表子組；日期仍台北。
- AC3 Invalid Input：Given 原始公式／備註含HTML及i18n符號、JSON無法解析、API失敗或格式不符，When 顯示／切換／重試，Then 純文字保留原值、固定錯誤與空值提示採當下語言，API錯誤原文不翻譯。
- AC4 Regression：Given 切分段／主檔／量測及歷史分頁或慢請求，When 操作，Then 原GET query與20筆偏移及過期回應保護保持；所有測試不得寫真資料，不改統計／圖表結果。
- AC5：Given 三語×1920/1366/1280/390，When 展開明細，Then 文字可換行，控制項可達，公式JSON純文字，不造成共用明細水平溢出。

## 計畫與任務
- [x] T1：登錄規格與拆分工作池；修改前記目的。
- [x] T2：模板紅燈檢查、字典與來源draft ledger、兩元件顯示層實作。
- [x] T3：單元／build／三語瀏覽器及原分段行為回歸。
- [x] T4：備份發布測試站、發布後驗證、文件同步；提交上傳以本件Git紀錄為準，STOP。

## 發布與回復
必要驗證通過直接發布 SpcSingleTest 測試前端，先核 IIS 實際目錄與 AppEnvironment=test，API沿用。備份整份測試前端，資產先index最後；回復同目錄備份，不覆設定、不改正式站。
實際結果及限制見[驗證紀錄](verification.md)，未執行者不得勾選。
