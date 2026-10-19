# EXE製作交付包
ID：DESKTOP-BUNDLE-CREATION-20261019；狀態：DONE。
主系統SPC；相依Portal；不更動DBschema／網站／IIS，不執行Apply或Rollback。
使用者確認EXE找不到新增／製作交付包功能；本Task補齊實際產包入口，不建立空JSON。
## 需求／BDD
- AC1 Given 主畫面 When 按製作交付包 Then 選API／Web已建置目錄、輸出位置及版本，環境／系統沿用主畫面；產出完整ZIP／SHA256／manifest，成功後自動帶回但不自動更新。
- AC2 Given API DLL When 讀取資料 Then 靜態PE metadata取得版本、AssemblyInformationalVersion來源commit、正確DbContext的migration attributes；不載入或執行API、不連線DB、不用檔名猜migration。已知缺DbContext的F表類別不誤列；與既有EF GetMigrations離線清單對照。
- AC3 Given 現行版本資料 When 讀取現行版本 Then 固定主機只讀DLL版本，拒絕任意host；讀取失敗可明確手填版本，不假造commit／版本。SPC舊Web版本在引擎未使用，介面說明雜湊／備份回復，非假造版號。
- AC4 Given 可選已審查SQL／migration IDs When 製作 Then SQL與ID須成對、均在完整catalogue，沿用既有產包器驗證及環境設定排除。目標pending仍於Plan核對，缺授權不執行。
- AC5 Given 缺資料、覆寫輸出、錯環境、來源在變動或失敗 When 製作 Then 不覆蓋既有交付包、不選取失敗manifest；成功前放在同父層獨立.incomplete目錄，hash驗證後才完成搬移。
- AC6 Given 複製EXE到操作電腦 When 製作 Then 自含靜態metadata工具runtime，仍不需SDK／PowerShell7；使用結構化參數、安全cache與背景程序，UI忙碌時不能重入或關閉。
## 測試規劃
Happy：兩系統本機產包並帶回；Boundary：無commit需手填、SPC Web無DLL、同名輸出；Invalid：SQL／IDs不完整、API來源變動、wrongDbContext；Regression：原更新／回復／授權／模組路徑及失敗碼。
## 限制
API／Web必須已建置；本功能不編譯產品、無自動SQL核准。SPC前端仍分環境build；來源commit只接受DLL中真實40位commit或人工提供，不用隨機值假造。程式未簽章。
