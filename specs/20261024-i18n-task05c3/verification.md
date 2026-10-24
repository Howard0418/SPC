# I18N-TASK-05C3 驗證、發布與回復

## 實作與限制
管制項目主清單/所有編輯頁籤、分段歷史試算、SpecificationLimitHistory與ChemicalFormulaSchedule固定介面三語，新增300鍵/900列draft，總計795鍵×3語。
兩抽屜內的LocalePicker沿用App同一handler，不增加語言API；公式選項以原JSON id作穩定key，避免切語言重建按鈕。scope原label與typed永久清除比對保持；只顯示層翻譯缺省scope名，資料庫群組名稱不翻譯。
公式/JSON（含原縮排與額外欄位）、單位、識別ID、台北日期/排程query、理由與sourceKey、數值精度、WE規則、比較與權限保持。更正舊「切換常態檢定方法」說明為固定Anderson-Darling，未動AD或公式引擎。
所有瀏覽器API讀寫被mock攔截，假token不送真API；含DELETE/purge、排程發布均非真資料驗收，不宣稱真SSO/真DB寫入。真人三語審稿留TASK20，hash不代表語意已核准。

## BDD對應
| AC | 驗證 |
|---|---|
| AC1/AC7 | 主清單/編輯器三語×1920/1366/1280/390十二情境；手機搜尋欄58px紅燈後僅此路徑沿用既有堆疊導覽。LSL可輸入/Save在視窗內；另三語手機排程與分段可捲到發布/儲存、容器限寬。390英文截圖檢視。 |
| AC2/AC3 | 三種scope×三語（9情境），LSL改0.2、DOM/頁籤/URL與草稿不重建、不額外讀寫；完整DTO逐欄比較，保留原custom JSON/滴定名稱/選定WE code。產品新增、反向USL/LSL阻擋且錯誤切語言；409原API文字及草稿保留。 |
| AC4 | 三語歷史試算回0.38/0.3/0.22，日期/樣本數格式保持，帶入分段/新增/編輯/刪除payload逐欄比較；規格歷史顯示、日期查詢由舊9項回歸涵蓋。 |
| AC5 | 三語排程空原因/反向日期阻擋、衝突保留草稿；發布revision9/原reason/空迄日null、預覽原Primary字串0.2000/Secondary null/ISO/sourceKey保持，改輸入清預覽；公式回復原reason及版本ID不變。舊排程7項涵蓋Viewer唯讀/台北含迄日/載入retry。 |
| AC6 | 三語一般刪除取消/確認及原ID；purge已結案阻擋、typed不符取消、原完整識別正確才送purge=true；Viewer原route guard/空態保持。模式轉換確認取消不寫入，確認後只依既有行為清chartType/JSON/管制線，原量測相關規格欄保持。 |

## 執行結果
- 修改前3模板固定中文紅燈，實作後51單元全部通過，含字典全覆蓋/參數/hash/模板完整編譯及文字參數不被二次解析。
- build:test通過（795鍵×3語）；既有Vite大chunk提醒，未做非本範圍拆包。
- 本機新增情境：第一輪33/34通過；purge測試需等待delete-impact GET後的對話框，修正測試時序後該項及新增6項通過。合計40個不同情境通過（33＋7），不冒稱同一輪40。
- 舊chemical-formula-history 9項、chemical-formula-schedule 7項全部通過。
- 共用介面17項通過。已發布測試站完整40項＋共用17項通過。

- 實際匿名/api/version、/api/health回HTTP200，environment=test/API0.1.72/health status=ok；同源14請求通過；index/兩JS/CSS逐檔SHA256與dist吻合。

## 發布及回復
已只更新SpcSingleTest：Web0.1.77/API沿用0.1.72；核IIS vdir根D:/SPC/release/test/backend及AppEnvironment=test。備份測試frontend共44檔並驗hash，4檔payload及4個保留設定均核對SHA256；目標D:/SPC/release/test/frontend，先assets後index，保留舊assets/web.config/appsettings，不重啟pool；無DB/API/正式及其他系統更新。
Rollback：將本次 .codex-tmp/i18n-task05c3/backup-frontend 的備份資產先回復、index最後回復，核對hash/靜態URL/health/version；不刪舊資產或環境設定。測試結果、備份與截圖不提交。

## 工作池
完成後剩18件國際化小工作（05D/05E＋06～21），另掃毒受阻1、Runtime/TransFiles人工故障驗收暫緩2。STOP於05C3，下一項05D量測輸入UI未開始。
