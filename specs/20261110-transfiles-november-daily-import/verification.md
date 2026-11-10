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
