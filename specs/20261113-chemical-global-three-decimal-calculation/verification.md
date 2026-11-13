# 驗證紀錄
- 功能 ID：`SPC-CHEM-GLOBAL-3DP-1113-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-13／本機與SPC測試站
- 實作狀態：完成
- 驗證狀態：自動化通過；登入畫面待使用者驗收
- 發布狀態：SPC測試API 0.1.131已發布；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | xUnit焦點測試 | 先取三位再計算 | 紅燈4項先失敗；修正後`0.3524 → 0.352 → 53.504`，焦點54/54通過 | 通過 |
| AC-002 | xUnit與API預覽 | Stored與decimalPlaces皆為3 | OutputDecimalPlaces=3；Preview固定回傳3；API0.1.131 health/version為test | 通過 |
| AC-003 | 初驗／複驗／Secondary測試 | 全部採相同三位規則 | Initial、Recheck及Secondary焦點回歸通過 | 通過 |
| AC-004 | 版本與資料異動檢查 | 新證據版本；無歷史資料批次更新 | EngineVersion=`portal-excel8-v4-three-decimal-input`；無migration及資料修補 | 通過 |

## 未執行項目、限制及後續

完整API測試579項中556通過、11略過、12失敗；其中本件直接相關2項已更新並於焦點回歸通過，其餘10項為既有藥液歷史判定／修復隔離失敗，與本件計算路徑無關。Windows瀏覽器自動化native pipe不可用，登入畫面由使用者驗收。正式站未授權，不發布。

## 基準與變更紀錄更新位置

已更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。原進度表新增本工作及11/13原生里程碑；登錄260、完成216、核心未完成44，OpenXML 0錯誤，備份為`進度表-藥液三位計算前-20261113-184500.xlsx`。
