# I18N-TASK-05D 驗證、發布與回復

## 實作與限制
SPC既有/measurements量測管理補登/修改頁、工站綁定及Soft Hold固定UI三語，新增80鍵/240列draft，總計875鍵×3語。Portal量測入口未改。
原主檔/警報/錯誤文字、單位、批號/工單/序號/作業員原值、數字parse及樣本順序、ISO與本地時間算法、最新批次查詢時區offset、InProgress處置狀態與綁定localStorage key/value保持。Soft Hold仍無略過/關閉按鈕，不因語言切換解除；失敗重送仍依既有回圈重送各警報（本次未改重試流程）。
兩視窗LocalePicker復用App原handler；本機綁定名稱缺值才翻譯，API主檔名稱不翻譯。預設繁中，真人審稿留TASK20，draft/hash檢查不代表真人語文核准。
所有瀏覽器API讀寫mock攔截，假token不送真API；不宣稱真SSO/真資料庫/真警報處置驗收。

## BDD對應與結果
| AC | 自動化驗證 |
|---|---|
| AC1/AC6 | 三語×1920/1366/1280/390主頁12情境，工站首綁及Soft Hold三語手機操作；Viewer保持原route guard，空主檔/載入錯誤可辨。首輪手機作業員input被圖示/側欄壓縮阻擋，僅/measurements沿用既有stackedMobileNav，圖示pointer-events-none/綁定與樣本標頭換行。390英文主頁/Soft Hold截圖檢視。 |
| AC2 | 三scope×三語，數字0/工單/序號/作業員/日期/DOM/URL保留，切語言無額外讀寫；重新確認同工站三語草稿DOM與localStorage值保持。 |
| AC3 | 三scope×三語POST逐欄比較，原帶空白作業員/序號保持，工單trim行為不變，數值0不視為空值，批號MANUAL格式；空工單/序號null、lotNo回批號；空樣本/作業員阻擋，Enter逐欄聚焦末欄送出、LSL/USL相等不OOS而0.199超界。 |
| AC4 | 三語latest query ppcId=1/date=2026-10-24/timezoneOffsetMinutes=480；測試瀏覽器固定Asia/Taipei，ISO04:34回填本地12:34後PUT batch73，回傳樣本順序2/1/3保持，0與原追溯欄位不變。 |
| AC5 | 三語卡控保留原alert message/type/value，切語言不消失；缺對策無寫入，第二警報API409仍留視窗與草稿；逐筆成功後才解除並reset、保留作業員。Workflow payload保持InProgress/rootCause/correctiveAction/responsibleUser原字串；失敗/重送ID順序91/92/91/92保持。 |

## 執行紀錄
- 實作前模板固定中文紅燈；54單元通過（原契約/字典覆蓋/參數/hash及模板完整編譯、安全文字插值）。
- build:test通過，875鍵×3語通過；既有Vite大chunk提醒，未擴張拆包重構。
- 最終本機35項＋追加同工站綁定3項＝38個不同情境通過；共用17項通過。去除新增行尾空白後重build，產物hash保持相同，未重跑無必要功能測試。
- 發布後完整38＋共用17全部通過。
- 真匿名/api/version與/api/health回HTTP200/environment=test/API0.1.72/status=ok，同源14請求通過，index/兩JS/CSS逐檔SHA256與dist吻合。

## 發布與回復
已只更新SpcSingleTest Web0.1.78/API沿用0.1.72；核IIS vdir根D:/SPC/release/test/backend與AppEnvironment=test，備份測試frontend47檔/4檔payload/4個保留設定逐項驗hash。
目標D:/SPC/release/test/frontend，先assets後index、保留舊assets及web.config/appsettings，不重啟pool；無DB/API/schema/正式/其他系統更新。
Rollback先回 .codex-tmp/i18n-task05d/backup-frontend 備份資產，再回index，核hash/靜態URL/health/version；不刪舊資產或環境設定。備份/測試結果/截圖不提交。

## 工作池
完成後剩17件國際化小工作（05E＋06～21），另掃毒受阻1、Runtime/TransFiles人工故障驗收暫緩2。STOP於05D，下一項05E匯入批次/預覽UI未開始；不恢復網頁匯入。
