# SPC 已驗收 API 功能正式對齊

- 功能 ID：`SPC-ACCEPTED-API-FORMAL-ALIGN-1117-TASK-001`
- 狀態：已完成／正式已發布
- 主系統：SPC API／Web
- 相依系統：Portal 使用既有 SPC API 契約；不發布 Portal
- 資料庫影響：無 schema、migration 或資料搬移；正式發布前仍依工具執行既有 COPY_ONLY／CHECKSUM／VERIFYONLY 備份門檻
- 發布影響：正式 API 與 Web 版本皆遞增；測試站保持
- rollback：回復本批 API／Web 檔案備份，不還原或搬移測試資料

## 需求與範圍

使用者於 2026-11-17 確認以下五項測試功能均已人工驗收，並明示授權正式發布：

1. PPC 公式儲存沿用外層稽核交易。
2. 藥液量測確認避免稽核與服務重複交易。
3. 空白第二滴定值在預覽與確認視為相同未提供值。
4. 滴定值先取三位再計算，分析值取三位。
5. 刪除量測沿用外層稽核交易並保留不可變 Receipt／Trace。

只將上述已驗收程式差異套入正式 API 0.1.127 基準。Web 功能不變，但依固定發布規則遞增可見版本。不得帶入測試資料、測試公式／F／排程、測試 `web.config`、未授權前端工作或資料庫變更。

## BDD 驗收

- Given 稽核篩選器已有關聯式交易，When 儲存 PPC 或刪除量測，Then 服務沿用既有交易且不因巢狀交易回傳 500／503。
- Given 藥液確認需要服務交易，When 保存量測，Then 量測、Receipt、Trace、統計與稽核同成同敗，通知只在提交後執行。
- Given 單滴定項目第二值為 `null` 或空字串，When 預覽後確認，Then 快照視為相同未提供值；有效第二值或其他輸入變更仍拒絕。
- Given 初驗或複驗滴定值，When 執行公式，Then 輸入先以 AwayFromZero 取三位、公式結果再取三位，Receipt 保留原始值及計算值。
- Given 正式發布完成，When 由正式 Web 與用戶端存取，Then 首頁、主 JS／CSS、API health/version 均成功且 environment 為 production，Web 與 API 顯示本批新版本。

## 驗證計畫

- Happy Path：五組既有焦點測試與整合測試通過；production publish 通過。
- Boundary：空白／有效第二滴定值、三位中點取捨、外層交易有無兩種路徑。
- Invalid Input：預覽後真正輸入／日期／規格／公式變更仍拒絕。
- Regression：量測保存、刪除、PPC 儲存、Receipt／Trace 與既有公式計算相關測試。
- 正式發布：先 Plan、正式庫備份驗證、檔案備份、Apply；完成 Web／資產 MIME、API production、實際瀏覽器 API 基底及 CORS 驗證。

## 正式發布結果

- 隔離候選由正式API0.1.127來源依原提交順序加入五項差異，API升0.1.133；正式Web0.1.138同源只升可見版本至0.1.139。migration 0，沒有測試資料、公式、F、排程或設定搬移。
- 五項合併焦點回歸70項通過；2項既有藥液歷史判定測試維持原失敗且未增加。API Release publish、Web production build及i18n 2863×3通過。
- Production Plan核對`PMR-SPC-SRV.pmr.com.tw`／`SpcApi`／`SpcWeb`／`PMR_SPC_2026`正確；Apply完成正式資料庫COPY_ONLY／CHECKSUM／VERIFYONLY與API／Web檔案備份，兩個App Pool均Started。
- 正式首頁、主JS`index-DrkovEHS.js`、CSS`index-m59bBhiY.css`皆200且MIME正確；前台版本0.1.139，API health/version為0.1.133／production，CORS正常。Chromium由正式Web來源直連8443 API，health/version皆200／production。
- 原進度表小工作總表292～294、302～303及專案里程碑323／327／331／363／367已同步；核心未完成由41降為37。公式錯誤0、OpenXML錯誤0、ZIP13項、drawing1、重複entry0。
