# I18N-CHAR-001-C 人讀報表／附件與剩餘頁面
- 2026-10-27 DONE，必要驗證與測試發布完成；使用者「繼續」授權只接續C。對應R1／R2／R4／R6及AC1～6，其他系統不恢復。
- 人讀Overview與CPK Excel寫入名稱時套用同一三語／缺值／代碼規則；舊Summary只有獨立ChartName時原樣保持。報表郵件內文無品質特性欄位，名稱只在附件，因此驗證依語系產生的附件bytes原封傳給sender、數值／檔名與收件人保持；不擅加警報郵件欄位、改歷史診斷或寄送。
- 藥液總覽、開啟的公式歷史標題、其人讀Excel與F表影響清單接線；保持原公式／設定／草稿／分頁／選取ID／排序／請求。F表僅additive nullable英文欄位，不改positional constructor／公式參照資料。
- 盤點排除：UploadPreview中的ResolvedCharacteristicName是存檔匯入快照，原輸入與診斷保留；RawDataCsvExporter及匯入／Migration識別欄位、停用未路由Dashboard舊分析程式不改、不恢復。ValueLabel、獨立圖名與歷史Message不是可反查翻譯主檔。保留此盤點清單以供真人核對。
- 先測backend名字helper／三語workbook與郵件附件／F表impact投影，再實作。Happy中英切換、Boundary空白與fallback、Invalid文字／Excel公式前綴、Regression數字／色彩／版面與原DTO及表格格式。
- 原化學人讀Excel工作表／欄名／檔名與格式保持，僅名稱cell依選語切換，匯入範本／CSV不變；不得將其名稱字串當公式執行。
- 測試Web0.1.110／API0.1.87；test IIS與PMR_SPC_TEST核對、成對備份後僅release/test發布，rollback回109/86原前後端備份，不需DB回復；正式另授權。必要自動化、build、文件與提交完成才DONE。TASK20真人審稿及TASK21跨站驗收未完成。
- [x] 核心先測與名稱／impact呈現
- [x] 剩餘頁面與附件驗收／回歸
- [x] build／測試發布／文件／提交

- 實際[驗證與回復](verification.md#i18n-char-001-c-2026-10-27完成)。SPC本案三件完成，其他系統及真人審稿未開始／未冒稱完成。
