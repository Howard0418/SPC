# 07 測試策略與驗證機制 (Testing Strategy)

本專案建立了解耦且完整的雙層測試防護網，包含後端單元與整合測試，以及前端端對端 (E2E) 測試。

## 1. 後端測試套件 (Backend xUnit Suite)
- **測試專案**：`tests/MesSpc.Api.Tests` (使用 xUnit 框架)
- **測試類型**：
  - **單元測試 (Unit Tests)**：
    - 使用 InMemory 資料庫模擬環境。
    - 覆蓋 `SpcEngine` 的各計算器（`ImrChartCalculator`, `XbarRChartCalculator`）、西方電氣規則判定器、常態性檢定（Jarque-Bera 與卡方 survival function 計算）、常態分佈平滑 PDF 曲線點，以及 `ClosedXML` Excel 匯入解析邏輯。
  - **整合測試 (Integration Tests)**：
    - 支援實體 SQL Server 連線，測試在併發寫入與 Unique Constraints 下的實際執行結果（如管制圖小分類代碼不重複防護）。
    - 權限測試覆蓋 Viewer GET 成功、非讀取請求 403、Editor 完整操作。
    - 密碼測試覆蓋 PBKDF2 正確／錯誤密碼驗證，以及匯入量測者預設 Editor 且無登入密碼。
    - 測試資料清理需驗證只移除測試前綴，正式 Operator 與維護主檔筆數保持不變。
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
  - 現場量測數據錄入頁面表單提交、**Excel 兩階段上傳**（智慧欄位對照 → 預覽校驗），計量型與計數型皆以 `.xlsx` 夾具驅動 UI，不使用 JSON 直接送出。
    - 夾具由 `E2EExcelFixtures`（ClosedXML）於執行時產生於 `bin/.../Fixtures/`。
    - 計數型若 DB 無 `Attribute` 檢驗項目，測試會透過 `E2EAttributeMasterDataHelper` 先建立臨時主檔再上傳。
  - SPC 互動圖表的非同步 ECharts 畫布渲染、規格設定切換、統計數據更新與異常紅點顯示。
- **執行指令**（預設 **有界面模式**，可看到瀏覽器操作過程）：
  ```powershell
  # 方式一：腳本（推薦）
  .\scripts\run-e2e-ui.ps1

  # 方式二：手動指定 runsettings
  dotnet test .\tests\MES.SPC.E2ETests\MES.SPC.E2ETests.csproj --settings .\tests\MES.SPC.E2ETests\playwright.runsettings
  ```
  前置條件：後端 `http://localhost:5243`、前端 `http://localhost:5173` 須已啟動。

  **正式流程端到端（登入 → 大/中/小分類 → 作業人員 → 匯入 → 畫圖）**：
  ```powershell
  .\scripts\run-e2e-ui.ps1 -Filter "FullyQualifiedName~Full_Formal_Workflow"
  ```
