# 正式API授權與發布

2026-11-03使用者明示同意本件SPC／Portal正式API及專用匯入開關。於日期顯示測試工作收尾、進度同步及提交後，依序執行本階段；該授權明列保留正式Web，不包含日期顯示前端正式發布。不重匯、不啟用排程、不改schema／角色或AD。

## 基準與部署

- 實際SPC：PMR-SPC-SRV、SpcApi、`C:/inetpub/wwwroot/production/backend`、API92之ProductVersion來源1cb8f8e9；此來源與原候選bfb9ef91的backend檔案差異0，故既有隔離API102候選可用。DB `172.16.110.16/PMR_SPC_2026`。
- 實際Portal：PMR-WEB、PmrPortalApi、`C:/inetpub/wwwroot/publish/portal-api`、API204／Web247，ProductVersion來源b022c576。比原API200候選更新，已棄用舊候選並從b022c576套本件三檔重建，保留正式修正。實際正式DB是既有白名單 `172.16.110.16/PMR_PORTAL_UAT`，並非測試DB。
- 兩站執行前已建立私有ACL檔案／設定備份及IIS備份：各主機 `C:/ProgramData/PMR-Releases/Production/TRANSFILES-ETCH-1103-API`。未提交備份或機敏設定。
- 無待執行migration。SPC候選54／既有已套59，Portal候選27／既有已套29；候選全部包含於已套用清單，額外歷史項目均為原先已存在，不是本次新增。發布後完整migration history保持。
- SPC首次SMB傳送被主機拒絕，在任何payload複製前回復原API92並驗證健康；改以既有WinRM傳送後成功發布API102。保留SpcWeb／原前端115。
- Portal標準 `scripts/publish-all.ps1`新增可選 `-ApiOnly`，預設All行為保持。先以真實腳本／隔離mock-dotnet執行預設與API-only兩模式：版本、build、maintenance範圍及設定／Web sentinel通過。由隔離來源根執行標準腳本，直接輸出到正式IIS UNC目錄 `\\PMR-WEB.pmr.com.tw\c$\inetpub\wwwroot\publish`；非手動複製包、未發布D:/Sites/PmrPortal。實際API218，Web247及其全部檔案保持。
- Portal正式5個API主組件與隔離SDK publish輸出逐檔hash相同（DLL／apphost／PDB／deps／runtimeconfig）。首次拿build目錄的deps相比不一致，是SDK build與publish依賴描述不同；改以同版無重建publish輸出後5檔全吻合，沒有為此重發正式或改資料。兩站IIS均無專用旗標環境覆寫。
- 兩站僅新增／設定 `EtchImport:AllowProduction=true`；其餘設定語意完全相同，既有認證／SSO金鑰不變。Portal在新API已完整發布後才啟用開關，未讓舊strict程式先放行。

## 發布後驗證

- SPC正式首頁／目前主JS與CSS：200／MIME正確。實際瀏覽器API基底 `https://PMR-SPC-SRV.pmr.com.tw:8443/api` 的health／version：200、production、0.1.102；使用者電腦直連8443可達，CORS preflight204／允許Origin正常。實際TransFiles production環境前置檢查通過。
- Portal正式首頁／site.js／site.css：200／MIME正確；Web app-version 1.0.247.0保持。Web實際上游 `https://login.pmr.com.tw:8443/health` 回ok、database ok、production；已發布DLL 1.0.218.0，為其等效API版本核對。無TLS／DNS／連接埠／Host／CORS或環境錯置失敗。
- 未登入的SPC inspect、Portal直連preview及Web代理preview均401／403拒絕，未繞過權限。沒有對正式送出confirm、建立測試量測或擷取真人登入權杖。
- 發布前後SPC69個表、Portal74個表的筆數／BINARY_CHECKSUM指紋**全部相同**；原量測、批次、主檔及角色保持。SPC38／Portal570受保護Web、設定與附件檔案，除已核准開關的主設定JSON外hash皆保持；主設定移除該旗標後的序列化內容完全一致，migration history相同。
- 再次正式ReadOnly逐份核對來源134份、10384原始點／速率，全部ALREADY_EXISTS／Verified；10/16 PT1已為第二次，沒有重匯、來源／現有資料指紋保持。
- 正式入口登入已逾期，已開啟既有正式登入頁讓使用者自行登入；登入後瀏覽器畫面／實際API版本驗收仍待完成，不能把HTTP成功當真人畫面通過。EXE原生畫面及正式工具登入／preview也保留人工待辦；未以正式新增寫入來驗收，因授權要求保留既有資料。

## 回復及證據

如需回復，確認各自正式Host／IIS／DB，讓API暫時離線或停止獨立API pool，以各主機備份還原API檔及主設定（撤回專用旗標），恢復服務後驗證原版本／環境與Web、健康。Portal仍依標準腳本準備／發布回復版本，保持Web與uploads，不做DB資料回復或重匯。備份均在該主機私有ACL路徑。

本機Git外證據：`D:/SPC/.etch-rules-20261103/formal-*`，含preflight、兩站備份摘要、migration catalog、部署、69／74表與38／570保護檔核對、HTTP／權限／CORS、來源ReadOnly查回。正式版本及專用旗標已更新；登入後瀏覽器及EXE人工項目未完成前，整件不標DONE。

## 原進度表與提交

原Voice/進度表.xlsx已備份後直接取代，SHA256 `954f154ec098423859e33eb1e999782fd5458fe3e35d39cf2a99df61c64f9d1d`。新增119～122列正式API實際部署事件與第47個原生藍色里程碑，原46物件及非本件內容保持；同表263列更新正式API部署／人工待驗，Portal本件摘要同步。231登錄、核心43／SPC直接21、完成187、人工11／核銷4保持，不把尚待驗收工作算完成。

ZIP／日期／內容／Print_Area／原生物件核對、OpenXML SDK候選與原路徑0錯誤、artifact重新開啟渲染／公式錯誤0、LibreOffice12頁列印預覽通過；Microsoft Excel人工開啟未做。更新前備份 `Voice/備份/進度表_咬蝕正式API部署前_20261103-130650.xlsx`，原檔hash護欄與取代後重讀通過。只提交本件程式／版本／發布選項測試及文件，不提交發布產物、設定、備份或證據。
