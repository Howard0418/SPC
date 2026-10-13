# 功能規格：SPC Raw Data 與常態檢定異常規劃
- 功能 ID：SPC-RAWDATA-NORMALITY-ISSUES-20261012
- 版本：2
- 狀態：可實作
- 涉及專案：SPC
- 授權依據：使用者於 2026-10-12 提供 `docs/SPC_RawData 異常說明-1012.xlsx`，要求分析文件問題、規劃後加入小工作，不立即執行，並依輕重緩急執行小工作。
- 現行需求基準：`docs/requirements.md`、`CHANGELOG_CUSTOM.md`
- 前案或相關規格：`specs/20261005-single-iis-site/spec.md`、`specs/20261011-completion-remaining-work-reminder/spec.md`

## 目的、現況與證據

本次只做文件分析與小工作規劃，不修改程式、不建置、不發布。

使用者提供的 Excel 有兩張工作表：

- `raw data 確認`：
  - A1：若一開始線別選擇 All，後續即使選單一線別，下載 raw data 仍顯示「請先查詢一張管制圖後再下載 raw data」。
  - A2：raw data 下載資料不正確，包含資料非該項目、`Value` 欄位出現日期、沒有顯示規格訊息。
  - A41:T67 為下載結果樣本；26 筆資料中 `Value` 欄有 13 筆被 Excel 判讀為日期、12 筆為數值、1 筆為文字日期樣式。樣本顯示 `PartProcessCharacteristicId=2606`、`Process=快蝕線(QE2)`、`Characteristic=雙氧水`，但 `Value` 混入 `2026-08-18`、`2026-07-05`、`0521-08-01` 等日期型內容，屬資料正確性風險。
- `常態檢定確認`：
  - A1：常態分布判定仍有問題，整體分析與 Minitab 分析有差異，需確認判定方式是否正確。
  - A2：直方圖 scale 建議調整；數據變異較小時圖表幾乎只剩一根柱子，分析意義不足。

程式觀察：

- Raw Data 下載前端目前依 `ppcId` 或 `selectedMapping.id` 判斷可否下載；若使用者先查詢 All 總表，再點選或切換單線別，狀態可能未同步到可下載的單一管制項目。
- 後端 raw data CSV 目前把 `Value` 也交給日期格式化邏輯處理；若數字字串可被 `DateTime.TryParse` 誤判，可能輸出日期而非原量測數值。
- Raw Data CSV 欄位目前未包含 LSL、Target、USL、Spec 或管制/規格文字資訊，與使用者要求「顯示規格訊息」不一致。
- 常態性目前畫面顯示 p-value 與常態判定；須確認後端檢定方法、樣本數限制、p-value 近似方式與 Minitab 的差異來源，不能只調整文案。
- 直方圖目前固定偏好 12 bins，且 domain 會納入規格/管制界線；變異很小或界線距離很遠時，實際量測值可能集中在少數 bin。

## 範圍與非範圍

範圍：

- 規劃 Raw Data 下載狀態、CSV 內容與規格訊息修正。
- 規劃常態檢定方法比對、驗收資料集與畫面揭露方式。
- 規劃直方圖 scale/bin 改善。
- 加入 `TODO.md` 小工作池並排序。

非範圍：

- 本次不修改前後端程式、不新增測試、不發布測試站。
- 本次不修改正式資料、不處理正式站發布。
- 不以 Excel 中的截圖內容取代實際系統驗證；後續 Task 需用 API/畫面/自動化測試重現。

## 小工作排序

| 優先 | Task ID | 類型 | 初步範圍 | 排序理由 |
|---|---|---|---|---|
| 1 | SPC-RAWDATA-1012-TASK-001 | Bug / 高頻操作 | 修正 All 查詢後 raw data 下載狀態與單一管制圖選取上下文 | 直接阻擋使用者下載，且屬高頻測試流程 |
| 2 | SPC-RAWDATA-1012-TASK-002 | 資料正確性 | 修正 raw data CSV `Value` 日期誤判、確保只輸出目標管制項目的原始量測值 | 影響資料可信度與後續分析 |
| 3 | SPC-RAWDATA-1012-TASK-003 | 資料完整性 | raw data 下載補上規格資訊欄位或清楚規格區塊 | 使用者明確要求，需與既有規格/管制線口徑一致 |
| 4 | SPC-NORMALITY-1012-TASK-001 | 統計正確性 | 比對常態檢定方法與 Minitab 差異，建立固定樣本驗收與必要修正 | 影響統計判定，但需先保證 raw data 正確 |
| 5 | SPC-HISTOGRAM-1012-TASK-001 | 圖表可讀性 | 改善直方圖 scale/bin 策略，避免小變異資料擠成單柱 | 視覺可讀性改善，依賴 raw data 與統計口徑較穩定後處理 |

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 使用者在 All 總表查看後，選定單一管制項目時，raw data 下載應使用該單一項目上下文。 | AC-001 | Given 已查詢 All 總表，When 使用者點選某一項目進入單一管制圖並點下載 raw data，Then 不出現「請先查詢一張管制圖」且下載檔案的 ppcId 與畫面項目一致。 |
| R-002 | Raw data `Value` 必須保留量測數值，不可被日期格式化邏輯轉成日期。 | AC-002 | Given 樣本包含 `7.92`、`7.75` 等值，When 下載 raw data，Then `Value` 欄仍為數值文字或數值格式，不出現 `2026-08-18`、`0521-08-01` 這類日期誤判結果。 |
| R-003 | Raw data 下載內容需可辨識對應規格資訊。 | AC-003 | Given 管制項目有 LSL/Target/USL 或規格文字，When 下載 raw data，Then 檔案中可看到對應規格資訊，且與圖表/API 使用的規格口徑一致。 |
| R-004 | 常態檢定判定需有明確方法、樣本數限制與可比對結果。 | AC-004 | Given 固定測試樣本與 Minitab 對照結果，When 系統計算常態性，Then p-value/判定差異在規格允收範圍內；若採不同方法，畫面與文件需清楚揭露方法與不可直接等同 Minitab 的限制。 |
| R-005 | 直方圖需在小變異資料下仍能看出分布，不應因固定 scale 讓多數資料擠成單一柱。 | AC-005 | Given 數值變異很小且規格/管制界線距離較遠的資料，When 顯示直方圖，Then X 軸範圍與 bins 能呈現資料分布，規格/管制界線以輔助標示呈現且不壓縮主要資料。 |

## BDD 驗收草案

```gherkin
Feature: SPC raw data 下載
  Scenario: All 總表後選定單一項目下載 raw data
    Given 使用者已選擇線別 All 並查詢總表
    When 使用者選擇其中一個管制項目並下載 raw data
    Then 系統下載該管制項目的 raw data
    And 檔案中的 PartProcessCharacteristicId 與畫面項目一致

  Scenario: raw data Value 保留原始數值
    Given 管制項目的量測值包含 7.92
    When 使用者下載 raw data
    Then Value 欄顯示 7.92
    And Value 欄不會顯示日期格式

Feature: SPC 常態檢定與直方圖
  Scenario: 常態判定與對照工具一致或清楚揭露差異
    Given 固定樣本已有 Minitab 對照結果
    When 系統執行常態檢定
    Then 系統顯示的 p-value 與判定符合允收差異
    Or 系統清楚顯示採用不同檢定方法造成差異

  Scenario: 小變異資料的直方圖仍可讀
    Given 量測值集中且規格界線距離資料很遠
    When 使用者查看直方圖
    Then 主要資料分布不會全部擠在單一柱
    And 規格線與管制線仍能辨識
```

## 測試規劃

- Happy Path：
  - All 總表後點選單一項目下載 raw data。
  - 單一線別一開始查詢後直接下載 raw data。
  - 有規格上下限的項目下載 raw data。
- Boundary Case：
  - `Value` 為小數、整數、0、負值、空值。
  - 樣本數不足以做常態檢定。
  - 規格/管制界線遠離資料集中區。
- Invalid Input：
  - 未選定任何單一項目時下載 raw data。
  - 起訖日反向或超過允許查詢範圍。
- Regression Risk：
  - SPC 圖表查詢與 RawData 使用不同 ppcId。
  - CSV 日期欄位仍需格式化，但 Value 不可被日期化。
  - 常態檢定調整不可破壞既有 API 契約與前端卡片顯示。

## 假設與未決問題

- 規格資訊欄位名稱與內容需在實作時以既有 API/資料模型確認；初步假設至少包含 LSL、Target、USL 或原規格字串。
- Minitab 對照需使用者或後續 Task 提供可重算的樣本與 Minitab 結果；若未提供，先以 Excel 樣本與公開統計方法建立自動化驗收，再標示與 Minitab 待人工比對。
- Excel 內含 6 張圖片作為視覺證據，本次只記錄文字與資料樣本；後續畫面驗證需重新在 SPC 測試站操作。

## 發布規劃

- 本次規劃：建置不適用，發布不適用。
- 後續程式 Task：測試通過後依專案規則發布 SPC 測試站；正式站需另行授權。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-12 | 依使用者提供 Excel 完成問題分析、驗收草案與小工作排序 |
| 2 | 2026-10-13 | 完成 `SPC-NORMALITY-1012-TASK-001`，揭露 SPC 與 Minitab 常態檢定方法差異 |
