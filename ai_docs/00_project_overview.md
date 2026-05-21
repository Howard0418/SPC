# 00 專案總覽 (Project Overview)

## 目前狀態 (Current Status)
本專案為企業級 SPC (統計製程管制) 系統，已具備完整的產品、製程、機台、檢驗特性等主檔維護功能，支援 Excel/CSV 量測數據與 SPC 管制設定之增量雙向匯入與管理，並提供即時互動式 SPC 管制圖與警報戰情面板。

## 已完成內容 (Completed Items)
- **企業級主檔 CRUD**：工廠 (Plant)、車間 (Factory)、產品 (Product/Part)、製程工站 (Process)、機台 (Machine)、檢測特性 (QualityCharacteristic)。
- **資料收集與匯入**：
  - 手動量測數據錄入與兩階段式 Excel/CSV 檔案校驗/匯入流程。
  - **客製化 SPC 管制項目增量匯入**：支援解析客戶自訂的「製程/藥液/產品管制項目」多工作表 Excel 檔案，動態建立大中小分類，並具備重複寫入與唯一性限制檢查的防呆機制。
- **SPC 運算與判定引擎**：
  - 核心計算器（Xbar-R, Xbar-S, I-MR 等六大管制圖），使用 `MathNet.Numerics` 做精確統計。
  - 西方電氣規則 (Western Electric Rules 1~4) 的自動判定與警報標記。
  - 製程能力指標 ($C_p, C_{pk}, P_p, P_{pk}$) 即時運算與排行。
- **高階戰情看板**：
  - 整合 ECharts 產出規格界限線、統計管制界限線、異常紅點標記的互動式圖表。
  - 戰情首頁提供即時警報走勢圖與 Cpk 後段班排行榜。
- **測試防護網**：
  - 包含 xUnit 後端單元與整合測試套件，使用 InMemory 資料庫並支援實體資料庫連線測試。
  - 包含 Playwright 端對端 (E2E) 測試套件，涵蓋完整的使用者操作流程。

## 注意事項 (Notes)
- 系統已全面部署至實體 SQL Server 資料庫環境 (`172.16.110.16;Database=PMR_SPC_2026`)。
- 新增的大中小分類與圖表參數配置皆具備資料庫唯一約束防呆限制。
- 專案支援軟刪除 (Soft Delete) 與自動稽核欄位填充。
