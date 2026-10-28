# 月報異常處置件數
- SPC-ADJUST-1028-TASK-005；版本1；狀態：測試完成，正式待授權。使用者同意004後接005；004[測試紀錄](../20261028-monthly-category-sheets/verification.md)已完成並上傳。
- 主系統SPC；無Portal／schema／角色／正式資料變更。僅發布測試站，正式另授權；009 F保持待確認。

## 需求與BDD
- AC-005：Given同期間分類內有OOC、OOS或主副圖重疊異常，When查詢月報或匯出／排程，Then每個案件只計一次，顯示「異常件數」「已處置件數」「未處置件數」，已＋未＝異常。各分類工作表皆保留欄位，空表明示無資料。
- 沿用OverallStatistics的目前查詢時點狀態：Closed／legacy ack已處置，Open／InProgress／無告警未處置；子組需所有相關告警完成。不是月底歷史狀態，畫面及匯出註明。
- AC-FULL：Given1001點以上或500筆以上告警，When查月報，Then讀完整期間，包含末筆、排除隱藏／不列計算點，不受告警分頁與互動圖預設1000點限制。原單張互動圖預設限制保持。
- AC-CHANGE：Given同月資料之後結案／重開，When重新查詢，Then依最新狀態改變，來源量測與告警不被查詢寫入。
- AC-CONSISTENCY：Given同一組摘要DTO，Then畫面與Excel欄位值一致；分類及既有產品內容保持。

## 計畫／測試／回復
- 核心先補測試：狀態、缺告警、OOC/OOS重疊、1001點尾端、結案變化、唯讀；共用Overall既有主副圖／子組／屬性案例做回歸。
- 摘要完整讀圖，取該圖全部量測ID的告警，直接呼叫OverallStatistics，避免新造另一套口徑。欄位加在Excel尾端保留既有欄序；畫面只在月報展示。
- Happy Path已／未及合計；Boundary空表、1001、子組；Invalid缺處置不能假已完成、查詢失敗傳錯誤不當0；Regression分類、日期、排除、語系、排程及單圖限制。
- build/test通過後備份發布SpcSingleTest，保留appsettings／web.config。rollback本件發布前的測試後端／前端；不發布正式。
- [x] 測試先行與實作。
- [x] 建置／測試站驗證。
- [x] 原Excel、規格、TODO／變更紀錄同步，依本件提交上傳。正式待授權，核心旗標保留。
- 結果見[驗證及回復](verification.md)。
