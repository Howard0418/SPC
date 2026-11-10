# 功能規格：11月藥液／咬蝕防重補匯與每日排程
- 功能 ID：`TRANSFILES-NOV-DAILY-1110`
- 版本：1
- 狀態：TASK-001～004完成；TASK-005待執行
- 主系統：TransFiles；相依 SPC API／Portal／測試及正式資料庫

## 需求
1. 先處理 2026-11-01～2026-11-10 藥液及咬蝕量來源，已有資料不重複匯入。
2. 藥液依日期、線別、班別、開收線、槽位、分析項目及初／複驗辨識；保留公式、F 表與計算證據。
3. 咬蝕量依日期、線別、第幾次量測及 25／50 點完整內容防重；第二次依已核准規則取代第一次。
4. 測試區驗證通過後，正式寫入另行授權；未授權前只做 dry-run／preview。
5. 歷史補匯驗收後，建立 Asia/Taipei 每日 09:00 處理前一日資料的非互動排程，預設停用至取得啟用授權。

## BDD 驗收
- Given 相同業務鍵與內容已存在，When 重跑，Then 略過且新增 0。
- Given 業務鍵相同但內容不同，When 預檢，Then 列為衝突且不自動覆蓋。
- Given 來源格式錯誤，When 預檢，Then 列出檔案、sheet、欄位／儲存格、原值、原因與修正建議。
- Given 每日 09:00 排程，When 前一日檔案完成，Then 只處理前一日，且重跑不重複。
- Given 測試資料庫已有同一業務鍵，When 唯讀對帳，Then 以8位小數語意比較初／複驗值，分類為已存在相同或同鍵異值，不因Excel浮點尾差誤報。
- Given TASK-003獲准執行，When 寫入測試庫，Then 先完成兩庫COPY_ONLY／CHECKSUM備份及RESTORE VERIFYONLY，只新增對帳為可新增者，並以相同來源重送確認0新增。
- Given 藥液為公式項目，When 匯入，Then 必須由SPC日期公式與F排程試算、Prepare Receipt並保存Trace；試算與Excel差逾0.00000001即整批拒絕。
- Given 咬蝕量可新增，When 匯入，Then Portal報表／樣本點與SPC PROCESS量測皆保存，錯置到CHEM類別或點數不完整即拒絕。
- Given TASK-004獲准執行，When 對正式庫預檢，Then 必須以正式庫當下狀態重新分類全部來源，不得沿用測試庫的160／21新增清單。
- Given 正式庫已有相同業務鍵，When 內容相同或不同，Then 分別略過或隔離；不得覆蓋正式既有量測。
- Given 正式套用清單確認無阻擋，When 寫入，Then 先備份 `PMR_SPC_2026` 與 `PMR_PORTAL_UAT` 並通過CHECKSUM／VERIFYONLY，交易後重送必須0新增。

## 非範圍
- 不修改來源 Excel、公式、F 值、現有量測或主檔。
- TASK-001～002 不寫入測試／正式資料庫；TASK-003只允許寫入測試資料庫；TASK-004只允許核准清單正式僅新增；不建立Windows排程。
