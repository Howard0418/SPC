# 技術計畫
- SPC-ADJUST-20261028／v2；[規格](spec.md)；只規劃不實作。

## 系統責任、影響與回復
|任務|主系統／相依|預計檔案／資料契約|DB／發布／rollback評估|
|001|SPC／無|PPC設定前端、MasterDataV2Controller、SpcService、各適用calculator、圖表重新查詢；範圍依圖型支援證據定案|預期無schema；不得變更歷史量測。未來測試API/Web備份後更新，可還原同次程式；既有設定值另備份，禁止批次清手動界限。|
|002|Portal／SPC|Portal VariableMeasurementEntry.cshtml、SpcProxyController及必要SPC查詢DTO；欄位須區分原量測、複驗分析結果、顯示來源與最終判定|預期純查詢／顯示、無schema，不改輸入與保存。執行時在Portal建立引用本主規格的索引；先核對兩站契約與測試目標，備份後可還原前版。|
|003|SPC／Portal根網址|App.vue、LoginView.vue、EtchAnalysisView.vue、測試環境預設與發布建置驗證；只改網站導向，不碰IIS binding／AD／SSO協定|無DB；未來SPC測試Web更新，保留環境設定及原bundle；不從SPC部署工具發布Portal。|
|004|SPC／無|SpcChartView、分類／報表DTO、SpcOverviewReportService；ControlScope=PROCESS/CHEM與GroupType/DisplayMode組合辨識|無預定schema，保留產品分類及既有篩選；畫面／Excel／排程報表分類契約一致；可還原前版API/Web。|
|005|SPC／Overall聚合|報表統計DTO／完整圖表點位與處置聚合、報表畫面／Excel；只讀目前狀態|無schema／資料寫入，避免500／1000筆截斷；需依004分類及001公式行為驗證後實作；還原同次API/Web。|
|006|SPC／無|SpcChartView、TrendChartView常態直方圖bar樣式|前端樣式，無DB、公式、資料契約變更；還原前版Web。|

## 執行順序與風險
最新順序改為008查詢載入 → 007 N2階段 → 009 QE2分析值 → 001管制線公式 → 002查詢判定 → 003入口網址 → 004三個Sheet → 005處置欄 → 006色彩。詳[截圖補充計畫](../20261028-chemical-screenshot-followup/plan.md)。同區域／公式工作逐件進行；007與008若同根因，僅共用一次修復並分別驗收，不重複改動。原004改為三個精確名稱的獨立Sheet，005各Sheet有已／未處置件數。
既有掃毒受阻、Runtime／EXE暫緩、三語暫停及校驗／CPK待確認維持原狀，不因新工作池恢復。若確認入口網址確實阻擋使用或導向錯環境，003可依既定排序規則升至第一，但本次不逕行實作。

## 驗證與交付安排
- 每件開工前補確切受影響檔案、支援圖型、資料契約及可重現案例；核心邏輯先有自動化測試再修改；四類測試見[tasks.md](tasks.md)。
- 驗收須比對使用者截圖（若有），保留原圖／新圖差異及對應AC；未提供則記錄程式觀察與待真人確認範圍，不虛構截圖。
- 001／002／004／005包含後端單元與SQLite／整合、前端UI、Excel內容／分類與必要回歸；003包含built bundle點擊／根網址／SSO回跳回歸；006以視覺與資料不變回歸為主，不新增鏡像樣式單元測試。
- 每件必要測試及build通過後才依各專案規範核對並發布測試站；正式站另授權。本次所有build／功能驗收／發布皆未執行。
- v1已新增6筆待執行；v2依截圖另新增3筆並補強原004／005，不填未開始的實際開發日期。同步TODO／需求／CHANGELOG及原Excel，更新涉及SPC／Portal的原生規劃標記，保留Overall里程碑及其他系統；最新驗證見截圖補充規格。

本次v2新增3件，總池28件；前次6件規劃已交付的歷史日期與證據保持，007～009另列規格，不把本次規劃父件重複計數。
