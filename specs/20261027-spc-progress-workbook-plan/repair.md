# SPC-PROGRESS-001-R 原檔無法開啟修復（2026-10-27）

使用者回報前件交付無法開啟；本件優先修復同一原檔，不新增功能，不恢復其他工作。已見同一工作表有重複rowBreaks、同一localSheetId有重複Print_Area，且有額外printerSettings二進位part。前次渲染及LibreOffice通過未充分驗證Excel OpenXML相容性，撤回B的「可正常Excel開啟」推論，直到修復通過。

計畫：保留失敗檔與原始備份，使用OpenXML SDK唯讀驗證精確定位；修正重複列印設定、封裝part與其他已證實格式問題，保留內容及原生圖形。套用前在副本驗證、重開及渲染；通過再直接取代原檔。DB／應用程式／發布不適用。回復可用修改前原始備份。

BDD：Given使用者回報的檔案，When檢查封裝及OpenXML schema，Then重複節點／列印名稱／part問題能定位；When修復後再次驗證，Then無新增schema錯誤且保留原10階段、現況矩陣、其他專案與35原生圖形。Happy為重開；Boundary重跑修復不增加重複設定；Invalid攔截重複及缺content type；Regression逐格與原生物件比對。若未以Microsoft Excel驗證，須明示驗證工具範圍。

- [x] R：完成根因、修復、必要相容性與回歸驗證、原檔取代及文件同步。

## 根因與驗證結果
- 根因：前件原生保留步驟曾對已補過列印設定的副本重跑，無去重保護，導致兩個rowBreaks與兩個相同localSheetId的Print_Area；OpenXML SDK 3.5.1／Microsoft365驗證在sheet1.xml回報Sch_UnexpectedElementContentExpectingComplex，錯誤數1。原始修改前備份錯誤數0，證明這是前件輸出造成的格式回歸。前件還帶入未引用、缺content type的printerSettings1.bin，本次一併移除該孤立part。
- 修復僅封裝層：去除重複分頁節點及列印範圍名稱、移除孤立printer part。沒有重寫儲存格、樣式、合併或原生圖形。修復後再次跑相同步驟，各ZIP part內容完全一致（冪等驗證通過）。
- OpenXML SDK先驗副本再驗指定原路徑，均SCHEMA_ERROR_COUNT=0。ZIP CRC、列印名稱唯一、rowBreaks單一檢查通過；sheetData／cols／mergeCells／drawing逐段與使用者回報版本一致，其他保留part逐位元組一致，35原生圖形及內容未變。
- 修復後LibreOffice唯讀轉PDF為A3橫式2頁；取代後從指定原檔重新匯入／渲染目視正常。未啟動Microsoft Excel人工開啟，因此不宣稱已完成Excel人工驗收；本件證據為OpenXML SDK相容性與封裝、重開、渲染及列印驗證。
- 已直接取代原路徑；新SHA256為3fac80c3331a92170ff057219b732ec5ef967bca4d5d5ae93f7246a8865f5c39。修改前原始備份保留；此次失敗交付版另保留在Voice/備份/進度表_20261027_無法開啟版本.xlsx，供追溯。無應用程式／DB或網站發布。
- 本件已完成；三語13件維持暫停，其他外部受阻1／人工暫緩2與既有資料差異維持原工作池，不自動續做。
