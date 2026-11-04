# 驗證紀錄

- 功能 ID：SPC-CHEM-HISTORY-JUDGMENT-1104
- 規格版本：4
- 日期／環境：2026-11-04／測試`PMR_SPC_TEST`、正式`PMR_SPC_2026`唯讀盤點
- 實作狀態：T-001、T-002完成；T-003～T-006未執行
- 驗證狀態：T-001、T-002通過；歷史補正與Portal原因畫面尚未開始
- 發布狀態：SPC測試API 0.1.107／Web 0.1.120已發布；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 核對現行`ChemicalHistoryQueryService`與兩張使用者截圖 | 不以目前主檔重判舊資料 | 已確認現行順序為SavedTrace→SavedSpecification→相符SavedCalculation；缺證據回Unknown。截圖顯示目前主檔有界限，但歷史匯入列仍缺權威判定。尚未逐筆查實際資料 | 規劃依據確認 |
| AC-002 | 以查詢API底層同一`ChemicalHistoryQueryService`、NoTracking及Rollback唯讀盤點正式／測試資料 | 分類總數與查詢一致 | 正式4,199總數＝90 Known＋4,109 Unknown；測試6,155＝4,544＋1,611。Unknown全部為`NO_HISTORICAL_EVIDENCE`；逐筆含MeasurementId、PPC、線別、槽位、項目、日期、批次與原因。JSON雜湊正式`78d622...1988`、測試`2bb29b...96e`。[完整盤點](inventory-20261104.md) | 通過（HTTP畫面留T-005） |
| AC-003 | 匯入格式與preview測試 | 格式缺漏在寫入前被拒絕／隔離 | 新增6項自動測試：Api／File／TransFiles／偽PortalDaily四種通用來源均以`CHEMICAL_EVIDENCE_REQUIRED`隔離並保留來源座標；部署前有效CHEM批次在狀態／量測寫入前拒絕；通用範本僅含PROCESS示例並引導Portal／TransFiles | 通過 |
| AC-004 | confirm防繞過及受控流程回歸 | 只有證據受控流程可寫入，舊批次不能繞過 | Portal內部服務才傳入信任旗標，外部controller沒有此入口。`MesSpc.Chemical.Tests` 96通過；舊PreviewReady CHEM批次拒絕且維持PreviewReady、量測0筆。歷史補正／重跑NoChange屬T-003，未開始 | T-002範圍通過 |
| AC-005 | API／回歸 | 既有OK／NG與功能不變 | `MesSpc.Api.Tests` 408通過／11項隔離SQL或外部基準明確略過；API Release建置0警告0錯誤；前端2770鍵×3語系與testhost建置通過。未改歷史查詢判定邏輯 | T-002回歸通過 |
| AC-006 | 測試站畫面與截圖 | 顯示固定原因及調整建議 | 未執行 | 未執行 |
| AC-007 | 備份／指紋／環境 | 測試先行，正式另授權 | 從Git HEAD加本件檔建立隔離候選，未帶工作區TrendChart變更。備份`D:/SPC/.chemical-history-t002-20261104/backup-test-20261104-115208`；測試health/version均200、test／0.1.107，首頁及主JS/CSS 200且MIME正確，保護設定hash保持。自動瀏覽器確認頁面與`/api/health`皆同源`https://spctest.pmr.com.tw`。正式未發布 | T-002通過 |

## 截圖證據摘要

- 截圖一：2026/10/30多列顯示0.70～1.03、`無法判定`及`歷史匯入（原量測人員未記錄）`。
- 截圖二：目前C5清潔硫酸為LSL80／Target100／USL120，C5酸浸硫酸為LSL8／Target10／USL12，兩項目前皆啟用且有濃度公式。
- 截圖不能單獨證明2026/10/30有效規格、數值單位或公式F階段；不作補判依據。後續驗收截圖須和量測ID、版本及API結果交叉核對。

## 未執行項目、限制及後續

使用者要求繼續後，T-002匯入防呆已完成並發布測試站；沒有匯入、補正或修改任何既有量測，無schema migration。T-003測試資料補正、T-004原因畫面、T-005截圖驗收與T-006整案收尾仍未執行；正式站與正式資料未變。

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
