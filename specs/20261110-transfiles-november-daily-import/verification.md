# 驗證紀錄

## TASK-001 結果
- 根因：`scheduled_import_config.json` 及預設值仍指向已不存在的舊藥液UNC路徑；已改為使用者確認且實際可存取的 `.../IPQC/藥水分析/藥水分析記錄表/2026年`。
- 規則修正：檔名含「補液位」或「更槽」列為 `excluded`；含「中班」為 `MIDDLE`，其餘為 `MORNING`；N1／N2 sheet 的「（開）／（收）」分別為 `OPEN／CLOSE`，並納入防重鍵。
- 回歸：`test_scheduled_import`/`test_chemical_daily_parser`/`test_etch_reports`/`test_etch_completeness` 共62項通過。
- 2026-11-01～11-10 dry-run：藥液8檔368列（早班365／中班3；OPEN 38／CLOSE 31／GENERAL 299），修正後來源內誤判重複0；補液位／更槽3檔排除。
- 咬蝕量：21份完整日報、1,750個樣本點可進入preview；11/10 QE1因缺 `S4/S14` 隔離，未自動補值。
- 安全界線：mode=`dry-run`、environment=`test`，沒有preview client，API呼叫0、測試／正式DB寫入0、未建立Windows排程。

## 進度表與回復
- 原檔備份：`Voice/備份/進度表-20261110-114349-TRANSFILES-NOV-DAILY-1110.xlsx`，備份與更新前SHA-256一致。
- 原檔已更新並從原路徑重讀；OpenXML 0錯誤、重複Print_Area 0、公式錯誤0、原甘特圖與新里程渲染通過。
- 中央計數：登錄243、核心未完34、完成207；本件是既有TransFiles正式排程計畫的執行切片，後續TASK-002～005不另外重複計數。

## 待辦
1. 修正或確認11/10 QE1的S4/S14後重跑。
2. 測試庫apply、正式補匯及每日09:00啟用分別依後續授權執行。

## TASK-002 測試庫唯讀對帳
- 強制核對實際資料庫為 `PMR_SPC_TEST` 與 `PMR_PORTAL_TEST`，只執行 SELECT；API呼叫0、DB寫入0。
- 藥液368筆全部唯一對應啟用中的CHEM主檔，映射錯誤0；8位小數語意比較後：已存在相同204筆、可新增160筆、同鍵異值4筆。
- 4筆同鍵異值的初驗值均相同，差異為測試庫已有複驗值而目前來源解析結果未帶複驗：11/02 PPC 2440、2459，11/04 PPC 2450，11/05 PPC 2426。未自動覆蓋或新增。
- 咬蝕量21份／1,750點在Portal測試庫均無相同業務鍵，列為可新增；11/10 QE1缺S4/S14仍維持隔離，不計入21份。
- 防重與浮點語意比較共69項回歸通過；對帳Excel已輸出，摘要與兩張明細表完成渲染及公式錯誤掃描0。

## TASK-003 測試庫匯入與重送
- 寫入前以既有維護身分只連到 `PMR_SPC_TEST`／`PMR_PORTAL_TEST`，各完成 `COPY_ONLY`、`CHECKSUM` 與 `RESTORE VERIFYONLY`；工具結果明示 `productionOpened=false`／`productionConnected=false`。
- 藥液只新增TASK-002核准的160筆；每筆均由日期公式與F快照試算，保存160份Receipt與160份Trace，逐筆與Excel在1e-8內相符。204筆既有資料未重匯，4筆同鍵複驗差異保持隔離。
- 來源日期缺少排程的5個PPC／日期組合以最近的前一版公式與F快照建立4份不可變排程修訂；預演先rollback，套用後再逐筆試算驗證，未改公式或F內容。
- 咬蝕新增21份Portal報表、1,750個原始點，SPC保存1,771筆PROCESS量測（每份原始點加1筆速率）；11/10 QE1缺S4／S14仍隔離。
- 相同來源重送：藥液新增0、略過160；咬蝕新增0、21份均Unchanged。匯入後唯讀對帳為藥液364筆相同、4筆同鍵異值，咬蝕21份相同，主檔映射錯誤0。
- 統計工作117筆完成；另43筆因對應PPC未設定 `ChartTypeId` 而失敗。量測、Receipt與Trace均完整，不重匯；須先補主檔圖型後只重跑統計。
- 驗證：NovemberTestImport Release build 0警告／0錯誤；原生TransFiles 69項、版控對帳2項通過。測試站首頁、`/api/health`、`/api/version` 均HTTP 200，API版本0.1.117、environment=test。
- 未執行：正式資料庫寫入、正式發布、每日09:00排程。

## TASK-004 正式僅新增補匯
- 正式兩庫重新對帳全部來源，不沿用測試新增清單：藥液41筆相同、207筆可新增、120筆同鍵異值；咬蝕21份可補Portal。映射錯誤0。
- 正式試算207筆全部使用正式既有公式／F快照，與Excel差異均小於等於1e-8；補173個來源日期區間，共64份不可變排程修訂，未修改公式或F內容。
- 寫入前 `PMR_SPC_2026` 與 `PMR_PORTAL_UAT` 均完成COPY_ONLY／CHECKSUM及RESTORE VERIFYONLY；正式目標與environment守衛通過。
- 藥液新增207筆、Receipt／Trace各207；120筆同鍵異值未覆蓋。統計144筆完成，63筆因既有PPC未設定ChartTypeId失敗，量測與追溯均完整，後續只可補圖型後重跑統計。
- 咬蝕正式SPC原已有12份，新增9份；Portal補齊21份／1,750點。SPC最終21份共1,771筆PROCESS量測；11/10 QE1缺S4／S14仍隔離。
- 相同來源第二次套用：藥液新增0／略過207，咬蝕新增0／21份Unchanged。正式後重新快照：藥液248筆相同、120筆同鍵異值，咬蝕21份相同，映射錯誤0。
- 測試：正式守衛3項、TransFiles 69項及對帳2項通過，Release build通過。正式Web首頁HTTP 200；實際API基底`:8443` health/version HTTP 200、版本0.1.104、environment=production。
- 未執行：網站程式發布、來源Excel修改、120筆衝突覆蓋、QE1缺值補造、每日09:00排程。

## TASK-005 每日09:00前一日排程
- 新增 `daily-production-import.ps1`：預設處理台北日期的前一日，以隔離工作目錄依序進行來源掃描、正式快照、逐筆對帳、公式／F預演、必要時兩庫備份、僅新增寫入及寫入後重對帳；同鍵同值回報 `NoChange`，不備份、不寫入。
- 新增 `install-daily-production-task.ps1`：固定每日09:00、StartWhenAvailable、IgnoreNew及2小時上限，且以主機守衛拒絕安裝到非 `PMR-SPC-SRV` 電腦，不保存密碼。
- 自動驗證：NovemberImportTarget 4項通過；三支PowerShell語法解析0錯誤；安裝器 `WhatIf` 可建立預期工作定義。
- 正式唯讀實機：11/09來源為藥液67筆及咬蝕3份，正式內容均相同，結果 `NoChange`，沒有備份或寫入；11/10來源包含1份隔離資料，工具非零退出並在快照／寫入前阻擋。
- 未完成：目前操作主機為 `PC-PA16560048`，不是核准排程主機，故未將正式排程錯裝到個人電腦；須於 `PMR-SPC-SRV` 以可讀UNC且已核准的維護身分執行安裝器，再確認09:00觸發與首份summary。
- 回復：排程尚未建立，無需回復正式資料；工具回復可移除本次兩支腳本。工具未修改網站、正式資料或來源Excel。
- 進度表：備份 `Voice/備份/進度表-20261110-141701-TRANSFILES-NOV-DAILY-1110-TASK-005.xlsx` 後更新原檔；中央列維持核心未完成34／SPC相關10，並標記工具完成、正式主機排程待啟用。Artifact重讀／公式掃描、甘特里程碑、ZIP封裝及OpenXML SDK驗證均通過（0錯誤）。
