# 驗證紀錄
- 功能 ID：FORMULA-IMPORT-002
- 規格版本：1
- 日期／環境：2026-10-31，本機與 SPC 測試站
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：已發布 SPC 測試 API 0.1.94；正式站未發布。

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter "FullyQualifiedName~ChemicalAnalysisFormulaVersionServiceTests\|FullyQualifiedName~ChemicalFormulaScheduleTests" --no-restore` | 等價 JSON 不新增版本 | 17 項通過；屬性順序與縮排不同的等價 JSON 回傳 `Created=false`。 | 通過 |
| AC-002 | 同上 | 差異 JSON 建立遞增歷史 | 17 項通過；精度值不同時建立 VersionNo=1，ConfigJson 與 PreviousConfigJson 均採 canonical JSON。 | 通過 |
| AC-003 | 同上；`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --configuration Release --no-restore` | 日期效期與收據不可回溯保持 | 日期排程／已發行收據回歸通過；Release build 0 警告、0 錯誤。 | 通過 |
| AC-004 | 備份後發布 `SpcApiTest` 並呼叫 `https://SPCTEST.pmr.com.tw/api/version`、`/api/health` | 測試 API 可用且為 test | IIS 實體目錄核對為 `D:\SPC\release\test\backend`；首次因 DLL 佔用立即停止，已由同次備份回復後以測試 AppPool 停止／啟動完成一致部署。version=`0.1.94`、environment=`test`、health=`ok`。 | 通過 |

## 未執行項目、限制及後續

- 未實際匯入 Excel 或寫入業務量測；僅進行測試 API 的 version／health 驗收。
- 正式發布、批次版本稽核及 Excel 端到端驗收留待後續任務。

## 基準與變更紀錄更新位置

已更新父規格任務、`TODO.md`、需求索引、變更紀錄及原進度表。原進度表備份為 `C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\Voice\備份\進度表-FORMULA-IMPORT-002-20261031-072500.xlsx`；OpenXML 驗證 0 錯誤、公式錯誤掃描 0 筆並完成兩張工作表視覺檢查。第 7 階段既有甘特條不因單一子工作延展，已同步完成狀態與實際日期。
