# 驗證
- 狀態：通過。共用測試站維持API0.1.63／Web0.1.67，不發布帶pending migration的DLL。
- SQL直寫程序、選版API、receipt發行及統計consumer尚未實作，不列為本件功能通過。

## 實作與驗證
- EF產生20261020040256_AddChemicalFormulaTraceSchema：六張關聯表、量測兩個nullable欄位、必要FK／索引／check及既有公式複合alternate key；沒有重建既有表或回填歷史版號。migration的SQL防護固定於ChemicalTraceSqlV1，不隨後續服務程式漂移。
- 六表：ChemicalFormulaScheduleRevisions、ChemicalFormulaEffectivities、ChemicalFormulaPolicies、ChemicalCalculationReceipts、ChemicalCalculationTraces、ChemicalStatisticsJobs。trace以量測／phase／版本編號留歷次初驗／複驗，job以量測／receipt去重。
- 核心測試先建，再模型與migration；SQLite驗證跨PPC FK、無效起迄與不回填，EF驗證receipt／trace不可修改、policy預設false；SQL provider model與snapshot無差異。
- 預設相關回歸：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter "ChemicalTraceSchemaTests|VersionHistory|ChemicalAnalysisFormulaVersion|ChemicalFTable|SpcPointExclusionCalculationTests|ChartSegmentDetailsTests|FormulaTraceCompatibilityTests|ChemicalTraceSqlRehearsalTests" -p:UseSharedCompilation=false --verbosity quiet`：42 passed、2明確Skip（需opt-in的跨引擎／隔離SQL），不把Skip當通過。
- backend Release build成功，0 errors；保留既有NU1902／NU1903警告。前端未修改，build不適用。

## 真實SQL Server演練（AC1～AC5）
- runner：`tools/testing/Invoke-ChemicalTraceSchemaRehearsal.ps1`，先編譯，再從PMR_SPC_TEST做COPY_ONLY／CHECKSUM備份、VERIFYONLY，還原全新且未存在的SPC_FORMULA_SCHEMA_VERIFY_前綴庫；驗證前綴及extended property，拒絕共用DB。無新登入、無來源權限變更。
- 最終通過庫：SPC_FORMULA_SCHEMA_VERIFY_20261020_5942ebad；亦於729dd93a驗證多表故障rollback。最後一回合1個完整SQL scenario通過，包含以下所有斷言，不是僅連線成功。
- Up兩次／Down／Up；既有量測值、日期、複驗值、PPC規格／公式、公式歷史、F歷史與F儲存格值SHA256一致；初始政策／trace為0，無假造舊版。
- 半開區間相鄰可存、重疊51102拒絕、跨PPC外鍵547拒絕；同PPC版號併發只能1成功，兩筆不同起日但重疊的並發也只能1成功（僅接受51102或1205，不吞其他錯誤）。
- published期間／排程、被引用公式與F歷史、receipt及trace皆不能SQL竄改；receipt量測日超出期間拒絕51107。
- 啟用的合成PPC缺receipt、來源hash不符、未換receipt而改量測值均拒絕；未啟用PPC仍可保存nullable關聯，舊流程相容。
- 真實SQL交易內寫receipt＋量測＋trace＋job，再故意重複job造成唯一性失敗，rollback後四表筆數恢復；另驗單trace rollback。
- 無登入FormulaSchemaProbe先授予INSERT，再加入SpcChemicalDirectWriter，實際INSERT仍回229；移除成員並刪除測試user。role僅deny核心表DML，沒有授予匯入程序EXECUTE，尚未可直接匯入。
- 成功隔離庫再做COPY_ONLY／CHECKSUM備份及VERIFYONLY。備份位於SQL Server原受限預設備份目錄，檔名為隔離庫名稱-source.bak／-verified.bak，不提交資料或備份。
- 早期fixture撞來源既有PPC唯一索引，修正Unit區別後通過；增加故障測試時修正expression tree不支援pattern的編譯錯誤，runner已改為先build才建立隔離庫。失敗診斷庫／備份保留，未自動刪除。

## 共用環境與限制
- 發布後查核在本件不適用；只做唯讀來源核對：新表0、新量測欄位0、migration歷史57、量測13739、公式歷史7、直寫role0；health=ok/test/0.1.63，原Web0.1.67維持。
- 隔離合成PPC會啟用以測gate，實際共用／正式PPC沒有啟用或升級；Portal／TransFiles、IIS bindings均不操作。
- 純schema防護不代表已完成「可信receipt發行→參數化受控procedure→原子業務確認→統計consumer」；到期／排程修訂／精確結果驗證及完整重送處理由TASK-003／005續做。
- rollback只在本件隔離庫進行；不得對已有真實trace的共用／正式庫執行破壞性Down。Down另有帶role成員拒絕的程式防護（未獨立演練此分支），不自動移除帳號成員。
