# 技術計畫
- 功能 ID：SPC-FORMULA-EFFECTIVE-TRACE-PLAN-20261020；spec v1，[規格](spec.md)。
- 本次交付僅規劃；下列都是建議，非既有功能／已套用schema。

## 責任與最小資料模型
1. 沿用ChemicalAnalysisFormulaVersions作為不可變公式快照；補強所有新增／修改／還原路徑與同PPC版號併發保護，不能另複製一套公式內容維護。
2. 建議新增ChemicalFormulaEffectivities關聯表：PPCId、FormulaVersionId、EffectiveFromUtc、EffectiveUntilUtcExclusive、Revision、發布狀態／操作者／原因。日期是生效時間；ChangedAt仍只是修改時間。以組合關聯防止指向其他PPC公式。
   排程採PPC發布修訂集合：編輯／接續無迄日區間產生新修訂，交易內切換目前修訂指標；解析只讀目前已發布修訂，舊修訂留作證據、不與目前集合一起匹配。已保存trace固定引用原修訂，過去已使用的期間不得因一般排程編輯而改變。
3. 建議新增ChemicalCalculationTraces：量測及初驗／複驗識別、SourceKind、原始輸入／單位、MeasuredAt快照、EffectivityId／修訂、FormulaVersionId、FTableHistoryId（適用者）、實際F引用值／內容雜湊、計算器及精度政策版本、原始結果／儲存結果／輸入雜湊／來源資訊。不可變快照由系統產生，不能相信客戶端宣稱版號。外部計算／舊資料ID可空，但有明確狀態。
4. F表沿用ChemicalFTableVersionHistories.CellsJson，不以可更動的FTableVersionId作唯一證據；若舊快照不足，發布時計算適用引用值快照，不假造既有歷史。實作需確認快照本身可保證不可變。
5. VariableMeasurements只增加必要的追溯／處理模式關聯；避免把多個量測共用的公式全文重複存入主檔。規格／界限目前僅能記當次值及可確認來源，不冒稱已有獨立版本。

## API與入口（路由名稱暫定）
- SPC提供effective-formula解析、chemical-calculation預覽／確認所需契約；請求含PPCId、具時區的MeasuredAt、原始初驗／複驗值。回傳版號、有效期間、F快照、精度、預覽雜湊與排程修訂；不接受自由公式字串作為正式選版來源。
- 純預覽不寫入業務／追溯資料；確認於交易內重新驗證日期／排程／輸入及版本狀態，與量測／追溯一同保存。批次逐筆選版並保留原本逐筆失敗／隔離報告口徑，不擅改成整批原子。
- Portal只送輸入與量測時間，顯示SPC回傳結果，日期或輸入變更需重預覽。沿用既有QA／Editor權限，查閱者不能發布排程。
- TransFiles保留既有直接匯入契約；先定位真正寫入端。建議由SPC解析／計算，直寫端在同交易內校驗排程修訂並寫量測與追溯。具體DB約束／受控寫入介面須TASK-001驗證可行，無法阻擋缺追溯舊客戶端時不得啟用。
- 不將direct_import.py規劃器當成真實DB匯入實作；不在規劃階段決定把直寫改成API匯入。若閘門必須改變既有契約，先提出具體方案與影響再確認。

## 相容性與併發
- 新表／可空關聯先擴充，舊資料不回填實際版號；按PPC啟用新模式，未啟用者既有結果維持Unknown／外部計算。
- 排程發布／確認寫入以PPC為鎖定範圍，在SQL Server同一交易序列化檢查重疊與修訂；一般unique index不能取代區間重疊檢查。直接寫入路徑必須遵守同一鎖定順序，先測死鎖與失敗回復。
- 兩種計算器先跑相容性基準；不能僅因同名函數就認定可替換。必要時沿用相容引擎／解析介面，選版服務仍只保留一份。
- 已引用版本／F快照不得物理刪除；排程未來調整、內容新增與歷史修正需分清。預覽Token不能跨PPC／日期重用。

## 驗證與發布
- 每個TASK先補核心單元測試，四類測試與BDD對應見 [tasks.md](tasks.md)、[verification.md](verification.md)。
- 涉及schema時：先備份還原隔離庫、EF Up／Down／Up與既有資料雜湊核對，再進測試庫；本次不產生或執行migration。
- 順序：SPC相容schema與API（預設不啟用）→ Portal／TransFiles客戶端及閘門 → 少量測試PPC啟用 → 圖表實際追溯 → 端到端。每次只做一個TASK，不並行高風險變更。
- 回復：先停新模式寫入／排程發布，關閉啟用旗標或暫停該PPC匯入，還原相容程式；優先保留新表與追溯證據，不對已有正式追溯資料執行破壞性Down。不自動退回無版本計算繼續寫已啟用項目。
- 正式更新、正式DB、AD／SSO／IIS binding均不在本次授權；不得發布到D:/Sites/PmrPortal。
