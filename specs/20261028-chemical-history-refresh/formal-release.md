# 002／008正式最小修補候選
- 狀態：使用者明確授權後，正式最小修補發布及本人登入唯讀驗證完成（2026-10-28）。下方候選準備紀錄保留追溯。
- 正式SPC API0.1.71，ProductVersion的提交碼9de1599不是完整的發布來源；實際為正式R2基準`a376290fd3da244397a6b04ce9270342eed80798`加已驗收的PPC選填MachineId單行修補及版號，見[當時驗證](../20261022-ppc-optional-machine-edit/verification.md)。保留的production-package DLL與正式目前SHA256皆為423204ffc1b0cf685c7e71b46f4beb6b71ec561690fcd126e2102c6142058f18；不可僅憑InformationalVersion回退來源。
- 以已驗證正式R2隔離來源另建本機候選0.1.90（三段版本，/api/version不支援四段版本核對）；只套002的GetMeasurements唯讀投影、ChemicalHistoryQueryService及既有trace Read可見性，保留MachineId已驗收修補，其他Controller、Program、權限、模型、migration與套件保持基底。候選建置及查詢測試通過後才可提出發布授權。當前來源另有78個backend差異，不直接發布0.1.89整包。
- Portal正式API196/Web239來源`bab8f0f49cf35abb8f903565d1e82b6668950228`；至本次目前來源的產品差異僅藥液頁面與兩個版號（008重試與002判定）。已驗證測試API199/Web242，正式發布時依根目錄scripts/publish-all.ps1自增為API200/Web243，不覆蓋正式環境設定。
- 正式目標：PMR-SPC-SRV的C:/inetpub/wwwroot/production/backend；PMR-WEB的C:/inetpub/wwwroot/publish/portal-api與portal-web。不改SPC frontend、不改F或公式、不寫入量測、不新增migration，不處理其他工作。
- 正式發布前須再次核對部署版本／來源及設定、備份實際目錄、檢查pending migration與模型不變；若與本次核對不同先重新評估，不直接套用。
- rollback：從本件各主機同批備份，僅還原本次變更的四個SPC程式檔，以及Portal API/Web各四個程式檔（dll/exe/pdb/deps.json）；先停該站實際pool，還原後重啟並核對版本／health／登入與查詢。不還原uploads、DB或覆蓋現場設定／後續量測。

## 本機候選驗證
- `scripts/prepare-chemical-history-formal-hotfix.ps1`以既有正式R2來源重建；確認R2相對a376的C#／csproj僅PPC單行修補及版號兩檔。候選相對該正式來源只改歷史查詢Controller、trace Read可見性及版號，新增唯讀投影服務；Program／模型／migration／套件不變。
- 正式隔離來源新增查詢／既有trace共13項測試通過，另先前PPC選填MachineId四項回歸通過；Release publish成功。
- 與R2的97個payload逐檔比對，只有MesSpc.Api.dll／exe／pdb／deps.json四檔不同，其他93檔相同；範例appsettings不納入payload。正式候選位於release-staging/chemical-history-formal-20261028/backend，DLL SHA256 d9dad5bf5a4fe9a99c10502aa6a1d2ad50cb33b9d043647ffb23b818eb864eba。
- 主程式測試站仍0.1.89，候選0.1.90尚未部署任何環境；沒有以候選冒稱正式或測試站已升版。
- 已提出本次002／008正式發布限定授權問題；未回覆前不執行備份／停站／正式複製或發布。009未知F與有效日保持待辦。

## 正式授權、發布及驗證（覆蓋前述候選未部署狀態）
- 使用者回覆「允許上述002／008正式修補發布」；範圍為SPC0.1.90四檔最小修補、Portal API200/Web243，備份／短暫維護／回復，不改F／公式／schema／既有量測。
- 兩台主機的備份根均為`C:/ProgramData/PMR-Releases/Production/SPC-CHEM-002-008-20261028`；SPC子目錄backend，Portal子目錄portal-api／portal-web。備份目錄限SYSTEM、管理員與執行者，未擴大正式應用權限。
- 發布前以候選編譯組件的EF實際註冊遷移清單核對54筆，pending=0。檔名／原始Migration attribute掃描曾把未註冊類別算入，改用EF組件metadata查明；沒有新增或套用DDL，不以手動補migration history通過檢查。
- SPC只停止SpcApi實際pool，Copy-ToSession四檔並逐檔SHA256核對；正式DLL0.1.90.0、/api/version=0.1.90/environment=production、health=ok；2個環境檔hash不變。SpcWeb未發布，原前端及其他93個payload保持。
- Portal從D:/PmrPortal根目錄`scripts/publish-all.ps1 -OutputRoot \\PMR-WEB.pmr.com.tw\C$\inetpub\wwwroot\publish`完成正式發布；實際API1.0.200.0／Web1.0.243.0。441個受保護設定／uploads hash一致；逐檔比對備份，API/Web分別只有各自dll/exe/pdb/deps.json四檔變更，ChemicalAnalysisRules與其他既有內容未變。
- 發布後本人再登入正式Web243；藥液查詢完整7098筆可讀（現場持續使用造成筆數隨時間變化，不代表本件新增）；QE2 10/27篩選7筆，其中蝕刻硫酸14.04／NG與初驗值／NG可見，F仍保持原值；缺相符歷史紀錄明示無法判定，未套最新規格猜OK。
- 正式複驗優先實例：既有ST1硫酸顯示複驗5.37、初驗3.90；該舊筆缺相符判定證據故顯示無法判定。複驗0、初驗NG／複驗OK及raw1.004／stored1／USL1.002保留NG由自動測試驗證；未為驗收新增正式量測。
- 發布後N2 10/27早班開線、收線各載入10筆，無console錯誤；查詢沒有重現Failed to fetch。本件沒有模擬正式斷線或重送保存，故不宣稱找到當時的網路根因；故障狀態／GET重試／不重複POST以受控自動測試覆蓋，正式已部署相同行為。
- 已保存正式查詢畫面於本機`.codex-tmp/chemical-four-20261028/query-judgment-formal.jpg`，不提交截圖。002與008必要驗證完成、核銷；007已驗證，僅009依使用者回覆等待F／日期，核心未完成26件。
- 原Excel更新完成：SPC第7階段及Portal摘要／明細、002／008各登錄完成；213登錄／197具名、171完成／26未完成、人工待驗11／核銷4。原生22物件數保持，只調整302等待色及304／307／308正式完成色／標記，其餘18個逐一不變；日期同為實際2026/10/28。
- Excel副本及原路徑OpenXML0錯誤，ZIP／重開計數／公式錯誤掃描／11頁列印通過；未以Microsoft Excel人工開啟。取代前核原檔未變，備份Voice/備份/進度表_藥液002008正式完成前_20261028-112135.xlsx；輸出SHA256 cfbdbe0a13a6a32bbb2de24bbb39228a5c7ad7909c0764d05c32f68924319613。需回復時先確認無後續更新再使用該備份。
