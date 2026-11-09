# SPC 管制項目設定漸進式載入驗證

- 功能 ID：`SPC-PPC-LOAD-1109-TASK-001`
- 日期：2026-11-09
- 實作：完成
- 測試站：Web `0.1.124`／API `0.1.112`（API未修改）
- 正式站：未發布

## 驗收結果

| 項目 | 結果 |
|---|---|
| 主清單先完成、輔助資料仍等待 | 行為測試通過；主清單callback與主載入完成先發生 |
| 輔助主檔失敗 | 行為測試通過；主清單保持並回報輔助錯誤 |
| 三語系 | 2,822鍵 × 3語系通過 |
| 測試建置 | Vite testhost Release建置通過 |
| 測試站Web | 首頁200；主JS與CSS皆200且MIME正確 |
| 瀏覽器實際API基底 | 同源`/api/health`、`/api/version`成功，環境`test`、API `0.1.112` |
| 權限回歸 | 匿名查詢`/api/part-process-characteristics`回401 |
| 發布一致性 | `dist`與測試站主JS SHA-256一致 |

## 發布與回復

- 測試站備份：`D:/SPC/release/test/frontend.backup-ppc-progressive-20261109`。
- 部署結果：首頁200；`index-DK-qSaXk.js`為`application/javascript`，`index-i5zm2vvn.css`為`text/css`。
- 回復：停止`SpcSingleTest`集區，以備份目錄還原`release/test/frontend`後重新啟動；本件沒有API、資料庫或schema變更。
- 原生瀏覽器操作管道仍不可用，因此未宣稱登入後視覺速度驗收；自動化已直接驗證主清單不等待輔助請求的時序。

## 進度同步

- 原進度表已備份為`Voice/備份/進度表-管制項目漸進載入前-20261109.xlsx`後取代。
- 小工作總表新增完成列，登錄241／完成199；核心未完成41、SPC相關18、人工12／待核銷4保持。
- 新增2026-11-09原生綠色里程碑；OpenXML錯誤0、公式錯誤0、Artifact重開與兩區渲染通過。
- 更新後SHA-256：`4EEAC7806D5589BB4449210294E71A65CD9FA33A8B9747BE4C87306E14944C0C`。
