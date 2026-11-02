# 十月藥液測試匯入驗證
- 日期2026-11-02；主工作SPC-HISTORICAL-IMPORT-1102-TASK-002，限定十月藥液子範圍。
- 1211筆隔離測試量測新增完成、54筆複驗保留；16來源例外未匯入、3筆完全相同來源略過。整件未DONE，不開始9月／咬蝕／正式。
- TransFiles實檔讀取修正及安全公式對帳模組；獨立受控runner見[integrations](../../integrations/transfiles/october-test-import/README.md)。沒有重新打包GUI／EXE，不宣稱既有EXE已支援完整矩陣匯入。

## 驗收結果
| 驗收 | 實際結果 | 狀態 |
|---|---|---|
| AC1 | 設定來源只讀36個十月檔、1230列；讀取前後SHA256相同，apply前再核全部檔案雜湊；未改原Excel | 通過 |
| AC2 | 1211初驗＋54複驗，受限AST Decimal與SPC實際引擎各自核對Excel快取，容差1e-8內一致；每列F引用明確；同日班別／N1N2開收線分辨 | 合格範圍通過；16列保留 |
| AC3 | 實檔load_rows先重現TypeError，新增整合測試再改try/finally close；公式函數／外部／跨列引用拒絕、0及F精度等測試，共25項通過 | 通過 |
| AC4 | 使用者已明確選擇報表日測試fixture；製程228及229、PPC2649～2765、公式18～134、F歷史12～128，共117組定義。F版本皆不全域啟用，原業務PPC／政策／啟用F快照前後一致 | 通過 |
| AC5 | Windows本人低權限連線，SQL實際172.16.110.16／PMR_SPC_TEST，preview／prepare後受控CommitChemicalMeasurementWithTrace新增1211筆；每筆同receipt再送均alreadyProcessed且ID相同；trace1265，統計1211 Completed | 通過 |
| AC6 | 網站ChemicalTraceQueryService逐筆查回1211筆，原始結果、公式JSON語意、F引用數量與實際數值、54複驗一致；117組圖表HTTP共1211點與保存最終階段結果逐點吻合 | 指定查詢範圍通過；日界線限制如下 |

## 寫入、查回與網站證據
- 本機非Git證據：`D:/SPC/.oct-chemical-test-20261102/` 的manifest、server-pure、import-result、statistics-result、website-service-verification、chart-all-verification及http-verification；不提交JSON／來源Excel或憑證。
- 版本服務正規化JSON欄序，因此網站公式採JSON語意比對，不拿字串欄序當公式差異。
- 量測ID85875～87085；只新增專用PPC量測、receipt及追溯，SQL程序不更新舊量測。來源與既有業務定義不變，正式未讀寫。
- 測試首頁HTTP200 text/html、當前主JS200 application/javascript、CSS200 text/css；health／version皆200、test／0.1.97。Chrome本人既有登入可讀WEB0.1.113／API0.1.97，公式總覽／v1快照及F版本記錄正常；未蒐集或保存權杖。
- Chrome已確認C1鍍銅1硫酸公式`Primary*24.5*F!$B$3`；F頁原全域F-20261020保持，TEST-OCTCHEM歷史可見。歷史計算看trace指定F，不能拿目前全域F比歷史。
- 瀏覽器PPC2649在9/30～10/31顯示17筆、I-MR及直方圖，沒有API離線。測試特性RECORD_ONLY避免業務郵件，trace仍保留raw的OK／NG；統計runner通知呼叫0。圖上未告警不等於全部來源判定OK。

## 尚未完成與調整建議
- [16筆定位與建議](source-issues.md)：日期不一致10、同日來源衝突4、初複驗公式不同2；不改原檔、不猜修正版或取樣倍率。
- 午夜+08測試fixture存UTC前一天16:00。既有圖表使用UTC日期篩選：10/1起少掉10/1凌晨資料，實測PPC2649出16筆；本批暫用9/30～10/31查詢完整17筆。全部117組以此加寬查詢核對1211點；資料均為10月，沒有匯入9月。日界線未修正，不宣稱十月日期UI完全驗收。
- 趨勢頁直接連結PPC2649未自動帶入項目，畫面停留選擇線別／查詢提示；本次未定位原因，不冒稱趨勢UI通過，不覆蓋使用者既有TrendChartView.vue修改。
- GUI／EXE交付、16例外及日期／趨勢驗收仍待辦；父TASK-002目前待辦旗標保持，核心39件不變，人工11／核銷4另計。

## 備份與回復
- 新增前COPY_ONLY／CHECKSUM備份及RESTORE VERIFYONLY通過；SQL主機備份檔`PMR_SPC_TEST_OCTCHEM_20261102_060933.bak`，完整位置記錄本機backup.json。
- 回復先停用專用PPC2649～2765，保留不可變歷史；完整DB回復須比對之後其他測試寫入，不直接覆蓋別件成果。不停trigger、不改schema／角色／全域F。

## 原進度表同步
- 已同步原`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/進度表.xlsx`的小工作總表TASK-002、專案摘要／同表明細與既有11/02原生里程碑；狀態「進行中／十月測試匯入，來源待釐清」，不將部分完成標整件完成。
- 備份`備份/進度表_十月藥液匯入前_20261102-141932.xlsx`；取代前核來源未變、取代後重讀SHA256 `33ee392f040a46035eb9c6f42f8a4319b1c5c9f70841176c056861747b0b2de0`。
- 候選及原路徑OpenXML SDK均0錯誤、ZIP／未授權儲存格及其餘原生物件比對通過，物件數保持；artifact重開／渲染及公式錯誤掃描通過，LibreOffice重開列印11頁並檢視第4／11頁。Microsoft Excel人工開啟未執行。
- 網站發布不適用，無網站程式變更，WEB0.1.113／API0.1.97保持；未正式發布或打包EXE。

## 第二滴定值補匯入與最終合計
首輪1208筆之外，同列I欄第二滴定值另先測試再支援Secondary，原3筆技術限制已解除；專用製程229／PPC2764～2765、量測87083～87085補匯入3筆，兩組網站圖表3點與追溯服務均吻合，統計3 Completed。合計1211量測、54複驗、1265計算、117組圖表，全數各自對帳；原始首輪JSON保持，不改歷史證據。補匯入前另完成COPY_ONLY及VERIFYONLY備份`PMR_SPC_TEST_OCTCHEM_20261102_061840.bak`。剩16筆來源問題（日期10／衝突4／初複驗公式2）。
