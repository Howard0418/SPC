# 驗證紀錄

- 功能 ID：`20261120-chemical-adjustment-excel-alignment`
- 規格版本：3
- 日期／環境：2026-11-20／來源活頁簿＋PMR_SPC_TEST唯讀查詢＋SPC測試站
- 實作狀態：TASK-001～002完成；TASK-003～004未執行
- 驗證狀態：TASK-001～002通過；完整API回歸另有16項既有失敗如實保留
- 發布狀態：測試 API `0.1.161/test` 已發布；Web維持`0.1.171`；正式站未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 固定SHA-256並讀取工作表、標題與資料列 | 列出所有資料 Sheet 與來源範圍 | SHA-256 `F634868A…48C7C`；11張資料Sheet／142列，另1張F表 | 通過 |
| AC-002 | Excel 與測試 PPC 自然鍵對帳 | 唯一映射、衝突隔離 | 114來源身分中110唯一配對、4未配對、0一對多；網站另3項，詳[矩陣](reconciliation-matrix.md) | 通過 |
| AC-003 | `NA`、空白、0、正數分類 | 不錯誤覆寫 | 槽容95相符／15空白；添加99相符／11 NA；稀釋90相符／19 NA／1差異。所有非相符列零寫入 | 通過 |
| AC-004 | 公式參照轉換與差異分類 | 分離門檻、槽容、目標與倍率 | 調整式74相符／14網站缺／3來源缺／19不同；調整量53相符／22皆空／7網站缺／3來源缺／25不同，詳[結果](reconciliation.md) | 通過 |
| AC-005 | API 單元／整合測試 | 三變數計算與缺值拒絕 | 三變數不分大小寫可計算；引用空值與已設定0／負數拒絕；排程採不可變公式版本參數。計算＋排程46項通過 | 通過 |
| AC-006 | 兩頁與匯出畫面測試 | 同源顯示、單一編輯權責 | 未修改程式 | 未執行 |
| AC-007 | 測試資料 preview/apply/retry/rollback | 受控同步與可回復 | 未寫入資料 | 未執行 |
| AC-008 | 聚焦與回歸測試、build、測試站 smoke | 無明顯 regression | 本次相關75項中73通過；2項既有排除測試仍失敗。完整614項：587通過／16失敗／11略過；Release build 0警告0錯誤；測試站Web／資產／API smoke通過 | TASK-002通過；既有失敗保留 |

## 文件檢查

- 規格、計畫、任務、驗證及來源盤點互相連結。
- 新小工作已按資料正確性風險及相依順序排入 `TODO.md`。
- 來源 Excel 未修改；未開啟或更新已停止例行維護的進度表。
- `python -m py_compile reconcile_readonly.py` 通過；以相同來源與相同測試查詢重跑，矩陣 SHA-256 兩次皆為 `7D18882F2246998B5C380A32795ABB04851E9D6E568A70D4C14986E51F51A754`。

## TASK-002 測試、發布與回復

- 紅燈：新增具名變數與非正數案例後，`ChemicalFormulaCalculationTests` 36項中4項失敗；證明修改前不支援三參數且不拒絕非正數。
- 綠燈：計算／排程聚焦46項全數通過；擴大到公式版本、追溯及量測共75項時73項通過，兩項既有 `ChemicalPortalMeasurementTests` 因現行服務將 `IsExcluded` 正規化為false而找不到排除列，與本次檔案無交集且未順手修正。
- 完整 API：614項中587通過、16失敗、11略過。其餘既有失敗集中於時間敏感的歷史證據判定及 EF InMemory `GroupBy` 翻譯；本件未宣稱完整回歸全綠。
- 建置：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj -c Release --no-restore` 通過，0警告／0錯誤。
- 版本：引擎 `portal-excel8-v5-adjustment-parameters`；API `0.1.160`→`0.1.161`；Web未改，維持`0.1.171`。前台既有版本區塊從同源`/api/version`動態顯示API版號。
- 發布：IIS `SpcApiTest` 實際目標 `D:\SPC\release\test\backend`；備份 `D:\SPC\release\backups\api-0.1.160-before-task002-20261120-144937`。`appsettings.json` 發布前後SHA-256同為 `811F4DA9…F8BFB`。
- Smoke：`https://SPCTEST.pmr.com.tw/`、主JS `index-A75FFLgU.js`、主CSS `index-BlAfmLFs.css`、同源`/api/health`與`/api/version`、直連`:8901/api/version`均HTTP 200且MIME正確，API回`0.1.161/test`。Playwright Chromium直接開啟同源`/api/version`亦回200、`0.1.161/test`；未見API離線或環境錯置。
- 回復：停止`SpcApiTest`集區，從上述備份還原API檔案並保持設定檔，再啟動集區與重驗health/version；未執行回復。無schema migration、PPC／公式／F／量測或資料寫入。

## 未執行項目、限制及後續

下一步為 TASK-003；本次未自動開始。對帳差異與六項業務規則仍保持隔離，TASK-004套用任何測試資料前仍須逐列核准。

## 基準與變更紀錄更新位置

- [需求索引](../../docs/requirements.md)
- [中央工作池](../../TODO.md)
- [變更紀錄](../../CHANGELOG_CUSTOM.md)
- [AI 修改目的](../../ai_docs/10_change_log.md)
