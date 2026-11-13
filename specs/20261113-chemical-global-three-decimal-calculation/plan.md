# 技術計畫
- 功能 ID：`SPC-CHEM-GLOBAL-3DP-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- `ChemicalFormulaCalculation`：輸入三位正規化、輸出三位取位、計算證據版本。
- `ChemicalFormulaScheduleService`：預覽契約回傳固定 `decimalPlaces=3`。
- xUnit：初驗、複驗、Secondary、中點與相容回歸。

## 實作方式與需求對應

先新增紅燈單元測試，再於建立公式變數時將 Primary／Secondary 以 AwayFromZero 取三位；公式結果的 Stored 固定取三位。更新引擎與精度策略版本，避免新舊Receipt證據混淆。

## API、檔案格式及資料模型

API欄位不變；`decimalPlaces`固定回傳3。無schema變更。

## 相容性與風險

- 已保存量測不重算；新預覽與重新輸入採新規則。
- 公式內部仍使用decimal運算，調整與判定沿用既有Raw結果。
- 風險為既有期待兩位的測試與前端格式；以回歸測試及實際API驗證確認。

## 驗證安排

- Happy Path：0.3524*152=53.504。
- Boundary：0.3525取0.353；負中點AwayFromZero。
- Invalid Input：既有非數字與超精度拒絕測試保持。
- Regression：初驗、複驗、Secondary、F快照、排程預覽與API build。

## 發布、備份及回復

無DB寫入，資料庫備份不適用。測試通過後使用SPC測試發布流程發布API，驗證Web／資產／health／version及瀏覽器API基底。正式站不發布。
