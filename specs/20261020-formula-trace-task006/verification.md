# TASK-006驗證（2026-10-20）
狀態：實作／自動化／測試發布完成；[Antigravity案例](antigravity-tests.md)供正常登入真人操作，不冒稱已人工完成。正式、Portal、TransFiles EXE及KM本件未更新。

## 測試與BDD
- 核心先紅：ChemicalTraceQueryService缺類別編譯失敗，再實作。5項新測試涵蓋Portal真正保存初複驗、混合舊資料、按日期／PPC／point隔離、24筆歷程分頁與目前值識別、損壞JSON明示不完整、固定F引用與多版本、非藥液及Authorize。
- `dotnet test tests/MesSpc.Api.Tests --filter 'ChemicalTraceQueryTests|ChemicalStatisticsProcessorTests|ChemicalPortalMeasurementTests|ChemicalFormulaScheduleTests|SpcPointExclusionCalculationTests|ChartSegmentDetailsTests'`：最終28 passed，無Skip。核心及分段／原子匯入／統計回歸通過。TASK-005的藥液／咬蝕59與Python19仍有效，本件不重跑無變更套件。
- `npx playwright test --config playwright.chart-segment.config.ts`：17 passed（原11＋新6）。含新舊／多版本／初複驗／F、精確量測ID與歷程分頁、503重試／舊回應隔離、管制圖與趨勢圖實際點擊、子組不假造單一版本；既有Xbar-R/S與P/U、行動版、修改歷史回歸通過。
- `npx playwright test --config playwright.chemical-schedule.config.ts`：7 passed。測試執行後Vite收尾掛住，僅結束本次明列node PID17920；未操作其他程序。
- SQL完整scenario：`tools/testing/Invoke-ChemicalTraceSchemaRehearsal.ps1 -TestFilter ChemicalDirectWriterSqlTests`最終PASS，隔離庫`SPC_FORMULA_SCHEMA_VERIFY_20261020_d90107e7`。同來源檔兩日期經共用選版／prepare→低權限SQL真正直寫→統計／Pending狀態→唯讀trace，分別保存2.47與3.7及不同公式版；初複驗、Up/Down/Up、權限、重送、晚期rollback及SQL GroupBy／關聯查詢通過。最終COPY_ONLY/CHECKSUM/VERIFYONLY。
- 初次跨日斷言期待3.70，實際既有canonical為3.7；只修正測試期待，不更動已確認精度政策。點位入口初版依選單scope判斷，批次直入時資料不足；修正使用實際PPC、由服務端確認適用性後3點位案例與全17項重跑通過。
- 前後端build成功；既有Pkcs弱點、既有EquipmentPointsView的table標記及Vite大chunk警告保留，不擴張修補。

## 實作依據與限制
- `ChemicalTraceQueryService`僅AsNoTracking查詢，先限PPC／分段／量測再分頁；分段顯示各階段最新保存證據，點位顯示全部歷程分頁。讀固定公式版本與receipt payload/F引用，不呼叫計算、不寫DB。不完整證據拒絕冒充目前公式。
- `ChemicalTraceQueryController`為Authorize GET，統一JSON；負分頁400、跨PPC／不存在404，匿名實測401。非藥液Applicable=false，不套公式。
- 共用ChemicalCalculationDetails嵌入分段及兩種點位畫面，顯示歷程／目前值、raw／stored與當時計算規格快照。子組缺全部成員ID及MR等衍生點缺量測識別時明示不可指定單一公式，改由分段逐筆查閱。獨立規格與管制界限版本仍非本批範圍；不以目前圖表判定冒充原判定。
- 資料不自動回填／重算；正常SSO Viewer403及使用者業務檔跨站操作保留真人案例，不以mock或SQLfixture冒稱已人工驗收。

## 測試發布與rollback
- SPC測試API **0.1.67**／Web **0.1.69**，IIS SpcSingleTest=`D:/SPC/release/test/backend`，靜態前端=`D:/SPC/release/test/frontend`；SQLMachine=PMR-MESSQL、DB=PMR_SPC_TEST。正式站及其他應用未發布。
- 備份：`D:/SPC/release/backup-formula-task006-20261020-135435`（backend＋frontend）；SQL預設備份目錄`SPC_TASK006_TEST_PRE_20261020-135435.bak`，VERIFYONLY成功。
- 本件無migration、role或AD變更；部署前後migration數、既有資料指紋、receipt數、policy啟用數、writer會員數及環境設定hash一致。dist與已發布index hash相同。
- 發布後health=ok/test/0.1.67、新trace API匿名401。`node scripts/verify-built-api.mjs https://spctest.pmr.com.tw`：14個bundle API requests全部同源；該fixture攔截所有API、不送mock token到後端，只驗證實際靜態bundle路由。
- rollback先停測試站、還原上述backend/frontend備份，保留DB與trace，不Down、不刪資料；本次未執行rollback。
- 必要自動化與實際部署驗證完成；後續僅使用者真人操作確認。SPC工作池清空，KM3件維持暫緩，不自動恢復。
