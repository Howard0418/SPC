# SPC-CHEM-MALFORMED-AUDIT-JUDGMENT-1120-TASK-001 驗證

日期：2026-11-20

## 結果

- 修正前：`MalformedOrNonObjectAuditCannotCrashHistoryOrBecomeEvidence`三組皆失敗，預期`Unknown`、實得`OK`。
- 修正後：`ChemicalSavedSpecificationTests` 14／14通過，涵蓋非物件、破損JSON、`details`型別錯誤與空陣列，以及合法、重複、偽造action與無證據fallback回歸。
- 完整API測試：591通過／13失敗／11略過。13項為既有歷史修復舊預期、EF InMemory `GroupBy`轉譯及Portal排除測試，與本次四個必要檔案無交集；未跨工作修正，不能宣稱完整套件全綠。

## 發布與連線

- 測試API：`0.1.161`→`0.1.162`；環境`test`。
- 測試Web：維持`0.1.173`，頁面可見API版本由`/api/version`反映本次`0.1.162`。
- `https://spctest.pmr.com.tw/api/health`：`ok`。
- 同源與`https://spctest.pmr.com.tw:8901/api/version`：皆為`0.1.162/test`。
- Web首頁、`index-DEMqJ0nr.js`與`index-pm66YOio.css`：HTTP 200，MIME分別為JavaScript與CSS。
- 發布前備份：`D:\SPC\release\test\backend.backup-malformed-audit-judgment-20261120-212319`；設定檔雜湊保持。正式站未發布。

## 使用者抽驗

1. 登入SPC測試站，確認頁面顯示API版本`0.1.162`且沒有「API離線」。
2. 以既有合法保存規格的藥液歷史資料查詢，確認原OK／NG與複驗顯示不變。
3. 破損payload屬安全回歸資料，不建議由畫面手動造資料；以本文件的14項自動測試作主要驗收證據。若透過受控測試fixture查詢，應顯示無法判定，原因碼`SAVED_SPEC_EVIDENCE_INVALID`、建議`EvidenceConflict`，不得顯示OK。

## 回復

如測試站出現回歸，可在停止API集區後，以上述備份回復`release\test\backend`，保留目前`appsettings.json`與`web.config`，再重驗health、version及環境；正式資料與schema不需回復。
