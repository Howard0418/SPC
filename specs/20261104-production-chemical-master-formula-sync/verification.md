# 驗證紀錄

- 狀態：正式資料同步與自動驗證完成；正式畫面人工驗收待使用者重新整理確認。
- 授權：2026-11-04 使用者明示「授權同步87項測試藥液公式至正式主檔」。
- 正式目標：`172.16.110.16 / PMR_SPC_2026`；來源：同伺服器`PMR_SPC_TEST`。API `/health`、`/version`成功且環境為`production`。

## 計畫與保護範圍

- 測試庫包含大量`TEST-*`隔離項目，因此沒有按整庫集合匯入；以正式PPC ID鎖定兩區共同且業務鍵相同的113項，正式獨有1項與測試專用項目均排除。
- 113項中87項初驗或複驗濃度公式不同，26項已相同；只合併`concentrationFormula`與`recheckConcentrationFormula`。
- 87項初驗／複驗合計174次以正式啟用F表試算通過，缺F或公式錯誤會在寫入前拒絕。

## 正式執行

- 2026-11-04 21:09:35建立`PMR_SPC_2026_pre_master_formula_sync_20261104-210935.bak`，使用`COPY_ONLY,CHECKSUM`且`RESTORE VERIFYONLY WITH CHECKSUM`成功。
- Serializable交易更新87項目前主檔並新增87筆不可變公式版本；正式公式與測試逐字一致，包含`F!$B$3`參照。
- 寫入後剩餘差異0、113項全相同；相同來源再驗證新增版本0。
- 前後保護指紋相同：VariableMeasurements `14993:200031266`、Receipt `358:1440155509`、Trace `358:1844749889`、啟用F與儲存格、80排程修訂／1039生效區間、2個管制界限分段均未改。

## 自動化驗證

- `ProductionChemicalMasterFormulaSyncPlannerTests`：9通過，涵蓋只換兩式、刪除空複驗、相同略過、鍵／環境／JSON拒絕。
- 公式版本服務合併執行：相關19通過。
- 工具build成功；正式套用輸出：`APPLIED: formulas=87; remaining=0; versions+=87; calculations=174`。
- 獨立重驗輸出：`VERIFIED: formulas=0; remaining=0; versions+=0`。

## 發布與待驗收

- 網站發布：不適用；沒有修改SPC Web/API程式或站台檔案。
- 正式API匿名查詢正確回401；Windows Computer Use native pipe仍不可用，沒有繞過登入或宣稱畫面通過。請重新整理「線別分析項目總覽」，確認C5清潔硫酸、C5酸浸硫酸、DP清潔PARRARINSE顯示`F!$B$3`；瀏覽器人工畫面尚未核銷。
- 回復：可由已驗證備份還原，或依本次87筆版本歷程逐項回復前版；不得重算／覆寫既有量測。

## 進度表

- 原檔已先備份為`進度表_正式主檔87項公式同步前_20261104-211207.xlsx`，SHA256與更新前原檔同為`E6FA7DCA...F0C230`。
- 已更新原檔父工作第265列、11/04實際狀態與第62個原生里程碑；核心45／涉及SPC22、完成189、人工11／核銷4保持。
- 取代後SHA256為`6774CEE3...76F0E`；15個指定儲存格外的封裝part保持、原61個圖形保留、OpenXML驗證0錯誤、artifact重開公式錯誤0，LibreOffice成功轉出PDF。Microsoft Excel人工開啟未執行。
