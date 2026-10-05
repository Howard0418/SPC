# 驗證與發布（2026-09-18）
## 自動驗證
- SPC核心7例：新增PreviewAsync，新增情境證明無量測/批次寫入、相同略過、內容不同拒絕；既有25/50點、原子寫入、數值/ID/圖表測試通過。
- SPC HTTP4例：匿名401、Viewer403、Editor/非測試provider403（preview/confirm），正式庫/非test設定拒絕。
- Portal5例：50/100點JSON載入、SQLite嚴格匯入預覽不寫、Pending→SUCCESS、相同ID與點數、衝突不覆寫、錯誤內容不可標SUCCESS、測試環境檢查通過。
- Portal新HTTP3例：匿名401、非品保403、品保但非SQL測試環境403，未發出SPC請求。
- TransFiles31例：4個mock HTTP預覽/取消前不confirm、衝突整批阻擋、部分失敗紀錄及重試略過、必須用本次預覽；27個既有GUI/化學轉檔測試通過。
- 共50個測試案例。本次未重跑與變更無關的整月解析，也未操作瀏覽器。既有全套Portal測試有無關編譯問題，沿用隔離Etch測試專案。
## 打包/發布
- SPC API 0.1.58：D:/SPC/release/test/backend。
- Portal API 1.0.169：D:/PmrPortal/release/test/portal-api。
- 兩API Release publish成功，IIS目標核對、備份、DLL SHA256及appsettings/web.config保留，健康端點皆200；見../../release-staging/etch-upload-20260918/deployment.json。
- TransFiles2026.09.18.2主exe已备份更新，PyInstaller包含etch_reports與etch_upload；雜湊一致，見../../release-staging/etch-upload-20260918/transfiles-package.json。版本顯示於標題/輸出，Windows檔案版本屬性未另設。
- 實站SPC preview回200，9/1 PT1為unchanged且52筆；Portal匿名preview回401。見../../release-staging/etch-upload-20260918/live-verification.json。
- 正式站、資料庫schema及既有業務資料均未修改；本次沒有呼叫實站confirm。
## 必要人工驗收（待使用者登入）
1. 重開TransFiles，確認視窗版本2026.09.18.2，選咬蝕量與來源/輸出資料夾。
2. 按「轉換並匯入測試站」，輸入有Portal品保及SPC寫入權限的AD帳號/工號與密碼。
3. 核對新增/補同步、相同、隔離數量；若預覽有錯誤會阻擋，不確認即不匯入。
4. 按確認後查看逐份SUCCESS/FAILED及咬蝕匯入結果JSON；失敗重新同流程，成功資料應略過。
5. 到Portal切換量測日期與線別，SPC選相同區間確認圖表。
登入身分下Portal→SPC整合及打包GUI尚待人工驗收，不以服務/Mock測試冒充完成。
