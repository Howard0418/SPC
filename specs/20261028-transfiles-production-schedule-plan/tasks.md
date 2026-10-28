# 小工作排序與測試矩陣
- 代號前綴TRANSFILES-PROD-1028；001[契約與來源防護已測](../20261028-transfiles-production-identity/spec.md)，主機／帳號及正式HTTPS核對待確認，仍未完成；002～008待執行，父規劃不另計。使用者已授權開始001，不自動啟動下一件。
- 全案先依既有中央排序處理真正阻擋使用／權限及資料正確性；新案不自動插隊或恢復舊暫緩。選取本案時依001→008逐件執行。

|序號／完整代號|優先／主系統與相依|工作及依賴|Happy Path|Boundary|Invalid Input|Regression|
|---|---|---|---|---|---|---|
|TRANSFILES-PROD-1028-TASK-001|P1／TransFiles；SPC、Portal|正式目標、寫入／非互動身分與最小權限契約；先於三類正式adapter|核准actor核對兩站／庫與授權範圍|token到期、服務／個人身分差異|錯環境、無權限、actor不符提前拒絕|既有測試守衛／登入、正式不擴權|
|TRANSFILES-PROD-1028-TASK-002|P1／TransFiles；SPC主檔|三類來源路徑設定、穩定快照與格式錯誤建議；依001的profile契約|檔案／資料夾可儲存、重開、檢核|UNC／子目錄、同檔追加、0與空值|無權限、鎖檔、缺欄／格式、來源變動|不改原檔，不猜主檔／日期|
|TRANSFILES-PROD-1028-TASK-003|P1／TransFiles；SPC|藥液正式匯入adapter與原始值／日期公式追溯；依001／002|初複驗、receipt／量測／job查回|複驗0、跨日版本、Pending重試|缺原始值／時區、衝突、過期receipt|F／公式／既有資料不猜改，舊測試入口保持|
|TRANSFILES-PROD-1028-TASK-004|P1／TransFiles；Portal、SPC|咬蝕正式受控流程與雙站恢復；依001／002，003收尾後執行|完整日報兩站SUCCESS／對帳|25／50點、跨站部分成功續跑|X-排除、負值／缺點隔離、同鍵異內容|不移除測試守衛、不重複日報／子組|
|TRANSFILES-PROD-1028-TASK-005|P1／TransFiles；SPC|落塵正式Particle匯入adapter；依001／002，004收尾後執行|R1～R9×四粒徑preview／confirm查回|Count0、大批次、重複檔／事件|無效日期、非整數／負值、無體積U圖|沿10/22修補，不退回舊Attribute當Particle|
|TRANSFILES-PROD-1028-TASK-006|P1／TransFiles；三類adapter|共用非互動runner、檢查點／去重／限次重試與紀錄；依003～005|手動／CLI同結果、重跑不重複|中斷後續傳、同檔追加、排程重疊|衝突、錯誤hash狀態、斷線不記成功|三類交易粒度、告警／統計狀態不誤報|
|TRANSFILES-PROD-1028-TASK-007|P2／TransFiles；執行主機|GUI固定時間設定與Windows排程管理；依006與核准身分|每日HH:mm／多時段、GUI關閉可執行|跨日、睡眠／漏跑補一次、重開機|時間空白／非法、路徑失權、認證過期|預設停用、防重入，停用不刪來源或資料|
|TRANSFILES-PROD-1028-TASK-008|P2／TransFiles；SPC、Portal|操作文件、獨立EXE、三類測試／正式小批對帳及排程啟用驗收；依001～007|來源→批次→資料→圖表／統計逐層對帳|零新增、部分成功、回復演練|未授權／未備份／未對帳不得啟用|舊版可回復，正式結果不以mock取代|

## 各件影響與回復
|Task|DB／權限影響|發布影響|回復|
|001|僅先定契約；實作若需正式執行身分／角色，具體列明另授權，不預先授予|可能SPC／Portal認證支援，範圍依契約決定|撤回本件設定／授權，保留既有角色|
|002|無schema；只讀來源與主檔|TransFiles GUI／設定|回原設定版本，原檔不動|
|003|核准後化學量測／receipt／trace／job寫入；現有協定優先，不預設新schema|TransFiles及必要SPC服務|停入口，按receipt／batch查回補正，不Down刪trace|
|004|核准後Portal／SPC日報／量測與同步狀態|兩站必要正式流程與工具|停止新送出，保留Pending並依ID續傳／補正|
|005|核准後SPC Particle批次／量測|工具及必要SPC端點|停止新批，依batch ID追溯與受控補正|
|006|沿adapter寫入；執行日誌／檢查點不存機敏資料|工具runner|停runner，保留成功檢查點與批次，不從0重灌|
|007|不改DB；可能Windows工作建立／更新與執行帳號設定|安裝／啟用排程另授權|停用本工作與profile，還原排程定義|
|008|只核准來源小批驗收；正式寫入／啟用另授權|獨立EXE與已核准網站候選|備份版本／設定／批次對帳，按核准方案回復|

001已執行契約與來源環境防護測試，實際身分尚未驗證；其餘7件未實作。未重包EXE、未開通正式權限、匯入或排程；001未結案，不以來源測試通過取代正式身分核對。
