# 技術計畫

- 功能 ID：SPC-CHEM-HISTORY-JUDGMENT-1104
- 規格版本：1
- 狀態：已規劃／未執行
- 規格：[spec.md](spec.md)

## 系統責任與受影響範圍

- SPC API：權威判定、Unknown原因碼、盤點、dry-run及受控補正。
- Portal：顯示SPC回傳的判定與固定原因，不自行以目前主檔重算。
- TransFiles：後續preview欄位／格式與receipt防重契約；本規劃不修改工具。
- SQL Server：預期沿用既有追溯、規格版本、計算結果及UploadDetails；先不假設需要schema migration。
- 發布：本次規劃無程式與發布。後續實作測試通過後只發布測試站；正式站與正式資料補正均須另行授權。

## 實作順序與需求對應

1. P0-A 唯讀盤點：從查詢API列出全部Unknown，核對量測、PPC、來源批次、保存追溯、規格快照、SPC計算及有效版本，產生固定原因碼與可補／不可補分類（R-001、R-002）。
2. P0-B 防止新增：先建測試，再補preview格式驗證與不可重複receipt；不符合格式的列在寫入前隔離（R-003、R-004）。
3. P0-C 受控補正：只對dry-run證據唯一且一致者，在測試資料庫建立可重跑補正；保存來源、當時規格及判定證據，不改量測值（R-004、R-007）。
4. P1 顯示改善：Portal顯示SPC固定原因與調整建議；保留Unknown語意、分頁及初複驗（R-005、R-006）。
5. 測試站驗收與截圖：逐類核對API、DB、畫面、主檔／版本、前後指紋及重跑0新增；整理正式候選但不執行（R-005～R-007）。

## API、格式與資料模型候選

- 保留既有`judgment`、`judgmentSource`、`precisionNote`；候選新增固定`judgmentReasonCode`與`repairEligibility`，舊前端可忽略。
- 盤點／dry-run不可直接寫入；輸出至少包含measurementId、ppcId、sourceBatch、measuredAt、phase、storedValue、evidenceSource、reasonCode及建議。
- 補正輸入採伺服器產生的manifest與內容hash，不接受前端直接提交OK／NG；同一receipt重跑須NoChange。
- 若既有UploadDetails／CalculationResult可保存不可變證據，就不新增schema；只有現有資料模型無法唯一追溯時才另提migration規格，不在實作時順手加表。

## 相容性與風險

- 最大風險是拿現在規格改寫歷史；因此所有fallback保持fail closed。
- 顯示值可能是原始滴定值、濃度或百分比，須先核對單位與公式階段，避免用錯尺度判定。
- 補正須排除已有SavedTrace／SavedSpecification／SavedCalculation的資料，且不得重複建立證據。
- 既有OK／NG、公式F、量測、圖表及告警資料不回填、不重算；跨Portal／SPC／TransFiles只改本件契約。
- 若需正式資料寫入，先獨立完成備份、權限、前後指紋及rollback，不與其他schema／權限／正式資料工作併行。

## 驗證安排

- Happy Path：當時規格與值一致，補正後得到相同OK／NG，重跑NoChange。
- Boundary：單邊界限、邊界相等、複驗0、初複驗不同、規格改版日、民國／西元日期及時區。
- Invalid Input：缺PPC、日期、規格版本、公式F、值含單位、LSL>USL、重複／衝突版本、保存值不符。
- Regression：既有SavedTrace／SavedSpecification／SavedCalculation、分頁、日期篩選、目前主檔、公式F、圖表及一般頁面。
- 實際環境：測試站Web／主JS／CSS、health/version、瀏覽器實際API基底與Unknown原因畫面；截圖遮罩敏感資訊。

## 發布、備份及回復

- 本次規劃：build、資料寫入與網站發布均不適用。
- 後續測試實作：先備份測試API／Web與測試DB，dry-run指紋通過後再寫測試資料；回復程式包並保留append-only補正證據。
- 正式候選：須使用者另行授權；補正前COPY_ONLY／CHECKSUM／VERIFYONLY及逐表指紋，失敗只回復本批，不能整庫覆蓋後續資料。
