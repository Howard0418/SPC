# 唯讀核對與調整建議
- 核對日期：2026-10-22（Asia/Taipei）。使用者澄清為「調整，不是測試」；僅做現況確認與文件規劃。
## 正式現況
| 項目 | 實際結果 |
| --- | --- |
| 主機 | PMR-SPC-SRV.pmr.com.tw／172.16.119.140 |
| API/IIS | SpcApi，Started，C:/inetpub/wwwroot/production/backend |
| Web/IIS | SpcWeb，Started，C:/inetpub/wwwroot/production/frontend |
| API入口 | http://172.16.119.140:8081/api |
| 環境/Provider | production／SqlServer |
| SQL主機 | 172.16.110.16 |
| 設定資料庫 | PMR_SPC_2026 |
| SELECT DB_NAME() | PMR_SPC_2026；連線成功 |
| API /api/version | 0.1.68，production |
| 已部署DLL | 0.1.68.0（FileVersion；與API三段版本一致） |
| /api/health | status=ok，version=0.1.68，production |
- 合併appsettings.json與appsettings.Production.json，檢查IIS pool/web.config及Machine/Process相關環境覆寫，未發現改寫連線或環境的設定。
- DB health原端點只回應環境與版本，不能單憑ok證明DB成功；本次另外用已核對設定建立連線且只執行SELECT DB_NAME()。
- 正式現況符合先前正式發布API0.1.68基準；測試API較新不代表正式版本錯誤，不自行追最新版或升版。
## 格式判斷與建議
- 現況未發現JSON解析、SqlServer連線字串格式、正式DB目標或API版本一致性異常，不排入無證據修復。
- 版本顯示：API採major.minor.patch（例0.1.68），DLL可有第4段build（例0.1.68.0）；比對前三段，末段0不是版本不符。只有前三段不同才核對部署/快取/入口。
- 設定格式：沿用ConnectionStrings:SqlServer、DatabaseProvider=SqlServer、AppEnvironment=production；正式資料庫必須PMR_SPC_2026。若未來出現格式錯誤，先確認JSON雙引號/逗號與連線欄位Server/Database等語法，再以SqlConnectionStringBuilder解析及SELECT DB_NAME()核對；不得貼出密碼或將正式設定替換成測試設定。
- API URL：現有正式前端/API為既有分站拓撲，沿用正式8081/api；不得直接套用測試站/api假設，也不因HTTP位址本身即擴張為本次IIS/HTTPS改造。
- 使用者未提供其他欄位、畫面、檔案或API payload的「格式」範例；目前不能判定Excel日期/數值或JSON資料內容不符。需先界定對象與預期格式，再決定最小調整。
## 本次文件驗證與限制
- 規格/工作池/需求/變更紀錄內容及相對連結檢查；git diff --check。
- 不修改任何程式/正式設定/資料/IIS/角色/Runtime；不啟動應用、migration或發布。build與發布不適用（文件型工作）。
- 原始唯讀腳本tmp_production_config_plan/inspect.ps1不提交；未記錄密碼、完整連線字串或業務資料。

## 補充：正式PPC重複提示唯讀調查
- 使用者回報「此SPC管制項目已存在相同…」提示。來源為MasterDataV2Controller Create/Update在SaveChanges之前的查重，回HTTP409；Update條件排除自身Id。
- 正式PMR_SPC_2026按ControlScope/PartId/ProcessId/MachineId/TankId/SlotId/CharacteristicId/Unit分組，COUNT>1之群組數為0。此為本次查詢時點結果，不保證未來或其他定義下永無重複。
- sys.indexes證實兩個啟用中的條件式業務唯一索引：PartId IS NULL；以及PartId/MachineId/TankId/SlotId/Unit皆非NULL。不可將條件式索引描述成所有NULL組合都有資料庫層完整保證；後續須審查nullable組合及並發覆蓋。
- 此提示代表本次送出的識別欄位與既有另一Id相符，並不代表已成功寫入第二筆。查重未過濾停用狀態，所以既有停用項目亦可能衝突；尚未定位使用者實際項目，不將可能原因當成根因。
- 尚需新增/編輯情境及項目ID或線別/槽體/檢驗特性，才能確認是合理阻擋、欄位被改成其他項目、前端送錯Id或既有資料問題。
- 建議先唯讀定位衝突項目，再評估顯示衝突Id與編輯入口；不得為避開提示而任意改單位/槽位或刪除有追溯資料的主檔。
- 本次只SQL SELECT及來源閱讀，未新增/修改/刪除正式PPC，未實作或發布。
