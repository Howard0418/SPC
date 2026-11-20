# 技術計畫

- 功能 ID：`20261120-chemical-adjustment-excel-alignment`
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC API：提供唯讀對帳所需 PPC 資料；計算器加入三個具名變數與缺值錯誤；沿用既有版本歷史與稽核。
- SPC Web 調整參數頁：維持三參數唯一編輯入口，增加來源／對帳提示時不得取得公式修改權。
- SPC Web 線別分析項目總覽：同列唯讀顯示槽容、添加／稀釋管制值與對帳狀態；兩式仍可依既有權限編輯。
- 資料庫：預期不需 schema migration，三參數仍位於 `ChemicalAnalysisConfigJson`。若實作盤點證明需要獨立版本欄位，須先修訂規格，不可直接加表。
- Voice：指定 Excel 只讀，不修改、不另存覆蓋。

## 實作方式與需求對應

### 階段 1：唯讀對帳矩陣（TASK-001）

1. 使用者先儲存並關閉來源 Excel；記錄原檔 SHA-256、大小、最後修改時間。
2. 讀取 11 張資料 Sheet，保留 Sheet、列號、開／收線、ECR 與有效日期。
3. 唯讀取得測試區 PPC 與 `ChemicalAnalysisConfigJson`，以正規化線別／槽別／項目建立候選映射。
4. 不以模糊名稱自動套用；一對零、一對多、多對一及開／收線異值全部隔離。
5. 解析兩式的儲存格參照與常數用途，輸出差異分類及建議，不寫 DB。

### 階段 2：計算參數契約（TASK-002）

1. 先建立核心紅燈單元測試與 API 整合測試。
2. 擴充 `ChemicalCalculationConfig` 的三個 nullable decimal 欄位。
3. 在公式儲存格集合加入 `TANK_CAPACITY_LITERS`、`ADDITION_CONTROL_VALUE`、`DILUTION_CONTROL_VALUE`。
4. 未引用的空參數不影響既有公式；引用空參數沿用「未知輸入不得當 0」原則回明確錯誤。
5. 只把已確認 Excel 公式轉為網站語法；既有公式不批次改寫。

### 階段 3：總覽交叉確認（TASK-003）

1. `rowOf` 從同一設定 JSON 讀出三參數。
2. 表格與 Excel 匯出加入槽容、添加、稀釋及對帳狀態；三參數為唯讀。
3. 設定警示納入「公式引用但參數未設定」，不把合法單向規則誤判為錯誤。
4. 更新桌機／手機、i18n、匯出及公式保存不覆寫參數的前端回歸。

### 階段 4：測試資料受控同步與驗收（TASK-004）

1. 固定已核准矩陣與來源 SHA-256，先輸出 preview。
2. 取得目標列 before snapshot、公式版本數、設定雜湊及保護表計數。
3. 只套用已核准欄位；每列保存後查回同一 JSON、版本與稽核。
4. 以低於門檻、門檻值、區間內、高於門檻、空參數及 `NA` 單向規則驗算調整／調整量結果。
5. 使用者在測試站依「線別 → 槽別 → 分析項目」抽驗兩頁一致性後才標完成。

## API、檔案格式及資料模型

- 不新增瀏覽器直接上傳桌面 Excel 的入口；對帳工具只在受控開發流程使用。
- `ChemicalAnalysisConfigJson` 候選欄位保持：
  - `tankCapacityLiters: decimal?`
  - `additionControlValue: decimal?`
  - `dilutionControlValue: decimal?`
  - `adjustmentFormula: string?`
  - `adjustmentAmountFormula: string?`
- 對帳 preview 必須帶來源 SHA-256 與 PPC 設定 before hash；apply 發現任一變動時拒絕過期預覽。
- `NA`／空白只存在來源證據與預覽狀態，不保存為 JSON 字串；經確認為不適用時網站值為 null，且公式不得引用該方向變數。

## 相容性與風險

- 新增具名變數向後相容：舊公式未引用時輸出不變。
- 公式與參數同存 JSON，總覽完整 PPC PUT 有覆寫風險；須增加測試證明用最新資料合併且不覆蓋並行參數異動，必要時改用公式專用端點，不能接受最後寫入者無聲覆蓋。
- 來源檔可能有合併儲存格造成線別／槽別只出現在第一列；解析時需向下填值作比對，但證據仍保留原始空白。
- 批次逐筆保存可能半套完成；TASK-004 應使用受控 preview/apply 或明確逐列結果與可重送 NoChange，不沿用目前前端中途失敗即留下前段已保存的行為作資料同步工具。
- 本案與現行公式／F 一致性工作碰觸同一計算區域；一次只執行本案一個 TASK，先完成範圍最小的唯讀 TASK-001。

## 驗證安排

- Happy Path：唯一 PPC、三正數、兩式可轉換，兩頁與匯出一致。
- Boundary：小數精度、門檻等值、最大合理槽容、單向規則、開／收線同值合併。
- Invalid Input：0、負數、`NA` 被公式引用、缺槽容卻引用、重複自然鍵、未知公式變數、過期 preview。
- Regression：濃度公式、初／複驗、F 快照、公式排程、版本歷史、參數異動 Email、既有量測與非藥液 PPC。
- 程式變更依範圍執行 xUnit／整合測試、前端單元／Playwright、i18n 及 `build:test`。
- TASK-001 文件型工作只做內容、連結、來源 hash 與唯讀查詢檢查；不建置、不發布。

## 發布、備份及回復

- 本次規劃：無程式、資料庫或站台變更，發布不適用。
- TASK-002 修改 API 時遞增 API 版本；TASK-003 修改 Web 時遞增 Web 版本。每次測試通過即依規則發布 SPC 測試站並完成首頁、主 JS／CSS、API health/version/environment 及瀏覽器 API 基底驗證。
- TASK-004 寫測試資料前建立設定 before snapshot；回復時只針對固定 PPC 還原原 JSON 並新增如實的版本／稽核紀錄，不刪除歷史。
- 正式站與正式資料均須另行授權；不得發布到 `D:\Sites\PmrPortal`。
