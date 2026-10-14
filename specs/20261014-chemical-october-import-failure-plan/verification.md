# SPC-CHEM-OCTOBER-IMPORT-FAILURE-20261014 驗證紀錄

## SPC-CHEM-OCT-IMPORT-TASK-001

狀態：完成。

### 驗收對照

| 驗收 | 結果 | 證據 |
| --- | --- | --- |
| 匯入預檢列出總筆數、可匯入筆數、錯誤筆數 | 通過 | 既有 UploadBatch `TotalRows`、`ValidRows`、`ErrorRows` 保留，前端摘要卡沿用。 |
| 每筆錯誤包含檔案、工作表、Excel 列號、欄位、錯誤代碼與修正建議 | 通過 | `UploadService.GetPreviewAsync` 回傳錯誤報告 DTO，包含 `sourceFile`、`sourceSheet`、`sourceRow`、`fieldName`、`errorCode`、`errorMessage`、`suggestion`。 |
| Excel 上傳能保留來源座標 | 通過 | `UploadsController` 解析 Excel 時於每列暫存 payload 加入 `__SourceSheet`、`__SourceRow`。 |
| 前端可讀化顯示 | 通過 | `UploadPreviewView` 未通過資料表新增來源欄，錯誤詳細原因顯示錯誤代碼與建議。 |

### 自動化驗證

- `dotnet test tests\MesSpc.Chemical.Tests\MesSpc.Chemical.Tests.csproj --filter ChemicalStageTests --no-restore -p:UseSharedCompilation=false`：通過，21 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：通過，0 warnings / 0 errors。
- `npm run build`（`frontend/mes-spc-web`）：通過；既有 Vite chunk size warning 可接受。

### 發布

- SPC 測試站 backend/frontend 已刷新。
- Backend 備份：`release/test/backend.backup-chem-oct-import-preview-20261014-151500`
- Frontend 備份：`release/test/frontend.backup-chem-oct-import-preview-20261014-151500`
- Backend staging：`release/staging/backend-chem-oct-import-preview-20261014-151500`
- 正式站未發布。

### Smoke 限制

- PowerShell 對 `https://SPCTEST.pmr.com.tw:8901/api/health`、`/api/version`、首頁皆回 `The SSL connection could not be established`。
- PowerShell 對 `http://172.16.110.27:8084` 目前回目標電腦拒絕連線。
- 需以瀏覽器或受信任憑證環境補做外部 smoke。
