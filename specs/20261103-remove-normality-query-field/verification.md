# 驗證與發布紀錄

## 程式與自動化驗證
- 舊版紅測：`/spc`、`/trend-chart`仍各存在1個`normality-method`，新期望為0。
- 修正後：`normality-fixed-ad.spec.ts`與`i18n-trend.spec.ts`共26項通過，涵蓋兩頁、桌面／390px、三語、P-value邊界、無變異／資料不足及固定AD查詢參數。
- 測試站與正式站各自從既有發布基線隔離建置；`check:i18n`均為2767鍵×3語系通過，`build:test`與`build:production`成功。
- 修改範圍只有兩個Vue查詢列模板、對應測試及前端版本；未改後端、資料庫、公式、F值或Portal。

## 測試站發布與驗收
- 發布：Web `0.1.115`；API保持`0.1.97`／`test`。
- 備份：`D:\SPC\release\test\frontend.backup-normality-field-20261103-083651`。
- HTTP：首頁、`index-D8IBkhbb.js`、`index-BFEI4qhU.css`、`/api/health`、`/api/version`均200，JS／CSS MIME正確。
- 瀏覽器：`/spc`與`/trend-chart`的`normality-method`數量皆0，查詢按鈕可見、頁尾顯示Web 0.1.115，主控台無錯誤。

## 正式站發布與驗收
- 使用者原始需求已明確授權測試與正式兩站；發布Web `0.1.115`，API保持`0.1.92`／`production`。
- 備份：`C:\ProgramData\PMR-Releases\Production\normality-field-20261103-083744\frontend-before`。
- 僅替換正式前端入口與新JavaScript；沿用正式站原`index-CLEpViWr.css`及原`web.config`，設備導覽與正式基線保持。
- HTTP：首頁、`index-Cpfpxv3p.js`、`index-CLEpViWr.css`、API health/version均200，MIME與環境標籤正確。
- 瀏覽器：兩頁欄位數量皆0、查詢按鈕可見、Web 0.1.115，主控台無錯誤。

## 進度表同步
- 原檔：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\Voice\進度表.xlsx`。
- 備份：同目錄`備份\進度表_查詢列常態欄位移除前_20261103-084256.xlsx`。
- 新增完成列253與第42個原生里程碑；登錄220、完成184、核心未完成35、SPC直接13、人工11、核銷4。
- Artifact Tool原路徑重讀、公式錯誤掃描0、ZIP通過、OpenXML SDK 0錯誤、列印範圍延伸至K253；LibreOffice重開並輸出11頁PDF，版面預覽通過。Microsoft Excel人工開啟未執行。

## 回復
- 測試／正式前端可用上述各站備份還原。
- 進度表可用上述原檔備份覆蓋；本件無資料庫回復需求。
