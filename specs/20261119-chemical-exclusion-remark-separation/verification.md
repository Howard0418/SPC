# 驗證紀錄

- Portal 回歸：`ChemicalMeasurementExclusionContractTests` 4/4 通過；API／Web Release build 通過。
- SPC API：`ChemicalPortalExclusionTests` 7/7 通過，確認固定備註只保存、不建立排除，並可從日報 payload 依 PPC 讀取。
- SPC Web：備註相關 Playwright 4/4 通過；繁中畫面另留本機驗證截圖 `test-remark-separated.png`（不納入版本庫）。
- i18n：三語 2,876 keys 檢查通過；Web test build 通過。
- 測試區：Portal API `1.0.285`／Web `1.0.324`；SPC API `0.1.154`／Web `0.1.163`。
- 圖表資料投影：固定備註同步附加至 `rawDataPoints`、`chartData.points` 及次圖點；Playwright 直接驗證 ECharts 實際 series meta 為 `CLOSE_LINE`。
- Portal 查詢頁：欄名改為「備註」，由日報 payload 的 `ExclusionReason` 顯示中文下拉備註；不再以啟用排除紀錄決定內容或顯示排除狀態。
- 保存重載：資料庫確認 PPC 2587 最近兩次 payload 均保存 `CLOSE_LINE`；修正日報重載為 PPC ID 優先配對，槽位＋分析項目僅供舊資料回退。API 焦點測試 8/8 通過。
- 自由備註顯示：測試庫 PPC 2551 的量測 91882 存在有效自由備註，圖表 API 的主圖點亦帶相同量測 ID；tooltip 改於顯示時依 ID 讀取目前備註。備註 Playwright 4/4 通過，包含儲存後 ECharts meta 與 tooltip 立即顯示、清除後立即移除。
- 提示可讀性：45字備註的瀏覽器測試確認輸出為20／20／5字三行，實際計算樣式為14px；備註 Playwright 4/4 通過。
- 保存即時刷新：備註保存與清除成功後均重新呼叫目前圖表 API、重建 ECharts 並隱藏舊 tooltip；Playwright 驗證重新查詢次數、最新 meta、提示顯示及清除結果，4/4 通過。
- 實站診斷：測試站深連結成功取得 PPC 2551 圖表、排除、固定備註與自由備註 API，但畫面重繪後網址呈現空 `ppcId`；補正刷新函式以 payload PPC ID 設回目前項目並直接呼叫圖表 loader，避免依空篩選值誤走總表分支。
- 連線：Portal 與 SPC 首頁、主要 JavaScript／CSS、health／version、瀏覽器實際 API 基底均成功，環境標籤為 `test`。
- 資料契約：固定下拉備註存 `SpcPointExclusion.Reason`；圖點自由備註存 `SpcPointRemark.Remark`，兩者互不覆蓋。
- 統計契約：固定備註點正常列入統計與繪圖；只有 SPC 人工排除影響統計。
- 資料驗證：固定原因啟用排除 0 筆、日報固定備註 10 筆可讀取、人工排除 1 筆仍啟用。
- 無 schema migration；正式區未發布。
- 依使用者既定規則，本件不更新進度 Excel。
