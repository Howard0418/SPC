# 驗證：I18N-TASK-05B
- 2026-10-24，規格v1；實作/必要驗證/測試發布完成；[規格](spec.md)｜[計畫](plan.md)｜[任務](tasks.md)。
## 驗收對照
|驗收|結果|狀態|
|---|---|---|
|AC1|四頁公開介面188新鍵三語；模板AST/靜態屬性及穩定key檢查。原資料名、狀態碼、WE及JSON不翻譯。|通過|
|AC2|同路由/DOM/草稿/選取值保留；機台/槽體、規則與大類別切換不額外讀寫API；量測查詢類型與0.2原數值保留。|通過|
|AC3|三語process/machine/tank/group新增或編輯/刪除fixture逐欄比對DTO與原旗標/父ID/數字；機台槽體prefix/trim/null處理、CONTROL_CHART/TREND_CHART、機台IDLE/MAINTENANCE及Rule1/WE/JSON原格式不變。規則僅既有編輯，不增加第9種規則。|通過|
|AC4|空白必填、非法JSON、刪除取消、409原API訊息/關聯資料及草稿保留；UI訊息按顯示時翻譯，API原文不再編譯；四頁空資料。|通過|
|AC5|四頁×三語×1920/1366/1280/390共48版面案例；模態/內嵌控制與儲存可達。手機槽體實測原14px→242px；四路徑用既有校驗上下排列，Viewer四頁原守衛不變。|通過|
## 測試與建置
- 新顯示helper先建測試，模組不存在時失敗；最終`npm run test:i18n`：37 passed、0 skipped。新增key/args helper保留API文字、延後解析與參數不改寫。
- 全字典345鍵×3語檢查；188新鍵/564 draft列與前批合計1035列覆蓋全部runtime key。來源/譯文hash及placeholder檢查通過；[本批帳本](translation-review.json)全部draft，reviewer/reviewedAt=null。
- 本機初輪3個失敗為唯讀槽體碼selector不存在，改用原readonly輸入並驗prefix，不改模型。手機製程編輯紅燈/槽體寬14px已先記規格，再限四頁修正佈局。
- 開發輪簡中文字「设定」與既有「设置」不一致已修正文案/帳本；曾受字典HMR重載干擾的草稿案例，改以最終build preview驗證。最終產物74 passed，加補4個空資料及1個取消/關聯409案例5 passed，共79個不同情境通過。
- `npm run build:test` Web0.1.74成功，保留既有大chunk警告；未新增/升級套件。開發EquipmentPointsView既有table/tr警告不在本件。
- Windows runner完成後等待preview cleanup，只核對並停止本次5200子行程，74及5兩輪exit0；沒有停止IIS或使用者瀏覽器。
- `SPC_I18N_TEST_URL=https://SPCTEST.pmr.com.tw`：foundation config已發布79 passed；master config先前主檔39 passed；shell config17 passed，合計135不同情境，皆無pageerror。`node scripts/verify-built-api.mjs https://SPCTEST.pmr.com.tw`：14個API請求均同源。
## 發布與回復
- 重新核對IIS SpcSingleTest實體根D:/SPC/release/test/backend與AppEnvironment=test；只發布相鄰D:/SPC/release/test/frontend。
- 備份37檔並逐檔SHA256核對；複製4個payload（assets先/index最後），4個web.config/appsettings雜湊不變、舊assets保留，未停pool/重啟API/改DB或操作正式。
- Web0.1.74，API沿用0.1.72；無token唯讀/api/version與/api/health皆200/test/0.1.72，health status=ok；實際index/主JS/CSS SHA256與dist吻合。
- rollback：`.codex-tmp/i18n-task05b/backup-frontend`回復Web0.1.73，assets先/index最後，保留現場設定/資料；不整庫回復。測試輸出、截圖/備份留`.codex-tmp/i18n-task05b/`，不提交。
## 界線與限制
- 瀏覽器全部業務API攔截為fixture，CRUD只檢查原DTO/前端行為，沒有合成token到後端、無實庫資料新增刪改；真實version/health另以無token唯讀呼叫核對。非真人SSO、真DB或真人翻譯驗收。
- 只處理大類別預設groups分支，小分類/types與公式內容仍留05C；沒有新增入口或修改既有tab/query路由規則，不宣稱所有分支已翻譯。
- 只四頁沿用既有校驗手機導覽，其他頁仍維持既有版面；非全站手機驗收。
- 無後端邏輯變更，後端build/test不適用；未改規則引擎、schema、角色、公式或其他系統，不發布正式。
- 剩19小工作（05C～05E三件＋06～21十六件），另附件掃毒受阻1、Runtime/TransFiles人工故障驗收暫緩2；STOP於05B。

- 簡中初稿先以Windows LCMapStringEx轉換字形，調整「设置/模块」與既有用語一致；英文初稿逐項撰寫，兩者均待真人審稿，不以機械檢查宣稱文意合格。

- 已同步需求、變更紀錄、主任務表/TODO及共用規範索引；提交上傳Develop僅本件必要檔案，不提交產物/測試輸出/截圖/備份。
