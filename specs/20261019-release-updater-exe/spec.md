# Windows 更新執行檔
ID：RELEASE-UPDATER-EXE-20261019；狀態：完成EXE與驗證；本次不更新站台。
主系統SPC；相依Portal；本次不變更schema、IIS、產品網站或執行Apply／Rollback。
使用者要求可直接執行的更新程式；沿用既有環境切換／遠端發布引擎，不另寫部署邏輯。
## 需求／BDD
- AC1 Given Windows x64 When 雙擊EXE Then 顯示繁體中文介面，可選Test／Production、SPC／Portal及manifest，預設Test／SPC，不需安裝SDK／PowerShell7。
- AC2 Given 選交付包 When 預檢 Then 顯示固定主機、DB、版本與即時結果，沿用雙端hash、WinRM與目標核對；操作在背景進行、禁止重複執行／中途關閉。
- AC3 Given 更新 When 本次預檢成功、選擇未改變、應用與DB授權均勾選且確認 Then 呼叫既有Apply；少任一條件拒絕，不以GUI省略既有防護。
- AC4 Given 同批原manifest When 回復且授權確認 Then 呼叫既有Rollback，保留DBschema；不要求已套用migration的包再次通過Apply用的Plan。
- AC5 Given 惡意路徑／quote／換行 When 啟動PowerShell Then 使用結構化Base64 JSON參數，不拼接使用者命令；內建來源抽取到Administrators／SYSTEM限定目錄，避免外部腳本替換。
- AC6 Given 關閉程式／發生錯誤 Then 不強殺發布程序；清楚顯示結束碼，預檢失敗不可更新。
## 驗證
核心先建立C#測試：預設／環境不符／缺授權／預檢過期／參數注入／Unicode／Rollback；編譯EXE、資源hash、UI離線render驗證，既有35項PowerShell回歸。只允許EXE預檢或離線self-test，不執行正式更新。
## 限制
使用.NET Framework4.8 WinForms與Windows PowerShell5.1；x64 Windows需既有IIS／WinRM／DB權限。UAC要求系統管理員，無儲存密碼／修改執行原則。未取得程式碼簽章憑證，不能宣稱已簽章。交付包仍需準備，EXE不自動把測試前端改成正式，也不包含資料庫／設定／實際交付包。
