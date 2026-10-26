# 07C 驗證與發布
- 2026-10-26 DONE。199新鍵／597列draft、累計1806鍵三語；校驗主檔／登記／歷史／設定／匯入固定UI、驗證及成功提示三語，五種視窗沿用LocalePicker原handler。
- AC1/AC5：列表三語四尺寸12、四模態三語兩尺寸24、三語編輯失敗／重送3、匯入勾選與草稿3、提醒邊界3，共45個不同情境本機分批／發布45通過。語言切換保留DOM／草稿／篩選／raw值／勾選與API次數，英文手機四模態截圖檢視。
- AC2/AC3：86單元（模板含動態文字／屬性、ledger hash／參數、raw與0、status／到期日期邊界）通過；Active/InCalibration/Inactive/Retired及未知raw狀態、今日／前日／30日內末日／31日後、缺日期保持原class與判定。提醒366阻擋、365加入及0保留，原遞減排序365/30/7/0；錯誤隨locale更新，409保留日期草稿。初版測試期待插入順序，依原排序修正後三語重驗通過，未改排序邏輯。
- AC4：編輯lastCalibrationDate維持字串、nextCalibrationDate不重算、原version/reason/recipient/includeCustodian與外校／Active保持；既有日期／欄位／100筆捲動6＋匯入／提醒／Email與Chat 8共14回歸通過。匯入FormData/hash/selectedRows與錯誤raw警示沿用原流程；本批未真匯入、寄信、下載正式憑證或異動資料，certificate端點與FormData原碼比對未改。
- build:test通過、1806鍵×3，既有chunk大小提示保留；無未處理JS錯誤，業務API全部攔截。
- 只SpcSingleTest Web0.1.88/API0.1.72；IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對，完整前端備份74檔、payload4／保護設定4、不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；發布四檔SHA256等於dist，14同源API請求通過。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07c/backup-frontend 備份，資產先index最後，保留web.config/appsettings；DB／後端／正式未改。
- draft真人審稿留TASK-20。剩16件接續07D；另受阻1／暫緩2。
