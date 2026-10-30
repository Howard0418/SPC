# 技術計畫
- 功能 ID：FORMULA-IMPORT-001
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- TransFiles：`chemical_direct_writer.py` 的 `SpcCalculationApi` 與 `parse_row` 為用戶端環境／列格式閘門；`test_chemical_direct_writer.py` 補充隔離測試。
- SPC：既有 `ChemicalFormulaScheduleController` 與 `ChemicalFormulaScheduleService` 定義 preview／prepare 與收據責任；本件不改動。
- 資料庫：不讀寫業務資料；以 InMemory 既有測試驗證服務行為。

## 實作方式與需求對應

1. 先新增測試：測試 environment 與測試 URL 正確時可建立 client，錯環境、非白名單、未啟用正式時仍拒絕（R-001）。
2. 執行 TransFiles 列格式與 API client 測試（R-001、R-002）。
3. 執行 SPC 日期公式排程單元測試，確認 preview／prepare 收據及角色防護（R-003）。

## API、檔案格式及資料模型

- 測試根網址：`https://spctest.pmr.com.tw/api`；正式根網址：`https://pmr-spc-srv.pmr.com.tw:8443/api`。
- 列資料：`PpcId`、`MeasuredAt`（ISO 8601 含時區）、`Primary`、選填 `Secondary`、`SamplingPhase`、`SamplingStage`，另由工具生成檔案 SHA256／工作表／列號來源鍵。
- 不新增 API、資料表、migration 或環境設定。

## 相容性與風險

- 測試只呼叫建構器與 mock，不送 HTTP，不碰憑證、Token、實際 Excel 或資料庫。
- 若測試暴露既有契約不符，只記錄並停止於本件範圍，不轉成實際匯入。

## 驗證安排

- `python -m unittest test_chemical_direct_writer.py`（AC-001、AC-002）。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter FullyQualifiedName~ChemicalFormulaScheduleTests`（AC-003）。
- 本件不需建置、IIS 發布、瀏覽器或正式 smoke test。

## 發布、備份及回復

本件僅測試／文件，無發布物。若測試檔需回復，可移除新增測試方法；不涉及資料、設定、IIS 或正式站。
