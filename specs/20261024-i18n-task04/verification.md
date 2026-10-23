# 驗證：I18N-TASK-04
- 日期：2026-10-24；規格 v1；實作/必要驗證/測試站發布完成。
- [規格](spec.md)｜[計畫](plan.md)｜[任務](tasks.md)；停止於 TASK-04，不開始 TASK-05。
## 驗收對照
|驗收|方式與實際結果|狀態|
|---|---|---|
|AC-001|47 keys × 3 locales 完整性/重複key/可攜語法/placeholder檢查；實際 adapter 的逐key繁中fallback及來源缺失診斷通過。html lang/三語顯示通過。|通過|
|AC-002|SMTP未存欄位DOM/值、品質履歷條件、URL保留且切換不再取API；SPC/趨勢兩頁ECharts實例ID/日期/URL/常態結果保留。|通過|
|AC-003|精確allowlist、重載同人恢復、不同subject/匿名/環境隔離、已登入但缺合法id不讀匿名cache。|通過|
|AC-004|核心storage throw與瀏覽器locale key讀取SecurityError/寫入QuotaExceededError降級，顯示提示且保留本頁語言與草稿。|通過|
|AC-005|Viewer/Editor角色與原路由拒絕保留；姓名HTML字串仍純文字；參數不二次解譯。|通過|
|AC-006|1920/1366/1280/390寬度×三語12組，header/選單控制在視窗內、鍵盤焦點與文字換行通過；390英文截圖目視。|通過|
## 命令與結果
- 核心測試先建，尚未實作時因 preferences.js 不存在失敗；實作後通過。最終 `npm run test:i18n`：26 passed，無略過。
- `npm run build:test`：成功；建置先檢查47×3鍵。既有大chunk警告保留；Vue I18n 11.4.16精確鎖定，Node22.20.0，無其他既有套件升版。
- `npx playwright test --config playwright.i18n.config.ts`：本機17 passed；Windows測後Vite cleanup等待，核對PID/5198命令後只終止本次子行程，runner exit0。
- `SPC_I18N_TEST_URL=https://SPCTEST.pmr.com.tw` 同config：已發布資產17 passed，含pageerror=0。初次本機fixture曾把src/api模組誤當API攔截而無法載入，已改pathname檢查；匿名案例改為站內狀態切換，避免離站SSO，此為測試修正，未放寬產品登入。
- `SPC_NORMALITY_TEST_URL=https://SPCTEST.pmr.com.tw` / playwright.normality.config.ts：8 passed，含新增兩張圖三語切換實例/條件保留回歸；AD/極小值/缺結果既有驗收不變。
- `node scripts/verify-built-api.mjs https://SPCTEST.pmr.com.tw`：14 API request均同源。
- 上述瀏覽器用合成localStorage登入fixture且攔截所有API；未送出業務寫入，不代表真人SSO、實庫計算或真人翻譯審核。真實version/health另以無token唯讀核對。
- `npm audit`列出既有相依14項（3 moderate/11 high），沒有Vue I18n/@intlify項；未擴張修補。既有EquipmentPointsView table/tr開發警告不在本件修正。
## 發布與回復
- 唯讀核對IIS SpcSingleTest，根路徑 D:/SPC/release/test/backend，AppEnvironment=test；Program.cs靜態根為相鄰frontend。
- 發布 D:/SPC/release/test/frontend，Web0.1.72；backend/API沿用0.1.72。先備份32檔及逐檔SHA256核對，再複製4個payload（assets先、index最後）；4個web.config/appsettings設定雜湊不變，保留舊assets，無停pool/後端重啟、migration、DB寫入或正式操作。
- 真實 /api/version、/api/health：200/test/0.1.72，health status=ok；實際提供 index.html、主JS及CSS SHA256與dist相同。
- 本機證據與備份：`.codex-tmp/i18n-task04/`（不提交），backup-frontend可回復原Web0.1.71。回復時先恢復備份assets、最後index，保留目前設定/現場資料；不整庫還原、不刪偏好key，舊程式忽略它。
## 限制與後續
- 只翻共用App header/sidebar/環境角色標籤，頁面內文仍原語言；47鍵/141筆[審核帳本](translation-review.json)均draft，hash已驗，真人審核留TASK-20。
- 未做全站手機layout改造；既有256px側欄仍壓縮390px主內容，這次只驗證新增header/選單控制與換行。各頁表格/圖表長文排版在TASK-05～07/21逐批驗收。
- 只用站內、環境及SPC本機subject保存偏好，不能當授權依據；非Portal帳號持久化、無跨站/跨分頁同步。未改登入、角色、原始資料、公式或圖表計算。
- 沒有新增backend邏輯，故本件不重跑後端build/test；不是後端測試已通過之聲明。
- 已同步 docs/requirements.md、CHANGELOG_CUSTOM.md、TODO.md、主任務表與共用規範索引。剩 TASK-05～21 共17包，另受阻1/暫緩2。
- 技術依據：[Vue I18n安裝](https://vue-i18n.intlify.dev/guide/installation)、[Composition](https://vue-i18n.intlify.dev/guide/advanced/composition)、[Fallback](https://vue-i18n.intlify.dev/guide/essentials/fallback)。
