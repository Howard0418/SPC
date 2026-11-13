# 技術計畫

- 功能 ID：`SPC-CHEM-C1-EXCEL-ALIGN-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC 測試資料庫：六個公式版本、六個排程修訂、六筆 Receipt／Trace v2及目前量測校正。
- 受控工具：固定測試環境與六個PPC／量測，提供plan／apply／verify。
- Portal、SPC API／Web、schema、正式環境：不修改。

## 實作方式與需求對應

1. 先建立純函式測試：公式JSON只允許指定欄位改變、排程於11/12切段且不產生缺口。
2. 唯讀plan核對公式、F、排程、量測、Receipt、Trace與保護指紋。
3. apply前執行測試庫COPY_ONLY／CHECKSUM備份及RESTORE VERIFYONLY。
4. Serializable交易內建立六個新公式版本與完整排程修訂，切換active revision。
5. 用正式計算服務Preview／Prepare建立新Receipt，更新目前量測、調整欄位並附加Trace v2及稽核明細。
6. 驗證六組Excel結果、查詢有效值、舊證據、保護指紋與冪等。

## API、檔案格式及資料模型

不變更API或schema；沿用公式版本、不可變排程、Receipt、Trace及UploadDetail稽核契約。

## 相容性與風險

- 不覆寫舊證據；目前量測改連新Receipt，舊Trace v1保留。
- 公式版本只允許精度及指定硫酸調整式差異；其餘JSON語意須相同。
- 交易前後以非目標資料指紋防止旁及。
- 失敗由交易rollback；提交後可由已驗證備份回復，或另建版本化回復。

## 驗證安排

- Happy Path：六組滴定值、Raw、Stored、調整與Excel逐格相同。
- Boundary：排程11/12台北日界線前後無缺口、無重疊。
- Invalid Input：公式／F／量測／舊證據漂移即拒絕。
- Regression：其他量測、公式、F、排程、PPC與主檔指紋保持；重跑0寫入。

## 發布、備份及回復

本件只有測試資料與設定修正，網站發布不適用；正式未授權且不操作。完成後同步原進度表。
