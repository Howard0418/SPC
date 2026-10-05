# 功能規格：2026 年 9 月咬蝕日報正式匯入
- 功能 ID：20260922-etch-production-import
- 版本：1
- 狀態：完成
- 涉及專案：Portal＋SPC
- 授權依據：2026-09-22 使用者確認 `PMR_PORTAL_UAT` 為正式 Portal，並授權建立正式匯入規格
- 現行需求基準：[需求索引](../../docs/requirements.md)
- 前案：[9 月咬蝕補匯](../20260918-etch-month-import/verification.md)、[正式庫唯讀盤點](../20260922-etch-production-inventory/verification.md)

## 目的、現況與證據
將 Excel `D:\SPC\docs\PD-3-581-06B-咬蝕量_2026.xlsx` 已在測試環境驗證的 48 份日報，使用相同 manifest 重新匯入正式 Portal `PMR_PORTAL_UAT` 與正式 SPC `PMR_SPC_2026`。

來源檔 SHA256 固定為 `cadd52c7d5e4d31188b9ab5cc7aae7faa68970f17a305dbe1ce9a04e1417f43a`。正式唯讀盤點顯示：兩個正式目標的 2026-09 咬蝕資料皆為 0，48 個日期＋線別鍵皆可新增，無既有內容衝突。

測試 SPC 現況只有 3,698 筆（A/B 原始點 3,650＋速率 48），原驗收預期的線速 48 筆目前不存在，因此測試資料庫不得作為複製來源。正式匯入必須由同一份 Excel／manifest 重建完整資料。

## 範圍
- Portal 正式目標：伺服器 `172.16.110.16`、資料庫 `PMR_PORTAL_UAT`。
- SPC 正式目標：伺服器 `172.16.110.16`、資料庫 `PMR_SPC_2026`。
- 只處理 manifest 內 48 份日報：
  - PT1：10 份
  - PT2：12 份
  - QE1：13 份
  - QE2：13 份
- Portal 建立 48 份日報與 3,650 個原始點。
- SPC 建立 3,650 個 A/B 原始點、48 個整體速率及 48 個實際線速，預期合計 3,746 筆。
- 正式缺少的咬蝕主檔，只能依測試已驗證規則受控補齊：`XBAR_S`、`ETCH_LINE_SPEED`、PT2 及各線完整 PPC、樣本數、圖型與規格。

## 非範圍
- 不從 `PMR_PORTAL_TEST` 或 `PMR_SPC_TEST` 複製資料列、ID、UploadBatchId 或計算結果。
- 不覆寫、不刪除正式既有資料。
- 不修改 Excel。
- 不匯入負值或未完整日報。
- 不發布應用程式到正式 IIS；若工具實作需要程式變更，先在本機／測試環境驗證，正式資料匯入另行執行。
- 不處理 2026-09 以外月份。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 來源必須是固定雜湊的 Excel 與既有 48 份 manifest | AC-001 | 執行前重新計算 SHA256；48 鍵、3,650 點及線別分布完全一致 |
| R-002 | 正式目標固定且不可誤連測試庫 | AC-002 | 工具須同時驗證伺服器、Portal=`PMR_PORTAL_UAT`、SPC=`PMR_SPC_2026`，並要求明確 production/confirm 參數 |
| R-003 | 匯入前正式庫必須無衝突 | AC-003 | 再次唯讀盤點 48 鍵；同鍵同內容可略過，不同內容或 Portal/SPC 單邊存在立即停止 |
| R-004 | 先備份再寫入 | AC-004 | 兩個正式庫各完成 `COPY_ONLY WITH CHECKSUM`，且 `RESTORE VERIFYONLY WITH CHECKSUM` 通過 |
| R-005 | 受控補齊 SPC 主檔 | AC-005 | 四線各有 A/B/RATE/LINE_SPEED 共 16 個已啟用 PROCESS PPC；A/B 樣本數 PT=25/50、QE=25/50，圖型為 `XBAR_S`；速率及線速為 I-MR／樣本數 1 |
| R-006 | Portal 與 SPC 僅新增，不覆寫 | AC-006 | Portal 48 份／3,650 點；SPC 3,746 筆；既有非本批資料指紋不變 |
| R-007 | 跨系統失敗可安全重跑 | AC-007 | Portal 先寫 Pending；SPC 成功後才標 SUCCESS；失敗保留證據，同內容重跑重用既有 Portal 日報 |
| R-008 | 匯入後完整對帳 | AC-008 | 3,650 BEFORE/AFTER/咬蝕量逐點一致；48 速率、48 線速一致；16 組圖表平均與樣本 S 一致 |
| R-009 | 重跑不得新增重複資料 | AC-009 | 第二次 dry-run／重跑 48 份皆 Unchanged，ReportId 與 UploadBatchId 不變 |
| R-010 | 隔離資料不得進正式 | AC-010 | 0902 PT2、0908/0911/0916 PT1、0918 四線在正式無本批資料 |

## 例外與邊界
- 任何正式既有同鍵不同內容：整批停止，不自動更新。
- 來源檔雜湊、manifest 筆數或點數改變：停止並重做盤點。
- 備份或 VERIFYONLY 失敗：不得寫入。
- 主檔存在但圖型、樣本數或規格與驗證基準不同：列入差異，停止自動修正，先審查。
- Portal 寫入成功但 SPC 失敗：Portal 維持 Pending，保存 run 證據；禁止手工刪除後重匯。
- 正式執行器不得自動啟動通知、排程或 IIS 應用程式。

## 已確認與執行授權
- 已確認：`PMR_PORTAL_UAT` 是正式 Portal 目標。
- 已確認：正式 SPC 目標為 `PMR_SPC_2026`。
- 已授權：建立規格、實作、雙庫備份及正式 apply。
- 已執行：48 份正式匯入、完整對帳及防重跑驗證。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-09-22 | 確認 Portal 正式庫後建立正式匯入規格 |
