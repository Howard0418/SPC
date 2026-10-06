# 技術計畫
- 功能 ID：20261006-chemical-formula-versioning
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

後端：
- `backend/MesSpc.Api/Domain/Entities/Models.cs`
- `backend/MesSpc.Api/Infrastructure/Data/AppDbContext.cs`
- `backend/MesSpc.Api/Controllers/MasterDataV2Controller.cs`
- 新增 migration：建立藥液公式版本表。
- 視需要新增服務：集中處理版本建立、差異判斷與回復。
- 測試：`tests/MesSpc.Api.Tests/` 新增或擴充主檔維護測試。

前端：
- `frontend/mes-spc-web/src/views/PartProcessCharacteristicsView.vue`
- 第一版只需在單筆編輯中提供版本查詢/回復入口；總覽批次頁留待後續。

文件：
- `TODO.md`
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `ai_docs/10_change_log.md`
- 本規格資料夾。

## 實作方式與需求對應

1. 建立藥液公式版本資料模型。
   - 對應 R-001、R-002、R-006。
   - 建議表名：`ChemicalAnalysisFormulaVersions`。
   - 以 `PartProcessCharacteristicId` 關聯目前主檔。
   - 儲存 `VersionNo`、`ConfigJson`、`PreviousConfigJson`、`ChangeType`、`ChangedBy`、`ChangedAt`、`Reason`。

2. 在 `PUT /part-process-characteristics/{id}` 偵測 CHEM 公式變更。
   - 對應 R-001、R-005。
   - 只在 `ChemicalAnalysisConfigJson` 實際變更時新增版本。
   - 非 CHEM 或公式未變更不新增版本。

3. 新增版本查詢與回復 API。
   - 對應 R-002、R-003、R-004。
   - 建議：
     - `GET /api/v1/spc/chemical-analysis-formulas/{ppcId}/versions`
     - `POST /api/v1/spc/chemical-analysis-formulas/{ppcId}/restore/{versionId}`
   - 寫入/回復權限先沿用現有主檔修改角色。

4. 前端單筆公式編輯加入版本紀錄與回復入口。
   - 對應 R-002、R-005。
   - 最小 UI：在藥液分析公式區塊加入「版本紀錄」按鈕，展開列表，提供「回復」。

## API、檔案格式及資料模型

建議資料表欄位：
- `Id`
- `PartProcessCharacteristicId`
- `VersionNo`
- `ConfigJson`
- `PreviousConfigJson`
- `ChangeType`：Create / Update / Restore
- `RestoredFromVersionId`
- `Reason`
- `ChangedBy`
- `ChangedAt`
- `CreatedAt` / `UpdatedAt`

回傳 DTO 建議包含：
- `id`
- `partProcessCharacteristicId`
- `versionNo`
- `changeType`
- `changedBy`
- `changedAt`
- `reason`
- `configJson`
- `restoredFromVersionId`

## 相容性與風險

- 既有 `ChemicalAnalysisConfigJson` 保持為目前使用中的公式來源，確保不影響既有計算。
- 新表只新增歷史紀錄，不搬移既有公式。
- 需補一個初始化策略：第一次修改公式時，若無版本歷史，先用修改前 JSON 建立基準版本，再建立修改後版本，或只建立修改後版本並保存 `PreviousConfigJson`。建議第一版採後者，避免一次產生兩筆使用者不易理解的紀錄。
- 回復不自動重算既有量測資料；後續若需要重算另開任務。

## 驗證安排

- 後端單元/整合測試：
  - 修改 CHEM 公式會建立版本。
  - 修改非公式欄位不建立版本。
  - 非 CHEM 不可建立/回復藥液公式版本。
  - 回復指定版本會更新目前 `ChemicalAnalysisConfigJson` 並記錄回復。
  - 未授權寫入/回復回 401/403。
- 前端 build：
  - `npm run build:test`
- 後端 build/test：
  - `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter ChemicalAnalysisFormula`
  - `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
- 測試站 smoke：
  - 前端首頁與 JS MIME。
  - 後端 `/api/version`。
  - 單筆公式修改與回復人工驗收。

## 發布、備份及回復

- 測試站：測試通過後發布 SPC 測試站 frontend/backend。
- 正式站：需另行授權。
- 回復方法：
  - 程式回復：退回 commit 並重新發布測試站。
  - 資料回復：公式可使用新增的版本回復 API 回到指定版本。
  - Migration 回復：若需 rollback，使用 EF Core migration down；正式站須先備份資料庫。
