# 驗證紀錄

- 功能 ID：`20261120-chemical-adjustment-excel-alignment`
- 規格版本：9
- 日期／環境：2026-11-20／來源活頁簿＋PMR_SPC_TEST唯讀查詢＋SPC測試站
- 實作狀態：TASK-001～005完成；2026-11-20使用者已驗收兩頁對應並接受PPC2587目前值22
- 驗證狀態：TASK-001～003通過；TASK-002完整API回歸另有16項既有失敗如實保留
- 發布狀態：測試 Web `0.1.172`／API `0.1.161/test` 已發布；正式站未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 固定SHA-256並讀取工作表、標題與資料列 | 列出所有資料 Sheet 與來源範圍 | SHA-256 `F634868A…48C7C`；11張資料Sheet／142列，另1張F表 | 通過 |
| AC-002 | Excel 與測試 PPC 自然鍵對帳 | 唯一映射、衝突隔離 | 114來源身分中110唯一配對、4未配對、0一對多；網站另3項，詳[矩陣](reconciliation-matrix.md) | 通過 |
| AC-003 | `NA`、空白、0、正數分類 | 不錯誤覆寫 | 槽容95相符／15空白；添加99相符／11 NA；稀釋90相符／19 NA／1差異。所有非相符列零寫入 | 通過 |
| AC-004 | 公式參照轉換與差異分類 | 分離門檻、槽容、目標與倍率 | 調整式74相符／14網站缺／3來源缺／19不同；調整量53相符／22皆空／7網站缺／3來源缺／25不同，詳[結果](reconciliation.md) | 通過 |
| AC-005 | API 單元／整合測試 | 三變數計算與缺值拒絕 | 三變數不分大小寫可計算；引用空值與已設定0／負數拒絕；排程採不可變公式版本參數。計算＋排程46項通過 | 通過 |
| AC-006 | 兩頁與匯出畫面測試 | 同源顯示、單一編輯權責 | 總覽從同一JSON唯讀顯示槽容／添加／稀釋，匯出同步三欄；公式保存保留參數，引用缺值顯示三語警示 | 通過 |
| AC-007 | 測試資料 preview/apply/retry/rollback | 受控同步與可回復 | 7筆核准列新增7版本／7稽核；重送7筆NoChange／0新增，備份VERIFYONLY通過、保護指紋不變；使用者驗收目前畫面並保留PPC2587值22 | 通過 |
| AC-008 | 聚焦與回歸測試、build、測試站 smoke | 無明顯 regression | TASK-003畫面64項與三語2895鍵通過，test build成功；測試Web／資產／API／瀏覽器同源API smoke通過。TASK-002既有API失敗另保留 | 通過 |
| AC-009 | 兩式門檻／槽容具名參數轉換、隔離、套用與冪等 | 不以LSL／USL或硬編碼常數作動作依據 | 91筆完成具名參數轉換並新增91版本／91稽核；17 NoFormula不變、5 MissingParameter隔離。重送NoChange、最新版本、樣本公式、計算及保護指紋通過 | 通過 |

## 文件檢查

- 規格、計畫、任務、驗證及來源盤點互相連結。
- 新小工作已按資料正確性風險及相依順序排入 `TODO.md`。
- 來源 Excel 未修改；未開啟或更新已停止例行維護的進度表。
- `python -m py_compile reconcile_readonly.py` 通過；以相同來源與相同測試查詢重跑，矩陣 SHA-256 兩次皆為 `7D18882F2246998B5C380A32795ABB04851E9D6E568A70D4C14986E51F51A754`。

## TASK-002 測試、發布與回復

- 紅燈：新增具名變數與非正數案例後，`ChemicalFormulaCalculationTests` 36項中4項失敗；證明修改前不支援三參數且不拒絕非正數。
- 綠燈：計算／排程聚焦46項全數通過；擴大到公式版本、追溯及量測共75項時73項通過，兩項既有 `ChemicalPortalMeasurementTests` 因現行服務將 `IsExcluded` 正規化為false而找不到排除列，與本次檔案無交集且未順手修正。
- 完整 API：614項中587通過、16失敗、11略過。其餘既有失敗集中於時間敏感的歷史證據判定及 EF InMemory `GroupBy` 翻譯；本件未宣稱完整回歸全綠。
- 建置：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj -c Release --no-restore` 通過，0警告／0錯誤。
- 版本：引擎 `portal-excel8-v5-adjustment-parameters`；API `0.1.160`→`0.1.161`；Web未改，維持`0.1.171`。前台既有版本區塊從同源`/api/version`動態顯示API版號。
- 發布：IIS `SpcApiTest` 實際目標 `D:\SPC\release\test\backend`；備份 `D:\SPC\release\backups\api-0.1.160-before-task002-20261120-144937`。`appsettings.json` 發布前後SHA-256同為 `811F4DA9…F8BFB`。
- Smoke：`https://SPCTEST.pmr.com.tw/`、主JS `index-A75FFLgU.js`、主CSS `index-BlAfmLFs.css`、同源`/api/health`與`/api/version`、直連`:8901/api/version`均HTTP 200且MIME正確，API回`0.1.161/test`。Playwright Chromium直接開啟同源`/api/version`亦回200、`0.1.161/test`；未見API離線或環境錯置。
- 回復：停止`SpcApiTest`集區，從上述備份還原API檔案並保持設定檔，再啟動集區與重驗health/version；未執行回復。無schema migration、PPC／公式／F／量測或資料寫入。

## TASK-003 測試、發布與回復

- 紅燈：既有總覽找不到三個唯讀欄位，Xlsx欄位契約亦缺槽容／添加／稀釋；新增2項畫面／匯出契約後均如預期失敗。
- 綠燈：`playwright.i18n-chemical.config.ts` 53項及`playwright.chemical-history.config.ts` 11項全數通過，涵蓋三語、1920／1366／1280／390寬度、桌機全寬、手機橫向捲動、匯出、版本歷史、公式保存及參數不覆寫。
- i18n／建置：`npm run build:test`通過，三語目錄2895鍵一致；僅保留既有Browserslist資料較舊及chunk大於500kB提示，無建置錯誤。
- 版本：Web `0.1.171`→`0.1.172`；API未改，維持`0.1.161/test`。無schema migration、PPC／公式／F／量測或資料寫入。
- 發布：IIS `SpcSingleTest`實際目標`D:\SPC\release\test\frontend`；更新前完整備份`D:\SPC\release\backups\web-0.1.171-before-task003-20261120-150531`，部署時移除27個舊雜湊資產，均可由備份回復。
- Smoke：首頁、主JS`index-BZ75TLhy.js`、主CSS`index-BJMaQS-8.css`、同源`/api/health`與`/api/version`均HTTP 200且MIME正確；資產含Web `0.1.172`與新三語欄位／警示，API回`0.1.161/test`。Playwright Chromium直接開啟同源`/api/version`回200；未登入首頁依既有流程導向Portal測試登入，未冒充登入後人工畫面驗收。
- 回復：停止`SpcSingleTest`集區，以上述備份鏡像還原前端，重新啟動後重驗首頁、資產與同源API；未執行回復。

## 未執行項目、限制及後續

TASK-004 已開始。2026-11-20 15:15 +08:00 重新確認來源 SHA-256 仍為 `F634868A1AD1B432C7AA9E107BE0D76B1F9851AE38A244BE3754D17EAC348C7C`，測試環境為 `PMR_SPC_TEST`；113筆啟用藥液 PPC 的設定快照 SHA-256 為 `1BC3EF162E6B6E4A588567B44E2EED7009D830D511C937161F806D8BF3591790`，當時公式版本683筆、稽核事件6853筆。

UTF-8唯讀 preview 重跑仍為114個來源身分、110唯一配對、4來源未配對、3網站限定；槽容95相符／15空白、添加99相符／11 NA、稀釋90相符／19 NA／1不同；兩式分類亦與 TASK-001 完全一致。第一次預檢時所有實際差異仍落在[六項未決業務規則](reconciliation.md)，當時尚無逐列核准清單，因此依 BDD「未確認差異零寫入」停止；後續核准與執行狀態如下。

### TASK-004 核准後Preview與阻擋

- 使用者核准`23／不建立／保留網站／採N2槽容／保留網站／保留網站`後，受控候選縮為PPC 2512、2514、2515、2516、2519、2550、2594共7筆；只替換錯用的槽容token，網站門檻、NA規則、係數、文字、其他欄位及既有排程保持。
- 新增失敗關閉工具與15項測試：7筆固定身分、固定舊／新公式SHA-256、只改`adjustmentAmountFormula`、重送NoChange及7組候選計算全數通過；工具Release build 0警告／0錯誤，最終plan為7筆Update。
- 初始snapshot後，PPC2587由既有頁面新增版本6並把稀釋值23改成22；此列不在7筆候選，工具將其納入其他PPC保護指紋，未覆寫。因與文字核准23衝突，仍需使用者再次確認。
- 2026-11-20 15:34 +08:00 建立`PMR_SPC_TEST_N2_CAPACITY_20261120-153400.bak`；SQL備份歷程確認COPY_ONLY及CHECKSUM成功，約96MB。應用帳號及Windows整合身分執行`RESTORE VERIFYONLY ... WITH CHECKSUM`皆因缺少master的CREATE DATABASE權限失敗；工具在交易前停止，公式版本、稽核及PPC均0寫入，不降低備份驗證門檻。
- 此次權限阻擋時尚未執行apply／retry；後續DBA完成VERIFYONLY後的套用結果見下節。來源Excel、既有排程、歷史試算、網站程式或版本均未修改，沒有測試站或正式站發布。

### TASK-004 測試資料套用與重送

- 使用者回報 DBA 執行`RESTORE VERIFYONLY ... WITH CHECKSUM`結果為「檔案1上的備份組有效」，完成時間`2026-11-20T15:38:31.1091985+08:00`；受控工具據此使用已固定備份，不重複建立未驗證備份。
- 2026-11-20約15:39 +08:00在`PMR_SPC_TEST`單一Serializable交易套用PPC 2512、2514、2515、2516、2519、2550、2594；目前設定只改調整量公式的核准槽容token，新增公式版本1400～1406及每列一筆`configuration.formula.update`服務稽核。其他PPC、其他公式版本、F歷史、量測、公式政策／修訂／生效區間指紋均不變。
- 套用後7筆槽容依序為600、250、630、630、1200、1000、1200，公式SHA-256前12碼依序為`87AC90C20820`、`A170B965C15B`、`129AB463EC17`、`3844C8EE78A3`、`E1B2D50F1671`、`517BFFD0CEC3`、`70C148F0CA80`；最新版本與目前設定JSON一致。
- 相同清單第二次apply為7筆`NoChange`，公式版本+0、稽核+0；套用後啟用藥液PPC快照SHA-256為`2BFDF6A54D170FA2DE110D61C7FD95F443BEBE6D7D6C26F5EB8606F7D2D6D858`。PPC2587保持最新並行值22，工具未覆寫。
- 測試：新增15項固定身分／欄位隔離／公式雜湊／NoChange／候選計算全過；既有計算、公式版本、排程及追溯聚焦60項為59通過／1項需Portal來源條件略過；受控工具Release build 0警告／0錯誤。
- 連線：測試站首頁、主JS`index-BZ75TLhy.js`、主CSS`index-BJMaQS-8.css`均HTTP 200且MIME正確；同源health為`ok`、version為`0.1.161/test`。本次只改測試資料與受控工具，Web `0.1.172`／API `0.1.161`未改、未重新發布，正式站未異動。
- 兩頁畫面：依computer-use技能初始化後，Windows native pipe連續失敗，重置後仍不可用；未冒充登入後「藥液量測調整公式」及「線別分析項目總覽」人工驗收。PPC2587最終採23或保留22亦待使用者確認，因此TASK-004尚未DONE。

### TASK-004 使用者人工驗收

- 2026-11-20使用者回覆第1項已驗收，確認「藥液量測調整公式」與「線別分析項目總覽」的槽容、添加／稀釋管制值及調整／調整量公式對應可接受。
- 驗收當下PPC2587稀釋管制值為22；依使用者對目前畫面的驗收結果保留22，不再恢復先前文字候選23。此核銷不產生新資料寫入、版本、發布或回復動作。

## TASK-005 具名參數轉換預覽與阻擋

- 轉換器以公式運算子與動作分支辨識添加／稀釋門檻，並只在乘法容量位置替換槽容；引號內文字、`/1000` 單位換算、Target、配槽目標、比重與倍率保持。`IF(Concentration>門檻,"","添加")`反向寫法明確對應添加參數。
- 核心紅綠測試9項通過；連同既有計算與N2槽容聚焦共24項通過。工具建置0警告／0錯誤。
- 測試庫113筆啟用藥液PPC快照為`2BFDF6A54D170FA2DE110D61C7FD95F443BEBE6D7D6C26F5EB8606F7D2D6D858`；preview為91筆可更新、17筆無公式、5筆因缺稀釋管制值隔離。91筆候選均以添加以下、區間內、稀釋以上代表值送入現行計算引擎，無語法、未知參數或除數錯誤。
- 2026-11-20 17:15:52～17:15:53 +08:00 已建立`PMR_SPC_TEST_ADJUST_PARAMS_20261120-171552.bak`；`msdb`確認COPY_ONLY流程完成且`has_backup_checksums=1`。應用SQL身分及Windows整合身分執行`RESTORE VERIFYONLY ... WITH CHECKSUM`均因缺少`master`的`CREATE DATABASE`權限失敗。
- 工具在Serializable交易之前失敗關閉；公式設定、公式版本、稽核、排程、F及量測均0寫入。須由DBA確認此新備份有效後，才可固定該備份證據並續行91筆套用、NoChange重送及保護指紋驗證。

### TASK-005 備份確認後套用與驗證

- 2026-11-20 20:38:53 +08:00 使用者確認上述備份組有效；工具固定該檔案及確認時間，不再建立未驗證的新備份。
- 以原快照`2BFDF6A54D170FA2DE110D61C7FD95F443BEBE6D7D6C26F5EB8606F7D2D6D858`為交易前守衛，在單一Serializable交易更新91筆目前設定，新增91個公式版本及91筆`configuration.formula.update`服務稽核；17筆無公式與5筆缺稀釋管制值未寫入。
- 套用後快照為`5DC845262C59D49EA739B65983EE650ABCD9D00DBCF2285F298956CC813CBFF6`；再次preview為91 AlreadyAligned／17 NoFormula／5 MissingParameter／0 Update，相同apply回`NoChange`，verify確認91筆目前設定均等於最新公式版本，且TASK版本／稽核皆為91。
- C5／清潔／硫酸（PPC2425）抽查通過：調整式使用`ADDITION_CONTROL_VALUE`／`DILUTION_CONTROL_VALUE`；調整量式三處槽容均使用`TANK_CAPACITY_LITERS`，`/1000`單位換算保持且未出現`/TANK_CAPACITY_LITERS`。
- 排程修訂、有效區間、F歷史及量測全表指紋在交易前後一致。未修改API／Web程式或設定，故版本維持API`0.1.161/test`、Web`0.1.173`，不需網站發布；正式資料與正式站未異動。
- 隔離的5筆為PPC 2513、2589、2590、2594、2595，皆因公式含高值動作但「稀釋管制值」未設定；依規格不以舊數字或USL代入，待日後補齊參數才可轉換。

## 基準與變更紀錄更新位置

- [需求索引](../../docs/requirements.md)
- [中央工作池](../../TODO.md)
- [變更紀錄](../../CHANGELOG_CUSTOM.md)
- [AI 修改目的](../../ai_docs/10_change_log.md)
