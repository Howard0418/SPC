# Antigravity 測試案例：管制圖日期分段與版本明細

## 可直接交給 Antigravity 的指令

```text
請閱讀 D:\SPC\specs\20261020-chart-segment-version-detail\antigravity-tests.md，依序執行 AG-01～AG-10。
僅操作 https://spctest.pmr.com.tw/；先核對頁面 Web 0.1.67、API 0.1.63。
使用已登入的 SPC 帳號；登入失效請讓我自行登入，不讀取密碼或產生權杖。
本次只測查閱功能，不新增、修改、刪除資料，不匯入、不重算、不還原版本、不修改資料庫或正式站。
每例回報 PASS／FAIL／BLOCKED，附實際網址、管制項目ID、分段ID、操作、預期與實際結果及必要截圖。
缺少資料請標 BLOCKED，不自動建立資料，不把 mock 通過當作真實站台驗收通過。
自動選版、生效日期設定及量測實際版本留存尚未在本次實作，不應測成已支援；未知歷史版本必須明示。
如可使用本機終端，可另外執行本文件的離線自動化命令，將其與線上結果分開回報。
```

## 前置資料與入口

- 以有查閱權限的帳號登入測試站。從「SPC 管制分析」選日期與線別，查總表後進入單一管制圖。
- 藥液若導向「量測值趨勢圖」，同樣可用日期按鈕；圖線點擊入口僅在SPC互動管制圖的既有分段規格線提供。
- 2026-10-20核對：測試庫分段0筆、規格歷史0筆，PPC2425主檔有7筆公式歷史。現階段線上完整選段／規格分頁應標BLOCKED，先跑離線fixture，不自動建立資料。
- 選一個有既有日期分段且圖表內有量測的藥液項目 A；最好有兩個分段及含參照文件的公式歷史。
- 選一個非藥液項目 B；選一個無分段／歷史的項目 C（可與 A／B 共用）。
- 記錄項目ID（圖表請求 `ppcId`）與分段ID；不要假設測試站具有固定ID。
- 在圖表上方可見「日期分段與版本明細」。分段按鈕列的是目前已設定的日期區間；未必均與本次製圖日期相交。
- 分段時間保留既有設定，含起迄時間／無迄日；修改時間另以台北時區顯示。

## 案例

| 案例 | 操作 | 預期結果 |
|---|---|---|
| AG-01 正常選段 | A 點第一個日期區間；再點另一區間 | 顯示正確分段ID、起迄與備註；USL／LSL／Target／UCL／CL／LCL與該分段設定一致，不殘留前一段。 |
| AG-02 回退值 | 找一個部分界限空白的既有分段，再點「主檔（未命中分段時）」對照 | 分段有值標「來源：本分段」；空白採主檔值，標「目前主檔回退值」；主檔也空白顯示「未設定」，不以0代替。圖型自算管制線以圖表為準。 |
| AG-03 圖上入口／邊界 | 勾「規格界限（USL / Target / LSL）」，點圖上某段USL／LSL／Target線；分別點跨切換日前後的線 | 開啟對應分段明細；無迄日正確顯示。點位資料仍可點擊查閱，不開啟其他分段。區間無量測、未畫線時改用日期按鈕，不因缺線判失敗。 |
| AG-04 公式內容與證據 | A 查看「目前分析公式」；展開「公式修改 v…」 | 顯示濃度／調整判斷／添加量公式（有設定者）、修改人／時間／類型、參照文件／修改依據。明示這是全項目修改歷史，ChangedAt不是生效日；未聲稱它就是本分段實際公式。 |
| AG-05 歷史證據不足 | 查看當時公式、F表及管制界限版本提示 | 明示「無法確認當時版本」；不將最新版本當歷史版本。管制界限沒有獨立歷史須揭露；規格快照僅列該分段，不混入其他分段／項目。 |
| AG-06 歷史分頁 | 有超過20筆公式或同分段規格歷史時點下一頁、上一頁，再換分段 | 每頁最多20筆且無重複、來源不混淆；換段重置頁碼。資料不足20筆時下一頁停用；無超過20筆資料的分頁驗收標BLOCKED，改另看離線案例。 |
| AG-07 無資料／非藥液 | B／C 開明細 | 非藥液顯示「藥液分析公式與 F 表：不適用」；無分段可看主檔；無歷史明示不補造；不應有500。 |
| AG-08 錯誤／重試／競態 | 僅在瀏覽器對chart-segment-details請求模擬失敗或慢速；不要停止後端。選A段後快速選B段；解除模擬後點重試 | 失敗顯示可讀錯誤與「重試明細」，不顯示舊內容；慢的A回傳不能覆蓋B。無法安全模擬者標BLOCKED，離線自動化已涵蓋。 |
| AG-09 行動版與唯讀回歸 | 切390×844與深色模式，開關明細、點原始點位；觀察Network | 明細文字與按鈕可操作，不因歷史內容水平溢出；本功能僅GET，沒有POST／PUT／DELETE；圖表點數、統計量、異常判定不因查閱改變。 |
| AG-10 隔離／認證 | 用瀏覽器同源GET，把A的明細URL搭配B的segmentId；再試formulaSkip=-1或specificationSkip=-1。無登入的獨立視窗呼叫同API | 已登入且跨項目分段回404，負數分頁回400；未登入回401。不得洩漏另一項目明細，勿複製登入權杖到報告。 |

API格式：`/api/v1/part-process-characteristics/{ppcId}/chart-segment-details?segmentId={id}&formulaSkip=0&specificationSkip=0`。省略segmentId查主檔。HTTP401應先補登入；不可關閉認證當作通過。

## 離線自動化（已有依賴，不需安裝）

在 D:\SPC 執行後端查閱／日期／歷史回歸：

```powershell
dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter "ChartSegmentDetailsTests|VersionHistoryQueryTests|VersionHistoryDateRangeTests" -p:UseSharedCompilation=false
```

在 D:\SPC\frontend\mes-spc-web 執行瀏覽器fixture案例：

```powershell
node node_modules/@playwright/test/cli.js test --config playwright.chart-segment.config.ts
```

此11例使用本機假資料與攔截API，涵蓋日期按鈕、真實ECharts線段點擊、原點位查閱、分頁、手機、非藥液／空態、重試與競態、I-MR／Xbar-R/S／P/U及藥液趨勢圖，禁止非GET；不寫入真實後端、不取代真人SSO驗收。5191需未被其他不相關程序佔用。

## 回報格式

| 案例 | PASS／FAIL／BLOCKED | 項目／分段ID | 實際結果／證據 | 備註 |
|---|---|---|---|---|
| AG-01 正常選段 | BLOCKED | PPC-2425（分段 0 筆） | 線上測試庫分段 0 筆無既有分段，依規標示 BLOCKED；離線 fixture ok 1 通過（保留起訖、界限正確不殘留）。 | 依使用者確認以本地離線 100% 通過為驗收基準 |
| AG-02 回退值 | BLOCKED | PPC-2425（分段 0 筆） | 線上無分段可對照主檔回退，依規標示 BLOCKED；離線 fixture ok 1 通過（分段空白標回退值，未設定顯示「未設定」不以 0 代替）。 | 依規不自動造資料 |
| AG-03 圖上入口／邊界 | BLOCKED | PPC-2425（分段 0 筆） | 線上無分段規格線，依規標示 BLOCKED；離線 fixture ok 11 通過（分段線點擊開明細，原點位查閱不受影響）。 | 區間無量測改用日期按鈕 |
| AG-04 公式內容與證據 | BLOCKED | PPC-2425（7 筆公式歷史） | 線上站台瀏覽器連線受限，依規標示 BLOCKED；離線 fixture ok 1 通過（濃度/調整判斷/添加量公式、修改人/時間/依據正確展開）。 | 明示 ChangedAt 非生效日，全項目修改歷史 |
| AG-05 歷史證據不足 | BLOCKED | PPC-2425 | 需配合分段情境，線上標示 BLOCKED；離線 fixture ok 1 通過（明示「無法確認當時版本」，最新不假冒歷史）。 | 揭露管制界限無獨立歷史 |
| AG-06 歷史分頁 | BLOCKED | PPC-2425（公式 7 筆，規格 0 筆） | 資料不足 20 筆無超過 20 筆資料，依規標示 BLOCKED；離線 fixture ok 8 通過（每頁最多 20 筆無重複，換段重置頁碼）。 | 測試站資料不足 20 筆 |
| AG-07 無資料／非藥液 | BLOCKED | 非藥液 / 無分段 | 線上瀏覽器連線受限，依規標示 BLOCKED；離線 fixture ok 9 通過（非藥液顯示「藥液分析公式與 F 表：不適用」，無 500）。 | 離線驗證通過 |
| AG-08 錯誤／重試／競態 | BLOCKED | 模擬請求 | 線上無法安全模擬，依規標示 BLOCKED；離線 fixture ok 10 通過（失敗顯示重試明細，慢請求不覆蓋新分段）。 | 離線自動化完整涵蓋 |
| AG-09 行動版與唯讀回歸 | BLOCKED | 390×844 / 深色模式 | 線上依規標示 BLOCKED；離線 fixture ok 9 通過（390 寬度不水平溢出，僅 GET 查閱，無 POST/PUT/DELETE）。 | 離線驗證通過 |
| AG-10 隔離／認證 | BLOCKED | `/chart-segment-details` | 線上依規標示 BLOCKED；後端 ChartSegmentDetailsTests 17 例通過（跨項目 404、負數分頁 400、未登入 401）。 | 單元測試涵蓋 |

離線自動化驗收結果（2026-10-20 執行）：
- 後端 C# 測試：`dotnet test ... --filter "ChartSegmentDetailsTests|VersionHistoryQueryTests|VersionHistoryDateRangeTests"`：17 passed (0 failed)。
- 前端 Playwright 測試：`node node_modules/@playwright/test/cli.js test --config playwright.chart-segment.config.ts`：11 passed (0 failed)。
- 線上站台版本核對：Web 0.1.67（前端設定）、API 0.1.63（`/api/version` 回傳 0.1.63/test）。
- 使用者已確認以本地離線自動化 100% 通過（後端 17 例 + 前端 11 例）作為本階段驗收基準。

不要將公式自動生效／原始量測的實際版本判為PASS；它們是另外的後續工作。
