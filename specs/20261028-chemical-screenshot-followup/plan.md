# 技術計畫
- SPC-CHEM-SCREENSHOT-20261028／v1；[規格](spec.md)；只規劃。

|任務|主系統／相依與預計範圍|資料庫／發布／回復|
|008|Portal VariableMeasurementEntry的fetchHistory／儲存後查詢、SpcProxyController、必要SPC查詢端點。先按請求URL／HTTP／Trace區分網路、TLS、權限、proxy或API錯誤，不預設是CORS。|預計查詢／錯誤呈現，無schema；禁止為修查詢重送寫入。未來核對測試環境後發布相關站，回復同次前版包及環境設定。|
|007|Portal selectedSamplingStage、updateSamplingStage、loadDailyReport、calendar與daily契約，SPC ManualMeasurementsController／既有N1N2階段規則。依008結果只補剩餘階段問題。|預期無schema；主檔只讀核對，不先補造設定／資料。若需資料修復，另列精確範圍、備份及授權。|
|009|Portal初／複驗preview、ChemicalPrecision／effective-trace，SPC日期公式與F表版本、receipt／trace；來源Excel公式、單位、titer及rounding逐層對照。|無預先核准的schema或歷史回填；設定／計算修正前先做golden test及來源核准。版本衝突阻擋實作但不阻擋本次規劃。程式回復不刪trace或回退資料。|
|原004／005|SPC SpcOverviewReportService、月報查詢與Excel輸出；分類用既有ControlScope及GroupType／DisplayMode；處置沿Overall去重。|不另開重複任務；預期無schema，保留產品／原報表資料契約。分Sheet後再加計數，最後比對畫面／匯出／既有排程。|

順序：008通用保存後查詢失敗 → 007 N2開收線 → 009 QE2濃度差異 → 原001管制線公式 → 原002查詢判定／複驗優先 → 原003入口網址 → 原004分Sheet → 原005處置件數 → 原006配色。每次只執行一件，不平行處理公式或相同查詢區塊；若證據指出同根因，可共用修復但逐項驗收。

Portal仍是輸入／查詢入口，SPC仍負責量測保存／計算／權威判定；TransFiles不在本件修改範圍。執行跨站Task時在Portal建立引用中央主規格的索引，本次只改SPC中央文件。

本次僅同步規劃、3件新增與原004／005補強，原Excel保留16個原生物件，更新既有2個規劃標記的大小／色彩與範圍說明，不畫開發完成條。原25件＋3＝28件；本批共9件待執行，不把月報／規劃父件重複計數。
