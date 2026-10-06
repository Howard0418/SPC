# 功能規格：藥液分析公式版本記錄與回復
- 功能 ID：20261006-chemical-formula-versioning
- 版本：1
- 狀態：可實作
- 涉及專案：SPC
- 授權依據：使用者要求「藥液分析公式會有需要常改動，要有日期記錄，並能回復之前的資料」。
- 現行需求基準：[docs/requirements.md](../../docs/requirements.md)、[TODO.md](../../TODO.md)
- 前案或相關規格：[20261005-spc-1002-workbook-plan](../20261005-spc-1002-workbook-plan/spec.md)

## 目的、現況與證據

目的：藥液分析公式會頻繁調整，需記錄每次異動日期、修改人、前後內容，並提供回復到既有版本的能力，避免公式改錯後無法追溯或回復。

現況觀察：
- 藥液分析公式目前儲存在 `PartProcessCharacteristics.ChemicalAnalysisConfigJson`。
- 前端 `PartProcessCharacteristicsView.vue` 單筆編輯時會組出 `chemicalAnalysisConfigJson`，再呼叫 `PUT /part-process-characteristics/{id}` 覆蓋目前設定。
- `ChemicalAnalysisOverviewView.vue` 目前為總覽/匯出/導向編輯，尚非批次儲存頁。
- `ChemicalFTableVersions`、`ChemicalFTableCells`、`ChemicalFTableReferences` 已存在，用於 F 表版本與公式引用，不等同於每個藥液分析公式本身的版本歷史。
- 目前沒有 `ChemicalAnalysisConfigJson` 的獨立歷史表，也沒有回復 API。

## 範圍與非範圍

範圍：
- 為藥液管制項目 `ChemicalAnalysisConfigJson` 建立版本歷史。
- 在建立/修改藥液分析公式時保存歷史紀錄。
- 提供查詢版本紀錄與回復指定版本的 API。
- 回復時仍更新目前 `PartProcessCharacteristics.ChemicalAnalysisConfigJson`，不覆蓋歷史紀錄。
- 僅處理單筆藥液公式版本與回復。

非範圍：
- 不做藥液公式總覽與批次儲存頁；該功能留待 `SPC-TASK-002`。
- 不改現有藥液計算公式語法。
- 不修改既有量測資料與歷史計算結果。
- 不修改 F 表版本資料模型；F 表仍沿用既有 `ChemicalFTableVersions`。
- 不發布正式站。

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 藥液分析公式每次新增、修改、回復都需留下版本紀錄。 | AC-001 | 修改一筆 CHEM 管制項目的藥液公式後，可查到包含前後 JSON、版本號、修改時間與修改人的版本紀錄。 |
| R-002 | 版本紀錄必須能回復指定舊版本。 | AC-002 | 呼叫回復 API 後，該管制項目的 `ChemicalAnalysisConfigJson` 回到指定版本內容，且新增一筆回復紀錄。 |
| R-003 | 非藥液管制項目不可建立藥液公式版本。 | AC-003 | 對非 CHEM 管制項目呼叫版本/回復 API 時回傳 400 或 404，不改資料。 |
| R-004 | 權限需與現有主檔編輯一致，未授權者不可修改或回復公式。 | AC-004 | 未登入或未授權呼叫修改/回復 API 時回 401/403。 |
| R-005 | 不影響既有單筆主檔編輯流程。 | AC-005 | 原本 `PUT /part-process-characteristics/{id}` 修改非公式欄位仍可正常儲存；公式有變更時才新增公式版本。 |
| R-006 | 公式版本需能支援後續批次儲存頁逐筆建立版本。 | AC-006 | 版本服務可由單筆 PUT 與未來批次 API 共用，不綁死在前端單一頁面。 |

## 例外與邊界

- 若公式 JSON 無變化，不新增新版本。
- 若目前公式為空，第一次啟用公式也需建立版本紀錄。
- 若回復到與目前相同內容，可回 200 並不新增版本，或新增一筆回復紀錄；實作前需固定策略，建議不新增重複版本。
- 若指定版本不存在、已不屬於該 PPC 或 PPC 非 CHEM，需回 404/400。
- 修改人來源沿用目前登入/使用者資訊機制；若測試環境缺使用者，需可用既有 fallback，但不可阻塞資料寫入。

## 假設與未決問題

- 假設版本紀錄先以 JSON 快照方式保存，不拆解公式欄位，避免改動既有公式格式。
- 假設回復只影響主檔公式設定，不自動重算既有量測結果。
- 未決但不阻擋：前端第一版版本清單放在單筆編輯 modal 或另開輕量側欄；實作前依現有 UI 結構選擇最小改動。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-06 | 初版 |
