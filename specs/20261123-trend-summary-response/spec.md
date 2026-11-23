# SPC-TREND-SUMMARY-1123 趨勢圖摘要回應相容

- 狀態：測試站已發布並驗證。
- 範圍：驗證並發布既有提交 2c05c458。主系統為 SPC Web，沿用既有 SPC API。無資料庫變更。

## 需求與驗收

- Given 摘要 API 回傳原始陣列或 items、data、results 包裝陣列，When 查詢趨勢圖摘要，Then 顯示相同摘要，返回摘要可使用快取。
- Given 摘要為空陣列或非支援格式，When 查詢，Then 分別顯示無資料或既有載入失敗訊息。
- Given Web 遞增版號並發布至測試站，When 開啟站台，Then 可見新版、主資產成功載入、瀏覽器同源 API 與直接 API 均回 test 環境。

## 計畫與驗證

- 執行趨勢圖回歸與 testhost build，保留原入口及資產供回復，驗證 Web 與 API，詳見 [驗證紀錄](verification.md)。
