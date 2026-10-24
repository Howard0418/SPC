# I18N-TASK-05C2 驗證與發布

## 實作與基準
公開線別分析總覽/F表、共用公式歷程及VersionHistoryRange固定UI三語；新增105鍵/315列draft，累計495鍵×3語。
原公式/JSON、業務名稱（含「未設定／未命名」既有篩選資料值）、F!儲存格、標準液、F表預設版本名稱「F 表新版」、原回復reason、changeType、日期/台北日界、API DTO及XLSX欄名/工作表/檔名不變。
F表套用/回復的成功提示仍沿用原load()清除行為，本次不改流程；Dry-run與總覽批次結果可即時切語言。讀寫全部mock攔截，假token不送真API，不宣稱真SSO或真資料庫寫入驗收。真人譯文審稿留TASK20，hash通過不代表文意已核准。

## BDD 驗收
| 驗收 | 自動化/證據與結果 |
|---|---|
| AC1 | 兩頁×三語×1920/1366/1280/390，24情境；原名與公式不被解析為HTML，模板/字典單元檢查。 |
| AC2 | 三語各驗總覽跨頁dirty rows、line/status/pageSize/原因保留，DOM仍連接/URL不變/無額外讀寫；F表未存code/name/value/formula/Primary/地址保留；歷史日期/頁碼及快照保留。 |
| AC3 | 精確比較所有PPC payload欄位，跨頁兩筆同批送出，trim公式及小數0/8、原config其他欄保留；成功/失敗各一後失敗草稿/原因保留再送；501字阻擋/500字可確認取消、reset與12筆預覽截8筆＋另4筆。 |
| AC4 | F表apply=false/true、evaluate expression與Primary=0.2、impact地址、回復原reason及ID逐項比對；404載入8格預設資料與409原文保留。 |
| AC5 | 兩處三語反向日期不送出、單邊fromDate、skip20/take21、clear及空態；API失敗retry；既有9項歷史回歸含設定頁公式/規格歷史。 |
| AC6 | 兩頁Viewer原路由guard、空態與API錯誤；手機原因欄初次52px紅燈，僅兩路徑沿用stackedMobileNav；標準液欄min-width與440px表格可橫捲，F表grid-cols-1/min-w-0限制整頁不被推寬，新增主區scrollWidth不超clientWidth檢查，歷史分頁換行。390英文截圖檢視。 |

## 執行紀錄
- 修改前模板檢查紅燈，固定標籤未翻譯；未改統計/公式引擎。
- 單元46項通過：全字典鍵/參數、draft來源與譯文hash、四模板完整編譯與固定文案、原文字參數安全插值。
- build:test通過，495鍵×3語通過；既有大chunk提醒未列為本次重構。
- 既有chemical-formula-history 9項通過（含共用元件在規格/管制項目頁）。
- 首輪新版情境42通過，2個Viewer測試因initScript每次重設Editor而失敗；修正為獨立Viewer起始fixture，非調整產品權限。
- 最終本機47項＋共用17項通過；已發布測試站47＋共用17項亦通過。

- 實際匿名 /api/version、/api/health：HTTP200、environment=test、version=0.1.72、health status=ok；同源14請求通過，index/兩JS/CSS逐檔SHA256吻合。

## 發布與回復
已僅發布SpcSingleTest Web0.1.76 / API沿用0.1.72，IIS實際後端根D:/SPC/release/test/backend、AppEnvironment=test核對後備份41檔frontend並驗SHA256；4檔payload及4個保留設定均驗hash。目標D:/SPC/release/test/frontend，assets先/index最後，保留舊assets及web.config/appsettings，不重啟pool；無DB/API/Portal/KM/正式發布。
Rollback使用本次 .codex-tmp/i18n-task05c2/backup-frontend，先回復備份資產後index，核對hash/靜態URL/health/version；新增未引用assets可保留。備份/截圖/結果輸出不提交。

## 工作池
完成本件後國際化剩19件（05C3、05D、05E＋06～21）；另掃毒受阻1、Runtime及TransFiles人工故障驗收暫緩2。STOP於05C2，下一項05C3管制項目/規格與排程未開始。
