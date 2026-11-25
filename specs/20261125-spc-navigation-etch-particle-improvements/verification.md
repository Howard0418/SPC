# 驗證紀錄：SPC 選單權限與 Portal 咬蝕格線貼上

- 工作代號：`SPC-PERM-NAV-ALIGN-1125`
- 日期：2026-11-25（台灣時間）
- 狀態：程式、測試站發布與技術驗證完成；正式站未授權、未發布。

## 驗收結果

| 驗收 | 方法 | 結果 |
| --- | --- | --- |
| AC-001 | 前後端頁面登錄契約、唯一權限碼／路由／三語 key 測試；Editor／Viewer 瀏覽器選單與直接網址測試 | 30 個可點頁面前後端契約一致；有權限才顯示且可進入，未授權直接網址導向第一個已授權頁面。Particle 改用獨立 `analysis.particles`。 |
| AC-002 | 舊版明示權限相容、精確撤權、角色預設、稽核讀取／匯出／證據與自我加權回歸 | 舊 Editor 原本未逐頁守門的頁面保持；既有 `analysis.spc` 保留 Particle 存取。權限下次儲存加入內部 v2 標記，取消勾選不會被補回。稽核權限仍不由角色預設取得且不可自授權。 |
| AC-008 | Web／API 建置、三語、測試站版本與連線 | Web `0.1.193`、API `0.1.177/test`；首頁、JS、CSS、同源及 8901 直連 health/version、前台版本與瀏覽器實際 API 基底均通過。 |

## 自動化與建置

- API 權限及安全稽核焦點：39 通過、1 個隔離 SQL 案例略過。
- 前端頁面登錄：4 通過；含前後端契約一致、動態子路由、角色 fallback。
- Chromium 權限行為：2 通過；涵蓋 Editor 精確頁面、Viewer 角色隔離與 Particle／SPC 分權。
- 三語目錄：2,925 keys × 3 locales 通過；Web testhost build 與 API Release publish 成功。
- 完整 API 套件另為 608 通過、13 失敗、11 略過；13 項均位於未修改的藥液歷史修復／查詢、量測排除或 InMemory `GroupBy` 測試。本工作未宣稱完整套件全綠，焦點權限與安全稽核套件已全數通過。

## 發布與連線

- 發布前：Web `0.1.191`、API `0.1.175/test`。
- 發布後：Web `0.1.193`、API `0.1.177/test`；前台可見兩個新版本。
- `https://spctest.pmr.com.tw/`：HTTP 200。
- 主 JavaScript `index-CmMbFjEK.js`：HTTP 200、`application/javascript`；含新版頁面權限登錄。
- 主 CSS `index-DqQ1aUa2.css`：HTTP 200、`text/css`。
- 同源 `/api/health`、`/api/version` 及 `https://spctest.pmr.com.tw:8901` 直連均成功，版本及 `test` 標籤一致。
- Chromium 從測試 Web 實際呼叫 `https://spctest.pmr.com.tw/api/version`，回 200／`0.1.177`／`test`；畫面同時顯示 Web `0.1.193` 與 API `0.1.177`。

## 發布事件、資料與回復

- 第一次複製時 IIS API 程序未因 `app_offline.htm` 及時釋放 runtime DLL，腳本中止；實際 API 仍為 `0.1.175/test`、設定雜湊未變。修正為明確停止／啟動 `SpcApiTest` 應用程式集區後重送成功。
- 完成版回復備份：`D:\SPC\release\backups\spc-perm-nav-align-1125-20261125-220107`，包含 Web／API；可在停止測試 API 集區後覆回，再核對 `0.1.191`／`0.1.175`。
- 無 schema migration、無批次更新 `PagePermissionsJson`、無正式資料異動。舊權限採執行期相容；只有管理者日後正常儲存該人員權限時，才寫入 v2 精確標記。
- 未發布至 `D:\Sites\PmrPortal`，Portal 與正式 SPC 均未異動。

## `PORTAL-ETCH-PASTE-1125` 驗證結果

| 驗收 | 方法 | 結果 |
| --- | --- | --- |
| AC-003 | Node 純函式測試、Razor 串接契約及咬蝕回歸 | PT1 五 OP 完整 5 × 10、PT2 二十欄、部分矩形、CRLF／LF、0 與空白均通過。貼上只更新當前 A／B side 草稿並呼叫原計算，不自動儲存。 |
| AC-004 | 原子套用、錯誤格定位與邊界測試 | 非數字、負數、非矩形、列／欄超界及 `BEFORE < AFTER` 全部整次拒絕，不回傳部分 updates；錯誤可對應 side／組次／OP／欄位。 |
| AC-008 | 焦點、咬蝕回歸、Release build、測試站 HTTP 與 Chromium 連線驗證 | Node 7 項、Razor 契約 2 項、既有咬蝕 12 項通過；Web Release build 0 警告／0 錯誤。測試 Web `1.0.364`、API `1.0.325/test`，新 JS、主 JS／CSS、同源 API 均通過。 |

### 發布、連線與回復

- 發布前 Web `1.0.363`／API `1.0.324`；使用乾淨隔離工作樹只帶入本工作，再從該專案根目錄執行標準 `scripts/publish-all.ps1`，避免夾帶使用者未提交的其他 Portal 修改。
- 發布後 `https://portaltest.pmr.com.tw/app-version` 為 `1.0.364.0`；已發布 Web／API DLL 分別為 `1.0.364.0`／`1.0.325.0`。API `http://172.16.110.27:8091/health` 為 `ok/test`。
- Login、`/js/etch-amount-paste.js`、`/js/site.js`、`/css/site.css` 皆 HTTP 200 且 MIME 正確；新 JS 與已驗證來源雜湊一致。Chromium 實際同源讀取版本與新 JS 成功，`/api/auth/me` 回 401，證明瀏覽器使用的 API 代理路徑可達且保持匿名拒絕。
- Web／API `appsettings.json` 發布前後 SHA-256 分別保持 `16C553C34A75BADDD6324E41BD18D89443DC84D6A192633DCD8D4900860FADD9`／`F8FCB2FC2200C66CD75EB9EDDCA895AAE14CAF53D4AF4EC4B52BA59429843279`。
- 回復備份：`D:\PmrPortal\.etch-paste-1125\backup-20261125-221836`；可透過 `app_offline.htm` 停用窗口覆回 Web `1.0.363`／API `1.0.324`，並保留現行設定。無 schema、權限或資料庫異動。
- Web 全套為 54 通過、3 失敗；3 項位於未修改的品保導覽／藥液公式契約，目前 `HEAD` 即不符測試，本件不宣稱全套全綠。焦點與咬蝕回歸全數通過。
- 原生電腦控制通道當下不可用；本件已由真實 Chromium 無頭模式完成公開測試站及實際 API 基底驗證，沒有使用個人帳密或寫入日報。

## `SPC-PARTICLE-PASTE-ENTRY-1125` 驗證結果

| 驗收 | 方法 | 結果 |
| --- | --- | --- |
| AC-005 | 前後端頁面登錄契約、`AuthorizationAccess` 權限測試、router guard | 新增 `data.particles`／`/particle-monitoring/entry`／三語「落塵監控資料輸入」；只有啟用 Editor 且明示取得此權限可由網站建立／確認 WebPaste 批次。只有 `analysis.particles` 不具寫入權。 |
| AC-006 | Node parser、Chromium preview／confirm 流程、既有 Particle preview／confirm API 回歸 | 49 欄可展開 48 筆 Long Format；空白略過、0 保留，表頭錯誤、非矩形、欄數、時間、負值／小數整批阻擋。貼上不寫入，preview 後才可 confirm；重複仍採 reject／明示 skip，無 overwrite。 |
| AC-007 | `ParticleUploadServiceTests`、`ParticleQueryServiceTests`、分析頁 Chromium 回歸 | API 白名單、分析篩選及 Location comparison 擴充 R1～R12；R10～R12 可預覽，R1～R9 既有查詢行為保持，C/U 計算器未修改。 |
| AC-008 | Release build、焦點自動化、測試站 HTTP／MIME／版本／環境及 Chromium API 連線 | API 粒子／權限／安全 39 項、前端純邏輯／語系／登錄 10 項、Chromium 2 項通過；Web `0.1.194`、API `0.1.178/test` 已發布。 |

### 發布、連線與回復

- 發布前 Web `0.1.193`／API `0.1.177`；發布後 Web `0.1.194`／API `0.1.178`，前台建置資產含同步版本。
- 首頁 HTTP 200；主 JS `index-DWptEWM4.js` 為 `application/javascript`，主 CSS `index-DFGwgE86.css` 為 `text/css`。
- 同源 `/api/health`、`/api/version` 與 `:8901` 直連均為 `0.1.178/test`、`ok`。真實 Chromium 導覽同源 API 兩端點均回 200／JSON，未繞過 Portal 登入。
- 回復備份：`D:\SPC\release\backups\spc-particle-paste-entry-1125-20261125-224026`；可停止 `SpcApiTest` 後覆回 Web `0.1.193`／API `0.1.177` 並重驗連線。
- 無 schema migration、無既有量測或權限資料更新、未建立驗收量測；TransFiles 原路由及 Bearer 契約保持。正式 SPC／Portal 與 `D:\Sites\PmrPortal` 均未異動。
- 原生桌面瀏覽器控制管線不可用；改由專案既有 Chromium 執行器完成真實測試站唯讀 API 驗證。登入後的人工畫面操作可由取得 `data.particles` 的測試帳號再確認，但不影響本次技術發布結果。

## `SPC-PARTICLE-ENTRY-LAYOUT-1125` 驗證結果

| 驗收 | 方法 | 結果 |
| --- | --- | --- |
| AC-009 | 先紅後綠的 Chromium 版面測試，檢查圖片載入、區塊順序與框線；重跑原貼上、preview 與 confirm 案例 | 修改前找不到配置圖區；修改後配置圖自然寬度大於 0、配置圖區位於貼上區上方且兩區框線非 0，原 49 欄預覽／確認仍通過。2/2 通過。 |
| AC-008 | Particle 語系、三語目錄、testhost build、測試站 HTTP／MIME／版本／環境與 Chromium API 驗證 | Particle 語系 3/3；三語 2,952 keys × 3；build 成功。測試 Web `0.1.195`、API `0.1.178/test`，首頁、主資產、圖片、同源與 8901 直連均成功。 |

### 圖資與版面

- 網站圖資：`frontend/mes-spc-web/public/assets/particle-floor-plan.png`，1,536 × 1,024 PNG，頁面以 `object-contain` 等比例顯示。
- 圖片外框使用 `border-slate-300`；貼上區另有獨立同層級框線，DOM 與畫面順序均為配置圖在前、貼上區在後。
- 影像生成初稿因中文字與原需求不一致未採用；最終圖資經再次校正並人工檢查主要機台標示與 R1～R12 編號後才納入網站。

### 發布、連線與回復

- 發布前：Web `0.1.194`、API `0.1.178/test`；發布後：Web `0.1.195`、API 仍為 `0.1.178/test`。本次無 API 程式變更，版本分列如實記錄。
- `https://spctest.pmr.com.tw/`、主 JavaScript `index-CW0G_6Mx.js`、主 CSS `index-BFh8airl.css` 及 `/assets/particle-floor-plan.png` 均 HTTP 200；MIME 分別為 JavaScript、CSS 及 `image/png`。
- 同源 `/api/health`、`/api/version` 與 `https://spctest.pmr.com.tw:8901` 直連均為 `0.1.178/test`；Chromium 直接載入同源 version／health／圖片均回 200。
- 專案的 `verify:built-api` 本機預設需另行啟動 5196 preview；以實站網址執行時因實站登入守門無法看到模擬 Token 頁面而逾時。本次不把該案例列為通過，改以專案 Chromium 直接驗證實際同源 API 基底；其餘焦點測試與建置均通過。
- 回復備份：`D:\SPC\release\backups\spc-particle-entry-layout-1125-20261125-230231\frontend`。回復時覆回該目錄後，應重新驗證 Web `0.1.194`、API `0.1.178/test`、首頁與主資產；新圖片即使殘留也不會被舊 `index.html`／JS 引用。
- 無 schema、資料、權限或 API 異動；正式 SPC、Portal 與 `D:\Sites\PmrPortal` 均未修改或發布。
