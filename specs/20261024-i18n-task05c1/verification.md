# I18N-TASK-05C1 驗證與發布

## 範圍與限制
本次完成保留的小分類/公式配置分支三語，新增45鍵/135列draft，累計390鍵×3語。公式 JSON、業務名稱、enum、搜尋原語意、抽樣及原DTO不變；WE規則唯讀、不新增規則寫入。
實際公開路由 /control-chart-groups 固定 groupsOnly=true；/control-chart-types 原先即轉往 /part-process-characteristics。未恢復入口，不能將本次視為公開管制項目編輯器已翻譯；該範圍留05C3。
測試專用 tests/fixtures/chart-types.html 經獨立 Vite config 建置至 .codex-tmp，**不進正式應用 dist、不發布**。隔離元件使用真正 Vue SFC/i18n/樣式，路由props僅測試入口為false；公開 route/ACL 另驗。
所有瀏覽器寫入及讀取以 route interception 模擬，假token不送真API；不宣稱真SSO/真DB寫入驗收。真人三語審核待TASK20。

## 自動化與 BDD 對應
- 修改前：擴大既有模板檢查，對未翻譯「大類別總管」紅燈；首輪公開路由測試揭露types被隱藏，依證據修正規格與測試入口。
- AC1/AC5：12種三語×1920/1366/1280/390元件情境，原名/JSON維持，WE八項唯讀、儲存可捲到；390英文截圖人工核對。
- AC2/AC3：各三語草稿DOM不重建/原URL與篩選保持/無額外讀寫、編輯DTO、抽樣50、計數enum、建立抽樣1/預設JSON、I_MR刪除確認/取消與ID。
- AC4：三語空白必填、非法JSON、409原文、規則讀取錯誤；切語言錯誤即時更新且原API訊息保留。
- AC6：孤立/空父類別顯示三語且原中文搜尋結果不變；空清單/無規則；公開路由與Viewer沿用既有檢查。
- 單元：40項通過；draft hash/placeholder/full key coverage、完整模板固定文案與編譯、參數原文不二次解析。
- 建置：npm run build:test 通過，390鍵×3字典通過；既有Vite大chunk提醒，未引入新的拆包改造。
- 隔離元件：23項通過；額外手機截圖1項通過，非增加新驗收數量。
- 公開應用本機回歸：98項通過（79基礎主檔＋17共用介面＋2公開路由維持）。
- 發布後公開回歸98項通過；verify-built-api同源14請求通過；真匿名version/health HTTP200、environment=test/API0.1.72/status=ok；四個靜態payload逐檔SHA256與dist吻合。

## 發布與 rollback
已僅更新SpcSingleTest實際 D:/SPC/release/test/frontend，Web0.1.75、API沿用0.1.72。發布39檔備份、4檔payload與4個保留設定均已核對hash；執行前核對IIS vdir後端根與AppEnvironment=test，備份frontend並核對SHA256，先assets後index且保留舊assets；web.config/appsettings保留，不重啟pool。
回復：先將本次 .codex-tmp/i18n-task05c1/backup-frontend 的備份資產複製回測試frontend，最後回復index.html，驗備份hash/health/version/靜態URL；環境設定保留，新增但未被舊index引用的資產可保留。沒有DB/migration/其他系統或正式發布。

## 工作池與交付
05C依原範圍拆C1～C3，C1完成後剩20件（C2/C3/05D/05E＋06～21），另附件掃毒受阻1、Runtime/TransFiles人工故障驗收暫緩2。下一項C2，未自動開始。
