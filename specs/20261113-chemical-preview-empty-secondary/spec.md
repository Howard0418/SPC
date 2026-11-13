# 藥液預覽空白第二滴定值正規化

- 功能 ID：`SPC-CHEM-PREVIEW-EMPTY-SECONDARY-1113-TASK-001`
- 主系統：SPC API
- 相依系統：Portal（沿用既有 payload）；SPC Web、TransFiles 不變。
- 資料庫影響：無 schema migration；不修改既有量測或公式。
- 發布影響：SPC 測試 API；正式站未授權。
- Rollback：回復第二滴定值正規化及 API 版本。
- 狀態：測試站已發布；核心實作完成，待登入後實際儲存人工驗收；正式站未授權。

## 現況與原因

Portal 預覽沒有第二滴定值的項目時送出 `null`，確認批次則送出空字串 `""`。前端預覽憑證快取把兩者視為同一空值，但 SPC API 建立公式快照時保留原始差異，造成預覽成功後數秒內確認仍回 `chemicalPreviewChanged`（HTTP 409）。

## Requirement

- R-001：第二滴定值為 `null`、空字串或純空白時，預覽與確認均須正規化為同一個未提供值。
- R-002：有第二滴定值的雙滴定公式仍須保留原值並正常計算。
- R-003：日期、班別、取樣階段、第一滴定值、公式、規格或排程真的改變時，既有衝突防護仍須拒絕。
- R-004：不得修改既有公式、F 表、主檔、量測資料、Portal UI或資料庫結構。

## BDD 驗收

- Given 單滴定項目預覽時第二滴定值為 `null`
- When Portal 確認批次送出空字串
- Then SPC 將兩者視為相同未提供值，接受同一預覽憑證並儲存量測。

- Given 預覽後實際輸入、日期、班別、取樣階段、規格或公式排程改變
- When 使用舊預覽憑證確認
- Then SPC 仍回傳預覽內容已變更，不寫入部分資料。

## 測試規劃

- Happy Path：先以 `null` 預覽，再以 `""` 確認，量測與 Receipt 正常保存。
- Boundary Case：純空白第二滴定值與 `null` 等價；有效數字保持原值。
- Invalid Input：第一滴定值或量測日期變更仍拒絕。
- Regression Risk：雙滴定公式、複驗、預覽憑證與交易原子性。
