# 06B1 驗證與發布
- 日期：2026-10-26；狀態：DONE（本件程式與測試站完成；真人譯文審稿留TASK20）。
- 固定UI／title／查詢與下載錯誤70新鍵／210列draft，累計1138鍵三語；僅Chart Display Box前固定UI及相關讀取錯誤，圖內／總覽表／OCAP留後續，不標全頁完成。

## BDD 對應
| 驗收 | 實際結果 |
|---|---|
| AC1／AC5 | spc與週月報×三語×四尺寸24情境，別名能力／異常分析英文頁首；DOM／欄位草稿／URL／ALL與原群組名稱保持，切語言無新增API。手機欄位可達，44px紅燈後沿用三路徑堆疊，截圖英文週期截字後改手機全寬、查詢另列。 |
| AC2 | 三語93天允許／94天阻擋／日期倒置、單線21與ALL、PRODUCT／PART固定ID31及ALL不帶日期、缺料號阻擋；2024-02閏月與2026-W01跨年原日期及comparisonPeriod保持；0規格／能力取位原樣。31天上限邏輯未改，週月選項自動產生合法期間，不繞過UI強造非法期間。 |
| AC3 | Summary503與Chart500原API文字、Chart404固定提示與切語言、排除清單503、下載500固定前綴、空總覽；主檔HTML/i18n外觀原文純文字。下載錯誤原本blob錯誤解讀規則保持，不宣稱新增blob錯誤JSON解析。 |
| AC4 | 三語放大後原canvas DOM保留（renderECharts原本會dispose重建，語言切換未重建）、CSV bytes／原檔.csv與chart query含AD／日期／批次逐欄相同；快取總覽返回不重新讀取。排除狀態只顯示映射，恢復仍DELETE /point-exclusions/91、空body並重讀圖表，不改寫入流程。 |

## 執行結果
- 實作前模板固定中文字紅燈；`npm run test:i18n` 65 passed（來源／譯文hash、完整key／參數／protected術語、模板完整編譯及既有回歸）。
- `npm run build:test` 通過；1138鍵×3，無套件增修，既有Vite大chunk提示保留。
- 本機最終37項全通過＋追加404／讀取／下載錯誤1項＝38不同情境通過；原分段／點位回歸17與shell17 passed。
- 發布後新38＋shell17＝55 passed，無未處理JS錯誤。真實業務API均攔截mock；未寫真DB。
- 同源檢查14請求通過；真匿名 /api/version 與 /api/health HTTP200、test／API0.1.72／status=ok。
- index／CSS／主JS／guideJS共4檔實際SHA256與dist一致；390英文查詢與週月報截圖檢視通過。

## 發布與回復
- 測試站 <https://SPCTEST.pmr.com.tw> Web0.1.81/API沿用0.1.72；正式、DB、後端、Portal、TransFiles未改。
- appcmd核SpcSingleTest/實際backend `D:\SPC\release\test\backend`、AppEnvironment=test；frontend `D:\SPC\release\test\frontend`。
- 備份54檔逐一驗SHA256，4個payload資產先index最後，保留舊資產與4份受保護設定，不重啟pool。
- 回復：重新核對上述目錄與test環境，從 `D:\SPC\.codex-tmp\i18n-task06b1\backup-frontend` 覆回原資產及index（資產先index最後），不覆web.config/appsettings、不刪其他資產；重驗原hash／健康。未實際觸發rollback。
- 本機部署摘要／截圖等不提交。

## 限制與停止點
瀏覽器業務讀寫全mock，真站只有匿名健康／靜態唯讀，不等於真DB／SSO／真人譯文驗收。原AD／數列／CSV協定／日期算法與權限保持；舊總覽表、圖內標籤及點位寫入表單仍原語言，B2／B3另做。
B拆三批後剩B2／B3／C／D及07～21共19件；另受阻1／暫緩2。STOP於06B1，不開始06B2。
