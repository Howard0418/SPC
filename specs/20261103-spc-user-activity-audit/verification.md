# SPC活動稽核驗證
- 功能 ID：SPC-AUDIT-20261103
- 規格版本：2
- 日期：2026-11-03
- 結果：TASK-001、002完成；儲存基礎已發布測試API98，003～009未開始，正式未發布。

## TASK-002 當前驗證與發布

- [002規格與BDD](task002-storage.md)：核心先紅測試再實作；本件20項通過（含隔離SQL），完整回歸主342／另一後端66通過，8＋1略過；Release build成功。
- AC-002儲存／去重及R-010交易基礎通過；AC-003只驗共用writer從Operator讀快照，完整JWT／SSO事件屬003；量測等實際串接屬005，進頁端點屬006，查詢／保留／效能屬007～009，未冒稱全站驗收。
- 隔離SQL：8併發同key只1列；不同key獨立、內容衝突拒絕；EF／SQL append-only、writer role SELECT／INSERT及拒絕UPDATE／DELETE／ALTER；實際EF idempotent SQL兩次與Up／Down／Up通過；業務回復／事件insert失敗不留成功資料。
- 測試實際目標PC-PA16560048、PMR_SPC_TEST／test；migration59→60且無其他pending。68個既有表全量排序JSON的逐表SHA-256在migration後及服務啟動後再次核對均一致，AuditEvents=0；未寫正式DB或變更既有帳號成員／驗證設定。
- 舊工具在iis-preflight因過時站台配置停止，未開始寫入；本件限定流程核對SpcSingleTest前端／SpcApiTest後端／SPC專用共用池，先IIS、API及SQL COPY_ONLY／CHECKSUM備份與VERIFYONLY，再只套新增migration與API98。前端全部檔案雜湊保持Web115。
- 實站https://spctest.pmr.com.tw：Web、主JS `index-D8IBkhbb.js`、CSS `index-BFEI4qhU.css`皆200且MIME分別text/html、application/javascript、text/css；health/version皆API0.1.98／test。實際瀏覽器載入的JS其tJ()固定返回 `/api`，同源再次驗證成功；瀏覽器總覽已載入，顯示Web115／API98且無API離線。無非標準port的用戶端路徑。
- 私有備份／部署證據：`C:/ProgramData/PMR-Releases/Test/SPC-AUDIT-002-20261103-API98-topology-verified/`。必要時用該目錄原API檔與雜湊回復API97，保留新表與證據；未演練實站rollback，已驗證隔離DBrollback。
- 版本截圖僅含測試環境與版本，無姓名／量測資料；存上述私有目錄`test-version.png`，SHA-256 `1943DEAE8522BC23BC6629B4D6D49BEFB7C583A588F2DF91C3CD27CE2C3886D8`，90日到期2027-02-01，無實際事件ID（本件只驗儲存，未建立使用者事件）。截圖／輸出／備份均不提交Git。
- 原進度表已同步第255列完成／待辦否與11/03綠色原生里程碑43，保留43個圖形；核心未完成42、SPC直屬20、完成186，人工11／核銷4另計。備份`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_SPC稽核002儲存完成前_20261103-093102.xlsx`；原檔輸出SHA-256 `608f329aa81291964804f15709c66d6dfdcdc03b1f8684b7964cb1511e68f182`。
- ZIP及OpenXML候選／原路徑0錯誤，Artifact重開／公式錯誤0／範圍渲染通過、LibreOffice列印通過；未做Microsoft Excel人工開啟。原檔更新前再次核對雜湊，其他專案及未變更part保留。
- 相依待辦：測試站Auth:Enabled=false，實際runtime尚未配置新writer role；003啟用收集前需處理驗證與runtime分權，不能宣稱目前已完整記錄誰登入／進頁。本次不開始003。

## 以下為001及原規劃歷史紀錄

## 已完成的唯讀盤點

- 核對現行 Portal SSO→SPC JWT 流程、本機 login 停用、Router 權限 guard、共用 actor resolver、Editor 預設權限解析。
- 核對現有校驗稽核、藥液／人工量測追溯及 `BaseEntity` 稽核欄位；確認它們都是模組或最後狀態紀錄，不能滿足全站活動歷程。
- 搜尋未找到全站 `AuditEvents`、稽核查詢／匯出頁、保存／封存與清理規格。
- 比對前案明確非範圍：既有共用 actor 解析器沒有建立完整 audit log。
- 完成事件格式、敏感排除、權限、保存、容量、可靠性、截圖與四類測試建議；拆成9件並依風險排序。

## TASK-001 本次進度

- 依使用者「SPC直屬先執行」指示，先選安全／權限優先且可獨立進行的 TASK-001；正式發布待辦及受外部資料阻擋的工作未插隊。
- 新增[event-contract-v1.md](event-contract-v1.md)，固定伺服器 EventId／前端 ClientEventId、結構化 Actor assurance、事件代碼與 Outcome 分離、現有31個 canonical PageKey 及 redirect／alias 對照。
- 補齊 `DetailsJson` 各類白名單、敏感負面清單、關鍵／一般事件失敗矩陣、格式錯誤分流、最小權限、保存／legal hold、截圖與 BDD。
- 唯讀核對現行只有 Viewer／Editor、Editor 對目前權限目錄預設全授予、`AuthorizationAccess`／Router／Viewer write guard 等相容性風險；因此稽核權限必須另採明示授權，不能直接加入既有 Editor 預設目錄。
- 技術契約可供核准，但保存與敏感權限屬業務／隱私基準；「開始執行」不推定為已核准。TASK-001 保持進行中，TASK-002 未開始。

## 驗證狀態

| 項目 | 結果 |
|---|---|
| 規格／計畫／任務追溯 | 規劃及 TASK-001 候選契約已完成；R-001政策核准未通過，AC-002～012未執行。 |
| Happy／Boundary／Invalid／Regression 規劃 | 已完成，見 spec、plan、tasks。 |
| 程式修改 | 未執行；TASK-001為契約定義，政策核准前不進入TASK-002產品實作。 |
| DB migration／資料寫入 | 未執行。 |
| build／自動化測試 | 不適用；本次沒有產品程式變更。 |
| 測試站／正式站發布 | 不適用；兩站均未修改。 |
| 截圖 | 本次沒有使用者提供的新截圖；未擷取登入或操作畫面。未來驗收截圖依AC-012遮罩並留在Git外。 |

## 尚待核准

登入範圍、操作範圍與截圖技術邊界已固定：只含 SPC 可見的 SSO／本機停用登入／登出請求，記成功進頁與業務操作，不記每次點擊／輸入／捲動；截圖只作 Git 外測試證據。仍待一次核准[event-contract-v1.md 第一節](event-contract-v1.md#一待核准的四項政策)：

1. 稽核權限全部明示授權，禁止 Viewer／一般 Editor 預設取得及自我授權。
2. 關鍵事件總保存5年、頁面／一般查詢總保存1年、測試截圖90日，legal hold優先。
3. v1不收集IP或原始User-Agent。
4. 一般查詢只記已送出的欄位名稱、日期範圍與結果筆數，不記查詢值。

上述政策阻擋 TASK-001 完成及 TASK-002 實作；不影響既有網站使用。

## 發布與進度表

- 應用程式發布：不適用。
- TASK-001 本次狀態同步：原檔「小工作總表」第254列改為「技術契約草案完成／待政策核准」，目前待辦仍為「是」；「專案進度表」11/03橙色里程碑及摘要改為001進行中、002未開始。登錄229、核心未完成44、完成184、人工11、核銷4、SPC直接相關22均保持。
- 本次更新前備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_SPC稽核001契約草案前_20261103-090957.xlsx`；原檔取代後 SHA-256：`6c76842f291d377cdfe9b05fb75ec460eb28fb8fba121d67393b31d453d9cfab`。
- 本次相容性驗證：ZIP封裝正常、OpenXML SDK 0錯誤、Artifact由候選及原路徑重開、關鍵儲存格／公式錯誤0、既有43個圖形保持且第43個里程碑名稱同步、兩處變更範圍渲染通過；LibreOffice可開啟並輸出PDF。Microsoft Excel人工開啟未執行。回復時以本次更新前備份覆蓋。
- 進度表：已在原檔「小工作總表」新增 `SPC-AUDIT-1103-TASK-001～009` 九列，目前待辦均為「是」；登錄229、核心未完成44、完成184、人工11、核銷4，SPC直接相關22件。父規格不重複計數。
- 「專案進度表」新增11/03原生橙色規劃里程碑與四列規劃事件，原42個圖形保持並新增1個；其他專案內容未改，列印範圍延伸至專案列110／總表列262。
- 更新前備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_SPC使用者活動稽核規劃前_20261103-085513.xlsx`。
- 驗證：ZIP封裝正常、OpenXML SDK 0錯誤、Artifact從候選及原路徑重開、關鍵儲存格／9代號／公式錯誤0、43圖形與渲染通過；LibreOffice可開啟並輸出12頁PDF。Microsoft Excel人工開啟未執行。
- 原檔取代後SHA-256：`16fcfd98b42ff389647c8c7944e2880dacee6d9fec88c017f970932e3c4a8ec0`；需回復時以更新前備份覆蓋。
> 核准收尾（2026-11-03）：使用者明示「同意」四項政策，R-001／TASK-001文件驗收通過，TASK-001 DONE。後文待政策核准狀態屬較早歷史。002～009及產品BDD尚未執行；build／發布不適用。

## 2026-11-03 核准收尾驗證

- 使用者同意四項具體政策，TASK-001文件內容、契約與BDD規劃核對通過；TASK-002～009未開始，產品行為測試未執行。
- 原進度表已取代並從原路徑重開；第254列已完成／待辦否，第255列待執行，核心43、SPC直屬21、完成185；11/03原生里程碑43改為綠色完成，43個物件保留，日期仍為實際11/03。
- 備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_SPC稽核001核准結案前_20261103-091455.xlsx`；輸出SHA-256：`9287d1847889cb9ca9ff2ede838d463e1c777371cd71112a17d9273f0c87df74`。
- ZIP正常、OpenXML SDK候選及原路徑0錯誤、Artifact重開／公式錯誤0／變更範圍渲染通過、LibreOffice列印PDF成功；未做Microsoft Excel人工開啟。未變更part逐一保留；回復可使用上述備份。
- 本次無程式／DB／角色異動，build及測試／正式站發布不適用。
