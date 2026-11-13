# 功能規格：N2 促進氯化亞錫三位精度修正

- 功能 ID：`SPC-CHEM-N2-TIN-PRECISION-1113-TASK-001`
- 版本：2
- 狀態：完成
- 涉及專案：SPC（主系統）；Portal 為操作與查詢端，不修改程式
- 授權依據：使用者要求新增頁面與 Excel 計算值必須相同
- 現行需求基準：[藥液量測項目精度](../20261113-chemical-entry-per-item-precision/spec.md)
- 前案：[N2 氯化亞錫目前公式排程補齊](../20261113-n2-current-formula-schedule/spec.md)

## 目的、現況與證據

N2／促進／氯化亞錫（PPC 2518）公式與 F 值均與 Excel 相同：`2.256*Primary*F!$B$9`、B9=1。輸入0.10的未取位結果為0.2256；目前公式796/v3設定`decimalPlaces=2`，伺服器先保存0.23，Portal再顯示成0.230，Excel依三位顯示0.226。

11/12量測94084已保存舊Receipt、初驗Trace v1及值0.23。修正不能覆寫舊Receipt／Trace，須建立新公式版本與新排程修訂，再以新的Receipt及Trace v2更新目前量測，保留前值追溯。

## 範圍與非範圍

- 僅處理測試庫 PPC 2518：公式內容與F不變，只將精度2改為3並建立新不可變公式版本。
- 新增完整排程修訂，保留舊revision 1741及全部期間；新active revision的11/07起開放段改用新公式與F248。
- 受控修正量測94084：0.23→0.226，建立新Receipt與初驗Trace v2，舊Receipt／Trace v1保持，新增修正稽核明細。
- 不改其他量測、公式、F、主檔欄位、schema、Portal／SPC網站程式或正式資料庫。

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 公式及F相同時，SPC須與Excel三位結果相同 | AC-001 | 0.10以`2.256×0.10×1`計算，Raw=0.2256、Stored=0.226、DecimalPlaces=3 |
| R-002 | 公式精度變更須版本化 | AC-002 | 新公式v4除`decimalPlaces=3`外與v3語意相同；主檔目前設定與新版本一致 |
| R-003 | 日期排程與舊證據須可追溯 | AC-003 | 新active revision完整保留29段邊界，開放段使用v4/F248；舊revision、Receipt及Trace不修改 |
| R-004 | 已保存錯誤取位值須受控修正 | AC-004 | 量測94084目前值0.226、連結新Receipt及Trace v2；Trace v1與原Receipt仍可查，查詢有效值0.226 |
| R-005 | 操作須備份、原子、冪等且隔離正式 | AC-005 | 備份驗證通過；失敗整筆rollback；再次plan pending=0；API為test且正式未異動 |

## BDD

- Given 公式為`2.256*Primary*F!$B$9`且B9=1，When 輸入0.10，Then SPC顯示及保存0.226。
- Given 11/12已有0.230的保存證據，When 執行精度修正，Then 舊Receipt與Trace v1保持，新增Receipt與Trace v2成為目前證據，查詢顯示0.226。
- Given 修正已完成，When 再次執行相同plan，Then 不新增公式、排程、Receipt、Trace或量測修訂。

## 例外與邊界

- PPC身分、公式字串、F248 B9、v3內容、active revision、量測ID／前值／舊Receipt或Trace與預檢不同時停止。
- 若除精度外有其他公式差異，停止，不自動合併。
- 備份或`RESTORE VERIFYONLY`失敗時不寫入。

## 假設與未決問題

無阻擋未決問題；本次使用者明示須與Excel三位結果相同，取代此項目現行兩位保存設定，不擴及其他項目。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-11-13 | 初版 |
| 2 | 2026-11-13 | 測試庫完成公式 v4／排程 revision 1743 與量測追溯修正；驗證 0.10→0.226 |
