# 保存後查詢驗證
- SPC-CHEM-1102-TASK-001／v1；2026-11-02；測試修正完成，正式／現場保存驗收待辦，不標整體DONE。
- 授權：使用者選擇開始001並本人登入Portal測試站；未授權正式發布。本件未改SPC程式／DB／公式／F／主檔代碼。

## 測試先行與實作
- 新`chemical-save-context-ui.cjs`於舊碼三項失敗：未依送出PPC定製程、GET-only重試仍查多個製程／未限制日期、等待時切換畫面未保留送出上下文；POST失敗保留草稿案例原本通過。測試工具的路由／readonly日期fixture問題先修正，以上紅測試才是產品差異證據。
- `submitChemicalRows`於等待確認前捕捉送出PPC／MeasuredAt；`showSuccessCard`以量測日設定起迄日期，保留成功批次及原分頁／GET重試。其他手動歷史查詢仍可清空日期讀完整資料。
- 既有effective UI測試原固定點選10/18，跨月後找不到日期；只固定測試時鐘為10/20，產品日曆不變。

|AC|驗證與結果|狀態|
|---|---|---|
|001|新UI測試涵蓋N2、STI、ST1、DP、DV2、DP.DV2及N10；送出製程／10/27完整單日範圍，查詢與合成日報的0值一致|自動通過，實際保存待現場|
|002|GET網路失敗後保留批次／已保存訊息；重試條件一致、僅1次確認POST。既有HTTP401/403/404/500安全分類及單頁逾時回歸通過|通過|
|003|確認POST500保留草稿，不報保存成功、不送歷史GET|通過|
|004|等待POST期間切換線別／日期，仍依原送出內容查詢|通過|
|005|完整1205筆分頁、重複／總數變動拒絕、取消舊GET、3製程並行、N2班別開收線與初複驗回歸|通過|

`node --test tests/chemical-save-context-ui.cjs tests/chemical-history.cjs tests/chemical-history-refresh-ui.cjs tests/chemical-stage-ui.cjs tests/chemical-effective-ui.cjs`：23 passed；`SpcProxyControllerTests|ChemicalEffectiveProxyTests`：15 passed。API／Web Release build各0警告、0錯誤。

## 隔離發布與實站驗證
- Portal工作區另有文具、通訊錄、語言JSON、發布腳本及團保文件修改，全部保留且未帶入發布。以HEAD `b40a87b`建立`tmp_publish/chemical-save-context-source-20261102`隔離來源，只帶本件藥液頁及測試。
- 核實IIS PmrPortalWeb=https portaltest.pmr.com.tw，實體`D:/PmrPortal/publish/portal-web`；API=`D:/PmrPortal/publish/portal-api`，內部8091。自隔離專案根目錄`scripts/publish-all.ps1 -OutputRoot D:/PmrPortal/publish`發布API1.0.215／Web1.0.258，讀取實際DLL版本一致；8個設定／web.config／上傳保護檔hash不變。
- 備份：`D:/PmrPortal/tmp_publish/chemical-save-context-backup-20261102/portal-api`及`portal-web`。回復時只對上述測試目錄啟用維護、從同批備份恢復本件產物，再重啟並驗證；保留現場設定及上傳，無DB回復。隔離來源與備份／證據不提交。
- Web首頁、`/app-version`、主`site.js`、藥液三個JS、`site.css`均HTTP200，JS=text/javascript、CSS=text/css；`/app-version`=1.0.258.0。
- Portal既有等效健康端點`http://172.16.110.27:8091/health`=ok，database=ok／dbEnvironment=test；此站沒有`/api/version`，API版號以實際IIS DLL1.0.215.0核對，不虛稱不存在端點成功。相依SPC同源`/api/health`與`/api/version`=200／0.1.94／test，本次未重新發布SPC。
- 瀏覽器重載頁面meta版本1.0.258.0，沒有API離線。頁面歷史呼叫使用相同origin的`/api/spc/...`，上游PortalApi設定8091／SPC設定SPCTEST，設定未變；2026/10/16唯讀查詢N2=20筆、ST1=4筆、DP=7筆、DV2=6筆，無查詢錯誤及瀏覽器console error。Web/API/TLS/DNS與瀏覽器實際同源路徑可達，沒有非標準外部連接埠需求。
- 實際主檔為ST1、DP、DV2；不把STI或DP.DV2自動改成業務代碼。測試站目前歷史大多為舊來源，未為驗收新增或覆寫量測；不能將歷史唯讀查詢說成現場保存成功驗收。

## 限制與待辦
- 原現場錯誤仍未重現，未取得該次HTTP／Trace ID／Network；本件修正可重現的查詢範圍與上下文風險，不能宣稱已證實現場網路根因。
- 真實N2／ST1／DP／DV2保存後與日報一致仍待現場操作驗收；正式網站未發布，需另行授權。本工作保留核心待辦旗標，其他三件及圖片不自動執行。
- 原Excel同步及Git結果於收尾追加；所有產物／證據留本機。

## 原Excel同步
- 已更新Portal量測摘要／明細、001總表狀態與來源、2026/11/02實際紀錄日期；001為測試完成／待正式，人工保存限制另列，未完成旗標仍是。35件核心未完成、人工11／核銷4不變。
- 專案進度表001原生11/02里程碑轉藍並記錄測試狀態，另三件紫色規劃保持；全部35個原生圖形保留，其中只調整001名稱／顏色、不改其餘錨點與圖形。只變更19個已列明相關儲存格，其他單元格及ZIP parts一致。
- 副本OpenXML SDK 0錯誤、ZIP／Artifact重新開啟、核心計數及公式錯誤0；摘要／明細／單件狀態渲染及LibreOffice11頁列印檢查通過。未做Microsoft Excel人工開啟。
- 備份：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\Voice\備份\進度表_保存001測試前_20261102-104134.xlsx`；原檔未變核對後已取代指定原路徑，SHA256 `647ddfbd8dab9d05b050d44df895abb349258f686b02ab9c4c3f9584434528aa`。需回復先確認無後續更新，再使用本次備份；不提交活頁簿／PDF／截圖／hash輸出。

## Git收尾
- Portal本件9檔已提交並推送main：f968782；未提交其他工作區修改或發布產物。SPC中央文件隨本次Develop提交上傳，提交識別以Git紀錄為準。
