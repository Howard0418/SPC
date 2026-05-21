# 07 測試策略與驗證機制 (Testing Strategy)

本專案建立了解耦且完整的雙層測試防護網，包含後端單元與整合測試，以及前端端對端 (E2E) 測試。

## 1. 後端測試套件 (Backend xUnit Suite)
- **測試專案**：`tests/MesSpc.Api.Tests` (使用 xUnit 框架)
- **測試類型**：
  - **單元測試 (Unit Tests)**：
    - 使用 InMemory 資料庫模擬環境。
    - 覆蓋 `SpcEngine` 的各計算器（`ImrChartCalculator`, `XbarRChartCalculator`）、西方電氣規則判定器，以及 `ClosedXML` Excel 匯入解析邏輯。
  - **整合測試 (Integration Tests)**：
    - 支援實體 SQL Server 連線，測試在併發寫入與 Unique Constraints 下的實際執行結果（如管制圖小分類代碼不重複防護）。
- **執行指令**：
  ```bash
  dotnet test .\tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj
  ```

## 2. 前端端對端測試 (Frontend Playwright E2E Suite)
- **測試專案**：`tests/MES.SPC.E2ETests` (使用 Playwright 框架)
- **測試場景**：
  - 登入認證流程與 Token 本地存儲。
  - 主選單與側邊欄導覽跳轉。
  - 產品與工站主檔的動態 CRUD 流程。
  - 現場量測數據錄入頁面表單提交、CSV/Excel 兩階段上傳校驗預覽。
  - SPC 互動圖表的非同步 ECharts 畫布渲染、規格設定切換、統計數據更新與異常紅點顯示。
- **執行指令**：
  ```bash
  dotnet test .\tests\MES.SPC.E2ETests\MES.SPC.E2ETests.csproj
  ```
