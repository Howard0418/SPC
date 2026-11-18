# PORTAL-SPC-CHEM-EXCLUSION-1118-TASK-001 驗證

## 自動化驗證

- SPC API：排除保存、原因驗證、取消排除、隱藏點位不回傳至圖表、既有可見排除計算及常態檢定回歸共 37 項通過。
- Portal Web：錄入固定選項、送出欄位、必填驗證及歷史查詢標示契約共 4 項通過。
- SPC Web：測試站模式建置成功；化學單線圖 9 項回歸通過。完整 Node 測試 193 項中 191 項通過，另 2 項既有 i18n 完整性失敗（`常態檢定方法` 固定中文及既有翻譯審查中繼資料差異），與本次排除功能修改無關。
- SPC API Release build：0 警告、0 錯誤。

## 資料與回復

- 共用既有 `SpcPointExclusions`，不新增 migration。
- 取消勾選會將該量測的有效排除紀錄停用；若需整批回復，可停用本功能建立的有效排除紀錄並回復前版網站。

## 待驗收

- 測試區以任一藥液項目勾選「不列入計算及繪圖」並選擇備註後儲存，確認重載及查詢可見、SPC 圖表不繪製且樣本數不含該筆。
- 取消勾選再儲存，確認點位恢復納入計算與繪圖。

## 測試區發布

- SPC：API 0.1.136 → 0.1.137；Web 0.1.142 → 0.1.143。
- Portal：API 1.0.263 → 1.0.265；Web 1.0.301 → 1.0.303（含發布腳本自動遞增）。
- SPC `/api/health`、`/api/version` 均回 HTTP 200、版本 0.1.137、環境 `test`；首頁與實際 JS／CSS 皆 HTTP 200 且 MIME 正確。
- Portal首頁／登入、`app-version`、測試 API health、同源 `/api/auth/me` 路徑、實際 JS／CSS 均可達；版本 Web 1.0.303、API DLL 1.0.265，資料庫環境為 `test`。
- SPC 測試區發布前備份：`D:\SPC\release\backup-chemical-exclusion-test-20261118-202851`。
- 正式區未發布。

## 進度表同步

- 原活頁簿「小工作總表」第 317 列更新為「測試區已發布／待人工驗收」，目前待辦維持「是」；「專案進度表」新增 2026-11-18 原生里程碑。
- 更新前備份：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\Voice\備份\進度表-20261118-chemical-exclusion-before.xlsx`。
- 原檔更新前 SHA256 `242B4A38373A3DA995EDE8E3C90696A042583647474669421F30BC8EDB93DC81`；更新後 SHA256 `A2067D9017DD9FE8626A265EDBECC95F177537C888BACD42EBAC4D12CC453B98`。
- Artifact Tool 重開驗證工作列、151 個原生圖形及新里程碑；公式錯誤掃描 0 筆，ZIP 封裝 13 entries／空名稱 0；已從原路徑重讀。Microsoft Excel 人工開啟未執行。
