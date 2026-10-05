# 驗證紀錄
- 功能ID：20260917-legacy-measurement-date；規格版本1；2026-09-17
- 實作、必要驗證：完成。發布：SPC release/test/backend完成，正式站未發布。
| 驗收 | 方式 | 結果 |
|---|---|---|
| AC-001 | SQLite：缺日報日期、23:59:59、次日排除、日報日期優先 | 通過 |
| AC-002 | SQLite：日曆可載入、班別/來源/舊CLOSE界線；既有Chemical回歸 | 通過 |
| AC-003 | SQLite：原ID更新、補日期、重複讀寫拒絕、預覽辨識既有資料 | 通過 |
| AC-001/002 | 實際Portal C1／2026-08-28／早班，按載入 | 已載入6筆既有資料、修改模式；量測值177.96、35.26、151.14、37.24可見，未送出 |

## 執行證據
- 新測試先跑：2失敗、1通過，證實日期回退與衝突缺口。
- dotnet test tests/MesSpc.Chemical.Tests --no-restore -v quiet：24/24通過。
- 追加預覽斷言後僅跑 MissingDailyDate_UsesMeasurementDate：1/1通過；無再次全跑。
- NU1900：NuGet弱點來源不可連線，不代表完成弱點掃描。
- Release publish成功；相關程式git diff --check通過。
- 實際IIS SpcApi路徑及AppEnvironment=test、PMR_SPC_TEST確認；備份 backend.backup-legacy-measurement-date-20260917-132827。
- 發布DLL與staging雜湊一致，appsettings*.json/web.config與備份一致；/api/version 200、environment=test。
- 不更動schema、不批次寫入真實量測、不發布Portal或正式站。還原方式：app_offline後回拷同路徑備份並移除offline。
## 同步文件
需求索引、業務基準、ai_docs/03_backend_api.md、ai_docs/10_change_log.md、CHANGELOG_CUSTOM.md。
## 待辦
N1/N2舊CLOSE歸類依最新指示暫緩；其他未分類階段仍不推定。真實儲存未執行，儲存路徑已以隔離測試驗證。
