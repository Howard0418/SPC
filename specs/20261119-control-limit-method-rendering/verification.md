# 驗證紀錄
- 功能 ID：`SPC-CONTROL-LIMIT-METHOD-1119-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-19／本機與 SPC 測試站
- 實作狀態：完成
- 驗證狀態：自動化與連線驗證通過／待使用者畫面驗收
- 發布狀態：SPC 測試站 API `0.1.144`／Web `0.1.152`；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `dotnet test ... --filter FullyQualifiedName~XbarRChartCalculatorTests` | 圖點採樣本標準差界限 | 修正前2失敗／2通過，UCL預期13.027844932、實際99；修正後4／4通過 | 通過 |
| AC-002 | 計算器三方法差異測試及相關公式回歸 | 非對稱資料產生不同界限 | `XbarRChartCalculatorTests`、`ControlLineFormulaSaveTests`、`PartProcessCharacteristicsControllerTests`合計16／16通過 | 通過 |
| AC-003 | 空白公式相容測試及完整API回歸 | 沿用既有固定界限 | Release測試105通過／1個既有Excel檢視測試略過 | 通過 |
| AC-004 | Web build與測試站連線 | Web／資產／API環境及版本正確 | i18n 2865×3、test build通過；首頁、JS、CSS、health、version及同源API皆200，MIME正確，API `0.1.143`／`test`，Web資產含`0.1.151` | 通過 |
| AC-005 | Xbar-S 方法切換回歸 | 標準全距與樣本標準差改變上方 Xbar 界限、下方 S 界限不變 | 修正前編譯失敗，證明計算器無公式參數；修正後 Xbar-R／Xbar-S 6／6、完整API106通過／1略過 | 通過 |
| AC-006 | Xbar-S續版測試站連線 | 新API／Web與test環境正確 | 首頁、JS、CSS、health、version與同源API皆200，MIME正確，API `0.1.144`／`test`，Web資產含`0.1.152` | 通過 |

## 未執行項目、限制及後續
- 尚待使用者在測試站重新整理後，於截圖所示 Xbar-S 項目切換標準全距法與樣本標準差法，確認上方 Xbar UCL／LCL 隨之改變。
- 正式站須另行授權；本次未修改資料庫、量測、公式設定或正式資料。
- 進度表依 2026-11-19 停止例行更新規則不適用，本次未開啟或修改 Excel。

## 基準與變更紀錄更新位置
已更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md` 與 `TODO.md`。
