# 驗證紀錄：SPC 選單與頁面權限一致化

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
