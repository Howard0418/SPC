# 功能規格：I18N-TASK-05C2 線別分析／F 表／公式歷程三語
- 版本：1；日期：2026-10-24；狀態：完成
- 授權：使用者「開始下一項」，依[主任務](../20261022-i18n-task01/tasks.md)執行單一小工作05C2。
- 主系統：SPC；相依：無跨系統調整；資料庫：無schema或資料調整；發布：僅SPC測試前端。
- 基準：[國際化契約](../../docs/i18n/contract.md)、[公式參照歷史](../20261019-chemical-formula-reference-history/spec.md)、[修改日期篩選](../20261019-version-history-date-filter/spec.md)。

## 目的與範圍
公開線別分析總覽、F表及唯讀ChemicalFormulaHistory補繁中/簡中/英文固定UI；共用VersionHistoryRange的日期篩選/錯誤/分頁同步三語，涵蓋其其他使用處之相同固定文案。
資料名稱、原公式及JSON、F!地址、預設標準液/版本名稱、changeType、儲存回復reason、日期/台北日界/skip/take、DTO及API原錯誤保持。總覽合成「未設定／未命名」已為既有篩選/匯出資料值，不隨語言改寫。Excel欄名/工作表/檔名維持原協定。無計算引擎、權限、後端、路由及發布正式調整。
語言切換不得重新載入資料、清除未存草稿、影響跨頁批次dirty rows或發出寫入。

## 需求與 BDD
| 需求 | 驗收 | Given / When / Then |
|---|---|---|
| R1 | AC1 | Given 兩公開頁及公式歷史，When 切三語，Then 固定標籤/訊息切換，資料/公式/日期原值不變。 |
| R2 | AC2 | Given 總覽跨頁篩選/未存公式/版本原因與F表草稿/試算/歷史日期，When 切語言，Then DOM/選取/草稿/查詢保持、無額外讀寫。 |
| R3 | AC3 | Given 批次異動，When 確認儲存且部分API失敗，Then 既有DTO/trim/小數0～8及成功失敗數保持，失敗草稿可再送，原因500字限制保持。 |
| R4 | AC4 | Given F表，When Dry-run/套用/試算/影響查詢/回復，Then apply旗標、地址/數值/原公式/Primary及原reason維持；翻譯只作用顯示。 |
| R5 | AC5 | Given 歷史日期，When 反向日期/單邊查詢/下一頁/清除/切語言，Then 反向不送出、日期原值與take21/skip20保持；API空態與錯誤可辨。 |
| R6 | AC6 | Given 390/1280/1366/1920尺寸及Viewer，When 三語操作，Then 控制項可操作、表格可橫捲，既有權限維持。 |

## 邊界與測試規劃
Happy Path：總覽批次儲存、F表試算/套用/歷史；Boundary：跨頁dirty、0/8小數、500字、缺值/空態/404；Invalid：日期反向、501字、409/500錯誤；Regression：XLSX原協定、角色、查詢DTO、共用日期元件其他使用處。
必要自動化：字典/模板/參數與hash、瀏覽器三語狀態/請求/下載、build、發布後同源/health/hash；不以mock冒稱真DB寫入或真人語文驗收。
若手機主區域被既有側欄壓縮，只於這兩路徑沿用既有stackedMobileNav，保持其他頁面配置。

## 回復與未決
僅測試frontend發布前核IIS目標及test環境、備份hash；先assets後index，保留環境設定。Rollback先回備份資產再index，API/DB無變更。無阻擋未決；翻譯draft留TASK20真人審稿。
