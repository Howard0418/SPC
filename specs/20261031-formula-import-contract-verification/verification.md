# 驗證紀錄
- 功能 ID：FORMULA-IMPORT-001
- 規格版本：1
- 日期／環境：2026-10-31，本機隔離測試
- 實作狀態：完成（僅契約測試）
- 驗證狀態：通過
- 發布狀態：不適用；本件不打包、不發布。

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 在 TransFiles 目錄執行 `python -m unittest test_chemical_direct_writer.py` | 白名單與正式防呆符合規格 | 6 項通過；測試 URL 可建立、非白名單／環境不一致及未明確開啟正式皆拒絕。 | 通過 |
| AC-002 | 同上 | 日期、原始值及來源列驗證失敗時拒絕 | 6 項通過；合格列保留原始值與含時區日期，NaN、空值、無時區、無效日期／PPC／班別均拒絕。 | 通過 |
| AC-003 | `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter FullyQualifiedName~ChemicalFormulaScheduleTests --no-restore` | preview 無量測寫入、prepare 受角色保護 | 7 項通過；既有測試確認 preview 不寫量測、prepare 為 Admin／Editor，並保護過期、偽造／變更輸入與排程。 | 通過 |

## 未執行項目、限制及後續

- 不呼叫測試或正式 API；不實際匯入 Excel、建立公式版本或寫入資料庫。
- 首次在 `D:\SPC` 工作目錄執行 Python 命令因找不到 TransFiles 模組而失敗；已改為工具目錄重跑且通過。此為測試啟動路徑問題，不是程式契約或匯入失敗。
- 公式版本差異／效期／冪等寫入留待 FORMULA-IMPORT-002。

## 基準與變更紀錄更新位置

- 已更新 `specs/20261029-chemical-formula-db-import-environment-history/tasks.md`、`TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md` 及原進度表。
- 進度表已先備份為 `C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\Voice\備份\進度表-FORMULA-IMPORT-001-20261031-012841.xlsx`，再更新原檔；小工作總表為 223 筆、核心未完成 31 筆、已完成 175 筆。OpenXML 驗證 0 錯誤、公式錯誤掃描 0 筆，並完成「專案進度表」與「小工作總表」視覺檢查。因本件是既有第 7 階段下的單一隔離驗證，未新增或延展原生甘特條，改同步該階段的完成紀錄與實際日期。
