# 驗證紀錄
- 功能 ID：SPC-OVERALL-001；規格版本1；2026-10-28。
- 實作與必要驗證完成；測試API0.1.88／Web0.1.111發布完成；原進度表與Git上傳已同步。

|驗收|驗證|結果|
|---|---|---|
|AC-001|啟用母體含無資料項目，停用／登錄數、前端卡片|通過；SQLite含無資料及停用項目；實際143啟用／0停用|
|AC-002|同類多點、雙類重疊、跨PPC、主副圖去重|通過；實際OOC24項97件、OOS9項17件，去重105件，重疊9件|
|AC-003|重複告警最新狀態、Closed／ack、處置中／無告警、子組及計數型|通過；5項聚合測試；實際目前已處置0／未完成105|
|AC-004|量測日兩端、計量及計數型1001筆、舊1000筆、反向／超長日期|通過；SQLite完整讀取與舊預設保持，3項無效日期驗證；實站反向日期400|
|AC-005|排除、趨勢／子組／I-MR、零資料、讀取失敗、手機／語言|通過；3項新Playwright、38項原圖表回歸（含各語系／390～1920寬度）|

## 測試與建置
- 核心修改前先建立測試並確認缺少Overall類別／完整讀取參數而編譯失敗，再實作；後端新驗收9項通過。
- `dotnet test backend/MesSpc.Api.Tests --no-restore`：56 passed／1 skipped。
- `dotnet test tests/MesSpc.Api.Tests --no-restore`：290 passed／8 skipped。
- `dotnet test tests/MesSpc.Chemical.Tests --no-restore`：59 passed。合計405 passed；9項條件式略過不計入通過。
- 新增計數型完整讀取fixture曾使用同時戳，觸發既有C圖時間字典重複鍵；改為不同秒數並維持1001筆，完整讀取及日期驗收9項重驗通過，未修改C圖核心。
- 前端41項不同情境通過；一開始Windows Vite子程序收尾逾時，改以獨立5199程序及無webServer收尾的臨時config執行；40項通過後，唯一舊寫入回歸因dev環境unknown與fixture的test不符，明確設定VITE_APP_ENV=test後單項通過。沒有放寬環境隔離或改動舊測試。
- Release API publish成功；Web `build:test`通過（2755鍵×3語系檢查），同站`/api`及Portal測試入口以建置環境變數指定；保留既有大型bundle提示，未擴張拆包。

## 發布與實際驗證
- 64位元IIS核對SpcSingleTest／D:/SPC/release/test/backend／SpcSingleTest集區，設定AppEnvironment=test及PMR_SPC_TEST。備份hash比對後發布；appsettings與web.config指紋保持，未發布正式站或Portal，未執行migration、資料匯入或處置寫入。
- 備份：`D:/SPC/release/backup-overall-20261028-082255`。回復本次backend／frontend備份並重啟同一測試集區即可，不需DB回復。受限ACL備份不提交。
- 實站health 200/test/0.1.88、/spc 200及內容與新index一致；內建瀏覽器API同來源測試通過。實際API 2026-10-01～28回200：143項，OOC24項97件，OOS9項17件，合併105件，已處置0、未完成105，未分析0。
- 實際前端以唯讀UI shell fixture驗證新bundle與真實API一致；所有跨站／非GET請求攔截，無寫入及pageerror。此為自動化畫面驗證，不宣稱真人登入驗收。

## 原進度表
- 已更新原檔 `C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/進度表.xlsx`；備份 `備份/進度表_20261028_Overall同步前.xlsx`。回復為同備份覆蓋原檔。
- SPC第3／4階段查證紀錄迄日10/28、原生甘特條延至10/28，新增原生紫色單日里程碑；截至日／圖例／未來灰底／同表明細及版本同步，不將查證涵蓋期間當全案工期。保留Portal／KM及其他專案內容與原13個圖形；新增後14個。
- 小工作總表新增SPC-OVERALL-001，203筆登錄／187具名小工作（168完成、19核心未完成）；11待人工註記、4待核銷另計，來源與完成紀錄保持，無恢復其他工作。
- artifact工具作者副本與重開計數／公式掃描通過；ZIP CRC、非授權儲存格／原生物件逐項比對通過；副本及指定原路徑OpenXML SDK Microsoft365均0錯誤；LibreOffice列印共10頁，已渲染並目視涉及的第1／2／10頁。未做Microsoft Excel人工開啟。
- 取代前原檔與起始副本hash相同；取代後由指定原路徑重讀、ZIP及OpenXML再驗通過。SHA256：`8bc2f6ab107fba80e86274cb4cdaacc55a408ca616e767a8b089a73b01597c99`。

## 證據與文件
本機證據置於`.codex-tmp/overall-20261028/`與`release-staging/spc-overall-20261028/deploy-proof.json`，不提交JSON／截圖／xlsx／產物。需求索引、TODO、CHANGELOG_CUSTOM及ai_docs/10_change_log同步；目前剩餘核心19件（其餘人工／待核銷另計）。

## Git收尾限制
暫存範圍檢查只含本件24個必要檔案，無產物／證據／本機設定／機敏資料，原未追蹤檔案保留。fetch確認Develop既有5筆先前REPORT-PPT-001-F／G／H／E及Portal LOGO提交尚未上傳；推送目前分支會連同這5筆一併上傳，與AGENTS每次僅上傳本件的範圍限制衝突。先完成本件繁中本機提交；上傳待確認，不宣稱已推送。

正式站未授權，維持不發布；無migration及資料寫入。

### 2026-10-28 上傳完成
使用者明確回覆「允許一併推送 Develop」，授權Overall與既有5筆報告／LOGO共6筆一併上傳。`git push origin Develop`成功，遠端由`e9119774`更新至`e6a1c227`。本次追加僅同步Git收尾紀錄，不改功能進度、日期、狀態或網站版本，進度表更新／build／發布不適用；既有19件核心待辦不變。
