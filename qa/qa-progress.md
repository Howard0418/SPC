# QA Progress

## QA-000 Project Inventory
- **Status**: Completed
- **Findings**:
  - Frontend: Vue 3 + Vite + TailwindCSS + ECharts + Playwright for E2E (`D:\SPC\frontend\mes-spc-web`)
  - Backend: .NET API (`D:\SPC\backend\MesSpc.Api`)
  - Tests: Multiple test projects in `D:\SPC\tests` (including Unit, E2E, Identity, Calibration, etc.)
  - Build scripts: `publish.bat`, `publish.ps1`, `run_tests.ps1`
- **Next**: Await user confirmation to proceed to QA-002 (Database / Migration Validation).

## QA-001 Build Validation
- **Status**: Completed
- **Findings**:
  - Backend API: `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore` completed successfully with 0 warnings, 0 errors. Output: `MesSpc.Api.dll`.
  - Frontend Web: `npm run build` in `D:\SPC\frontend\mes-spc-web` completed successfully. Produced assets and chunk outputs in `dist`.

## QA-002 Database / Migration Validation
- **Status**: Completed (Previously Blocked)
- **Findings**:
  - 依照使用者提供之 DB 連線字串與設定，並加入 `TrustServerCertificate=True` 解決憑證問題後，後端 API 可正常啟動。
  - EF Core 在啟動時能成功連線至 `PMR_SPC_TEST` 資料庫，並正常執行 Migration / 資料查詢作業。

## QA-003 Backend API Smoke Test
- **Status**: Completed
- **Findings**:
  - `dotnet run` 啟動成功，伺服器監聽在 `http://localhost:5243`。
  - 基本的 HTTP 要求回應正常（伺服器已內建 Fallback 至 SPA 或正常處理請求）。

## QA-004 Frontend Smoke Test
- **Status**: Completed
- **Findings**:
  - 在 `D:\SPC\frontend\mes-spc-web` 執行 `npm run dev` 啟動前端開發伺服器成功。
  - 伺服器監聽於 `http://localhost:5173`。
  - 發送 HTTP 請求成功取得前端 Vue SPA 進入點 (`index.html`)，標題為 "SPC"。
  - 無編譯錯誤或崩潰狀況發生。
- **Next**: Await user confirmation to proceed to QA-005 (Login / Permission Validation).

## QA-005 Login / Permission Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - **Backend**:
    - 本機密碼登入 API (`api/v1/auth/login`) 已停用 (回傳 410 Gone)。
    - 登入全面改用 SSO 機制 (`api/v1/auth/portal-sso`)，以 HMACSHA256 驗證 Portal 傳入之 Token，並動態分配權限 (Role / PagePermissions)。
  - **Frontend**:
    - `router/index.js` 實作了路由守衛 (Router Guard)。
    - 若開啟 Auth (VITE_AUTH_ENABLED=true)，則會驗證 `mes_spc_token`。
    - 實作了細部頁面權限驗證 (`pagePermissionByPath`) 以及 `editorOnly` 權限驗證 (限 Editor Role)。
- **Next**: Await user confirmation to proceed to QA-006 (Basic Data CRUD Validation).

## QA-006 Basic Data CRUD Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - `MasterDataV2Controller.cs` 實作了 Parts, Processes, Machines, Tanks, QualityCharacteristics, 與 PartProcessCharacteristics (PPC) 等基礎主檔的 CRUD。
  - **資料保護機制**:
    - 在刪除工站 (Process) 時，會主動檢查是否有關聯的機台、PPC、量測記錄 (Variable/Attribute) 或異常通報 (Alerts)。若有，則回傳 HTTP 409 Conflict，防止產生孤兒資料。
    - 刪除 PPC 時，同樣會檢查量測與計算結果，避免誤刪；並提供 `purge=true` (永久清除) 的強刪機制，但若包含已結案異常則會強制阻擋以保留品質追溯。
  - **資料完整性驗證**:
    - 新增或更新時，有妥善驗證業務範圍 (Control Scope)、連動檢查 (`RequiresPart`, `RequiresMachine`, `RequiresTank`)。
- **Next**: Await user confirmation to proceed to QA-007 (SPC Data Input / Import Validation).

## QA-007 SPC Data Input / Import Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - **檔案匯入 (UploadsController)**:
    - 支援 CSV 與 Excel 兩種格式，且區分計量型 (Variable) 與計數型 (Attribute) 匯入。
    - 具備防呆機制：上傳時透過 MD5 檢查檔案 Hash 與 JSON Payload Hash，避免重複上傳。
    - 支援特製的「藥液分析 (Chemical Matrix)」Excel 格式匯入，系統可自動辨識 Excel Header 並轉譯成標準單筆量測。
  - **手動錄入 (ManualMeasurementsV1Controller)**:
    - 支援單筆或整批手動修改，且限制只能修改 `SourceType == Manual` 的紀錄，防止污染自動化機台資料。
    - **修改與刪除保護**：修改手動資料時，會自動作廢關聯的 SPC 警報（標註為 `Superseded` 並寫入原因），同時刪除舊的計算結果並重新計算 (`CalculateVariableAsync`)。
    - **稽核軌跡 (Audit Log)**：所有手動修改與刪除，皆會將修改前、修改後的資料與執行人員寫入 `UploadDetails.PayloadJson` 中作為異動追溯。
- **Next**: Await user confirmation to proceed to QA-008 (Xbar-R Validation).

## QA-008 Xbar-R Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - **公式檢查 (`XbarRChartCalculator.cs`)**:
    - 管制上限/下限計算精確採用標準公式：
      `UCL_X = X_DoubleBar + A2 * R_Bar`
      `LCL_X = X_DoubleBar - A2 * R_Bar`
      `UCL_R = D4 * R_Bar`
      `LCL_R = D3 * R_Bar`
    - 當指定特殊配置時，可切換為 MR Method (`UCL_X = mean + mrMultiplier * mrBar`) 或 Sigma Method。
  - **常數驗證 (`SpcConstants.cs`)**:
    - 查對 Montgomery 標準常數表：
      - `n=2` (A2=1.880, D3=0, D4=3.267) -> 正確
      - `n=5` (A2=0.577, D3=0, D4=2.115) -> 正確
      - 支援 `n=2` 至 `n=25`，數值完全吻合標準。
  - **異常判定**:
    - 支援 Western Electric Rules 判定以及 Out Of Spec 判定，並能整合回傳各別資料點的違反狀態。
- **Next**: Await user confirmation to proceed to QA-009 (Xbar-S Validation).

## QA-009 Xbar-S Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - **標準差與公式 (`XbarSChartCalculator.cs`)**:
    - 單批次標準差 (`StandardDeviation`) 使用無偏樣本變異數估計 (分母為 `n - 1`)，計算正確。
    - 上下限公式：
      `UCL_X = X_DoubleBar + A3 * S_Bar`
      `LCL_X = X_DoubleBar - A3 * S_Bar`
      `UCL_S = B4 * S_Bar`
      `LCL_S = B3 * S_Bar`
      完全符合標準 SPC 公式。
  - **常數計算 (`SpcConstants.cs`)**:
    - `A3`, `B3`, `B4` 不是直接寫死查表，而是利用更精確的 `c4` 函數公式動態展開推算 (`3 / (c4 * sqrt(n))`)，這確保了計算的統計精確度，也能完美對應 Montgomery 常數表。
- **Next**: Await user confirmation to proceed to QA-010 (I-MR Validation).

## QA-010 I-MR Validation
- **Status**: Completed (Static Code Validation)
- **Findings**:
  - **公式檢查 (`ImrChartCalculator.cs`)**:
    - 單值圖 (Individuals Chart) 以相鄰兩點之差值的絕對值 (`Math.Abs(values[i] - values[i - 1])`) 計算移動全距 (Moving Range, MR)。
    - 管制界限公式：
      - `Sigma = MR_Bar / d2` (其中 n=2 對應的 `d2 = 1.128`)
      - `UCL_I = I_Bar + 3 * Sigma` (等價於 `I_Bar + 2.66 * MR_Bar`)
      - `LCL_I = I_Bar - 3 * Sigma`
      - `UCL_MR = D4 * MR_Bar` (`D4 = 3.267`)
      - `LCL_MR = D3 * MR_Bar` (`D3 = 0.0`)
    - 公式完全符合標準 I-MR 管制圖。
  - **排除點處理 (Exclusion)**:
    - 當計算 MR 時，若組成移動全距的兩個相鄰點中有任何一點被標記為 `IsExcluded`，則該組 MR 點同樣不參與管制界限違反判定。此邏輯嚴謹正確。
- **Next**: Await user confirmation to proceed to QA-011 (p / np Chart Validation).
