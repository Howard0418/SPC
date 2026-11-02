# 九月藥液測試匯入驗證
- 2026-11-02；既有TASK-002九月藥液子範圍。1146筆合格量測／71複驗已隔離測試匯入；78來源例外及父TASK-002仍未完成。
- 十月1211筆與16來源例外保持。未處理咬蝕／落塵、未讀寫正式、未網站發布或打包EXE。

## 驗收結果
| 驗收 | 實際驗證及結果 | 狀態 |
|---|---|---|
| AC1 | 月份起迄／非法月份新增測試先紅後綠，合併公式測試10項通過；九月32檔1227候選、21報表日。原檔SHA256讀前後及apply前核對一致；十月既有manifest純SPC引擎1262次回歸吻合，九月manifest配十月marker在DB存取前被拒絕 | 通過 |
| AC2 | 1146初驗＋71複驗共1217次Decimal／SPC引擎與Excel快取對帳，容差1e-8；含2筆明確I欄Secondary輸入。3筆內容相同略過，78例外不送出；初複驗及F來源保留 | 合格範圍通過 |
| AC3 | 測試API test／0.1.97、實際SQL172.16.110.16／PMR_SPC_TEST、本人的Windows整合低權限execute核對通過；COPY_ONLY/CHECKSUM/VERIFYONLY後，preview／prepare及受控commit新增1146筆；每筆同receipt重送一次皆alreadyProcessed、ID不變 | 通過 |
| AC3查回 | 網站ChemicalTraceQueryService1146筆查回，初複驗raw、公式JSON語意、F引用數及數值、Primary／Secondary、單位核對通過；統計1146 Completed、通知呼叫0 | 通過 |
| AC4 | 原業務PPC／政策／啟用F快照及匯入前全部既有量測COUNT／CHECKSUM指紋保持（含十月）；測試特性RECORD_ONLY、未全域啟用F，不改角色、schema或trigger | 通過 |
| 圖表與網站 | 九月9/1～9/30圖表HTTP117組／1146點，逐ID與最終初／複驗結果、PortalDailyDate對帳。Chrome實站PPC2766顯示九月19筆、I-MR及直方圖、無API離線 | 管制圖指定範圍通過；趨勢UI仍待驗 |

## 實際新增範圍及追溯
- 製程230，code `TEST-SEPCHEM-20261102`；PPC2766～2882，公式135～251，F歷史129～245；量測87086～88231。
- 每個公式／F／規格組態使用獨立測試項目，僅相應九月報表日排程。日期採報表日12:00+08:00合成測試時間，非現場歷史；因此九月圖表不需要像十月午夜fixture加寬到前一月，並未修改共用日期邏輯或十月資料。
- F頁目前啟用版本保持原來全域F；本批使用歷史排程引用快照，應以量測trace內引用的F作對帳，不把當前全域F當歷史基準。
- 測試RECORD_ONLY抑制業務告警／郵件，trace仍保留公式raw的OK／NG；不能以圖表未告警推論全部來源OK。
- 本機非Git證據`D:/SPC/.sep-chemical-test-20261102/`：manifest、server-pure、backup、import-result、statistics-result、website-service-verification、http-verification、chart-all-verification。驗證服務與統計同時執行，trace報告中的jobs為較早快照；最終1146 Completed以statistics-result為準。

## 網站與發布
- 首頁200 text/html、當前主JS200 application/javascript、CSS200 text/css；`/api/health`與`/api/version`皆200、test／0.1.97。Chrome本人既有登入在同源API基底下成功載入9/1～9/30的九月測試管制圖19筆。
- 沒有網站程式變更，Web0.1.113／API0.1.97保持，發布不適用。共用受控runner只擴充核准九月選項，十月預設保持；既有GUI／EXE未重新交付，沒有蒐集／保存HTTP權杖。
- 既有趨勢頁帶入及GUI交付限制保持，未覆蓋使用者未提交的TrendChartView.vue；不宣稱趨勢UI已驗收或正式匯入完成。

## 例外與下一步
- [78筆來源定位與調整建議](source-issues.md)：日期不一致40、同日來源衝突30、初複驗公式不同8；不改原始檔、不自選新版或補造取樣倍率。
- 九月與十月均已完成合格部分；父TASK-002仍保留目前待辦旗標。核心39件、人工11／核銷4保持，不以94筆來源例外新增94個工作。

## 備份、回復與進度
- SQL主機新增前COPY_ONLY／CHECKSUM及RESTORE VERIFYONLY通過，檔名`PMR_SPC_TEST_SEPCHEM_20261102_070427.bak`；完整位置在本機backup.json。回復先限定停用PPC2766～2882、保留追溯；完整DB回復需先比對之後其他測試寫入，不能覆蓋十月成果。
- 原進度表候選已同步TASK-002、小工作總表／專案摘要／同表明細及既有11/02原生里程碑，九月1146／十月1211及來源例外分列，不改其他專案或完成旗標。
- 候選OpenXML SDK0錯誤、ZIP及未授權儲存格／part／其餘原生物件一致、38物件保持；artifact重開渲染與公式錯誤掃描通過，LibreOffice列印11頁並檢視第4／11頁。Microsoft Excel人工開啟未執行。已於驗證後取代原檔並重讀，目前待辦旗標39件；備份`備份/進度表_九月藥液匯入前_20261102-150528.xlsx`；輸出SHA256 `211e42f0975d93812392be14fdac5b38432a1645ef748d1b66b6491933da92a9`。

## 11/02後續趨勢驗收
見[續作紀錄](ui-followup.md)：另組趨勢20筆／1複驗的公式F、HTTP圖表及統計通過；來源1146及78例外保持。UI已顯示20點，續查追溯時登入逾期；未全驗收，不標DONE。最新中央核心35／SPC13以續作及取消更槽紀錄為準，以上39為當時歷史計數。
