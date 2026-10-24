# 技術計畫：I18N-TASK-05B v1
- [規格](spec.md)｜[任務](tasks.md)
四Vue View使用global useI18n、既有LocalePicker及語言change入口。顯示訊息可存key/args，渲染時翻譯；原API安全訊息直接顯示，不改表單模型。新增小型純顯示helper先建單元測試；不重構業務流程。
三語JSON/穩定語意key新增獨立draft審核帳本，不改前批審稿紀錄。只翻groupsOnly可達分支，types隱藏設定留05C。數量用named參數，不翻原名稱、代碼、JSON。
測試Happy三語主要操作與DTO、Boundary零資料/長文/數字、Invalid空白/JSON/衝突/取消、Regression狀態/權限/既有主檔與shell。全部API攔截，CRUD只fixture。必要build/test通過直接發布測試前端Web0.1.74/API沿用0.1.72。
發布前重新核對SpcSingleTest/AppEnvironment=test，備份前端/設定hash，assets先index最後，保留舊assets與設定，不停API或改DB。回復同批前端備份Web0.1.73、保留現場設定/資料；完成提交上傳Develop後STOP，剩19小工作。

手機紅燈驗證：製程390px編輯控制不可達、槽體輸入僅14px寬。四頁路徑於App沿用既有calibration導覽上下排列，main允許縮小；重新驗證三語/尺寸，不改其他頁路由佈局。
