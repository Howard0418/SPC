# 驗證紀錄
- 功能 ID：SPC-CHEM-EFFECTIVE-VALUE-1113-TASK-001
- 規格版本：1
- 日期／環境：2026-11-13／Portal 測試 IIS、SPC 測試 API
- 實作狀態：完成
- 驗證狀態：通過（既有非本次完整類別 2 項失敗另列）
- 發布狀態：Portal 測試 API 1.0.232／Web 1.0.273 已發布；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `node --test tests/chemical-effective-ui.cjs`；`dotnet test ... --filter FullyQualifiedName=...InteractiveChartUsesTheSameSavedRecheckEffectiveValueAsHistory` | 複驗優先，入口、查詢、圖表同為有效值 | Portal UI 9/9 通過；SPC 焦點測試 1/1 通過，初驗 12.18／複驗 12.41 的歷史查詢與圖表同為 12.41。 | 通過 |
| AC-002 | `node --test tests/chemical-effective-ui.cjs` | 無複驗時最終值為初驗 | Portal UI 測試確認直接輸入 12.18 顯示「最終（查詢／圖表）：12.18（初驗）」。 | 通過 |
| AC-003 | Portal／SPC build、`publish-all.ps1` 與 HTTP 驗證 | 不重算／不寫回，使用保存值 | Portal API/Web、SPC API build 通過；Portal 測試 API 1.0.232／Web 1.0.273 已發布。Portal 首頁／登入導向及 CSS/JS 皆 200 且 MIME 正確；Portal `/health` 回 test；SPC `/api/health`、`/api/version` 與 Portal 實際使用的 `https://spctest.pmr.com.tw/api/*` 均回 test／0.1.126。 | 通過 |

## 未執行項目、限制及後續

完整 `ChemicalHistoryQueryTests` 類別執行結果為 8 通過、2 失敗；失敗為既有 `LegacyUsesMatchingSavedCalculationSnapshotAndKeepsUnknownHonest` 與 `MissingHistoricalEvidenceReturnsFixedReasonAndActionWithoutUsingCurrentLimits`，與本次新增圖表有效值測試無關，未修改其業務邏輯。本次新增焦點測試獨立通過。

## 基準與變更紀錄更新位置

已更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md` 及原始進度表。`TODO.md` 有未屬本件的既存修改，為保留使用者工作區，本件以主規格及進度表登錄，未混入其修改。
