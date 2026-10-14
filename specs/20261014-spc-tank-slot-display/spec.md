# 小型變更：SPC 槽體／槽位顯示一致

日期：2026-10-14
狀態：DONE
主系統：SPC
相依系統：無
資料庫影響：無
發布影響：需發布 SPC 測試站 frontend；正式站另行授權
Rollback：還原本次前端與文件提交，重新發布 frontend

## Requirement

使用者回報管制圖監控明細的「槽體」顯示為完整名稱，例如 `蝕刻(鎳)`，但 SPC 管制項目總覽的「槽位」欄位顯示為 `蝕刻`。業務上槽體與槽位在此畫面代表同一位置，顯示應一致。

## Scope

- 調整 SPC 管制項目總覽前端顯示邏輯，保留槽體／槽位完整名稱。
- 不修改資料庫 schema。
- 不修改後端 API 契約。
- 不調整主檔名稱或既有資料。

## Acceptance Criteria

- AC-001：當總表 API 回傳 `slotName = 蝕刻(鎳)` 時，總表「槽位」欄完整顯示 `蝕刻(鎳)`。
- AC-002：管制圖監控明細與總表顯示同一位置名稱時，不得一邊保留括號、一邊切掉括號後內容。
- AC-003：原本空白槽位仍顯示 `N/A`。

## Verification

- Playwright：`spc-summary.spec.ts` 加入完整槽體名稱回歸檢查。
- Frontend build：需通過，既有 chunk size warning 可接受。
