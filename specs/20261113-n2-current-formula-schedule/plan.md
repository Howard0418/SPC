# 技術計畫

- 功能 ID：`SPC-CHEM-N2-CURRENT-SCHEDULE-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC 測試資料庫：新增兩個不可變排程修訂及其完整期間副本，切換兩個 Policy 的 active revision。
- 受控工具：固定測試環境、PPC、起日與預期最新版本，支援 plan／apply／verify。
- Portal、SPC API/Web 與 TransFiles 程式不修改。

## 實作方式與需求對應

1. plan 唯讀核對資料庫、PPC 身分、目前修訂、最後結束點、最新公式與 F 248，產生來源雜湊。
2. apply 前執行 `COPY_ONLY, CHECKSUM` 備份及 `RESTORE VERIFYONLY`。
3. Serializable transaction 內重讀 plan；每個 PPC 以 EF 新增完整不可變修訂，逐段精確複製目前 active revision 的 UTC 邊界，再附加 2026-11-07 起的開放式區間並切換 Policy。既有共用發布服務因舊匯入 Receipt 與目前 active revision 已存在的歷史差異會拒絕，本件不放寬服務規則、不改 Receipt，而以受控資料工具保留修正前狀態。
4. 同交易驗證預覽、歷史保持與非目標資料；失敗 rollback。
5. verify 與再次 plan 驗證冪等。

## API、檔案格式及資料模型

不變更 API、schema 或產品資料模型。沿用 `ChemicalFormulaScheduleRevisions`、`ChemicalFormulaEffectivities`、`ChemicalFormulaPolicies` 的既有不可變契約。

## 相容性與風險

- 風險：選錯公式或覆蓋歷史。以最高版本、固定 PPC 身分、plan hash、不可變新增及舊指紋防護降低。
- 風險：並行發布。Serializable transaction 與 active revision 重核，漂移即停止。
- 風險：舊 Receipt 已連結於非目前 active revision 的歷史公式。保留所有 Receipt／Trace 與舊修訂，精確複製目前 active revision，不利用本件回填或重算歷史。
- Rollback：套用失敗由交易自動回復；提交後如需回復，發布另一個完整修訂恢復前一 active revision 的期間契約，不刪除歷史。重大回復可使用本件已驗證完整備份。

## 驗證安排

- Happy Path：兩個 PPC 於 2026-11-12 預覽成功，公式版本分別 v4/v3。
- Boundary：新區間從 2026-11-07 00:00+08:00 精確開始；之前既有空缺保持。
- Invalid Input：版本、F、PPC、active revision 或來源雜湊漂移即停止。
- Regression：既有修訂／區間、量測、Receipt、Trace、公式、F、其他 Policy 指紋保持；重跑 0 寫入。

## 發布、備份及回復

本件只有測試資料修正，網站發布不適用。寫入前備份 `PMR_SPC_TEST` 並驗證；正式資料庫與正式站未授權、不操作。
