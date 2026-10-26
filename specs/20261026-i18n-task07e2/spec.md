# I18N-TASK-07E2 操作手冊與SSO顯示三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。
- SystemGuideView固定說明及PortalSsoView固定載入／錯誤／返回連結三語；保留既有業務手冊內容、路徑／術語／公式識別，譯文draft真人審稿留TASK-20。
- 不改token解析、hash清除、setAuthSession／permissions、SSO grace period、redirect安全檢查與sessionStorage、LoginView空模板及Portal launch轉址；SSO頁復用原LocalePicker handler，不新增認證或偏好API。
- 主系統SPC，無Portal／DB／後端／登入協定／權限變更，只發布測試前端；測試使用假token或無token，禁止連線真Portal／AD。
## BDD／測試矩陣
- AC1 Happy：Given 手冊或SSO固定畫面，When 三語切換，Then 固定文字／連結更新，既有DOM與路徑保持，不增加API。
- AC2 Boundary：Given SSO缺token或無效格式，When 顯示與切語言，Then 原失敗分支及hash清除行為保持，僅固定錯誤翻譯。
- AC3 Invalid：Given 假token或不安全redirect，When 執行原流程，Then 原解析／身分映射／安全redirect行為保持，無真網路或機敏輸出。
- AC4 Regression：Given 手冊固定術語／路徑與原登入程式，When 比對，Then 未恢復偏態峰態或方法下拉，無登入／claims／ACL改動，未接路由舊頁不新增入口。
- AC5 手冊與錯誤頁三語四尺寸、必要模板／draft hash／參數與瀏覽器、build及測試發布後提交上傳。
## 任務
- [x] 規格與目的。
- [x] 固定文字／draft契約。
- [x] 必要驗證／測試發布。
- [x] 文件／提交上傳接續08。
驗證詳見[紀錄](verification.md)。Rollback核對test目錄後資產先index最後還原完整前端備份、保留設定；正式另授權。
