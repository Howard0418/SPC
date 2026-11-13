# 技術計畫

- 功能 ID：`SPC-CHEM-N2-P400-SCHEDULE-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC 測試資料庫：新增 PPC 2515 的不可變排程修訂並切換 active revision。
- 受控工具：擴充既有 N2 排程工具的 P400 profile，固定測試環境、身分、日期邊界與最近公式。
- Portal、SPC API/Web、TransFiles：不修改。

## 實作方式與需求對應

1. plan 唯讀確認 PPC 2515、active revision 1736、28 段、公式 1157/v4、F248 與來源雜湊。
2. apply 前建立 `COPY_ONLY, CHECKSUM` 備份並執行 `RESTORE VERIFYONLY`。
3. Serializable transaction 內精確複製 28 段、附加 2026-11-07 起開放段，再切換 Policy。
4. 同交易驗證 PreviewAsync 與 PrepareAsync；Prepare 產生的測試 Receipt 以 savepoint 回復，業務資料保持。
5. verify 與再次 plan 驗證冪等、保護資料及測試 API 環境。

## API、檔案格式及資料模型

不變更 API、schema 或產品資料模型；沿用既有不可變排程與計算憑證契約。

## 相容性與風險

- 選錯項目／版本：固定 N2、催化、CHAR-000847、PPC 2515、1157/v4、F248，任何漂移即停止。
- 改寫歷史：只新增修訂，並以前後指紋及舊 ID 內容核對。
- 舊 Receipt 與共用發布驗證可能不相容：不放寬產品規則；精確複製目前 active revision，只增加未使用的未來段。
- 回復：交易失敗自動回復；提交後可發布另一完整修訂恢復原期間，重大回復可使用已驗證備份。

## 驗證安排

- Happy Path：11/12、滴定值 0.17 的 Preview／Prepare 成功。
- Boundary：開放段從台北 11/07 00:00 精確開始。
- Invalid Input：PPC、版本、F、active revision 或來源雜湊漂移即停止。
- Regression：既有排程、Receipt、Trace、量測、公式、F、其他 Policy 指紋保持；重跑 0 寫入。

## 發布、備份及回復

僅測試資料修正，網站發布不適用。正式環境未授權且不操作；進度表於驗證完成後同步。
