# 計畫：正式 N1／N2 舊 CLOSE 轉收線

## 受影響
- 資料：`PMR_SPC_2026.dbo.VariableMeasurements` 鎖定 198 筆。
- 文件：本規格、CHANGELOG、需求索引、前案盤點狀態。
- 不改應用程式。

## 步驟
1. 對 `PMR_SPC_2026` 做 COPY_ONLY＋CHECKSUM 備份並 VERIFYONLY。
2. Serializable 交易：UPDLOCK 候選、核對 198、衝突 0，只 UPDATE 兩欄。
3. 逐欄比對、剩餘 0、OPEN＋CLOSE 增加 198 後 commit。
4. 證據寫入 `release-staging/chemical-close-stage-apply-20260922/`。
5. 回復：核對現值仍為本批結果後，依 before.json 只還原兩欄。

## 驗證與發布
SQL 對帳為準。不發布 IIS／正式站。
