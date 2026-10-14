# SPC-IMPORT-VIA-TRANSFILES-ONLY-20261014 驗證紀錄

## 完成項目

- `SPC-TRANSFILES-IMPORT-TASK-001`：完成 TransFiles 直接匯入契約與錯誤報告文件。
- `SPC-TRANSFILES-IMPORT-TASK-002`：完成 SPC 匯入批次/錯誤查詢 API 與近期匯入 API。
- `SPC-TRANSFILES-IMPORT-TASK-003`：完成 SPC Web 匯入頁轉型，不再顯示 Excel 上傳入口。
- `SPC-TRANSFILES-IMPORT-TASK-004`：完成首頁新資料匯入提示與批次確認入口。
- `SPC-TRANSFILES-IMPORT-TASK-005`：完成相關測試與操作說明。

## 驗收對照

| 驗收 | 結果 | 證據 |
| --- | --- | --- |
| SPC 網站不再提供 Excel 人工匯入 | 通過 | `/uploads` 改為匯入批次與錯誤追蹤頁，不再載入 `VariableUploadView` / `AttributeUploadView`。 |
| TransFiles 匯入失敗可回 SPC 網站查詢 | 通過 | `GET /api/uploads` 可查批次，既有 `GET /api/uploads/{id}/preview` 錯誤報告包含 `sourceFile/sourceSheet/sourceRow/rawValue/errorCode/suggestion`。 |
| 新資料匯入後可確認製圖 | 通過 | `GET /api/uploads/recent-imports` 提供首頁新資料提示；首頁按鈕導向批次查詢，批次預覽可再進入圖表。 |
| 格式不符有調整建議 | 通過 | `docs/imports/transfiles-direct-import-contract.md` 定義必填錯誤欄位、錯誤代碼與修正原則。 |

## 自動化驗證

- `dotnet test tests\MesSpc.Chemical.Tests\MesSpc.Chemical.Tests.csproj --filter ChemicalStageTests --no-restore -p:UseSharedCompilation=false`：通過，22 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：通過，0 warnings / 0 errors。
- `npm run build`（`frontend/mes-spc-web`）：通過；既有 Vite chunk size warning 可接受。

## 發布

- 已刷新 SPC 測試站 backend/frontend。
- Backend 備份：`release/test/backend.backup-transfiles-direct-import-20261014-163000`
- Frontend 備份：`release/test/frontend.backup-transfiles-direct-import-20261014-163000`
- Backend staging：`release/staging/backend-transfiles-direct-import-20261014-163000`
- PowerShell 對 `https://SPCTEST.pmr.com.tw:8901/api/version`、`/api/uploads/recent-imports`、首頁皆回 `The SSL connection could not be established`，需以瀏覽器或受信任憑證環境補做外部 smoke。
- 正式站未發布。
