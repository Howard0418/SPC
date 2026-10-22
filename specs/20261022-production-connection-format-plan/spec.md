# SPC正式連線與API版本調整規劃
- 功能ID：SPC-PROD-CONFIG-PLAN-20261022；版本1；狀態：唯讀核對與規劃完成，後續1件待確認且未實作。
- 使用者澄清為調整、不是測試；授權僅唯讀確認、建議與加入小工作，不立即執行修正/發布。
- 主系統SPC；Portal/TransFiles僅記錄相依，不修改。無schema/資料/角色/IIS/Runtime變更。
## 需求與驗收
- R-001：核對正式主機、IIS路徑/API URL、部署DLL/API回報、有效DB設定與實際DB_NAME，不輸出密碼/完整連線字串。
- R-002：區分設定格式錯誤、連線錯誤、版本落差；有證據才列修正，不因測試站較新即判正式錯誤。
- R-003：依阻斷使用→安全隔離→資料正確→高頻操作→文件排序小工作，逐件執行且本次只規劃。
- AC-001 Given 正式設定可讀，When 合併Production設定且檢查覆寫，Then 僅記錄非機敏連線目標/DB_NAME/API版本與差異。
- AC-002 Given 格式異常，Then 建議具體欄位/正確格式、驗證與回復，任務保留未執行。
- AC-003 Given 尚無其他調整細節，Then 不虛構Excel/API格式錯誤，不排入未授權功能實作。
## 計畫與任務
- [x] PLAN-001：唯讀核對正式DB/API，整理建議與工作池。
- 後續小工作見tasks.md；完成規劃後STOP，不立即修正。
## 驗證
- 文件型工作：內容/相對連結及staged diff；build/發布不適用。
- Happy：正式目標/版本一致；Boundary：JSON/IIS環境覆寫/版本格式；Invalid：缺設定/格式/連線失敗；Regression：正式環境完全不修改。
- [核對與建議](verification.md)、[排序小工作](tasks.md)。
