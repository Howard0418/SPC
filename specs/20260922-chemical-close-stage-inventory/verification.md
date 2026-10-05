# 驗證紀錄：N1／N2 舊晚班匯入資料轉收線盤點
- 功能 ID：20260922-chemical-close-stage-inventory
- 日期：2026-09-22
- 範圍：兩庫唯讀（盤點當次）
- 資料修正：盤點當次未執行；後案已轉換
- 發布：不適用

## Schema
| 資料庫 | SamplingStage | PortalDailyDate | 階段索引 |
|---|---:|---:|---:|
| PMR_SPC_TEST | 有 | 有 | 1 |
| PMR_SPC_2026 | 有 | 有 | 1 |

## 候選結果（轉換前）
條件：CHEM、N1／N2、SamplingPhase=CLOSE、SamplingStage=GENERAL。

| 資料庫 | 候選 | N1 | N2 | 同鍵目標衝突 | 候選內重複鍵 |
|---|---:|---:|---:|---:|---:|
| PMR_SPC_TEST | 0 | 0 | 0 | 0 | 0 |
| PMR_SPC_2026 | 198 | 16 | 182 | 0 | 0 |

正式候選日期 2026-08-03～2026-09-03。

## 決策
使用者其後授權正式轉換，198 筆已改為 OPEN＋CLOSE。見 [正式轉換](../20260922-chemical-close-stage-apply/verification.md)。

## 證據
- inventory.json（後續重跑已為剩餘 0）
- 
elease-staging/chemical-close-stage-inventory-20260922/inventory.json
