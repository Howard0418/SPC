# 技術計畫

- 功能 ID：`PORTAL-CHEM-SAVE-FRESH-PREVIEW-1113-TASK-001`
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- Portal Web：`chemical-effective-trace.js`封裝刷新與附加token；量測頁在confirm前呼叫。
- Portal測試：Node單元測試及Playwright頁面行為測試。
- SPC API／DB：契約不變，只接受Portal既有preview與confirm呼叫。

## 實作方式與需求對應

1. 先加入舊token存在時提交仍應再次preview的紅燈測試。
2. `refresh`依列建立初驗／複驗輸入，透過注入的preview函式取新結果、記錄token並更新送出值。
3. `submitChemicalRows`先並行刷新日期公式列，再attach並confirm；直接輸入列原樣通過。
4. 失敗不執行清空草稿的成功分支。

## API、檔案格式及資料模型

無API或資料模型變更；沿用`/api/spc/chemical-effective/preview`與`confirm`。

## 相容性與風險

- 儲存前多一次preview：僅變更列且為日期公式時發生，屬唯讀計算。
- 結果一致性：confirm payload改採最後一次preview的濃度／調整結果。
- 回復：回復Portal Web前版資產；無資料庫回復需求。

## 驗證安排

- Happy Path：舊token→新preview→confirm使用新token。
- Boundary：有／無複驗、單／雙原始值。
- Invalid Input：preview 失敗、confirm 409、缺原始值。
- Regression：直接輸入、既有保存值載入、精度、查詢與順序測試。

## 發布、備份及回復

已依Portal標準流程發布測試站 API 1.0.243／Web 1.0.284；正式站未授權、不發布。發布後已驗證首頁、主JS／CSS、Portal API health/version與瀏覽器實際SPC API基底。登入後實際按儲存仍待使用者驗收。
