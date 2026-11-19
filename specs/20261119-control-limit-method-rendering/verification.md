# 驗證紀錄
- 功能 ID：`SPC-CONTROL-LIMIT-METHOD-1119-TASK-001`
- 規格版本：3
- 日期／環境：2026-11-19／本機與 SPC 測試站
- 實作狀態：完成
- 驗證狀態：n=50 防呆自動化與測試站連線驗證通過；待使用者人工驗收
- 發布狀態：SPC 測試站 API `0.1.155`／Web `0.1.164`；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `dotnet test ... --filter FullyQualifiedName~XbarRChartCalculatorTests` | 圖點採樣本標準差界限 | 修正前2失敗／2通過，UCL預期13.027844932、實際99；修正後4／4通過 | 通過 |
| AC-002 | 計算器三方法差異測試及相關公式回歸 | 非對稱資料產生不同界限 | `XbarRChartCalculatorTests`、`ControlLineFormulaSaveTests`、`PartProcessCharacteristicsControllerTests`合計16／16通過 | 通過 |
| AC-003 | 空白公式相容測試及完整API回歸 | 沿用既有固定界限 | Release測試105通過／1個既有Excel檢視測試略過 | 通過 |
| AC-004 | Web build與測試站連線 | Web／資產／API環境及版本正確 | i18n 2865×3、test build通過；首頁、JS、CSS、health、version及同源API皆200，MIME正確，API `0.1.143`／`test`，Web資產含`0.1.151` | 通過 |
| AC-005 | Xbar-S 方法切換回歸 | 標準全距與樣本標準差改變上方 Xbar 界限、下方 S 界限不變 | 修正前編譯失敗，證明計算器無公式參數；修正後 Xbar-R／Xbar-S 6／6、完整API106通過／1略過 | 通過 |
| AC-006 | Xbar-S續版測試站連線 | 新API／Web與test環境正確 | 首頁、JS、CSS、health、version與同源API皆200，MIME正確，API `0.1.144`／`test`，Web資產含`0.1.152` | 通過 |
| AC-007 | n=50 標準全距 API／Web 防呆 | Web 不顯示不適用選項且 API 不修改原設定 | 失敗測試先證明 API 原回 `200`、Web 原仍顯示；修正後計算器／儲存焦點 17／17、Web 焦點 2／2、完整 API 116 通過／1 略過，test build 通過 | 通過 |
| AC-008 | 測試站發布與連線 | API／Web 版本、環境、首頁、資產與瀏覽器同源基底正確 | 發布前 API `0.1.154`／Web `0.1.163`；發布後 `/api/version`=`0.1.155`／`test`、health ok，首頁、主 JS／CSS 均 200 且 MIME 正確，主 JS 含 Web `0.1.164` | 通過 |

## 未執行項目、限制及後續
- 2026-11-19 實站唯讀重驗 `#2620`（Xbar-S、350筆、7個子組、每組 n=50）：目前保存 `STANDARD_RANGE`，但 API 回傳上方 Xbar `UCL=0.307306007／CL=0.275085714／LCL=0.242865422`，內容仍為系統 Xbar-S 的 `A3 × S̄`，未標示 `calculationMethod=STANDARD_RANGE`。根因是 `SpcConstants.TryGetFactors(50)` 不支援；現有 A2／D3／D4 表只涵蓋 n=2～25，計算器依既有安全規則回退系統標準公式。
- 同批資料獨立驗算：平均移動全距法為 `0.480349048／0.275085714／0.069822381`，樣本標準差法為 `0.480908995／0.275085714／0.069262434`；兩者 UCL 僅差 `0.000559947`，圖形看起來接近屬資料結果，但數值並非相同。下方 S 圖固定為 `0.098512619／0.075557502／0.052602386`，符合不隨上方方法改變的契約。
- 實站唯讀重驗 `#2437`：I-MR 已保存樣本標準差法，API 明確回傳 `calculationMethod=SIGMA_METHOD`，I 圖 `UCL=100.424002647／CL=93.848571429／LCL=87.273140210`，首圖點 UCL 同為 `100.424002647`。
- 計算器回歸重新執行：Xbar-R、Xbar-S、I-MR 合計 8／8 通過。自動切換測試站設定因程式測試身分被稽核層以 `BUSINESS_AUDIT_UNAVAILABLE` 拒絕，四次變更均未寫入；`#2620` 原公式保持不變。
- 結論：I-MR、Xbar-S 的移動全距及樣本標準差計算可驗；標準全距法明確限定子組 2～25。n=50 時 Web 不再提供該選項並顯示原因，API 亦拒絕直接送出，已消除靜默回退。
- 完整 `i18n-ppc.spec.ts` 舊測試組本輪為 37 通過／11 失敗；其中 3 項為既有 I-MR fixture 的 `sampleSize=1`、期待仍寫成 5，另有既有權限路由期待及排程頁籤平行執行逾時。這些不屬本次公式防呆，未據此宣稱完整 Web suite 通過；本次新增及直接相關 2 項已獨立通過。
- 正式站須另行授權；本次未修改資料庫、量測、公式設定或正式資料。
- 進度表依 2026-11-19 停止例行更新規則不適用，本次未開啟或修改 Excel。

## 基準與變更紀錄更新位置
已更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md` 與 `TODO.md`。
