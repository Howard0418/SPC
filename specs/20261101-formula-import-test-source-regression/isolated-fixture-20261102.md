# T3b隔離測試基準

- 2026-11-02使用者回覆「允許」，授權以上8筆來源公式／F另建隔離測試項目驗算；不修改既有業務PPC、全域啟用F表或正式資料。主系統TransFiles，SPC測試服務／PMR_SPC_TEST相依；無schema／角色／IIS變更，不發布網站或打包。
- 範圍：專用標記`TEST-FORMULA005-20261102`製程、8個測試特性／PPC；保留來源PPC對照但不覆寫來源主檔。建立獨立未啟用F表版本及歷史，僅新PPC排程引用。日期固定來源報表日2026/10/27單日，僅測試fixture日期，非業務生效日。
- 公式：由I欄去除空白量測IF包裝、將當列H改Primary，其餘係數及F引用原樣保留；8位儲存精度供原始結果對帳，與業務顯示精度區分。使用既有公式版本與Schedule服務，不直接造排程／receipt／trace；不prepare／confirm／匯入量測。
- Given獨立測試排程 When代表原始值及F試算 Thenraw應與Excel快取在1e-8內一致、stored及規格判定可追溯。
- Boundary：0有效、單日邊界前後拒絕。Invalid：非數值與空值拒絕。Regression：既有51來源PPC公式／政策／排程、啟用F表前後一致，不產生量測／receipt。
- 建立前先核test URL版本與DB名PMR_SPC_TEST；transaction內建立、失敗回滾；以專用code去重，重跑先核一致性，不自動改寫已有fixture。只將安全ID／結果落檔；設定及憑證不得提交。
- 回復：停用專用PPC即可排除日常使用；保留不可變公式／排程歷史，不停trigger或大範圍刪除。交付專用ID及核對紀錄，必要永久清除另採限定關聯方案。
