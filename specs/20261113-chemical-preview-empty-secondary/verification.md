# 驗證紀錄

| 驗收 | 驗證方式 | 結果 |
|---|---|---|
| AC-001 空值正規化 | 新測試先以 `null` 預覽、再以空字串確認 | 修正前精確重現 `chemicalPreviewChanged`；修正後通過並保存 2.47 與一筆 Receipt |
| AC-002 真實差異仍拒絕 | `ChemicalPortalMeasurementTests`、`ChemicalFormulaScheduleTests`及稽核整合焦點回歸 | 20/20 通過；既有日期、輸入與 token 差異拒絕保持 |
| AC-003 Release build | `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj -c Release --no-restore` | 通過，0 warning、0 error |
| AC-004 測試站連線與版本 | 發布後首頁、目前 JS/CSS、health/version、Chrome 直連同源 API、DLL 雜湊 | 通過；API 0.1.130／Web 0.1.129／test |

## 根因與修正

- 2026-11-13 15:09:23～15:09:33 預覽全部回 200，15:09:40 確認立即回 409；15:10:41 重算後 15:10:45 仍立即 409，排除十分鐘逾時。
- Portal 預覽將未提供的第二滴定值正規化成 `null`，批次確認 payload 則保留 `""`。前端 token key 視兩者相同，SPC payload hash 原先視為不同。
- `ChemicalPortalMeasurementService.InputAsync` 現在只把 `null`、空字串及純空白第二滴定值統一為 `null`；有效第二滴定值保持原字串，第一滴定值、日期、班別、取樣階段、公式與規格防護不變。

## 測試站發布

- Release ID：`spc-chemical-preview-empty-secondary-20261113`；來源 commit `c3cf91fdd869903be7d824db487b52a6592832f6`；migration 0。
- `PMR_SPC_TEST` 備份 `PMR_SPC_TEST-spc-chemical-preview-empty-secondary-20261113-9997315425264f4891e08d4aefa80e63.bak` 已通過 CHECKSUM 與 `RESTORE VERIFYONLY`。
- 發布期間暫時授予 `SpcAuditApiTest003` 的 `dbcreator` 已由 finally 撤除；未授予 `sysadmin`，最終為 `PMR_SPC_TEST|SpcAuditApiTest003|0|0|1`。
- 首頁 200/text-html；目前 JS `index-DOTcJAJQ.js` 200/application-javascript；CSS `index-D8u9ukxx.css` 200/text-css。
- health/version 與 Chrome 直接讀取同源 API 均回 `0.1.130 / test / ok`；Windows瀏覽器控制服務不可用，因此未接管使用者既有登入頁面。
- 部署 DLL ProductVersion `0.1.130+c3cf91fdd869903be7d824db487b52a6592832f6`，SHA-256 `648418F68FCB77A94ADB102979D18A578A8FF3EFB4F7167255EEFBB24CA5AAEB`。

## 進度表與界線

- 中央原檔先備份為 `進度表-藥液空白第二滴定值修正前-20261113-152300.xlsx`，再以驗證後候選檔取代；最終 SHA-256 `7CFAE09C4D0B7DF196CC52F983AA734C33EE7B75CC3BEE1E88B3DC90587911A4`。
- Artifact Tool 重讀、公式錯誤掃描、渲染、ZIP 重讀及 OpenXML SDK 驗證通過（0錯誤）；套件 13 parts、繪圖錨點計數 104 前後保持。
- 本件核心實作完成；待使用者重新整理 Portal 後以原資料實際儲存，人工驗收註記不重複計入核心未完成。目前核心未完成 42 件，其中 SPC 相關 19 件。
- 正式站未授權，不執行正式發布。回復程式使用本 release 的網站備份；進度表可由上述備份覆蓋回復。
