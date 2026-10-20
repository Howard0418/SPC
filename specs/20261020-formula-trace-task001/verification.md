# 驗證紀錄
- 日期：2026-10-20；實際基準／相關回歸：通過，詳下。
- 應用程式build／發布不適用：僅測試與契約。未查／改DB、未執行migration、未修改Portal／TransFiles來源。
- 已確認直接DB為後續目標；現有EXE使用何來源版未執行驗證，不據來源推稱EXE已完成直寫。

## 命令與結果
- `tools/testing/Invoke-FormulaTraceBaseline.ps1 -PortalRoot D:/PmrPortal -TransFilesRoot <既有TransFiles根目錄>`：Python 10 passed、實際兩計算器與SPC日期9 passed；0 skipped。只執行AST選取的純函數、連結Portal原始Evaluator及EF InMemory。
- 初次指定PORTAL_FORMULA_COMPATIBILITY但未補載入規則時編譯因缺Portal類別失敗；加入明確opt-in後上列實際來源基準通過。
- 預設模式（不提供Portal來源）相關回歸：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter "FormulaTraceCompatibilityTests|ChemicalFTableServiceTests|ChemicalAnalysisFormulaVersionTests|VersionHistory|ChartSegmentDetailsTests" -p:UseSharedCompilation=false --verbosity quiet`：23 passed、1明確Skip（跨庫來源未提供），不把它當跨引擎通過。
- 缺少TransFiles根目錄的負向runner檢查：MISSING_SOURCE_REJECTED=True，未進入測試／匯入。
- 未安裝套件；既有NU1902／NU1903警告保留，不在本件改套件。

## AC對照與限制
- AC1：3個共同算式及F引用通過；另驗證1/3、缺Primary與Excel大寫IF差異，證明不能直接替換引擎。
- AC2：切日、日期預設、正負中點及滴定精度取2位基準通過。
- AC3：無效檔名固定日期回退、無效日曆／時鐘未拒絕、Portal缺參數可能成0均已固定為現況反例，不是新契約允許行為。
- AC4：預設Skip與完整runner缺來源拒絕通過；Python使用-B、AST純函數及socket防護，不啟動GUI／業務Excel／DB。
- AC5：[contract.md](contract.md)與主規格同步直接DB決策；SQL程序、權限隔離、receipt／outbox併發及EXE部署尚未驗證，留TASK-002／005／006，不冒稱本件已實作直寫。
- 文件連結與git差異檢查通過；應用程式未變更、不重新發布測試站。API0.1.63／Web0.1.67維持，正式站未操作。
