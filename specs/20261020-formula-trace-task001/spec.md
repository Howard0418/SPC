# SPC-FORMULA-TRACE-TASK-001：入口、日期與精度契約基準
- 版本：1；狀態：DONE（僅TASK-001基準／契約）；主規格：[日期選版規劃](../20261020-formula-effective-trace-plan/spec.md)。
- 授權：使用者「開始SPC實作」；每次一件，僅執行TASK-001。另確認仍須TransFiles直接寫DB，不採既有API匯入作最終方案。
- 範圍：核實現有來源程式、建立跨計算器／轉檔基準測試與直寫契約；不修改應用程式、其他專案、DB、migration、權限或發布。

## 現況與需求
- R1：以真實來源區分已存在與目標。TransFiles GUI目前經SPC API預覽／確認；direct_import.py是規劃器；直接DB寫入為待實作目標，不聲稱已存在或已驗證EXE。
- R2：測試實際Portal ExcelFormulaEvaluator與SPC ChemicalFTableService，捕捉語法、八位小數輸出、ROUND、缺參數及F引用差異，不能因測試通過就宣稱兩引擎等價。
- R3：以實際TransFiles純函數測試日期來源、回退與精度損失；不啟動GUI、讀業務Excel、連網或寫入原始檔。
- R4：訂出直接DB原子寫入、版本過期及舊客戶端防護契約；SQL執行權限與交易／併發實證留TASK-002／005，不先執行。

## BDD與驗收
- AC1 Happy：Given實際兩套計算器及簡單算式，When套用同輸入／F引用，Then記錄一致基準及差異反例，無DB／網路副作用。
- AC2 Boundary：Given切日前後檔名、長小數與正負中點，When執行來源函數，Then固定現況、列出需阻擋或保存原值的位置。
- AC3 Invalid：Given錯誤檔名／無效日期時間／缺參數，When執行現有來源，Then揭露回退或錯誤，不把這些行為當新功能預期。
- AC4 Regression：Given一般SPC測試未提供Portal來源，When執行，Then相容性套件明確略過；完整runner需真實Portal檔與TransFiles來源，缺一即失敗，不假通過。
- AC5 契約：Given使用者確認直接DB目標，When完成方案，Then主規格與工作池同步，清楚分開現有API、未來純預覽、確認準備及DB交易。
- 完成只代表TASK-001基準／契約完成，不代表日期選版、直寫DB或後續五件已實作。
