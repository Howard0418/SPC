# 線別分析項目總覽公式版本記錄補強驗證

功能 ID：SPC-CHEM-OVERVIEW-FORMULA-VERSION-20261007  
日期：2026-10-07；更新：2026-10-10  
狀態：DONE；已實作並發布測試站檔案，HTTP smoke 補驗通過

## 驗證

- Happy Path：已規劃總覽頁修改單筆公式後需建立同一份藥液公式版本記錄，並保存參照文件備註。
- Boundary Case：已規劃無變更不建版、批次多筆各自建版、部分失敗只成功者建版。
- Invalid Input：已規劃無效公式 JSON、非 CHEM 項目、未授權儲存/回復、參照文件備註過長。
- Regression Risk：已規劃保護管制項目設定頁既有版本記錄/回復，以及 F 表版本記錄。
- 2026-10-09 Happy Path 部分實作：總覽頁新增版本參照文件/修改依據輸入欄，批次儲存時隨既有 `PUT /part-process-characteristics/{id}` 傳送 `chemicalAnalysisFormulaVersionReason`。
- 2026-10-09 Regression Risk 防護：後端 `PartProcessCharacteristic` 新增 `[NotMapped] ChemicalAnalysisFormulaVersionReason`，不新增資料表，不改公式 JSON 格式；既有 `ChemicalAnalysisFormulaVersionService` 仍是唯一建版來源。

## 結果

- AC-001：`TODO.md` 新增 `SPC-CHEM-OVERVIEW-VERSION-TASK-001`。
- AC-002：小工作已補強參照文件備註驗收，排序在 IIS 測試站環境問題之後、管制圖點位備註之前。
- AC-003：2026-10-09 已實作總覽頁參照文件備註送出、後端 PUT 寫入版本 `Reason`，並補測試斷言版本備註保存。
- AC-004：2026-10-09 靜態檢查通過：確認 `chemicalAnalysisFormulaVersionReason` 由前端 payload 送出、後端傳給版本服務，測試中使用 `測試人員` 而非真實姓名。
- AC-005：通過。`dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "ChemicalAnalysisFormulaVersion|Update_Should_Record_ChemicalFormulaVersion" --no-restore -p:UseSharedCompilation=false`：11 passed；`dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors；前端 `npm run build` 通過，產出 `index-BSgajB2z.js`、`index-CRaqMmV-.css`，僅保留既有 chunk size warning。
- AC-006：測試站檔案已發布至 `release/test/backend` 與 `release/test/frontend`；備份 `release/test/backend.backup-chemical-overview-version-reason-20261009-002558`、`release/test/frontend.backup-chemical-overview-version-reason-20261009-002558`；testhost 前端產出 `index-kMhx5TtG.js`、`index-CRaqMmV-.css`，且發布後資產包含 `chemicalAnalysisFormulaVersionReason`。
- AC-007：2026-10-09 HTTP smoke 未通過，`8084/api/version`、既有 `8081/api/version` 與 `8083/` 皆回連線被拒，判定為測試 IIS/網路層阻擋。
- AC-007 補驗：2026-10-10 HTTP smoke 通過。`http://172.16.110.27:8084/api/version` 200 且 `environment=test`、`version=0.1.59`；`http://172.16.110.27:8084/` 200；既有 `http://172.16.110.27:8081/api/version` 200、`http://172.16.110.27:8083/` 200。單站首頁引用 `/assets/index-96H3lPaN.js`，該 JS 包含 `chemicalAnalysisFormulaVersionReason` 且不含 `/api/api`。正式站未發布。
