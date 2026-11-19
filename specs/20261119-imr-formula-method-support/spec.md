# SPC-IMR-FORMULA-METHOD-1119-TASK-001

## 問題
I-MR 管制項目可在介面選擇樣本標準差法或平均移動全距法，但後端未讀取 `FormulaConfigJson`，造成選項與 JSON 已改、UCL/CL/LCL 卻不變。

## BDD
- Given I-MR 項目選擇樣本標準差法，When 查詢管制圖，Then I 圖以樣本標準差 `s` 計算 `mean ± multiplier × s`，回傳方法及逐點管制線一致。
- Given I-MR 項目選擇平均移動全距法，When 查詢管制圖，Then I 圖以 `mean ± MrMultiplier × MRbar` 計算。
- Given I-MR 項目，When 編輯進階公式，Then 不可選擇需要子組全距的標準全距法；Xbar-R/S 原四種選項不變。
- Given 未選自訂方法，Then I-MR 沿用既有 `3 × MRbar / d2`，維持相容。

## 驗證資料
測試區 `#2437` 為 I-MR、21點、已存 `SIGMA_METHOD`；修正前仍回傳 UCL 97.8818、CL 93.8486、LCL 89.8153，修正後預期約 100.4240、93.8486、87.2731。
