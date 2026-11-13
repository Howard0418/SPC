# 技術計畫

- 功能 ID：`SPC-CHEM-N2-TIN-PRECISION-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC 測試資料庫：新增公式版本、排程修訂、Receipt、Trace與修正稽核明細；更新一筆目前量測及PPC目前公式設定。
- 受控工具：固定測試環境、PPC、日期、公式、F、量測及前值，支援plan／apply／verify。
- Portal、SPC API/Web、TransFiles：不修改。

## 實作方式與需求對應

1. 先以純函式測試目前開放段的受控替換：只允許固定起點、舊公式／F與完整期間一致，保持所有前段。
2. plan唯讀核對公式v3、F248、revision1741、29段、量測94084、Receipt與Trace v1，產生來源雜湊。
3. apply前執行`COPY_ONLY, CHECKSUM`與`RESTORE VERIFYONLY`。
4. Serializable交易內建立公式v4、更新PPC目前設定、建立完整排程修訂並切換active revision。
5. 使用正式計算服務Preview／Prepare，建立新Receipt；更新量測為0.226、附加Trace v2與稽核明細，保留舊證據。
6. 驗證查詢有效值、前後保護指紋、舊證據、冪等及test API後提交。

## API、檔案格式及資料模型

不變更API或schema；沿用公式版本、不可變排程、Receipt／Trace及Portal日報稽核明細契約。

## 相容性與風險

- 歷史證據不可覆寫：舊公式、revision、Receipt、Trace只讀保持；目前量測以Trace v2及新Receipt追溯修正。
- 誤改其他設定：新公式設定以JSON語意比對，除`decimalPlaces`外必須一致。
- 同時異動：Serializable鎖定PPC及量測，來源雜湊漂移即停止。
- 回復：交易失敗自動回復；提交後可由已驗證備份回復，或發布新版本／修訂並以原Receipt建立可追溯回復，不刪歷史。

## 驗證安排

- Happy Path：0.10→Raw 0.2256→Stored 0.226，查詢有效值0.226。
- Boundary：排程開放段起點UTC 11/06 16:00不變。
- Invalid Input：公式／F／量測／Receipt／Trace／版本漂移即停止。
- Regression：其他量測、Receipt、Trace、公式、F、排程、PPC欄位及規格指紋保持；重跑0寫入。

## 發布、備份及回復

本件僅測試資料及設定修正，網站發布不適用；正式環境未授權且不操作。完成後同步原進度表。
