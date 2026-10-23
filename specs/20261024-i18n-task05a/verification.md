# 驗證：I18N-TASK-05A
- 2026-10-24；規格v1；實作/驗證/測試發布完成；[規格](spec.md)｜[計畫](plan.md)｜[任務](tasks.md)。
## 驗收對照
|驗收|執行方式與結果|狀態|
|---|---|---|
|AC-001|新增110鍵三語；兩頁標題/指南/欄名/placeholder/狀態/前端驗證/成功/刪除確認/數量翻譯。模板AST檢查固定中文及未知key，參數/字典集合檢查通過。API資料名稱原文不改。|通過|
|AC-002|抽屜內復用頂列相同changeLocale；切語言後相同DOM/草稿/選單值/URL、篩選及指南展開保留，既存前端驗證/成功訊息即時翻譯。|通過|
|AC-003|兩頁三語完整POST/PUT/DELETE mock，逐欄比對原DTO、路徑、布林、數字及列舉；decimalPlaces=0/8，Variable/Attribute、DIRECT/FORMULA/RECORD_ONLY及濃度原始值保留。|通過|
|AC-004|必填空白無寫入、刪除取消無DELETE、409原安全API訊息/草稿保留；HTML/大括號/@參數純文字，成功訊息不插入HTML。|通過|
|AC-005|兩頁×四尺寸1920/1366/1280/390×三語24組，抽屜/語言控制/儲存可達，長文可換行；Viewer直接路由拒絕，無pageerror。|通過|
## 測試與證據
- 先建標題切換行為測試，修改前因無英文標題失敗；實作後本機39項全部通過。Windows webServer cleanup等待，僅核對並終止本次Vite5199行程後runner exit0。
- `npm run test:i18n`：30 passed；新增4項含110鍵/330 draft列hash/placeholder、兩頁模板與全部三語文字參數，與原pilot47鍵/141列合計覆蓋157鍵/471列。原pilot帳本不改寫。
- `npm run build:test`：Web0.1.73成功，先檢查157鍵×3語，保留既有大chunk警告；未新增/升級套件。開發時EquipmentPointsView原table/tr提示不在本件修正。
- `SPC_I18N_TEST_URL=https://SPCTEST.pmr.com.tw`，playwright.i18n-master.config.ts：已發布39 passed；playwright.i18n.config.ts：17 passed，原偏好隔離/storage失敗/草稿/權限回歸通過。`node scripts/verify-built-api.mjs https://SPCTEST.pmr.com.tw`：14請求同源。
- 瀏覽器以合成localStorage登入且攔截所有API（含CRUD）；不把fixture token送真後端，不建立/刪改實庫資料，不代表真人SSO或真人文意審核。真實API核對另以不帶token唯讀請求進行。
- 本機preview之1366英文Parts與390英文Characteristics抽屜截圖目視，欄位/語言與底部儲存正常；原生輸入框長placeholder可截顯示，模型內容不改。證據在`.codex-tmp/i18n-task05a/`，不提交。
## 發布與回復
- 腳本重新唯讀核對IIS SpcSingleTest根路徑D:/SPC/release/test/backend與AppEnvironment=test，靜態根相鄰frontend；只更新D:/SPC/release/test/frontend。
- 備份35檔逐檔SHA256吻合，複製4個payload（assets先/index最後），4個web.config/appsettings設定雜湊不變；保留舊assets，未停pool/重啟API、未操作DB/正式。
- 已發布Web0.1.73，實際index/主JS/CSS SHA256與dist相同；真實/api/version及/api/health為200/test/0.1.72，health status=ok。API沿用不升版。
- rollback：`.codex-tmp/i18n-task05a/backup-frontend`可回復Web0.1.72；先恢復assets再index，保留環境設定及現場資料。不整庫還原、不刪偏好/量測。
## 限制與後續
- 本次只完成05A；05B其餘主檔、05C管制項目/公式、05D量測、05E批次/預覽均未開始。不將父TASK-05全體標DONE。
- 110新鍵/330列[帳本](translation-review.json)均draft，真人審核留TASK-20；API錯誤原文保留，翻譯契約在TASK-08。未改全站手機sidebar，390主內容仍受既有側欄限制；本次驗證抽屜/控制，未冒稱全站手機驗收。
- 無後端邏輯變更，後端build/test不適用；無正式/Portal/KM/TransFiles發布或DB變更。
- 工作池按實際可追蹤小工作重新計數：原17包中05拆5批，完成05A後剩20＝05B～E四件＋06～21十六件，未增加新功能。另受阻1（附件掃毒）/暫緩2（Runtime、TransFiles人工故障驗收）不恢復。

- 完成後同步TODO、主工作池、docs/requirements.md、CHANGELOG_CUSTOM.md、docs/i18n/README.md與修改紀錄；必要檔案提交/上傳Develop，STOP於05A。

- 最後將CRUD測試fixture的新增ID改為獨立31/41，避免與既有列11/21重複；只重跑受影響6項三語CRUD，全部通過，產品程式/發布產物不變。
- 文件相對連結297項與git diff --check通過。
