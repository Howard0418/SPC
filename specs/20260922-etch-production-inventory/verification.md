# 驗證紀錄：咬蝕正式庫唯讀盤點
- 功能 ID：20260922-etch-production-inventory
- 日期：2026-09-22
- 實作：盤點腳本完成；無寫入
- 驗證：通過（唯讀）
- 發布：不適用

## AC-001
| 項目 | 測試 | 正式（appsettings Production） |
|---|---:|---:|
| Portal 庫 | PMR_PORTAL_TEST | PMR_PORTAL_UAT |
| Portal 9月日報 | 48 | 0 |
| Portal 原始點 | 3650 | 0 |
| SPC 庫 | PMR_SPC_TEST | PMR_SPC_2026 |
| SPC ETCH:2026-09 量測 | 3698（A/B 3650＋RATE 48；缺 LINE_SPEED） | 0 |

- 相對測試 48 鍵：正式 Portal／SPC 皆缺 48，無額外鍵；目標庫確認後可新增。
- 證據：diff.csv、findings.json、raw-query.json、char-breakdown.json
- 路徑：specs/20260922-etch-production-inventory/ 與 release-staging/etch-prod-inventory-20260922/

## AC-002
隔離勿上正式：2026-09-02:PT2、09-08/11/16:PT1、09-18 四線。

## 正式主檔缺口（寫入前阻擋）
現行 EtchReportSyncService 要求每線 4 個已啟用且 MachineId 對應的 PROCESS：ETCH_A_AVG／ETCH_B_AVG／ETCH_RATE／ETCH_LINE_SPEED。
正式現況：
- 有 PT1／QE1／QE2 的 A/B/RATE（圖型多為 I-MR）
- 缺 PT2 機台連結 A/B/RATE
- 缺 ETCH_LINE_SPEED
- 無 XBAR_S（另有 Xbar-s(25) 孤兒項）

## 建議下一步
1. 確認 Portal 寫入目標是否為 PMR_PORTAL_UAT
2. 確認 SPC 目標為 PMR_SPC_2026
3. 授權後另開正式匯入規格；同一 Excel／manifest 重匯，不從測試庫複製列
4. 寫入前兩庫 COPY_ONLY＋VERIFYONLY；先補主檔再 apply
