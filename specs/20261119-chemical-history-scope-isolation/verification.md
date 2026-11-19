# 驗證紀錄

- 日期：2026-11-19
- 自動化：SPC 範圍回歸 8 項、Portal Proxy 回歸 11 項通過。
- 人工驗收：使用者確認測試區資料已調整，人工驗收通過；依使用者指示不再發布測試區。
- 資料：未刪除或改寫既有咬蝕量；PROCESS 資料保持原值與咬蝕功能歸屬。
- 正式狀態：未發布，仍需另行授權。
- 進度表：原檔已備份並更新「小工作總表」第 328 列及「專案進度表」11/19 原生里程碑；輸出重讀、雜湊一致、公式錯誤掃描 0、OpenXML 驗證 0 錯誤。Microsoft Excel 人工開啟未執行。
- 回復：以 `Voice/備份/進度表-20261119-chemical-history-scope-before-20261119-091658.xlsx` 覆蓋原檔。

## 正式資料全量重核（2026-11-19）

- 唯讀統計：有效量測 18,460 筆，其中 `CHEM` 4,797 筆、`PROCESS` 13,663 筆；孤兒 PPC 0 筆。
- 13,663 筆 PROCESS 全部為 175 個 EtchReport 批次，分布 PT1 2,244、PT2 4,557、QE1 1,900、QE2 4,962；非 EtchReport 0 筆。
- 2026-11-10 截圖對應 253 筆 PROCESS、3 批，值域 0.060～1.620；確認為正確咬蝕資料被舊正式查詢混入，不是資料值或歸屬欄位錯誤。
- `ProcessMeasurementsControlScopeTests` 1 項與 Portal `SpcProxyControllerTests` 11 項通過，確認 CHEM 篩選及代理契約。
- 正式 SPC／Portal API 尚未發布本隔離契約；未刪除、搬移或改寫任何正式量測。
