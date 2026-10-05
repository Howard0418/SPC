# 驗證紀錄：正式 N1／N2 舊晚班編碼轉收線
- 功能 ID：20260922-chemical-close-stage-apply
- 日期：2026-09-22
- 範圍：PMR_SPC_2026 198 筆
- 資料修正：已提交
- 發布：不適用（未發布網站）

## 備份
| 項目 | 結果 |
|---|---|
| 資料庫 | PMR_SPC_2026 |
| COPY_ONLY | 是 |
| CHECKSUM | 是 |
| VERIFYONLY | 通過 |
| backup set | 5098 |
| 路徑 | C:\\Program Files\\Microsoft SQL Server\\MSSQL16.MSSQLSERVER\\MSSQL\\Backup\\PMR_SPC_2026_pre_chemical_close_stage_20260922_033158.bak |

## 交易結果
| 檢查 | 結果 |
|---|---|
| 更新筆數 | 198（N1=16、N2=182） |
| 對應 | CLOSE＋GENERAL → OPEN＋CLOSE |
| OPEN＋CLOSE | 10 → 208 |
| 剩餘 CLOSE＋GENERAL | 0（重跑盤點亦為 0） |
| 其他業務欄位 | 不變 |
| RowVersion | SQL 自動遞增 |
| MIDDLE | 37 筆未改 |
| 衝突 | 0，已 rollback 條件未觸發 |

## 證據
- release-staging/chemical-close-stage-apply-20260922/backup-proof-20260922_033158.json
- release-staging/chemical-close-stage-apply-20260922/prod-20260922-113203/before.json
- release-staging/chemical-close-stage-apply-20260922/prod-20260922-113203/after.json
- release-staging/chemical-close-stage-apply-20260922/prod-20260922-113203/result.json

回復：先核對現值仍為本批 OPEN＋CLOSE，再依 before.json 指定 ID 只還原兩欄。
