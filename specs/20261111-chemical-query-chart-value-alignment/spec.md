# 小型變更：藥液查詢與管制圖分析值一致化
- 功能 ID：`SPC-CHEM-QUERY-CHART-ALIGN-1111-TASK-001`
- 版本：1
- 狀態：完成；Portal 與 SPC 測試／正式站均已發布
- 專案／授權依據／需求基準：Portal＋SPC；使用者 2026-11-11 指示新增頁、查詢頁及藥液管制圖均以分析值為主，複驗有值時以複驗分析值為主。

## 問題、預期行為與範圍
- SPC API 已建立唯一的有效分析值 `effectiveValue`，但 Portal 查詢頁仍自行用欄位組合判斷，形成契約分歧風險。
- 查詢表未顯示班別與取樣階段，使用者可能把同日不同班別或 N1／N2 開收線資料誤認為同一筆。
- 預期：同一量測列以 `RecheckValue ?? MeasuredValue` 顯示、判定與繪圖；查詢頁直接使用 API 的 `effectiveValue`，並顯示班別及取樣階段。
- 比對鍵：日期／量測時間、班別、取樣階段、線別／機台、槽位、分析項目；不得跨列取值。
- 非範圍：不修改量測、公式、F 表、規格、判定歷程或資料庫 schema；不重匯資料。
- 發布／回復：先發布測試站；正式站另行授權。回復為還原 API 欄位與 Portal 顯示版本。

## 需求與驗收
- R-001：複驗分析值存在（包含 0）時，查詢與管制圖採複驗值；否則採初驗分析值。
- R-002：Portal 查詢頁使用 SPC API 的 `effectiveValue`，舊 API 相容回退複驗／初驗欄位。
- R-003：查詢列顯示班別與取樣階段，避免跨班別、開／收線誤比。
- AC-001：Given 初驗 19.04 且無複驗，When 查詢或繪圖，Then 顯示 19.04。
- AC-002：Given 初驗 19.04 且複驗 20.16，When 查詢或繪圖，Then 顯示 20.16 並標示複驗。
- AC-003：Given 同日有早班／中班或開線／收線，When 查詢，Then 每列顯示自己的班別與取樣階段，不互相套值。

## BDD
```gherkin
Feature: 藥液有效分析值一致化
  Scenario: 無複驗時使用初驗
    Given 同一量測列只有初驗分析值
    When 使用者查詢或開啟管制圖
    Then 查詢與圖表使用該初驗值

  Scenario: 有複驗時優先使用複驗
    Given 同一量測列同時有初驗與複驗分析值
    When 使用者查詢或開啟管制圖
    Then 查詢與圖表使用複驗值且保留初驗顯示

  Scenario: 同日不同班別不得混用
    Given 同日同項目有不同班別或取樣階段紀錄
    When 使用者查看查詢結果
    Then 每列顯示班別與取樣階段並只使用該列有效值
```

## 計畫與任務
- 受影響檔案：SPC measurements 查詢契約；Portal 藥液查詢頁與契約測試；需求及變更紀錄。
- [x] T-001：先建立 Portal 有效值與班別／取樣階段失敗測試。
- [x] T-002：查詢 API 回傳取樣階段，Portal 改採 `effectiveValue` 並顯示識別欄位。
- [x] T-003：完成測試、建置、測試站發布及實站連線驗證。（Portal測試／正式及SPC API測試已發布；正式SPC仍依正式發布流程管理）
- [x] T-004：同步進度表、提交並上傳。

## 驗證
- Happy Path：初驗或複驗的有效值顯示一致。
- Boundary Case：複驗為 0 時仍優先；同日多班別／階段分列。
- Invalid Input：有效值缺失時維持 `-`，不得補 0 或跨列取值。
- 實際結果及證據：Portal 契約2/2、Web回歸41/41、SPC API回歸96/96（另1略過）與兩專案 Release build 通過。Portal 測試 API `1.0.231`／Web `1.0.272` 已發布；Login、app-version、CSS、JS、API health、Test DB標籤及匿名302通過。SPC測試交付包Plan通過，但Apply在必要的`PMR_SPC_TEST`備份階段安全停止，現行API仍`0.1.125`且health／version／test正常，無檔案或DB套用。
- 發布狀態：使用者於2026-11-11明示正式發布授權後，Portal以既有正式API `1.0.230`／Web `1.0.271`基準只加入本件修正，正式升至API `1.0.231`／Web `1.0.272`。正式Plan核對`PMR-WEB`、`PmrPortalApi/PmrPortalWeb`及`PMR_PORTAL_UAT`，無migration；Apply完成IIS／應用與DB備份、`COPY_ONLY/CHECKSUM/RESTORE VERIFYONLY`、檔案套用與smoke。`https://login.pmr.com.tw:8443/health`回200／production，`/app-version`回`1.0.272.0`，Login、CSS與JS均200且MIME正確，匿名PortalLinks維持401。SPC API `0.1.126`仍待具備測試庫備份權限後先完成測試發布，未越過測試先行或更新正式SPC。
- 2026-11-11 使用者再次授權發布後重試SPC測試Apply；受限證據固定回報`failed-before-apply`、phase=`backup-database`、newFiles=0，仍無法完成必要備份。測試API保持`0.1.125`／test，`SpcApiTest`與`SpcSingleTest`池均為Started，Web、health與version 200。未套用API126、未接續正式SPC發布。
- 畫面驗收：電腦操作工具回報瀏覽器存取主機與原生pipe均無法連線，未冒稱登入畫面驗收完成。
- 進度表：原檔已備份後更新小工作總表與Portal／SPC正式里程碑；候選及原路徑重讀、渲染、ZIP部件數與OpenXML SDK 0錯誤通過。回復可使用`進度表-20261111-chemical-query-formal.xlsx`。
- 2026-11-11 R3後續：以使用者明示授權暫時加入`dbcreator`後完成測試庫`COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY`與API套用，測試API為`0.1.126／test`；Web、主要JS／CSS MIME、health與version均通過。暫時角色已於finally撤除，最終`dbcreator=0／sysadmin=0／BACKUP DATABASE=1`。
- 2026-11-09 N1早班資料唯讀核對：正式與測試均有`表處／氫氧化鉀`初驗`19.04`，另有`促進／氯化亞錫`初驗`0.2256`（畫面依精度顯示`0.23`）；兩者PPC、槽位及分析項目不同，不可跨列比值。兩環境各自相同，無複驗值，`effectiveValue`均等於初驗值；未修改資料。
- 依既有正式發布授權，使用同一已驗證API候選建立正式R1；Plan核對`PMR-SPC-SRV／PMR_SPC_2026／SpcApi／SpcWeb`，migration為0。Apply完成正式庫`COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY`與檔案套用，SPC正式API升至`0.1.126／production`，Web維持`0.1.127`。
- 正式站首頁、主要JavaScript／CSS、`/api/health`、`/api/version`及前端資產內實際API基底`https://PMR-SPC-SRV.pmr.com.tw:8443`均驗證成功；兩個正式AppPool為`Started`。未重匯或改寫量測、Receipt／Trace、公式、F表、規格或主檔。
