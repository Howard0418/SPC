# 藥液日別調整公式／調整量公式補齊

- 父工作：`SPC-CHEM-F-CONSISTENCY-1103-TASK-001`；主系統SPC，相依TransFiles與Portal。
- 根因：`TestChemicalReimport.FormulaConfig`及`OctoberProductionFormulaFImporter.ConfigJson`明確將兩式寫成`null`，日別公式版本因此只保留濃度與F。
- 本階段先修來源preview契約與測試匯入器，再只對`PMR_SPC_TEST`既有日別排程受控補齊。2026-11-20 使用者已授權正式套用；正式作業只接受核准的 3,215 列 preview（SHA-256 `04509df0a8cfd99df150f8d098ec4df6f5c5161c5dcb385ae41e5b97c196e3e2`），並須同時通過 production API、`172.16.110.16` 與 `PMR_SPC_2026` 守衛。

## BDD

- Given Excel同列有「調整」「調整量」公式，When 解析日報，Then 將濃度格轉為`Concentration`、同列常數轉為固定數字，文字與運算保持，未知／跨列參照拒絕，不猜值。
- Given Excel該公式空白，When 建立日別公式版本，Then 保持未設定，不硬塞通用公式。
- Given 同PPC不同日期公式不同，When 建立排程，Then 每個日期使用該日Excel兩式，不以目前主檔覆蓋歷史。
- Given N2促進氯化亞錫等已核准NA／更槽例外，When 修補，Then沿用核准未設定規則，不把更槽公式重新加回。
- Given重跑相同來源，When再次規劃，Then新增版本與排程均為0；量測、Receipt、Trace、F、濃度式及正式資料保持。

## 測試範圍

- Happy：調整與調整量轉換、日別版本保存、邊界輸出與Excel相同。
- Boundary：早／中班、同PPC跨日不同公式、空白公式、換行文字。
- Invalid：未知格位、跨列參照、來源SHA或既有排程漂移均拒絕。
- Regression：既有濃度、複驗、F快照與量測防重不變。

## 2026-11-10 測試區執行結果

- 68 份九月起核准來源、3,215 列均可解析；2,746 次初／複驗試算與 Excel 相符，錯誤 0。
- `PMR_SPC_TEST` 備份通過 `COPY_ONLY`、`CHECKSUM`、`RESTORE VERIFYONLY` 後，新增 89 個日別公式版本並發布 82 組排程、2,420 個有效區間；F 歷史新增 0。
- 修正前後有效量測 1,753、Receipt 5,273、Trace 5,273、啟用藥液主檔 113、啟用 F 表 1，均保持不變；正式資料庫未連線、未寫入。
- 修復驗證再次執行 2,746 次試算，錯誤 0；相同修復原因固定為 82 筆排程修訂，重跑不再建立修復排程。
- Python 11 項、C# planner 26 項及 Release build 通過。正式區套用與畫面驗收仍屬父工作待辦。
- 正式套用前須完成 COPY_ONLY／CHECKSUM 備份及 RESTORE VERIFYONLY；交易內只允許新增核准來源所需的不可變公式版本、歷史 F 快照、排程修訂及有效區間並切換對應 policy，量測、Receipt、Trace、現行主檔與目前啟用 F 不得改變。正式唯讀預演若發現歷史 F 缺口，須以同一固定來源補入，不得改動啟用 F。相同來源重跑須為 0 新增。

## 2026-11-10 TransFiles EXE 續作

- 現場 parser 已同步調整／調整量公式、空白未設定、NA 分支及未知格位拒絕；68 份來源中 2,606 筆公式列與核准 preview 差異 0，2,371 筆含兩式。
- 72 項 TransFiles 相關測試通過，完整來源掃描 23.31 秒；獨立測試 EXE 已收錄必要模組、受控腳本及根憑證，啟動 5 秒存活且 Responding=True。
- 測試交付包不覆蓋 11/03 版本，未呼叫 API、未寫資料庫、未發布網站。EXE 核心打包完成，原生畫面列人工確認；父工作仍待正式兩式及正式 F 表畫面授權／驗收。

## 2026-11-20 正式套用結果

- 使用者明示授權後，以固定 SHA-256 `04509df0a8cfd99df150f8d098ec4df6f5c5161c5dcb385ae41e5b97c196e3e2` 的 3,215 列來源執行；正式 API、SQL 主機、資料庫及實際連線守衛均通過。
- 唯讀預演對應 98 個主檔、執行 2,746 次初／複驗試算且錯誤 0，交易完整回滾；預計異動與正式套用結果一致。
- 備份 `PMR_SPC_2026_pre_adjustment_schedule_20261120_104902.bak` 已完成 COPY_ONLY／CHECKSUM 與 RESTORE VERIFYONLY。
- 正式新增 3 個歷史 F 快照、97 個不可變公式版本、82 組排程修訂及 2,482 個有效區間；相同修復原因保持 82 筆，重驗 2,746 次錯誤 0。
- 有效量測 18,460、Receipt 1,128、Trace 1,128、啟用藥液主檔及目前啟用 F 前後不變；十個保護資料表指紋一致。沒有網站程式或版本發布。
- 正式 F 表登入畫面仍待人工確認；Windows 與瀏覽器操作服務均離線，未宣稱畫面驗收完成。
