# 06A 驗證
- 日期：2026-10-26；狀態：DONE（06A程式及測試站完成，真人譯文審核留TASK20）。
- 兩共用元件104新鍵／312列draft，累計1068鍵三語。原始來源／JSON／公式／enum／數值／日期／讀取query／分頁與generation保護保持；本次無計算、DB、後端、權限或正式站變更。

## BDD 與結果
| 驗收 | 實際驗證 |
|---|---|
| AC1／AC5 | 兩公開圖表入口×三語×四尺寸24情境：分段、公式證據、aria、DOM、已展開details／URL保持，切語言無額外讀寫；手機明細無水平溢出、關閉按鈕可達。390英文截圖檢視。 |
| AC2 | 0／null／無迄日／空歷史／非藥液／未知舊量測／不完整證據；三語日期固定台北2026/10/19 12:00:00；舊回歸確認子組不代表第一筆、點位無識別不推定。 |
| AC3 | raw含HTML/i18n符號純文字、JSON公式不解析提示、格式不符固定錯誤切語言即時更新、API原文保留與重試；舊分段清單及明細失敗重試回歸通過。 |
| AC4 | 三語公式／規格偏移20；量測501歷程traceSkip20、不夾帶segmentId、返回segmentId11與0偏移、切主檔重置。既有慢請求、換分段、規格線點選及原點位鑽取回歸通過；全部GET，不重算／寫入。 |

## 執行
- 翻譯前兩模板固定中文／aria紅燈，實作後通過。`npm run test:i18n`：62 passed，含字典完整key／參數／來源hash／protected token與模板編譯。
- `npm run build:test` 通過；1068鍵×3語，無套件增修，既有Vite chunk大小提示保留。
- 新瀏覽器本機最終28項＋修正測試深連結條件後單獨1項＝29不同情境通過；原分段／證據／點位回歸17 passed；共用shell17 passed。
- 手機原側欄將分段明細壓至74px，內容93px溢出紅燈；僅spc/trend-chart沿用既有手機堆疊後三語四尺寸通過。未改兩主頁其餘文案或圖表演算法。
- 發布後新29＋shell17＝46 passed，無未處理頁面JS錯誤。
- 同源檢查14請求通過；真匿名 `/api/version`／`/api/health` HTTP200，environment=test、API0.1.72、status=ok。
- 真實index/CSS/主JS/guideJS共4檔SHA256與dist一致。

## 發布與回復
- 只更新 <https://SPCTEST.pmr.com.tw> Web0.1.80，API沿用0.1.72；正式、Portal、TransFiles未更新。
- appcmd核對 `SpcSingleTest/` 實際backend `D:\SPC\release\test\backend` 與AppEnvironment=test；前端目標 `D:\SPC\release\test\frontend`。
- 發布前備份52檔逐一比對SHA256；4個payload先資產後index，保留舊資產及4份受保護設定，不重啟pool。
- 備份 `D:\SPC\.codex-tmp\i18n-task06a\backup-frontend`；回復先重核測試環境與目錄，再覆回備份資產與原index（資產先、index最後），不覆web.config/appsettings、不刪其他檔案；重核原hash及健康。未實際觸發rollback。
- 截圖／部署摘要等只留本機 `.codex-tmp/i18n-task06a`，不提交。

## 限制／停止點
瀏覽器業務API全mock、假token不送真API；真站僅健康與靜態唯讀。不冒稱真DB/SSO/真人語文或正式驗收。原固定AD／偏態峰態移除規則、公式及圖表數值未變。譯文仍draft／uncertain待TASK20。
TASK06拆四批後，06A完成，剩06B～D與07～21共18件；另附件掃毒受阻1、Runtime與TransFiles人工故障驗收暫緩2。STOP於06A，不開始06B。
