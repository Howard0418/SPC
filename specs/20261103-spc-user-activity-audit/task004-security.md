# TASK-004：安全、操作者、權限與設定稽核

- 代號：SPC-AUDIT-1103-TASK-004；2026-11-04使用者要求下一項小工作後開始，依安全／權限順位及003已完成的相依選取。
- 狀態：DONE（本件測試階段完成）；2026-11-04測試API106已發布，Web119保持，正式未發布。主系統SPC，Portal只作原身分來源；不改Portal／TransFiles／KM。
- 基準：[核准契約v1](event-contract-v1.md)、R-005／R-008。新增程式先建測試；只發布測試站，不動正式、量測、公式或F資料。
- DB：沿用AuditEvents與Operator.PagePermissionsJson，無schema migration、SQL角色或實際使用者授權變更。安全管理者白名單採私有設定的Operator不可變ID；預設空集合，不替任何人授權。

## 範圍與實作計畫

1. 新增獨立敏感權限判斷：`admin.audit.read`、`admin.audit.export`、`admin.audit.evidence`只讀目前DB的啟用／未刪除Operator及明示JSON；不信任JWT舊權限或Editor預設目錄。export／evidence都必須同時有read。
2. 敏感授權變更只允許目前啟用的Editor安全管理者（`Audit:SecurityAdministratorOperatorIds`白名單），不得新增自己的敏感權限；白名單空時無人可授權。普通管理者不能藉改綁白名單人員或已持有敏感權限者的帳號／工號／角色／啟停／權限、刪除該人員，或新建帳號碰巧取得白名單ID而繞過授權。一般姓名／聯絡資料操作保持原管理權限；關閉收集時也不能走未留痕的敏感異動路徑。
3. `Audit:SecurityEnabled`預設false；啟用須已有003的AuthenticationEnabled、Auth、有效環境／金鑰及受限runtime。測試開啟本件旗標，白名單保持空，不改既有登入金鑰或實際角色。
4. MVC filter包覆Operators新增／更新／停用／角色／頁面權限／刪除、Portal人員同步、SMTP設定保存及報表排程設定保存；依目前DB核對操作者與原管理頁權限。一般其他業務、公式、量測及前端導覽不擴張。
5. DB操作與成功事件同DbContext／外層交易。更新／刪除先依原DB狀態建立事件，避免自行停用或降權後遺失原可信操作者；新增取得真正ID後補事件，成功提交前必須持久化。DirectorySync沿用外層交易，不再開巢狀交易；不保存完整before／after或聯絡資料。
6. SMTP還涉及檔案，不能宣稱SQL能回復檔案。先持久化固定`security.setting.prepare`意圖（同類Security白名單欄位），再保存設定與同交易成功事件；失敗rollback DB並在同程序受限鎖內回復原檔，回復失敗安全告警、維持503，已保存意圖供追查。程序中斷留下未結清意圖，須依關聯ID核對檔案／DB後補結果，不自動重送郵件或任意覆蓋設定。
7. 包覆401／403與MVC模型錯誤，記`security.access.check`或對應固定操作失敗碼；只保存canonical PageKey、固定原因、欄位名稱／權限代碼／狀態，不讀取原始body、query、JWT、IP、UA、設定值或例外原文。拒絕稽核不可用不放行，僅固定告警。
8. Catalog可顯示三個明示權限，Editor預設不得包含；原有一般頁權限預設保持。稽核查詢頁與匯出仍是007，本件不新增對外查閱端點。

外部寄信、send-now、匯出內容、前端進頁／一般查詢事件不在本件串接範圍，仍依005／006；只沿用原功能與記錄其401／403拒絕，不實際寄信驗收。

## BDD與驗收

- AC1 Given沒有明示授權的Editor／Viewer、未知／停用／已刪除身分、損壞JSON或僅JWT舊權限，When檢查敏感權限，Then拒絕；DB撤權立即生效。明示read正常通過，缺read的export／evidence拒絕。
- AC2 Given普通Editor、空白名單、自我加權或偽造權限代碼，When修改敏感權限，Then安全拒絕且DB不變；白名單管理者可替其他人依依賴授權，不可透過身分變更繞過白名單。
- AC3 Given正常管理者更新隔離操作者，When保存，Then產生正確可信actor、不可變target ID與實際異動事件；角色／啟停／權限獨立可追溯，不保存密碼／Email／部門或原物件。
- AC4 Given成功事件插入失敗，When新增／更新／刪除或同步，Then整個業務交易回復且不回成功；不得留下成功事件。錯誤回應／安全log不含provider或request原文。
- AC5 GivenSMTP設定保存且稽核不可用，Then在檔案修改前拒絕；若最後DB保存失敗，Then回復原檔及DB、保留已保存意圖／安全失敗結果。正常成功保留prepare及對應change結果，不把prepare當成設定已套用。
- AC6 Given未登入、Viewer寫入、失效角色及格式錯誤，WhenAPI拒絕，Then原401／403／400保持並留固定碼；記錄故障不變放行，無重複拒絕事件。
- AC7 Given原一般業務與003登入／登出，Then必要回歸保持；安全旗標關閉時非敏感舊行為保持，敏感權限仍無角色預設。
- AC8 測試發布必須核對實際IIS／DB、備份、設定保護、Web／主JS／CSS200及MIME、API health/version與實際前端使用基底；不夾帶既有TrendChart修改。瀏覽器工具故障不能偽造真人操作，009完整E2E仍待辦。

測試面：Happy涵蓋授權及成功事件；Boundary涵蓋self revoke／grant、舊JWT、無jti、批次、no-op及檔案／DB邊界；Invalid涵蓋權限缺依賴、未知碼／長度、actor不明、模型錯誤、稽核故障；Regression涵蓋原Editor頁目錄、Viewer guard、003、設定密碼保留及公式／圖表相關必要回歸。自動化先紅後綠，SQLite真實交易及隔離SQL驗證不得指向正式庫。

## 任務與發布／回復

- [x] T1：核對契約、相依、範圍及記錄修改目的。
- [x] T2：先建核心測試並確認紅燈。
- [x] T3：實作敏感權限、原子事件、拒絕及設定意圖。
- [x] T4：必要自動化、隔離SQL／HTTP／build與回歸通過。
- [x] T5：備份並僅發布SPC測試API，保留Web119與現場資料；驗證實際連線及環境。
- [x] T6：同步需求、工作池、進度表實際日期／原生里程碑、提交上傳；未驗部分明列，不開始005。

回復為還原本件前API105及本件私有旗標（保留003設定與既有白名單），保留append-only事件，不刪表／事件、不回升runtime權限。SMTP未結清意圖僅依具體關聯ID處理，不能以整庫還原覆蓋其他人的新資料。

## 驗證紀錄

- 2026-11-04：TDD先紅後綠；第一批21項、4項敏感權限／身分改綁邊界、2項小寫HTTP方法／舊JWT角色邊界先失敗後修正。最終本件36項本機通過（含真實Kestrel HTTP），隔離SQL另1項通過。主API402通過／11條件略過，另一後端66通過／1略過；既有UnitTest1的2個CS8602警告未新增或修改。Release publish通過。
- AC1／2：僅讀目前DB明示權限，export／evidence依賴read；失效actor／JSON、舊JWT、空白名單、自我加權、既有持權者及未來白名單ID身分改綁均fail closed；旗標關閉也不能繞過敏感授權。普通非敏感操作回歸通過。
- AC3／4：SQLite與隔離SQL實際交易驗證成功事件／可信actor、自行停用／刪除、DirectorySync及排程；AuditEvents insert故障回503且回復業務異動，無殘留成功事件。未保存Email／電話／SMTP密碼／請求原文或完整物件。SQL受限runtime UPDATE／DELETE稽核事件均拒絕。
- AC5：SMTP檔案故障回復、SQL回復、prepare與結果的關聯ID通過；先保存意圖，未結清意圖不能當成已套用。未實際寄信；程序中斷／檔案回復失敗仍需依本件規格人工核對，不宣稱跨SQL與檔案的原子交易。
- AC6／7：本機真實HTTP匿名401、Viewer403、無效JSON400及成功PUT通過；現行DB已降權的舊JWT不能執行SMTP test／send-now。原API402項回歸含登入／公式／圖表相關測試通過。
- 隔離SQL先以Windows管理身分嘗試，因無CREATE DATABASE權限在建立前停止；改用既有受ACL保護的測試管理設定，僅建立並清除精確GUID隔離庫。WITHOUT LOGIN合成runtime只存在該庫；未升權實際API runtime，未更改任何實際使用者、角色或白名單。
- AC8：由HEAD e488d71b隔離來源只覆蓋本件11個API必要檔；API106候選編譯與依賴比對通過。實際IIS SpcApiTest／SpcSingleTest、SPC專用共用pool及PMR_SPC_TEST／test核對後，先備份IIS、API105、原私有設定與SQL COPY_ONLY／CHECKSUM／VERIFYONLY，再僅替換4個API檔並啟用Audit.SecurityEnabled。管理者白名單保持空，原Auth／金鑰／受限runtime與Web119全部29檔hash保持，migration60／pending0、68個業務表指紋保持。
- 實際https://spctest.pmr.com.tw首頁、主JS index-Cts0y9wi.js及CSS index-BJWc18W2.css皆200且MIME正確；health/version皆0.1.106／test。Playwright載入已發布bundle的路由fixture共14個API請求全部同源/api（該fixture攔截API，未把合成token送到站台）；再對該實際同源基底重驗health/version成功，實站匿名operators為401且僅新增一筆安全Denied事件，未保存查詢值。此為自動化HTTP／資料驗證，不冒稱真人登入操作或009完整E2E。
- 實站probe最初的驗證器誤以環境必為小寫test及Details必為空物件，核對既有契約後改為Test與僅SchemaVersion=1，再唯讀重核原同一事件通過；未重送／重寫事件，未因此修改產品。
- 私有備份及部署／前後指紋／probe證據：C:/ProgramData/PMR-Releases/Test/SPC-AUDIT-004-20261104-API106/；本機測試證據.audit004-20261104/results/不提交。回復用該批files與previous-appsettings.json還原API105及原旗標，保留所有事件／業務資料；已驗隔離SQL交易回復，未實際演練站台rollback。
- 瀏覽器操作工具仍有既有連線故障，未新增登入畫面／截圖；全案進頁、其他業務事件、查閱及保存尚屬005～009，不宣稱全站稽核完成。正式未授權，本件不動正式／Portal／TransFiles／前端產品，前端build不適用。
- 原Excel「小工作總表」257列已完成／待辦否、258列僅標004相依解除且未執行；11/04實際日期及新增綠色原生里程碑53、主摘要與同表明細同步，原52圖形及其他專案／未授權儲存格保持。登錄233、核心43、涉及SPC20、完成189、取消1，人工11／待核銷4另計。備份`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_SPC稽核004安全權限完成前_20261104-100535.xlsx`；輸出SHA-256 `e53381e184f8fb9c1ee3a442d29f73b1e2dbeffaefb96a0aedc4e281a20e6f45`。
- 候選及原路徑ZIP／OpenXML均0錯誤；artifact重開、公式錯誤0、變更範圍渲染及LibreOffice13頁列印（5／6／13頁）通過；未做Microsoft Excel人工開啟。原檔取代前重核來源hash、取代後重讀通過。提交上傳僅本件來源／測試／文件，不含輸出、設定、憑證或原Excel。
