# 技術計畫
- 功能 ID：FORMULA-IMPORT-002
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- `ChemicalAnalysisFormulaVersionService.cs`：正規化 JSON 供版本比較與保存。
- `ChemicalAnalysisFormulaVersionServiceTests.cs`：驗證等價重送與差異版本歷史。
- `ChemicalFormulaScheduleTests.cs`：回歸日期效期與收據不可回溯。

## 實作方式與需求對應

- R-001：以 `JsonDocument` 遞迴輸出、物件屬性排序的 canonical JSON；解析失敗則維持相容的 Trim 行為。
- R-002：保留現有每 PPC 版本號遞增及 PreviousConfigJson 追溯；只有 canonical JSON 不同才建立版本。
- R-003：不修改排程服務，執行其現有回歸測試確認邊界仍受保護。

## API、檔案格式及資料模型

不新增 API、DTO、資料表或 migration。既有 `ChemicalAnalysisFormulaVersions` 欄位維持相容，新的版本快照採 canonical JSON。

## 相容性與風險

陣列與字串不重排，故不會把可能有計算意義的順序／公式內容視為相同。無效舊設定不拋出新錯誤。

## 驗證安排

- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter "FullyQualifiedName~ChemicalAnalysisFormulaVersionServiceTests|FullyQualifiedName~ChemicalFormulaScheduleTests" --no-restore`（AC-001～003）。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`。

## 發布、備份及回復

測試通過後僅發布 SPC 測試 API `release/test`，依既有測試站部署程序做 health/version 驗證；正式站、正式資料與 Portal 不在本件範圍。回復為測試 API 發布前備份。
