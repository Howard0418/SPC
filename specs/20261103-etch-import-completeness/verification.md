# 驗證紀錄

日期：2026-11-03。程式、測試相依API、獨立EXE與文件已更新；整件仍待正式發布授權及EXE原生GUI人工畫面，未標DONE。

最新：使用者已核准正式API／開關，SPC102／Portal218已實際發布，正式Web115／Portal247保持；69／74表及保護檔核對通過。以下保留授權前階段紀錄，現行正式結果與剩餘人工項目以[正式階段](formal-release.md)為準。

## 測試與BDD

核心修改前先建立失敗測試。AC1～6、8通過；AC7的打包、回歸、文件／進度／版本／測試站通過，原生GUI人工畫面及正式端到端保留待辦。

| 範圍 | 結果 |
|---|---|
| TransFiles相關9組（解析、完整性、上傳、排程、GUI流程、落塵、設定、TLS） | 109通過；其中新完整性24項。Happy Path、邊界、Invalid Input、Regression均有覆蓋 |
| SPC chemical／咬蝕核心 | 96通過，涵蓋真實SQLite交易、25／50／1結構、機台對應、防重、封存／部分／異值、跨系統內容指紋及第一次→第二次→重送／拒絕逆蓋 |
| SPC主要API回歸 | 356通過、10既有略過；另既有backend 66通過／1略過 |
| Portal咬蝕 | 12通過；僅可信SPC完整第一次指紋可允許第二次，客戶端不能授予覆寫權 |
| Release編譯 | SPC測試101、Portal217／260及隔離正式候選SPC102、Portal API218編譯成功 |

工具不再用dry-run hash表示DB已匯入；每個日期四線都有清冊，首筆失敗之後仍列NOT_PROCESSED。來源預覽／確認間改變會阻擋；後置查回不完整為VERIFY_FAILED，不能算成功。舊平鋪模式保留匯出，不繞過完整日報寫入。藥液與落塵規則未擴張，排程未啟用。

## 真實來源與正式唯讀核對

來源 `PD-3-581-06B-咬蝕量_2026.xlsx` 原檔未改，SHA256 `61ea1878edf1336635eaa29b97ef8af7dc2dcba27395144df9b79b6132bd7270`。範圍2026-09-01～10-31，312區塊全部列帳：READY134、QUARANTINED9、EXCLUDED15、EMPTY10、FUTURE112、OUT_OF_SCOPE32；合計312。與前次合格原始業務值差異0，公式摘要警告64。

PT1 B44舊範圍只有20點、PT2 H44只有40點；完整原始樣本為25／50，已在規則與輸出列建議 `=AVERAGE(B38:F42)`／`=AVERAGE(H38:Q42)`。工具不改原Excel，也不為配合錯誤摘要更改DB值。

以ReadOnly連線在正式 `172.16.110.16/PMR_SPC_2026` 執行新版Inspector：134份均ALREADY_EXISTS／Verified，10384原始點與速率吻合，前後全VariableMeasurements指紋一致、ChangeTracker無變更。10/16 PT1已是第二次；同一原始檔hash與完整Before／After一致，只讀認定StoredMeasurementNo=2，不補寫元資料。本次**沒有正式資料寫入或重匯**。

## 實際測試HTTP流程與查回

SPC／Portal實際測試HTTPS，合成 `2000-01-02/PT1`，先確認該業務鍵不存在。短效合成Editor與Portal測試QA身分只用於本次fixture，權杖／Cookie只在記憶體與子程序stdin，不用真人Cookie、不寫證據或命令列。

- 第一次preview／confirm 200；第二次preview／confirm 200，Portal報表ID保持、50原始點改為第二次；SPC51筆原ID保持、2批次、Before保留51原值。
- 第二次重送200且unchanged；第一次反蓋及同次異值均409。既有業務量測指紋保持。最後只清除本次合成日報、批次及合成Operator，既有資料不變；稽核事件保留。
- 實際工具HTTP逐份讀134份來源：測試DB133份完整吻合並查回略過，0 Portal confirm／0重匯；10/16 PT1舊測試資料缺少同來源hash／次數證據，被正確列CONFLICT且預覽停止，未擅自改測試舊資料。此與正式同檔已保存第二次的134／134通過是不同環境結果，不能混同。

## 測試發布與瀏覽器

SPC實際目標 `SpcApiTest`／`SpcSingleTest`、`D:/SPC/release/test`，API0.1.101、Web0.1.116；保留Auth、受限runtime、startup不遷移及3設定hash，60 migrations／pending0、68個業務表指紋不變。前端未改。

Portal由隔離來源根 `scripts/publish-all.ps1` 發布測試API1.0.217／Web1.0.260至本機IIS `D:/PmrPortal/publish/portal-api`、`portal-web`；保留原已部署文具修補，排除工作區其他未提交Web／團保／發布腳本修改。8個設定／web.config／uploads受保護檔hash保持。未發布到 `D:/Sites/PmrPortal`。

SPC首頁、目前主JS `/assets/index-BEPAWGgw.js` 與CSS `/assets/index-BFEI4qhU.css`均200／MIME正確；`/api/health`及`/api/version`為test／0.1.101。已部署前端基底為同源`/api`，上述亦從實際同源驗證。Portal首頁／主site.js／site.css 200／MIME正確，`/app-version`260；Web實際上游 `http://172.16.110.27:8091/health` ok／Test、已部署DLL API217。Portal無`/api/version`，以health／DLL及Web app-version等效驗證。

使用者重新登入後，瀏覽器實際 `/spc` 顯示Test Web116／API101，總覽385項及查詢資料正常載入，無API離線／逾時；Portal與SPC同源實際HTTP流程亦通過。直接將health當頂層瀏覽頁曾被用戶端擋下，未以該失敗宣稱成功；改由登入後實際頁面載入與版本、同源API及業務HTTP結果確認連線。無新增用戶端非標準port需求、CORS或TLS例外。

測試備份及部署紀錄留Git外：`C:/ProgramData/PMR-Releases/Test/SPC-ETCH-COMPLETE-20261103-API101`、`TRANSFILES-ETCH-1103-PORTAL217`。回復先核對實際Test／DB／IIS，使用該份檔案備份及標準發布腳本還原原版，保留設定與附件，不用資料重匯回復前端。

## EXE與正式候選

工具版本2026.11.03.1已使用既有spec重新PyInstaller打包至 `TransFiles/發布版_20261103_咬蝕完整性規則/BatchConvertTool.exe`；SHA256 `a9625e1819110de419aa7d55fadef2a14cf4a598cc3ca0974dd7c0bc54b9abd8`。附同版規則、使用說明與既有主檔mapping，舊EXE未覆蓋。封裝內有新入口、三模組及版本常數。原生控制服務回報pipe unavailable，無法完成EXE真實視窗／按鈕驗收，明列待人工，沒有冒稱畫面通過。

正式候選從既有正式來源SPC `bfb9ef91`／API92及Portal `2086020`／API200隔離套本件差異，編譯候選API102／API218；不帶測試Audit、藥液新功能或工作區其他修改。無設定最小ZIP已產生：SPC SHA256 `00cda68342f9a100a10031a00d122a29ba46b8d735e4b86d58e204169f4ff8bf`、Portal `32bb6cd68665d70b1b60be89e876f8f537f6057a988141c90fcf08fa3495539a`，[範圍與授權](release-candidate.md)。候選不是已部署成果。正式發布仍需另行授權，屆時先核對實際正式DLL、hash、DB、Host／路徑及來源基準，若正式已有其他補丁則保留重整候選；Portal仍須標準publish-all，不得以候選複製繞過。正式Web保持現版；專用`EtchImport:AllowProduction`須兩站明確核准才可啟用；不重匯歷史資料、不啟用定時排程。

## 文件、原進度表與提交

現行規則、三專案引用及中央工作池已同步；TransFiles非Git，最小已套用patch／測試保存在 [integrations](../../integrations/transfiles/etch-completeness/README.md)，隔離原始備份先git apply --check再套用，7檔文字與實際工具完全吻合。不複製整包工具或提交EXE／證據／設定。

原 `Voice/進度表.xlsx` 已備份、驗證後直接取代（SHA256 `03f78c083b079c4d67a31f7e5d6b7a284414eaf90c7f17eddf59350082d78ef9`）。11/03新增實際測試事件111～114列與第45個原生藍色里程碑；原44物件及非本件儲存格／其他封裝part保持，Portal僅更新本件相依摘要。同活頁簿「小工作總表」263列新增本件：登錄230、核心42／SPC直接20、完成187、取消1、人工11／核銷4。

OpenXML SDK原候選及原路徑重讀均0錯誤，ZIP／內容／日期／原生物件／單一Print_Area檢查、artifact重開渲染及LibreOffice12頁列印預覽通過，公式錯誤0；Microsoft Excel人工開啟未做。更新前備份 `Voice/備份/進度表_咬蝕完整性規則前_20261103-123949.xlsx`；取代前原檔hash護欄通過，回復可用該備份。其餘小工作不啟動。

本機證據集中 `D:/SPC/.etch-rules-20261103`（Git外），含失敗先行、來源核對、正式唯讀、HTTP、測試發布、打包／進度驗證。提交上傳僅含本件必要檔案；正式發布產物不提交。
