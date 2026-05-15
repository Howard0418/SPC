# Phase 1: Project Inventory and Assessment Report

## 1. 現況分析報告 (Current Status Analysis)

### 1.1 技術棧 (Technology Stack)
- **Frontend**: Vue 3 + Vite + TailwindCSS + ECharts. 採用 Composition API, 結構清晰.
- **Backend**: .NET 10.0 + Entity Framework Core. 
- **Database**: 支持 SQLite (開發) 與 SQL Server (企業級), 目前架構已具備多資料庫切換能力.
- **API**: RESTful API, 使用 Swagger (OpenAPI) 作為文件.
- **Logic**: SPC 計算使用 MathNet.Numerics, 公式運算具備初步的 FormulaEngine.

### 1.2 系統架構 (Architecture)
- **Backend**: 分為 Controllers, Services, Domain (Entities), Infrastructure (Data). 符合基本分層架構.
- **Frontend**: 使用 Vue Router 管理 20+ 個頁面, 包含主檔維護、資料上傳、SPC 圖表與警報管理.

---

## 2. 功能盤點表 (Feature Inventory)

| 類別 | 功能名稱 | 狀態 | 備註 |
| :--- | :--- | :--- | :--- |
| **主檔維護** | Product / Part / Process / Machine | 已完成 (初版) | 缺少層級關係 (Plant/Factory) |
| **主檔維護** | Station / Inspection Item | 已完成 (初版) | |
| **主檔維護** | PartProcessCharacteristic | 已完成 (初版) | 用於綁定零件、製程與特性 |
| **資料收集** | 手動錄入 (MeasurementEntry) | 已完成 | |
| **資料收集** | CSV 批次匯入 (Variable/Attribute) | 已完成 | 支援暫存區預覽與錯誤校驗 |
| **SPC 計算** | 管制圖計算 (Xbar-R, etc.) | 部份完成 | 需補強更多圖表類型與計算邏輯 |
| **SPC 計算** | 判讀規則 (Rules Engine) | 部份完成 | 需補強自定義規則 |
| **異常管理** | Alert 事件記錄 | 已完成 | 缺少完整的 CAPA / 簽核流程 |
| **異常管理** | Alert 處理工作流 | 部份完成 | 見 `AlertsWorkflowView` |
| **系統基礎** | JWT 身份驗證 | 已完成 | 可配置啟用/禁用 |
| **系統基礎** | 多語言支援 | 尚未完成 | |
| **報表/Dashboard** | 綜合看板 (Dashboard) | 已完成 (初版) | 需更豐富的指標 (OEE, Cpk Ranking) |

---

## 3. 風險清單 (Risk List)

| 風險 ID | 風險描述 | 嚴重程度 | 建議對策 |
| :--- | :--- | :--- | :--- |
| **R01** | 缺少併發控制 (RowVersion) | 高 | 所有資料表引入 RowVersion 欄位 |
| **R02** | 稽核紀錄缺失 (Audit Trail) | 中 | 實作 CreatedBy/UpdatedBy 與 ChangeLog |
| **R03** | 刪除操作為物理刪除 | 中 | 引入 `IsDeleted` 實作軟刪除 |
| **R04** | 資料庫效能風險 | 低 | 雖然目前資料量小, 但缺乏 Index 優化策略 |
| **R05** | 測試覆蓋率不足 | 高 | 建立 Unit Test 與 Integration Test 專案 |

---

## 4. 改造優先順序 (Refactoring Priority)

1.  **P0 (核心底層)**:
    - 建立 `ai_docs` 文件庫.
    - 改造 `BaseEntity` 引入 Audit 欄位與軟刪除.
    - 強化 SQL Server 連接與 Migration 機制 (含 RowVersion).
2.  **P1 (主檔擴充)**:
    - 補齊 Plant -> Factory -> ProductionLine -> Machine 架構.
    - 補齊 Unit, Shift, Operator, Customer, Supplier 主檔.
3.  **P2 (SPC 引擎)**:
    - 獨立 `SpcEngine` 模組, 實作標準判讀規則 (Nelson Rules).
    - 建立動態公式引擎 (Formula Engine) 版本化管理.
4.  **P3 (品質流程)**:
    - 完善 CAPA / OCAP 流程與簽核工作流.
5.  **P4 (系統加固)**:
    - 建立完整的 RBAC 權限體系.
    - 補齊各層級測試.
