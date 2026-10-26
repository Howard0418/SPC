# I18N-TASK-07E1 查詢／追溯與咬蝕入口三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成；[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。07E依公開路由拆E1查詢／追溯／咬蝕入口、E2操作手冊與SSO顯示，範圍不增加。
- SpcQueryView／GenealogyView／EtchAnalysisView固定UI、提示、tree tooltip三語。保留raw批號／工單／序號／來源／主檔／狀態、日期數值與原識別；樹狀圖只翻譯tooltip固定標籤，參數HTML編碼，series/raw data不改。
- 保留查詢／批次轉圖／CPK匯出URL與month、lot/workorder路徑、點擊節點重查、Portal連結及rel、etch特性code與線別映射、PPC與日期query，不改資料／計算／權限；locale不重讀、重建圖或清草稿。
- 路由盤點：首頁/已導向完成三語的/spc；DashboardView／ChemicalsView／VariableUploadView／AttributeUploadView未接公開路由，保留原檔，不新增入口或宣稱其舊程式已三語化。公開登入LoginView為空模板轉址，SSO固定顯示另E2。
- SPC單一系統，Portal僅既有入口連結，無DB／後端／Portal變更。只測試前端，業務API全攔截；正式另授權。
## BDD／測試矩陣
- AC1 Happy：Given 三頁已查與選擇，When 切三語，Then UI／tooltip更新，原DOM／草稿／查詢／圖表實例與series保持。
- AC2 Boundary：Given 無PPC、空資料、0/null與未知raw狀態，When 顯示，Then 原fallback／判定與連結可用性保持，不自行修計算或既有fallback。
- AC3 Invalid：Given API失敗或raw含HTML/i18n，When 表格／tooltip顯示，Then 原文純文字、固定提示可切語言、查詢值保持。
- AC4 Regression：Given 工單／批號查詢、CPK月匯出／轉圖／etch卡片，When 操作，Then 原端點／query／month／PPC與原code保持，不新增寫入。
- AC5 三頁三語四尺寸／手機表格及明細，必要模板／draft hash／參數／瀏覽器／build與發布後提交上傳。
## 任務
- [x] 規格／工作池／目的。
- [x] 固定顯示／draft契約。
- [x] 必要驗證／測試發布。
- [x] 文件／提交上傳接續07E2。
驗證詳見[紀錄](verification.md)。Rollback核對test目錄後資產先index最後還原完整前端備份、保留設定；draft真人審稿留TASK-20。
