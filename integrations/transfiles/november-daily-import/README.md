# 11月藥液／咬蝕防重預檢最小修正

主規格：`specs/20261110-transfiles-november-daily-import/`。

原生TransFiles不是Git repository，本目錄保存可版控的最小差異說明：

1. `scheduled_import.py` 的藥液預設UNC路徑改為 `.../01_IQC.IPQC/IPQC/藥水分析/藥水分析記錄表/2026年`。
2. 掃描時將檔名含「補液位」或「更槽」列為 `excluded`，不交給parser。
3. `chemical_daily_parser.py` 依檔名輸出 `shift=MORNING/MIDDLE`，依sheet輸出 `samplingPhase=OPEN/CLOSE/GENERAL`。
4. `chemical_business_key` 新增 `shift` 與 `samplingPhase`，防止中班或開／收線被誤略過。
5. 對應的parser與scheduled import測試已加入原生工具；四組62項通過。

本件沒有呼叫API、寫入DB或建立Windows排程。

TASK-002新增 `inventory_reconciliation.py`：以業務鍵及8位小數語意內容雜湊分類 `insert_candidate`、`existing_same`、`same_key_different_content`、`duplicate_existing_key`。測試資料庫快照腳本只允許 `PMR_SPC_TEST`／`PMR_PORTAL_TEST` 且只執行SELECT；對帳及Excel輸出腳本位於主規格目錄。

TASK-003新增 `tools/NovemberTestImport`：只允許測試資料庫，寫入前備份驗證；藥液依核准清單建立日期公式Receipt／Trace，咬蝕同步Portal報表／點位及SPC PROCESS量測。相同來源重送必須0新增；正式補匯與09:00排程不在本工具本次執行範圍。
