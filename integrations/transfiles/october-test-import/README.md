# 十月藥液隔離測試匯入

## 後續核准九月範圍
`build_manifest.py --month 2026-09`只讀九月，省略月份仍維持十月。runner執行九月必須使用marker `TEST-SEPCHEM-20261102`，與十月marker混用即在DB存取前拒絕；九月以報表日12:00+08:00為明示測試時間。九月支援公式明確引用的同列I→Secondary，不猜跨列或缺少值，初複驗不同仍隔離。新增月份邊界測試`python -m unittest test_month_scope test_chemical_excel_reconcile`；[九月規格及驗證](../../../specs/20261102-september-chemical-test-import/verification.md)。

九月製程230／PPC2766～2882，1146量測、71複驗；不覆寫十月範圍。共用runner另在before/after比對匯入前既有量測COUNT及指定核心欄位CHECKSUM，與定義／全域F快照一起檢查。`--apply`建立真實受控匯入、`--statistics`僅處理指定marker、`--verify`使用網站追溯服務查回。其餘限制與回復措施同下；父TASK-002尚未全完成。

對應 [規格](../../../specs/20261102-october-chemical-test-import/spec.md)。本次為獨立受控 runner，**不是已重新打包的 GUI／EXE**；不將完整矩陣匯入能力冒稱既有EXE已交付。

## 組成與驗證
- 2026-11-05：來源搬移時可加 `--source-root '<實際來源資料夾>'`，只覆寫此次唯讀讀取路徑，不修改轉檔工具設定、不啟用排程。未指定仍使用原設定，來源不存在立即拒絕；驗證 `python -m unittest test_source_root test_month_scope`。詳 [正式對齊與逐檔核對](../../../specs/20261105-production-align-test-excel/spec.md)。
- `tool-fixes.patch`：TransFiles實檔讀取改為try/finally關閉，及先紅後綠實檔測試。零上下文patch需`git apply --unidiff-zero`，套用前先確認目標基準原碼，不覆蓋其他修改。
- `chemical_excel_reconcile.py`／`test_chemical_excel_reconcile.py`：複製至TransFiles同層；僅同列原始值、明確F引用與四則運算，使用受限AST與Decimal；不使用eval，不寫API／DB。
- `build_manifest.py`：由TransFiles設定讀來源，只讀2026年10月，保留SHA256、初複驗及逐列阻擋；依滴定表頭識別M／N複驗，排除加藥欄。相同內容去重、衝突不選勝者。
- `OctoberImport.csproj`／`Program.cs`：沿用SPC實際運算、版本、日期排程、預覽、prepare及受控SQL commit；不繞過DB actor比對，不製造HTTP權杖。正式環境被固定guard拒絕。

先在TransFiles執行 `python -m unittest test_chemical_direct_writer test_chemical_excel_reconcile test_chemical_daily_parser`。

```powershell
python integrations/transfiles/october-test-import/build_manifest.py --tool-root '<TransFiles目錄>' --output-dir '<本機證據目錄>'
dotnet run --project integrations/transfiles/october-test-import/OctoberImport.csproj -- --pure '<本機證據目錄>'
dotnet run --no-build --project integrations/transfiles/october-test-import/OctoberImport.csproj -- --preflight '<本機證據目錄>'
```

`--apply` 才執行測試資料備份／VERIFYONLY、專用定義與受控寫入；只允許同一已核對manifest重試，不任意換來源後重用序號。`--statistics`只處理專用PPC的既有統計工作、強制record-only且封鎖郵件；`--verify`只讀取網站使用的追溯服務對帳。所有模式先通過SPC引擎與Excel逐筆重算；使用者已明確同意本批報表日測試時間／時段，不作現場歷史。無需為一般應用部署使用本runner。

## 查閱與限制
- 測試製程`TEST-OCTCHEM-20261102`，ID228；PPC2649～2765。線別總覽對N1/N2另依原機台分組，但新增PPC屬獨立測試製程，原業務主檔不改。
- 現行F表的「目前啟用版本」保持原樣；本批使用排程指定的未全域啟用快照，應查量測追溯內引用的F值，不能拿目前啟用F當歷史對帳基準。
- 測試特性record-only避免業務告警；trace的公式raw規格判定保留。圖表無告警不代表全部來源判定OK。
- 午夜+08測試日期存UTC前一天16:00；既有圖表以UTC日期篩選，查本批完整十月圖形須暫選9/30～10/31。實際匯入資料均為10月報表，沒有匯入9月。此日界線限制另留既有TASK-002待辦，不更動共用日期邏輯。
- 16筆來源問題見 [清單與建議](../../../specs/20261102-october-chemical-test-import/source-issues.md)。原始檔、輸出JSON、備份、EXE、bin／obj不提交。

## 回復
先停用本批專用PPC2649～2765，保留不可變公式／F／計算追溯。必要完整DB回復須核對期間其他測試寫入，不能直接覆蓋其成果；本次備份識別記錄在本機證據與verification。不要停trigger、改角色、刪歷史或修改正式庫。

第二滴定值補充：`build_secondary_manifest.py`只從首輪隔離的3筆明確同列I公式建立子manifest；H→Primary、I→Secondary，先紅後綠測試，數值與Excel/SPC一致後，以marker `TEST-OCTCHEM-20261102-SECONDARY`（製程229、PPC2764～2765）補匯入3筆。其來源單位明確為g/L；`--align-units`僅核對／對齊指定隔離製程的來源單位，不修改業務項目。合計117組、1211筆、54複驗。

## 九月趨勢畫面驗收fixture
`build_trend_manifest.py --source <九月證據目錄> --output-dir <獨立證據目錄>`只選已驗證DV／顯影單一公式F規格組，最多31筆且須含複驗。runner marker固定`TEST-SEPCHEM-20261102-TREND`，summary.purpose必須trend-ui；混用原九月marker在DB存取前拒絕。獨立TREND_CHART製程保留統計ChartTypeId供受控匯入統計佇列使用，不更改既有管制圖或正常項目。初次fixture漏設統計ChartTypeId時，只允許`--align-trend-statistics`修復此專用單組、record-only且原ChartTypeId空白的測試項目，不能用於一般主檔。
本組僅重現來源作網站驗收，不是額外現場量測，不能與原1146筆相加當來源筆數。來源衝突仍隔離；測試`python -m unittest test_trend_fixture test_month_scope test_chemical_excel_reconcile`。
