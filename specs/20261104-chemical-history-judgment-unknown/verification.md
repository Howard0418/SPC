# 驗證紀錄

- 功能 ID：SPC-CHEM-HISTORY-JUDGMENT-1104
- 規格版本：6
- 日期／環境：2026-11-04／測試`PMR_SPC_TEST`、正式`PMR_SPC_2026`唯讀盤點
- 實作狀態：T-001～T-004完成；T-005～T-006未執行
- 驗證狀態：T-004自動化、測試DB唯讀與測試站連線通過；登入後實際畫面／截圖留T-005
- 發布狀態：SPC測試API 0.1.109／Web 0.1.120、Portal測試API 1.0.221／Web 1.0.261已發布；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 核對現行`ChemicalHistoryQueryService`與兩張使用者截圖 | 不以目前主檔重判舊資料 | 已確認現行順序為SavedTrace→SavedSpecification→相符SavedCalculation；缺證據回Unknown。截圖顯示目前主檔有界限，但歷史匯入列仍缺權威判定。尚未逐筆查實際資料 | 規劃依據確認 |
| AC-002 | 以查詢API底層同一`ChemicalHistoryQueryService`、NoTracking及Rollback唯讀盤點正式／測試資料 | 分類總數與查詢一致 | 正式4,199總數＝90 Known＋4,109 Unknown；測試6,155＝4,544＋1,611。Unknown全部為`NO_HISTORICAL_EVIDENCE`；逐筆含MeasurementId、PPC、線別、槽位、項目、日期、批次與原因。JSON雜湊正式`78d622...1988`、測試`2bb29b...96e`。[完整盤點](inventory-20261104.md) | 通過（HTTP畫面留T-005） |
| AC-003 | 匯入格式與preview測試 | 格式缺漏在寫入前被拒絕／隔離 | 新增6項自動測試：Api／File／TransFiles／偽PortalDaily四種通用來源均以`CHEMICAL_EVIDENCE_REQUIRED`隔離並保留來源座標；部署前有效CHEM批次在狀態／量測寫入前拒絕；通用範本僅含PROCESS示例並引導Portal／TransFiles | 通過 |
| AC-004 | confirm防繞過及受控流程回歸 | 只有證據受控流程可寫入，舊批次不能繞過 | Portal內部服務才傳入信任旗標；T-003再以既有不可變追溯產生SHA-256 manifest。唯一證據補連／重跑NoChange、無證據、衝突、過期manifest及交易晚期失敗5項測試通過；重跑前仍重驗全部證據 | T-002／T-003通過 |
| AC-005 | API／回歸 | 既有OK／NG與功能不變 | `MesSpc.Api.Tests` 414通過／11項隔離SQL或外部基準明確略過；`MesSpc.Chemical.Tests` 96通過；Portal API 261及藥液UI 39通過。SPC／Portal API與Portal Web Release建置0錯誤；已判定列原因空白且`NotRequired`，Unknown語意、初複驗、分頁及舊API回退測試通過 | T-004通過 |
| AC-006 | 測試站畫面與截圖 | 顯示固定原因及調整建議 | Portal自動化確認Unknown直接顯示原因／建議、固定代碼可搜尋、已判定列保持精簡；登入後實際畫面與遮罩截圖尚未執行，保留T-005 | T-004自動化通過；截圖待T-005 |
| AC-007 | 備份／指紋／環境 | 測試先行，正式另授權 | 測試庫6,155筆中4,544 Known＋1,611 Unknown；所有Unknown為`HISTORICAL_EVIDENCE_MISSING／SourceEvidenceRequired`。前後VariableMeasurements 18,108、UploadDetails 17,872及連結指紋`1ee27e67...b5f`一致，寫入0。測試SPC API0.1.109／Portal API1.0.221／Web1.0.261的首頁、主資產MIME、health／version及Portal實際API基底皆成功；正式未發布 | T-004通過 |

## 截圖證據摘要

- 截圖一：2026/10/30多列顯示0.70～1.03、`無法判定`及`歷史匯入（原量測人員未記錄）`。
- 截圖二：目前C5清潔硫酸為LSL80／Target100／USL120，C5酸浸硫酸為LSL8／Target10／USL12，兩項目前皆啟用且有濃度公式。
- 截圖不能單獨證明2026/10/30有效規格、數值單位或公式F階段；不作補判依據。後續驗收截圖須和量測ID、版本及API結果交叉核對。

## 未執行項目、限制及後續

使用者要求下一項後，T-004固定原因與建議顯示已完成；實際沒有資料寫入、無schema migration。瀏覽器操作工具本輪無法建立可控制的登入頁面，因此不宣稱登入後畫面或人工截圖通過；T-005與T-006仍未執行，正式站與正式資料未變。

### T-004測試、唯讀核對、發布與回復

- 自動化：SPC API 414通過／11略過、藥液96通過；Portal API 261及藥液UI 39通過。SPC API、Portal API與Portal Web Release建置0錯誤，僅SPC出現既有NuGet弱點來源離線警告1。
- 實際測試庫唯讀：總數6,155＝4,544 Known＋1,611 Unknown；1,611筆全部為`HISTORICAL_EVIDENCE_MISSING／SourceEvidenceRequired`。前後VariableMeasurements 18,108、UploadDetails 17,872及連結指紋`1ee27e6754f9dfd45c37a93dfe5fd508a3649ec508b205b4a66b8e056bdc3f5b`一致，寫入0。
- 測試發布：SPC API 0.1.109／Web 0.1.120；Portal API 1.0.221／Web 1.0.261。SPC備份`D:/SPC/.chem-unknown-t004-20261104/backup-spc-api-20261104-152143`；Portal備份`D:/SPC/.chem-unknown-t004-20261104/backup-portal-20261104-152553`。Portal四個保護設定發布前後雜湊皆為`B1851801...CE767`；本件不發布正式站。
- 連線驗證：SPC與Portal測試首頁成功；SPC主JS／CSS、Portal公開CSS／JS MIME正確；SPC `/api/health`、`/api/version`及Portal Web實際使用的API基底`/health`成功且環境為test。未登入Portal藥液頁302至Login、代理API 401，拒絕路徑正常。
- 回復：SPC停測試集區後還原上述備份四個API主檔；Portal依備份還原API／Web發佈目錄。無DB或schema變更需回復。
- 進度表：已同步2026-11-04單日原生里程碑59、專案摘要及「小工作總表」第267列；原58個圖形保留，核心45／涉及SPC22／完成189／人工11／核銷4不變。輸出SHA-256 `120f526b3d4955a2865c75b899583329bf90f85c211c267a45379ccfc76e3a2c`；ZIP、OpenXML SDK 0錯誤、artifact重新開啟與公式錯誤0、LibreOffice 14頁列印檢查通過。更新前備份為`進度表_藥液Unknown_T004前_20261104-153516.xlsx`；Microsoft Excel人工開啟未執行。

### T-003測試、dry-run、發布與回復

- 自動化：`ChemicalHistoryRepairTests` 5通過；`MesSpc.Api.Tests` 413通過／11項明示略過；`MesSpc.Chemical.Tests` 96通過；Release build 0錯誤（NuGet弱點來源離線警告1）。
- 前置偵錯：第一次唯讀檢查曾把所有Receipt未連結CHEM納入，得到3,766筆；因與網站Unknown 1,611不符而停止、寫入0。修正為先呼叫同一`ChemicalHistoryQueryService`後，最終數量完全對上。
- 修正清單：測試庫1,611筆皆須補來源當時的Receipt／Trace、公式F與規格證據；明細CSV只留本機，不提交。
- 測試發布備份：`D:/SPC/.chemical-history-t003-20261104/backup-test-api-20261104-131801`；API DLL SHA-256 `BAA8A7DA...B49573`。回復時停測試集區，還原備份中4個API主檔後啟動；本件無DB寫入需回復。
- 進度表：已同步2026-11-04單日原生里程碑、專案摘要及「小工作總表」第267列；原57個圖形保留並新增第58個，核心45／涉及SPC22／完成189／人工11／核銷4不變。輸出SHA-256 `453dfb06...f3f4d65`；ZIP、OpenXML SDK 0錯誤、artifact重新開啟與公式錯誤0、LibreOffice 14頁列印檢查通過。更新前備份為`進度表_藥液Unknown_T003前_20261104-132451.xlsx`；Microsoft Excel人工開啟未執行，需回復時以該備份取代原檔。

### T-002發布與回復

- 隔離候選：Git `3acfca8a`加T-002必要產品檔；API DLL SHA-256 `63150C319D731A6D1D55D462CE99941AA0AD2E632F6F6CB0819E5A4724E3D74D`，未包含使用者既有`TrendChartView.vue`修改。
- 測試站：API 0.1.107／Web 0.1.120；主JS`index-DTWZEqIP.js`、CSS`index-BJWc18W2.css`均200且MIME正確。
- 回復：停止測試集區後，以上述備份還原backend四個主檔與frontend入口／資產；本件沒有DB資料或schema需回復。正式站不在回復範圍。

### T-002進度同步（2026-11-04）

原進度表第267列已更新為「T-001／T-002完成；T-003待執行」，仍為待辦「是」；登錄235、核心45、涉及SPC22、完成189，人工11／核銷4另計。專案進度表新增橘色原生里程碑57，原56個圖形保持，並記錄測試API107／Web120與無schema／資料寫入。

同步前備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_藥液Unknown_T002前_20261104-115747.xlsx`。原路徑輸出SHA-256：`4710a2e5e053f0042d42fa2f6d438b039d6983a2f32d765c35b4b84749c5924d`。候選及取代後原路徑ZIP封裝、artifact重開、公式錯誤0、OpenXML SDK錯誤0、摘要／事件／任務範圍渲染及LibreOffice 14頁列印（第5、14頁）通過；未做Microsoft Excel人工開啟。

## 基準與變更紀錄更新位置

已同步`docs/requirements.md`、`TODO.md`、`CHANGELOG_CUSTOM.md`及原進度表「小工作總表」。中央新增第267列一件，狀態「待執行／只規劃」、待辦「是」；登錄234、核心44、涉及SPC21、完成189，人工11／核銷4另計。

原進度表同步2026-11-04實際規劃日期、主摘要、同表明細與橘色原生里程碑54；原53個圖形及其他專案內容保留。備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_藥液歷史Unknown規劃前_20261104-101542.xlsx`；輸出SHA-256：`0ac30f66f13108597e94d2b7977c5d482ecfb85a3c1aacedeb9b1b25240cd447`。

候選及原路徑ZIP／OpenXML均0錯誤；artifact重開、公式錯誤0、摘要／事件／任務範圍渲染及LibreOffice13頁列印（第5、13頁）通過。未做Microsoft Excel人工開啟。原檔取代前重核SHA-256，取代後由原路徑重讀登錄／核心／完成數、第267列與54個圖形通過。

### T-001進度同步（2026-11-04）

原進度表第267列已更新為「T-001唯讀盤點完成／T-002待執行」，仍為待辦「是」；登錄235、核心45、涉及SPC22、完成189，人工11／核銷4另計。專案進度表新增橘色原生里程碑56，原55個圖形保持；摘要明示正式4,109、測試1,611筆Unknown及資料寫入0。

同步前備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表_藥液Unknown盤點前_20261104-105750.xlsx`。原路徑輸出SHA-256：`1d531fa9f3b10834ee07c7ffeda6198ec944f0c6cbe070b37c96120cde0c1828`。候選及取代後原路徑ZIP套件完整、artifact重開、公式錯誤0、摘要／事件／任務渲染及LibreOffice14頁列印（第5、14頁）通過；未做Microsoft Excel人工開啟。
