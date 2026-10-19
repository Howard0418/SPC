# 公式參照備註與共用歷史驗證
- 功能 ID：SPC-CHEM-REFERENCE-HISTORY-20261019；2026-10-19。
- 實作完成；SPC測試站已發布，正式未操作。

## 自動化與行為驗證
- 核心修改前新增邊界／超長測試；舊服務在501字RecordChange及Restore兩例失敗，修正後相關17項通過。
- 命令：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~ChemicalAnalysisFormulaVersion|FullyQualifiedName~Update_RejectsLongFormulaReference|FullyQualifiedName~Update_Should_Record_ChemicalFormulaVersion|FullyQualifiedName~Update_Should_Not_Record_ChemicalFormulaVersion" -p:UseSharedCompilation=false`。
- AC-001／002：Playwright兩頁共用同PPC API、設定頁備註送出／總覽查詢，含公式修改、舊紀錄與空歷史。
- AC-003：後端0／500／501字及Restore寫入前拒絕；Controller確認無欄位／歷史寫入。前端maxlength與送出驗證，Vue純文字插值（無v-html）。
- AC-004：公式未變不建版本後端測試、前端僅備註不送出，以及批次一成功一失敗保留草稿／備註。
- AC-005：既有回復與非CHEM拒絕／角色屬性測試通過，原權限機制不改；總覽唯讀歷史，仍由原設定入口回復。未冒稱正式真人權限端到端驗收。
- `node frontend/mes-spc-web/node_modules/@playwright/test/cli.js test --config frontend/mes-spc-web/playwright.chemical-history.config.ts`：6 passed。僅127.0.0.1與mock API，封鎖外部請求，不寫真實資料；含390px與錯誤重試。
- Release dotnet publish成功；npm run build:test成功。既有Pkcs9.0.4 NU1902／NU1903及chunk大小警告保留。

## 回歸限制
較廣PartProcessCharacteristicMaintenance回歸共21例，18通過、3失敗：Create_Should_Use_Configured_Group_FieldRequirements（回應物件與字串斷言差異）、Update_Should_Save_Unit_And_ItemSpecificRules（既有驗證拒絕）、Calculation_Should_Prefer_ItemRuleGroup_Over_ChartTypeRuleGroup（RuleGroupId為null）。以HEAD原始Controller及版本Service單獨重跑相同3例仍全部失敗，已還原本次工作原碼；確認非本次新增回歸，不修改未授權規則或把全套寫成通過。

## 發布、備份與回復
- IIS唯讀核對SpcSingleTest／pool SpcSingleTest／D:/SPC/release/test/backend；AppEnvironment=test，前端同級frontend。沒有操作D:/Sites/PmrPortal、其他站台或正式站。
- 備份D:/SPC/release/backup-chemical-reference-history-20261019-171718；發布包release/staging-chemical-history-20261019/backend及testhost dist。逐檔SHA256相同，appsettings.json／web.config hash不變，無新migration／資料修補。
- 過程遇覆寫鎖定與驗收腳本保留變數HOME誤用，造成回復嘗試；沒有將失敗標完成。修正腳本變數、已卸載應用，保留hash相同依賴只覆寫異動檔，最終發布及核對成功。
- 最終HTTP /api/version=0.1.61、environment=test、/api/health=ok、首頁200；實際首頁資產index-CMj196Mo.js與本次dist相符，Web0.1.64。不存在PPC ID0送501字參照備註回400，無真實資料寫入。
- 回復：卸載並停止SpcSingleTest後還原同批backend／frontend備份、保護原設定，再啟動核對健康；資料不刪除，不整庫還原。暫存腳本／證據與產物不提交。
- 真人線上儲存未執行，避免變更使用中的公式；功能以自動化mock瀏覽器及InMemory測試驗證，線上確認版本／資產／健康與無效請求拒絕。

- 補充邊界測試：設定頁只改參照備註時，原始稀疏JSON不得因預設欄位補齊而改寫；先失敗後修正，前端最終6 passed。最終HTTP資產SHA256與本地dist相同。
