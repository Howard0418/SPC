# 正式候選與授權範圍

候選已編譯、尚未發布：SPC API0.1.102（正式來源bfb9ef91／API92加本件三檔及先前已驗證機台對應）；Portal API1.0.218（正式來源2086020／API200加本件三檔）。不含測試Audit、其他尚未核准正式功能、schema、角色、AD、IIS binding或正式Web修改。

候選包與逐檔SHA256留本機Git外 `D:/SPC/.etch-rules-20261103/formal-candidate`；不含環境設定／憑證／來源資料，ZIP僅本件API主組件與相符metadata。編譯不能當正式部署／端到端驗收。

若授權正式發布，範圍為兩站本件API及專用 `EtchImport:AllowProduction` 啟用；不重匯既有134份、不啟用排程、不更動正式Web。正式僅新增／第二次受控取代仍須完整日線與查回核對，不能以版本發布當資料匯入許可。

執行前核對實際正式Host／IIS路徑、DLL版本與hash、DB及上游；若正式基準有其他補丁，先保留並重整隔離候選，不能降版／帶測試新功能。Portal依其AGENTS標準publish-all流程準備／發布，不直接拷貝此ZIP繞過正式流程；保護設定／uploads及正式Web現版。完整備份與回復點後才部署，失敗回復原版，驗證Web／目前主JS及CSS200/MIME、實際API基底health／version環境與瀏覽器端到端。

正式仍須另行授權，依[D:/SPC/AGENTS.md](../../AGENTS.md)：「正式站仍須另行授權，並核對實際目標環境。」本件修改工具與測試的授權不等於正式API／寫入開關發布授權。尚未執行上述正式發布或開關變更。
