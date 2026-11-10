# 功能規格：TransFiles 每日匯入開關

- 功能 ID：`TRANSFILES-IMPORT-SWITCH-1110-TASK-001`
- 主系統：TransFiles；相依 SPC／Portal 正式資料匯入
- 狀態：完成

## 需求與範圍
- 使用者可在本機控制每日排程的總開關，並分別選擇藥液、咬蝕量；不得要求修改程式或排程參數。
- 總開關關閉時停用Windows排程；開啟時至少勾選一種資料類型。
- 排程執行時只掃描後續處理已啟用類型；停用類型的錯誤、候選及衝突不得影響啟用類型，也不得寫入停用類型。
- 設定保存於本機ProgramData，不保存密碼；既有防重、隔離、備份與正式目標守衛保持。
- 本件不立即補匯、不修改網站、不改資料庫schema。

## BDD驗收
- Given 總開關關閉，When 儲存，Then Windows排程為Disabled，手動執行runner回報Disabled且不讀寫資料庫。
- Given 總開關開啟且只勾藥液，When 儲存並執行，Then 來源報告只保留chemical，咬蝕不進入對帳或寫入。
- Given 總開關開啟且只勾咬蝕量，When 儲存並執行，Then 來源報告只保留etching，藥液不進入對帳或寫入。
- Given 總開關開啟但兩類皆未選，When 儲存，Then 明確拒絕且不改原設定。

## 驗證風險
- Happy Path：兩類皆開、僅藥液、僅咬蝕皆可保存。
- Boundary：總開關關閉時允許保留類型偏好。
- Invalid Input：開啟但零類型拒絕；損壞JSON採安全預設並提示。
- Regression：排程時間、正式資料庫守衛、防重鍵及既有匯入結果不變。
