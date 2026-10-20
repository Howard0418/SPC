# TASK-003 驗證與發布紀錄
- 狀態：實作及必要自動化驗收通過；已發布SPC測試站。使用者／Antigravity完整人工案例仍待執行。
- [規格](spec.md)｜[操作案例AG-01～08](antigravity-tests.md)。

## 自動化結果
- 核心先建立測試並確認缺少實作時失敗，再實作；日期／計算13項初版通過，後續增加超精度拒絕、實際F歷史含F!地址及nullable儲存格。
- 最終相關後端（含真實Portal evaluator來源opt-in）78 passed／0 skipped：台北半開區間、跨日不同公式、UTC等價、缺版／缺輸入／重疊、過期／偽造token、改輸入／操作者／規格／修訂、未來版本不改已發行時點、純preview不寫DB、prepare只存receipt、角色宣告及既有公式／F／分段／統計回歸。
- 真實SQL完整scenario 1 passed：PPC序列化交易使並發發布僅一人成功；舊修訂拒絕、receipt正常保存、失敗無部分排程、F歷史不可改、policy仍false、量測0。隔離庫`SPC_FORMULA_SCHEMA_VERIFY_20261020_2f28e51a`；COPY_ONLY／CHECKSUM／VERIFYONLY通過，保留診斷庫及受限備份。初次隔離庫9023536f亦通過。
- 新畫面7 passed；既有公式／F／規格歷史9 passed。初次Playwright收尾因sandbox無權結束Vite而等待，僅結束本次5193程序後正常exit0；後續回歸正常結束。
- Release publish、frontend testhost build成功。僅既有Pkcs NU1902／NU1903與Vite大chunk警告，未安裝套件或擴大修補。

## 測試站發布
- 實際IIS `SpcSingleTest`，binding HTTPS SPCTEST 443／8901，實體`D:\SPC\release\test\backend`；DB `PMR_SPC_TEST`／SQL機器PMR-MESSQL核對後執行。
- 部署前正式取得測試庫COPY_ONLY／CHECKSUM備份並RESTORE VERIFYONLY：SQL預設備份目錄`SPC_TASK003_TEST_PRE_20261020_122954.bak`。
- 發布API0.1.64／Web0.1.68；一併套用已隔離演練的TASK-002 migration。appsettings.json／web.config hash相同，既有量測／公式歷史／F資料SHA256相同。所有匯入policy未啟用，不會提前阻擋舊入口。
- HTTPS health=ok/test/0.1.64；新排程API匿名401；已發布bundle同源驗證14 API requests通過，index hash與build一致。
- 正常SSO瀏覽器已見編輯者登入及Web0.1.68／API0.1.64；進入藥液公式分頁時自動審核將動作判為涉及http://172.16.110.27而拒絕，停止該路徑，未繞過。正常登入後面板完整人工驗收未完成，不把mock測試冒稱真人驗收。
- 另一個以設定JWT金鑰簽發Editor／Viewer token的驗證計畫被自動審核拒絕、未執行；未輸出或持久化token，改用匿名及正常SSO。實際Viewer API403留AG-08，已完成後端授權宣告及前端Viewer回歸。
- 本機部署備份`release/backup-formula-task003-20261020-122947`；回復可停測試站、還原該backend/frontend與原設定。schema為相容新增且未啟用policy，可保留；不可對已有receipt／trace執行破壞性Down。需DB回復時另核對新增資料再從已驗證備份還原。
- 正式站、Portal／TransFiles／KM未修改。真正直寫與統計consumer仍TASK-005；Portal整合TASK-004、圖表真實版本及跨系統TASK-006未執行。

## 重跑入口
- dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter ChemicalFormula -p:FormulaCompatibilityPortalSource=D:/PmrPortal/src/PmrPortal.Api/Services/ExcelFormulaEvaluator.cs
- tools/testing/Invoke-ChemicalTraceSchemaRehearsal.ps1 -TestFilter ChemicalFormulaScheduleSqlTests（只能全新隔離庫；會產生可驗證備份，不可指定正式來源）。
- frontend/mes-spc-web：node node_modules/@playwright/test/cli.js test --config playwright.chemical-schedule.config.ts；既有回歸用playwright.chemical-history.config.ts。
