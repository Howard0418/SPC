# I18N-CHAR-001-B 圖表摘要、警報與追溯
- 2026-10-27 DONE，必要驗證與測試發布完成；依使用者「SPC繼」接續B，不自動開始C／其他系統。
- 對應R3／R5、AC1～5。盤點本件SpcChartView、TrendChartView、SpcSummaryTable、AlertsView、AlertsWorkflowView、SpcQueryView；後端ChartSummaryDto／ChartMonitorContext及SpcService、AlertDtoMapper、TraceabilityV1Controller。
- 原summary ChartName是品質特性名生成，新增明示CharacteristicName／NameEn／Code三欄，舊ChartName原值不變；無新metadata的舊DTO或獨立圖名literal fallback，不將任意chartName反查翻譯。monitor／alert／trace僅新增英文名，未知診斷與歷史文字不改。
- 先建立投影相容／原數值測試及summary名字helper測試，再實作。前端只換名稱文字，圖表locale更新僅setOption呈現title，不重新查詢／計算／重建series／zoom；原分類、識別與etch規則原字串不動。警報／追溯保留原代碼，新增可讀名稱。
- Happy三語／Boundary缺英文與舊DTO／Invalid字面值安全／Regression搜尋與選取、數字、圖表instance/series/zoom、原API欄位／查詢與寫入不變。InMemory／mock API驗證，不真寫入寄信。
- 化學總覽／F表等盤點遺漏與人讀報表依原C最後全SPC盤點處理；不在B宣稱全站完成。
- 測試Web0.1.109／API0.1.86，無schema／角色／AD／正式資料變更；實際test IIS／PMR_SPC_TEST preflight，成對備份後只發布release/test，正式另授權。Rollback回Web108/API85完整備份，無DB回復需求。
- [x] 先測／投影與helper
- [x] 六頁／元件接線與隔離驗收
- [x] 必要build/test／測試發布／文件與提交上傳

- 警報兩頁搜尋補中英文特性名OR比對，保留原代碼／其他原條件，使用英文搜尋後切語仍保留結果與查詢值，對應R3。

- [實際驗證與回復](verification.md#i18n-char-001-b-2026-10-27完成)。本件完成，不自動開始C。
