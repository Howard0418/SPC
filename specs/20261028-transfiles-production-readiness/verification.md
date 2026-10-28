# 正式匯入能力核對結果
- 日期2026-10-28；唯讀現況核對完成。結論：目前這台TransFiles不能視為三類皆已具備正常正式匯入能力；未執行正式端到端寫入驗收。

## 三類結果
|資料|可核對能力|目前阻擋／限制|
|---|---|---|
|藥液|一般Excel有variable/excel預覽及批次confirm路徑；日期公式原始值入口有API預覽／prepare＋受控SQL保存協定，正式必要表／程序存在|本機已儲存production的Portal及SPC URL都是空白；日期公式GUI明確僅測試，內建SQL腳本硬性要求PMR_SPC_TEST與Windows整合認證。不能因正式程序已存在就認定工具可正式寫入；一般Excel路徑也未正式正向驗證。|
|咬蝕量|解析、完整25／50點、X-排除及測試匯入已有實作；歷史曾另以受控EtchTrialImport完成正式匯入|TransFiles走Portal `/api/etch-import-test/preview`及confirm，再呼叫SPC測試匯入端點；Portal及SPC都有測試環境／庫名守衛，正式環境不通過。另案正式SQL工具的歷史成功不能代替目前GUI流程。|
|落塵監控|2026/10/22修正版已接回Particle Long Format preview→人工確認；歷史測試站有效1152筆及製圖驗證已完成|正式URL未設定；本次正式ParticleMeasurements及Particle批次均0。一般Particle匯入API路徑存在，不等於已正式驗證；完整原表先前有78筆格式問題，需依實際來源重檢，不可整檔反覆送入。|

## 本次實測
- 本機只透過connection_settings的白名單讀取profile：test=`portaltest`／`spctest`；production兩個URL皆空白。未修改設定，也未讀取／輸出帳密、token或完整SQL連線字串。
- 三個現存EXE雜湊與交付記錄一致：10/15首筆LOG `D5246BFE…FF531`；10/20日期公式DB測試 `EB4777FF…15139`；10/22落塵修正 `CA636A73…41329`。程序盤點因系統拒絕存取未成功，因此沒有宣稱已確認使用者正在執行哪一個版本，未關閉或重啟工具。
- 正式SPC實際environment=production、DB=PMR_SPC_2026、API0.1.90.0；DLL SHA256 `D9DAD5BF5A4FE9A99C10502AA6A1D2AD50CB33B9D043647FFB23B818EB864EBA`與[正式最小修補](../20261028-chemical-history-refresh/formal-release.md)相符。正式來源基準a376290的EtchTestImportController亦有相同測試庫守衛。
- 正式Portal API1.0.200.0；環境Target=Production、DB=PMR_PORTAL_UAT、SPC base指向既有正式內部API。沒有改IIS、程式、環境設定或權限。
- 正式DB只SELECT：VariableMeasurements、ParticleMeasurements、UploadBatches、ChemicalCalculationReceipts、ChemicalStatisticsJobs及兩個化學直寫程序都存在。應用連線在三張量測／批次表的INSERT權限查詢為1；這只是DB權限存在，不證明使用者工具正式路徑可通，也沒有實際INSERT測試。
- 依PPC ControlScope CHEM／CHEMICAL彙總的現存量測3362列；SourceReference LIKE ETCH:%彙總3736列。此為查詢當下／指定條件的量測列，不等於本次匯入筆數或日報份數，也不以此和歷史不同篩選條件直接判定資料增減。
- 正式ParticleMeasurements=0，Particle UploadBatches=0。第一次落塵彙總錯用MeasuredAt未成功；核對模型改用MeasurementTime後查詢成功，沒有把失敗查詢當0。
- Python隔離測試43通過：etch_upload、chemical_direct_writer、direct_import、dust_particle_preview、dust_conversion_flow、etch_reports、connection_settings、tls_context。執行前閱讀測試，來源資料為暫存／mock，沒有真實HTTP、SQL提交或覆寫來源。
- SPC相關測試：Particle 16通過；Etch 24通過。合計83項；這些驗證解析、預覽／確認契約及隔離資料結果，不是正式端到端成功證據。
- 既有落塵測試站實際1152筆／九位置證據見[測試紀錄](../20261022-particle-empty-chart-plan/task002-verification.md)；歷史咬蝕正式受控工具見[正式匯入紀錄](../20260922-etch-production-import/verification.md)。不重複匯入，不核銷人工故障驗收。

## 後續範圍與保留
- 若要正式啟用：先確定使用版本及正式profile；另規劃咬蝕正式流程與日期公式正式client授權／actor／重送契約。不能只改網址、改DB名、移除測試守衛或沿用管理者帳號繞過。
- 正式正向驗證必須先固定來源檔／日期／線別／筆數、預查重複／衝突、備份及可追蹤批次回復，再另行取得實際寫入授權；本次沒有建立preview／prepare／confirm、呼叫SQL寫入程序、清資料或發布／打包。
- 本件只有唯讀稽核與文件，不改現有功能／發布／小工作狀態。進度表不適用，核心未完成26件及人工11／核銷4保持；沒有新增或自動開始正式啟用工作。
