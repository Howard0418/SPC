# SPC-FORMULA-TRACE-TASK-002：有效期與追溯schema
- 版本1；狀態：DONE（schema／隔離驗證，未部署）；依據[主規格](../20261020-formula-effective-trace-plan/spec.md)及[TASK-001契約](../20261020-formula-trace-task001/contract.md)。
- 授權：使用者確認開始下一件；本件只做資料模型、EF migration與隔離演練，不啟用公式選版、DB直寫或正式／共用測試庫升級。
- 主系統SPC；Portal／TransFiles不修改。SQL角色只在隔離庫驗證，不建立登入或修改正式帳號。

## 模型與驗收
- R1／AC1 Happy：新增排程修訂、有效期間、PPC啟用政策（預設不啟用）、不可變計算receipt／trace及可重試統計job；既有量測只增加nullable關聯與來源hash，舊資料不回填。
- R2／AC2 Boundary：期間半開區間、同PPC版號唯一、同修訂期間不可重疊；FK不可跨PPC引用公式／排程／receipt，published修訂與已引用公式／F歷史不可變。
- R3／AC3 Invalid：無效起迄、重複量測phase版本、重複來源／receipt、跨PPC、竄改receipt／trace須由關聯／約束或SQL trigger拒絕，不只在API驗證。
- R4／AC4 Regression：隔離SQL Server由測試庫備份還原，Up重跑／Down／Up與備份VERIFYONLY、交易rollback及併發唯一性測試，既有業務筆數／值不變；不碰來源庫業務資料。
- R5／AC5 Gate：新增直寫role預設deny核心表DML，尚無EXECUTE匯入程序，fail closed。完整receipt確認／DB程序／統計consumer留TASK-003／005，不能宣稱已可直接匯入。
- 執行順序：先建立對應測試，再模型與migration，最後隔離SQL驗證。不得自動開始TASK-003。

## 發布與回復
本件先交付schema與演練，不把帶有新migration的DLL發布共用測試站（既有啟動會自動Migrate）；待後續API／備份與測試庫升級階段再發布。正式無操作。
Down只在本次前綴隔離庫演練；保留隔離備份，不刪既有正式資料或新追溯。原碼可revert本次提交。
